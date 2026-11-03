-- Stock adjustments (back office: Item > Stock adjustment): the stock counted on the shelf, item by item.
-- A draft (status 0) can be changed or deleted; posting it (fn_post_stock_adjustment, status 1) sets the stock
-- of each item to what was counted and writes a stock movement (type 5) per changed item. Posted ones stay as they are.
--
-- qty_before = the stock the system had when the item was counted (added to the adjustment): sales and purchases
-- made between counting and posting are kept.
--
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f stock_adjustment_functions.sql

DROP FUNCTION IF EXISTS public.fn_stock_adjustment_get_all(date, date);
DROP FUNCTION IF EXISTS public.fn_stock_adjustment_get_lines(integer);
DROP FUNCTION IF EXISTS public.fn_stock_adjustment_find_items(text, integer);
DROP FUNCTION IF EXISTS public.fn_stock_adjustment_save(integer, text, jsonb, boolean, integer);
DROP FUNCTION IF EXISTS public.fn_stock_adjustment_delete(integer);

-- Adjustments made between two days (inclusive), and every draft: drafts first, then newest first.
-- value_change = what the posted adjustment changed in stock value (difference x cost); NULL for a draft.
CREATE OR REPLACE FUNCTION public.fn_stock_adjustment_get_all(p_from date, p_to date)
RETURNS TABLE (
    adjustment_id integer, adjustment_no varchar, adjustment_date timestamptz, reason varchar, status smallint,
    user_name varchar, line_count bigint, value_change numeric, posted_at timestamptz)
LANGUAGE sql STABLE
AS $$
    SELECT a.adjustment_id, a.adjustment_no, a.adjustment_date, a.reason, a.status,
           u.full_name, count(l.adjustment_line_id),
           CASE WHEN a.status = 1 THEN round(coalesce(sum(l.difference * l.unit_cost), 0), 2) END,
           a.posted_at
    FROM stock_adjustments a
    LEFT JOIN users u ON u.user_id = a.user_id
    LEFT JOIN stock_adjustment_lines l ON l.adjustment_id = a.adjustment_id
    WHERE a.status = 0 OR a.adjustment_date::date BETWEEN p_from AND p_to
    GROUP BY a.adjustment_id, u.full_name
    ORDER BY a.status, a.adjustment_date DESC, a.adjustment_id DESC;
$$;

-- The lines of one adjustment, with the item's stock now.
-- Draft: difference = counted - qty_before as it will be posted; posted: what was posted, at the cost of that day.
CREATE OR REPLACE FUNCTION public.fn_stock_adjustment_get_lines(p_adjustment_id integer)
RETURNS TABLE (
    product_id integer, item_name text, base_unit varchar, qty_before numeric, counted_qty numeric,
    difference numeric, unit_cost numeric, stock_qty numeric)
LANGUAGE sql STABLE
AS $$
    SELECT p.product_id, fn_item_description(p.name::text, p.size::text, p.color::text, NULL), p.base_unit,
           l.qty_before, l.counted_qty,
           coalesce(l.difference, l.counted_qty - l.qty_before), coalesce(l.unit_cost, p.avg_cost), p.stock_qty
    FROM stock_adjustment_lines l
    JOIN products p ON p.product_id = l.product_id
    WHERE l.adjustment_id = p_adjustment_id
    ORDER BY l.adjustment_line_id;
$$;

-- Standard items to count: the barcode of any of its packagings (exact), or every word in the name.
CREATE OR REPLACE FUNCTION public.fn_stock_adjustment_find_items(p_search text, p_limit integer DEFAULT 50)
RETURNS TABLE (
    product_id integer, item_name text, barcode varchar, base_unit varchar, stock_qty numeric, avg_cost numeric)
LANGUAGE plpgsql STABLE
AS $$
DECLARE
    v_search text := nullif(trim(p_search), '');
    v_words  text[];
