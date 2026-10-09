-- Purchase functions used by the Inventory app (PurchaseRepository).
-- Safe to re-run: read functions are dropped and recreated.
-- Apply:  psql -h localhost -U postgres -d Pos -f purchase_functions.sql

-- ---------------------------------------------------------------------------
-- Supplier is optional for now (until the supplier form exists).
-- To make it required again later:
--   ALTER TABLE purchase_invoices ALTER COLUMN supplier_id SET NOT NULL;
--   and change the LEFT JOIN below back to JOIN.
-- ---------------------------------------------------------------------------
ALTER TABLE public.purchase_invoices ALTER COLUMN supplier_id DROP NOT NULL;

CREATE OR REPLACE VIEW public.v_purchase_list AS
 SELECT pi.purchase_id,
    pi.purchase_no,
    pi.supplier_invoice_no,
    pi.supplier_id,
    s.name AS supplier_name,
    pi.invoice_date,
    pi.total,
    pi.paid_lbp,
        CASE
            WHEN pi.status = 1 THEN pi.total - pi.paid_lbp
            ELSE NULL::numeric
        END AS balance_lbp,
    round(pi.total / pi.exchange_rate, 2) AS total_usd,
        CASE pi.status
            WHEN 0 THEN 'Draft'::text
            WHEN 2 THEN 'Cancelled'::text
            ELSE
            CASE
                WHEN pi.paid_lbp >= pi.total THEN 'Paid'::text
                WHEN pi.paid_lbp > 0::numeric THEN 'Part paid'::text
                ELSE 'Unpaid'::text
            END
        END AS status_text,
    pi.status
   FROM purchase_invoices pi
     LEFT JOIN suppliers s ON s.supplier_id = pi.supplier_id;

DROP FUNCTION IF EXISTS public.fn_purchase_get_all(integer, integer, text);
DROP FUNCTION IF EXISTS public.fn_purchase_get_lines(integer);
DROP FUNCTION IF EXISTS public.fn_purchase_get_by_id(integer);
DROP FUNCTION IF EXISTS public.fn_purchase_find_packaging(text, integer);

-- ---------------------------------------------------------------------------
-- One page of purchase invoices, newest first (built on v_purchase_list).
-- Every row carries total_count (all matching invoices) as its LAST column:
-- BaseRepository.GetPagedAsync splits on it.
-- p_search: NULL/blank = all. Otherwise matches the purchase number, the
--           supplier's invoice number or the supplier name (contains, case-insensitive).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_purchase_get_all(
    p_page      integer DEFAULT 1,
    p_page_size integer DEFAULT 50,
    p_search    text    DEFAULT NULL)
RETURNS TABLE (
    purchase_id integer, purchase_no varchar, supplier_invoice_no varchar,
    supplier_id integer, supplier_name varchar, invoice_date date,
    total numeric, paid_lbp numeric, balance_lbp numeric, total_usd numeric,
    status smallint, status_text text, item_count bigint,
    created_by_name varchar, posted_by_name varchar,
    total_count bigint)
LANGUAGE plpgsql STABLE
AS $$
DECLARE
    v_pattern text;
BEGIN
    IF p_page IS NULL OR p_page < 1 THEN RAISE EXCEPTION 'Page must be 1 or more.'; END IF;
    IF p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 500 THEN
        RAISE EXCEPTION 'Page size must be between 1 and 500.';
    END IF;

    IF nullif(trim(p_search), '') IS NOT NULL THEN
        v_pattern := '%' || replace(replace(replace(trim(p_search), '\', '\\'), '%', '\%'), '_', '\_') || '%';
    END IF;

    RETURN QUERY
    SELECT v.purchase_id, v.purchase_no, v.supplier_invoice_no,
           v.supplier_id, v.supplier_name, v.invoice_date,
           v.total, v.paid_lbp, v.balance_lbp, v.total_usd,
           v.status, v.status_text,
           (SELECT count(*) FROM purchase_lines l WHERE l.purchase_id = v.purchase_id),
           cu.full_name, pu.full_name,
           count(*) OVER ()
    FROM v_purchase_list v
    JOIN purchase_invoices pi ON pi.purchase_id = v.purchase_id
    LEFT JOIN users cu ON cu.user_id = pi.user_id     -- who created it
    LEFT JOIN users pu ON pu.user_id = pi.posted_by   -- who posted it (needs Database/user_tracking.sql)
    WHERE v_pattern IS NULL
       OR v.purchase_no ILIKE v_pattern
       OR v.supplier_invoice_no ILIKE v_pattern
       OR v.supplier_name ILIKE v_pattern
    ORDER BY v.invoice_date DESC, v.purchase_id DESC
    LIMIT p_page_size
    OFFSET (p_page - 1) * p_page_size;
END $$;

-- ---------------------------------------------------------------------------
-- The item lines of one purchase invoice, in the order they were entered.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_purchase_get_lines(p_purchase_id integer)
RETURNS TABLE (
    purchase_line_id integer, product_id integer, packaging_id integer, item_name text, packaging_name varchar,
    barcode varchar, quantity numeric, factor numeric, base_qty numeric, base_unit varchar, is_weighted boolean,
    unit_cost numeric, line_total numeric)
LANGUAGE sql STABLE
AS $$
    SELECT l.purchase_line_id, l.product_id, l.packaging_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.name, pk.barcode,
           l.quantity, l.factor, l.base_qty, p.base_unit, p.is_weighted,
           l.unit_cost, l.line_total
    FROM purchase_lines l
    JOIN products p           ON p.product_id = l.product_id
    JOIN product_packaging pk ON pk.packaging_id = l.packaging_id
    WHERE l.purchase_id = p_purchase_id
    ORDER BY l.purchase_line_id;
$$;

-- ---------------------------------------------------------------------------
-- The header of one purchase invoice (for the add / edit form).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_purchase_get_by_id(p_purchase_id integer)
RETURNS TABLE (
    purchase_id integer, purchase_no varchar, supplier_invoice_no varchar, supplier_id integer,
    supplier_name varchar, invoice_date date, exchange_rate numeric, sub_total numeric,
    discount numeric, total numeric, paid_lbp numeric, status smallint, notes varchar)
LANGUAGE sql STABLE
AS $$
    SELECT pi.purchase_id, pi.purchase_no, pi.supplier_invoice_no, pi.supplier_id,
           s.name, pi.invoice_date, pi.exchange_rate, pi.sub_total,
           pi.discount, pi.total, pi.paid_lbp, pi.status, pi.notes
    FROM purchase_invoices pi
    LEFT JOIN suppliers s ON s.supplier_id = pi.supplier_id
    WHERE pi.purchase_id = p_purchase_id;
$$;

-- ---------------------------------------------------------------------------
-- Packagings that can be purchased (active standard items, can_purchase), for
-- adding lines. An exact barcode match returns just that packaging; otherwise
-- every word must appear in the item name, or the barcode starts with the text.
-- last_cost = current average cost of one packaging (avg_cost * factor).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_purchase_find_packaging(p_search text, p_limit integer DEFAULT 50)
RETURNS TABLE (
    packaging_id integer, product_id integer, item_name text, packaging_name varchar,
    barcode varchar, factor numeric, base_unit varchar, is_weighted boolean, last_cost numeric)
LANGUAGE plpgsql STABLE
AS $$
DECLARE
    v_search  text := nullif(trim(p_search), '');
    v_pattern text;
    v_words   text[];
BEGIN
    IF v_search IS NULL THEN RETURN; END IF;

    -- Exact barcode (scanner): only that packaging
    RETURN QUERY
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.name, pk.barcode, pk.factor, p.base_unit, p.is_weighted, round(p.avg_cost * pk.factor, 2)
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.barcode = v_search
      AND pk.is_active AND pk.can_purchase AND p.is_active AND p.product_type = 1;
    IF FOUND THEN RETURN; END IF;

    v_pattern := replace(replace(replace(v_search, '\', '\\'), '%', '\%'), '_', '\_');
    v_words   := regexp_split_to_array(v_pattern, '\s+');

    RETURN QUERY
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.name, pk.barcode, pk.factor, p.base_unit, p.is_weighted, round(p.avg_cost * pk.factor, 2)
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.is_active AND pk.can_purchase AND p.is_active AND p.product_type = 1
      AND (NOT EXISTS (SELECT 1 FROM unnest(v_words) w WHERE p.name NOT ILIKE '%' || w || '%')
           OR pk.barcode LIKE v_pattern || '%')
    ORDER BY p.name, pk.factor, pk.packaging_id
    LIMIT greatest(coalesce(p_limit, 50), 1);
END $$;

-- ---------------------------------------------------------------------------
-- Saves a purchase invoice (new when p_purchase_id is NULL, otherwise updates a draft).
-- p_lines is a JSON array: [{ "packaging_id": 12, "quantity": 10, "unit_cost": 30000 }, ...]
--   (item and pack size come from the packaging; line totals are recalculated here).
-- p_post = true also posts it through fn_post_purchase: stock and average cost are
--   updated, the invoice gets its number and can no longer be changed.
-- Returns the purchase_id. All or nothing.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_purchase_save(
    p_purchase_id         integer,
    p_supplier_id         integer,
    p_supplier_invoice_no text,
    p_invoice_date        date,
    p_discount            numeric,
    p_notes               text,
    p_lines               jsonb,
    p_post                boolean,
    p_user_id             integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id     integer := p_purchase_id;
    v_status smallint;
    v_rate   numeric;
    v_sub    numeric;
    v_lines  integer;
BEGIN
    IF coalesce(p_discount, 0) < 0 THEN RAISE EXCEPTION 'Discount cannot be negative.'; END IF;
    IF jsonb_typeof(coalesce(p_lines, '[]'::jsonb)) <> 'array' THEN RAISE EXCEPTION 'Lines must be a JSON array.'; END IF;
    IF p_supplier_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier % not found.', p_supplier_id;
    END IF;

    -- Header
    IF v_id IS NULL THEN
        v_rate := fn_current_rate();
        IF v_rate IS NULL THEN RAISE EXCEPTION 'Set the exchange rate before entering purchases.'; END IF;

        INSERT INTO purchase_invoices (supplier_id, supplier_invoice_no, invoice_date, exchange_rate,
                                       discount, notes, status, user_id)
        VALUES (p_supplier_id, nullif(trim(p_supplier_invoice_no), ''), coalesce(p_invoice_date, CURRENT_DATE), v_rate,
                coalesce(p_discount, 0), nullif(trim(p_notes), ''), 0, p_user_id)
        RETURNING purchase_id INTO v_id;
    ELSE
        SELECT status INTO v_status FROM purchase_invoices WHERE purchase_id = v_id FOR UPDATE;
        IF NOT FOUND THEN RAISE EXCEPTION 'Purchase invoice % not found.', v_id; END IF;
        IF v_status <> 0 THEN RAISE EXCEPTION 'This purchase is already posted or cancelled and cannot be changed.'; END IF;

        UPDATE purchase_invoices
        SET supplier_id = p_supplier_id,
            supplier_invoice_no = nullif(trim(p_supplier_invoice_no), ''),
            invoice_date = coalesce(p_invoice_date, CURRENT_DATE),
            discount = coalesce(p_discount, 0),
            notes = nullif(trim(p_notes), '')
        WHERE purchase_id = v_id;

        DELETE FROM purchase_lines WHERE purchase_id = v_id;
    END IF;

    -- Lines
    IF EXISTS (SELECT 1 FROM jsonb_to_recordset(coalesce(p_lines, '[]'::jsonb)) AS x(quantity numeric)
               WHERE x.quantity IS NULL OR x.quantity <= 0) THEN
        RAISE EXCEPTION 'Every line needs a quantity more than 0.';
    END IF;
    IF EXISTS (SELECT 1 FROM jsonb_to_recordset(coalesce(p_lines, '[]'::jsonb)) AS x(unit_cost numeric)
               WHERE x.unit_cost IS NULL OR x.unit_cost < 0) THEN
        RAISE EXCEPTION 'Unit cost cannot be empty or negative.';
    END IF;

    INSERT INTO purchase_lines (purchase_id, product_id, packaging_id, quantity, factor, unit_cost, line_total)
    SELECT v_id, pk.product_id, pk.packaging_id, x.quantity, pk.factor, x.unit_cost, round(x.quantity * x.unit_cost, 2)
    FROM (SELECT (e.item->>'packaging_id')::integer AS packaging_id,
                 (e.item->>'quantity')::numeric    AS quantity,
                 (e.item->>'unit_cost')::numeric   AS unit_cost,
                 e.n
          FROM jsonb_array_elements(coalesce(p_lines, '[]'::jsonb)) WITH ORDINALITY AS e(item, n)) x
    JOIN product_packaging pk ON pk.packaging_id = x.packaging_id
    ORDER BY x.n;   -- keep the order the lines were entered in
    GET DIAGNOSTICS v_lines = ROW_COUNT;

    IF v_lines <> jsonb_array_length(coalesce(p_lines, '[]'::jsonb)) THEN
        RAISE EXCEPTION 'One of the items was not found. Remove it and add it again.';
    END IF;

    -- Totals
    SELECT coalesce(sum(line_total), 0) INTO v_sub FROM purchase_lines WHERE purchase_id = v_id;
    IF v_sub - coalesce(p_discount, 0) < 0 THEN RAISE EXCEPTION 'The discount is bigger than the invoice.'; END IF;
    UPDATE purchase_invoices SET sub_total = v_sub, total = v_sub - coalesce(p_discount, 0) WHERE purchase_id = v_id;

    IF p_post THEN
        PERFORM fn_post_purchase(v_id, p_user_id);
    END IF;

    RETURN v_id;
END $$;