BEGIN
    IF v_search IS NULL THEN RETURN; END IF;

    -- A scanned barcode finds its item only
    RETURN QUERY
    SELECT DISTINCT ON (p.product_id) p.product_id, fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.barcode, p.base_unit, p.stock_qty, p.avg_cost
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.barcode = v_search AND p.product_type = 1
    ORDER BY p.product_id;
    IF FOUND THEN RETURN; END IF;

    v_words := regexp_split_to_array(replace(replace(replace(v_search, '\', '\\'), '%', '\%'), '_', '\_'), '\s+');
    RETURN QUERY
    SELECT p.product_id, fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           (SELECT pk.barcode FROM product_packaging pk WHERE pk.product_id = p.product_id AND pk.is_default LIMIT 1),
           p.base_unit, p.stock_qty, p.avg_cost
    FROM products p
    WHERE p.product_type = 1 AND p.is_active
      AND NOT EXISTS (SELECT 1 FROM unnest(v_words) w WHERE p.name NOT ILIKE '%' || w || '%')
    ORDER BY p.name, p.product_id
    LIMIT greatest(coalesce(p_limit, 50), 1);
END $$;

-- Adds (p_adjustment_id NULL) or changes a draft and its lines; p_post = true posts it right after.
-- p_lines: [{"product_id": 5, "counted_qty": 12, "qty_before": 10}, ...]  (qty_before NULL = the stock now)
-- Returns the adjustment_id.
CREATE OR REPLACE FUNCTION public.fn_stock_adjustment_save(
    p_adjustment_id integer,
    p_reason        text,
    p_lines         jsonb,
    p_post          boolean,
    p_user_id       integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id     integer := p_adjustment_id;
    v_status smallint;
    v_bad    text;
BEGIN
    IF coalesce(trim(p_reason), '') = '' THEN RAISE EXCEPTION 'Type the reason of the adjustment (for example "Stock count").'; END IF;
    IF p_lines IS NULL OR jsonb_array_length(p_lines) = 0 THEN RAISE EXCEPTION 'Add at least one item.'; END IF;

    IF EXISTS (SELECT 1 FROM jsonb_array_elements(p_lines) j WHERE coalesce((j->>'counted_qty')::numeric, -1) < 0) THEN
        RAISE EXCEPTION 'The counted quantity cannot be negative.';
    END IF;
    IF EXISTS (SELECT 1 FROM jsonb_array_elements(p_lines) j GROUP BY (j->>'product_id')::int HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'An item is in the adjustment twice.';
    END IF;
    SELECT coalesce(fn_item_description(p.name::text, p.size::text, p.color::text, NULL), j->>'product_id') INTO v_bad
    FROM jsonb_array_elements(p_lines) j
    LEFT JOIN products p ON p.product_id = (j->>'product_id')::int
    WHERE p.product_id IS NULL OR p.product_type <> 1
    LIMIT 1;
    IF v_bad IS NOT NULL THEN RAISE EXCEPTION '"%" is not a stock item.', v_bad; END IF;

    IF v_id IS NULL THEN
        INSERT INTO stock_adjustments (reason, user_id) VALUES (trim(p_reason), p_user_id)
        RETURNING adjustment_id INTO v_id;
    ELSE
        SELECT status INTO v_status FROM stock_adjustments WHERE adjustment_id = v_id FOR UPDATE;
        IF NOT FOUND THEN RAISE EXCEPTION 'Adjustment % not found.', v_id; END IF;
        IF v_status <> 0 THEN RAISE EXCEPTION 'This adjustment is already posted; it cannot be changed.'; END IF;
        UPDATE stock_adjustments SET reason = trim(p_reason) WHERE adjustment_id = v_id;
        DELETE FROM stock_adjustment_lines WHERE adjustment_id = v_id;
    END IF;

    INSERT INTO stock_adjustment_lines (adjustment_id, product_id, counted_qty, qty_before)
    SELECT v_id, p.product_id, (j->>'counted_qty')::numeric, coalesce((j->>'qty_before')::numeric, p.stock_qty)
    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS x(j, ord)
    JOIN products p ON p.product_id = (j->>'product_id')::int
    ORDER BY x.ord;

    IF p_post THEN PERFORM fn_post_stock_adjustment(v_id, p_user_id); END IF;
    RETURN v_id;
END $$;

-- Deletes a draft; a posted adjustment stays (it changed the stock).
CREATE OR REPLACE FUNCTION public.fn_stock_adjustment_delete(p_adjustment_id integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_status smallint;
BEGIN
    SELECT status INTO v_status FROM stock_adjustments WHERE adjustment_id = p_adjustment_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Adjustment % not found.', p_adjustment_id; END IF;
    IF v_status <> 0 THEN RAISE EXCEPTION 'A posted adjustment cannot be deleted: it has changed the stock.'; END IF;
    DELETE FROM stock_adjustment_lines WHERE adjustment_id = p_adjustment_id;
    DELETE FROM stock_adjustments WHERE adjustment_id = p_adjustment_id;
END $$;
