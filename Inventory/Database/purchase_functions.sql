-- Purchase functions used by the Inventory app (PurchaseRepository).
-- Safe to re-run: read functions are dropped and recreated.
-- Apply:  psql -h localhost -U postgres -d Pos -f purchase_functions.sql

-- ---------------------------------------------------------------------------
-- Supplier: a draft may be saved without one; posting needs one (fn_purchase_save),
-- because posting adds the total to what we owe that supplier (suppliers.balance_lbp).
-- The column stays nullable for drafts and for older purchases entered before suppliers existed.
-- ---------------------------------------------------------------------------
ALTER TABLE public.purchase_invoices ALTER COLUMN supplier_id DROP NOT NULL;

-- ---------------------------------------------------------------------------
-- Purchase currency: a purchase is entered in the currency of the supplier's invoice.
--   unit_cost, line_total, sub_total, discount, total  = in that currency
--   currency_rate = 1 unit of it in the primary currency (1 for the primary), kept with the invoice
--   total_lbp     = total in the primary currency (what the supplier balance, payments and costs use)
-- Item costs (avg_cost) and supplier balances stay in the primary currency: posting converts.
-- exchange_rate (USD rate when entered) is kept for the "Total $" column.
-- ---------------------------------------------------------------------------
ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS currency char(3);
ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS currency_rate numeric(18,6) NOT NULL DEFAULT 1;
ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS total_lbp numeric(18,2) NOT NULL DEFAULT 0;
-- Purchases entered before this were all in the primary currency
UPDATE public.purchase_invoices
SET currency = (SELECT code FROM currencies WHERE is_primary), currency_rate = 1, total_lbp = total
WHERE currency IS NULL;
ALTER TABLE public.purchase_invoices ALTER COLUMN currency SET NOT NULL;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'purchase_invoices_currency_fkey') THEN
        ALTER TABLE public.purchase_invoices
            ADD CONSTRAINT purchase_invoices_currency_fkey FOREIGN KEY (currency) REFERENCES currencies(code);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'purchase_invoices_currency_rate_check') THEN
        ALTER TABLE public.purchase_invoices
            ADD CONSTRAINT purchase_invoices_currency_rate_check CHECK (currency_rate > 0);
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- Purchase returns (goods sent back to the supplier): a purchase_invoices row with doc_type 2, numbered PR-...
-- Same lines, currency and discount as a purchase; posting it does the opposite (fn_post_purchase_return):
-- the stock goes down, the average cost is taken back, what we owe the supplier goes down.
-- return_of_id = the posted purchase the goods came on (NULL = a return without its purchase):
-- then only its items can go back, at most what was bought on it less what earlier returns took.
-- Amounts on a return are positive, like on a purchase; reports subtract them.
-- ---------------------------------------------------------------------------
ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS doc_type smallint NOT NULL DEFAULT 1;
ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS return_of_id integer REFERENCES public.purchase_invoices(purchase_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'purchase_invoices_doc_type_check') THEN
        ALTER TABLE public.purchase_invoices
            ADD CONSTRAINT purchase_invoices_doc_type_check CHECK (doc_type IN (1, 2) AND (doc_type = 2 OR return_of_id IS NULL));
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_purchase_return_of ON public.purchase_invoices (return_of_id) WHERE return_of_id IS NOT NULL;

CREATE OR REPLACE VIEW public.v_purchase_list AS
 SELECT pi.purchase_id,
    pi.purchase_no,
    pi.supplier_invoice_no,
    pi.supplier_id,
    s.name AS supplier_name,
    pi.invoice_date,
    pi.total,                                   -- in the purchase currency
    pi.paid_lbp,
        CASE
            WHEN pi.status = 1 AND pi.doc_type = 1 THEN pi.total_lbp - pi.paid_lbp
            ELSE NULL::numeric
        END AS balance_lbp,
    CASE WHEN pi.currency = 'USD' THEN pi.total
         ELSE round(pi.total * pi.currency_rate / pi.exchange_rate, 2) END AS total_usd,
        CASE pi.status
            WHEN 0 THEN 'Draft'::text
            WHEN 2 THEN 'Cancelled'::text
            ELSE
            CASE
                WHEN pi.doc_type = 2 THEN 'Returned'::text
                WHEN pi.paid_lbp >= pi.total_lbp THEN 'Paid'::text
                WHEN pi.paid_lbp > 0::numeric THEN 'Part paid'::text
                ELSE 'Unpaid'::text
            END
        END AS status_text,
    pi.status,
    pi.currency,
    pi.currency_rate,
    round(pi.total * pi.currency_rate, 2) AS total_lbp,  -- drafts too (posted: equals pi.total_lbp)
    pi.doc_type,                                         -- 1 purchase, 2 return to the supplier
    pi.return_of_id
   FROM purchase_invoices pi
     LEFT JOIN suppliers s ON s.supplier_id = pi.supplier_id;

DROP FUNCTION IF EXISTS public.fn_purchase_get_all(integer, integer, text);
DROP FUNCTION IF EXISTS public.fn_purchase_get_lines(integer);
DROP FUNCTION IF EXISTS public.fn_purchase_get_by_id(integer);
DROP FUNCTION IF EXISTS public.fn_purchase_find_packaging(text, integer);
DROP FUNCTION IF EXISTS public.fn_purchase_returnable_lines(integer);   -- no longer used
DROP FUNCTION IF EXISTS public.fn_purchase_save(integer, integer, text, date, numeric, text, jsonb, boolean, integer, text, numeric);

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
    status smallint, status_text text, item_count bigint, currency text, total_lbp numeric,
    created_by_name varchar, posted_by_name varchar,
    doc_type smallint, return_of_no varchar,
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
           v.currency::text, v.total_lbp,
           cu.full_name, pu.full_name,
           v.doc_type, ro.purchase_no,
           count(*) OVER ()
    FROM v_purchase_list v
    JOIN purchase_invoices pi ON pi.purchase_id = v.purchase_id
    LEFT JOIN purchase_invoices ro ON ro.purchase_id = v.return_of_id   -- the purchase a return is of
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
    discount numeric, total numeric, paid_lbp numeric, status smallint, notes varchar,
    currency text, currency_rate numeric, total_lbp numeric,
    doc_type smallint, return_of_id integer, return_of_no varchar)
LANGUAGE sql STABLE
AS $$
    SELECT pi.purchase_id, pi.purchase_no, pi.supplier_invoice_no, pi.supplier_id,
           s.name, pi.invoice_date, pi.exchange_rate, pi.sub_total,
           pi.discount, pi.total, pi.paid_lbp, pi.status, pi.notes,
           pi.currency::text, pi.currency_rate, round(pi.total * pi.currency_rate, 2),
           pi.doc_type, pi.return_of_id, ro.purchase_no
    FROM purchase_invoices pi
    LEFT JOIN suppliers s ON s.supplier_id = pi.supplier_id
    LEFT JOIN purchase_invoices ro ON ro.purchase_id = pi.return_of_id
    WHERE pi.purchase_id = p_purchase_id;
$$;



-- ---------------------------------------------------------------------------
-- Packagings that can be purchased (active standard items, can_purchase), for
-- adding lines. An exact barcode match returns just that packaging; otherwise
-- every word must appear in the item name, or the text is in the barcode (any part:
-- "6663" finds 145556663). last_cost = current average cost of one packaging (avg_cost * factor).
-- ---------------------------------------------------------------------------
-- Finding a part of a barcode quickly (pg_trgm, as the item name search)
CREATE INDEX IF NOT EXISTS ix_packaging_barcode_trgm ON public.product_packaging USING gin (barcode gin_trgm_ops);

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

    -- The same label written another way: saved without its check digit, or with / without a leading 0
    -- (fn_barcode_variants, Database/product_functions.sql)
    RETURN QUERY
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.name, pk.barcode, pk.factor, p.base_unit, p.is_weighted, round(p.avg_cost * pk.factor, 2)
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.barcode = ANY (fn_barcode_variants(v_search))
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
           OR pk.barcode LIKE '%' || v_pattern || '%')
    ORDER BY p.name, pk.factor, pk.packaging_id
    LIMIT greatest(coalesce(p_limit, 50), 1);
END $$;

-- ---------------------------------------------------------------------------
-- Saves a purchase invoice (new when p_purchase_id is NULL, otherwise updates a draft).
-- p_lines is a JSON array: [{ "packaging_id": 12, "quantity": 10, "unit_cost": 30000 }, ...]
--   (item and pack size come from the packaging; line totals are recalculated here).
-- p_post = true also posts it through fn_post_purchase: stock and average cost are
--   updated, the invoice gets its number and can no longer be changed.
-- p_currency = the invoice currency (NULL = primary); unit costs and the discount are in it.
-- p_currency_rate = 1 unit of it in the primary currency (NULL = today's rate; always 1 for the primary).
-- p_doc_type = 2: a return to the supplier (Type on the purchase screen; a draft can still change it).
-- p_return_of_id = for a return, the posted purchase the goods came on (NULL = without its purchase);
--   the return then has that purchase's supplier.
-- Returns the purchase_id. All or nothing.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_purchase_save(integer, integer, text, date, numeric, text, jsonb, boolean, integer);
CREATE OR REPLACE FUNCTION public.fn_purchase_save(
    p_purchase_id         integer,
    p_supplier_id         integer,
    p_supplier_invoice_no text,
    p_invoice_date        date,
    p_discount            numeric,
    p_notes               text,
    p_lines               jsonb,
    p_post                boolean,
    p_user_id             integer,
    p_currency            text    DEFAULT NULL,
    p_currency_rate       numeric DEFAULT NULL,
    p_doc_type            integer DEFAULT 1,
    p_return_of_id        integer DEFAULT NULL)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id       integer := p_purchase_id;
    v_status   smallint;
    v_rate     numeric;
    v_sub      numeric;
    v_lines    integer;
    v_currency text;
    v_cur_rate numeric;
    v_doc_type smallint := coalesce(p_doc_type, 1);
    v_return_of integer := p_return_of_id;
BEGIN
    -- A draft keeps the purchase it is a return of; its type can change until it is posted (not when it is of a purchase)
    IF v_id IS NOT NULL THEN
        SELECT CASE WHEN return_of_id IS NULL THEN coalesce(p_doc_type, doc_type) ELSE doc_type END, return_of_id
        INTO v_doc_type, v_return_of
        FROM purchase_invoices WHERE purchase_id = v_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'Purchase invoice % not found.', v_id; END IF;
    END IF;
    IF v_doc_type NOT IN (1, 2) THEN RAISE EXCEPTION 'Unknown document type %.', v_doc_type; END IF;
    IF v_doc_type = 1 AND v_return_of IS NOT NULL THEN RAISE EXCEPTION 'Only a return can be of another purchase.'; END IF;
    IF v_return_of IS NOT NULL THEN
        SELECT supplier_id INTO p_supplier_id FROM purchase_invoices
        WHERE purchase_id = v_return_of AND doc_type = 1 AND status = 1;
        IF NOT FOUND THEN RAISE EXCEPTION 'Goods can only go back from a posted purchase.'; END IF;
    END IF;

    -- Currency and its rate
    v_currency := upper(coalesce(nullif(trim(p_currency), ''), (SELECT code FROM currencies WHERE is_primary)));
    IF NOT EXISTS (SELECT 1 FROM currencies WHERE code = v_currency AND is_active) THEN
        RAISE EXCEPTION 'The currency % is not active. Choose another one or activate it in Settings > Currencies.', v_currency;
    END IF;
    IF (SELECT is_primary FROM currencies WHERE code = v_currency) THEN
        v_cur_rate := 1;
    ELSE
        v_cur_rate := coalesce(p_currency_rate, fn_currency_rate(v_currency));
        IF v_cur_rate IS NULL OR v_cur_rate <= 0 THEN
            RAISE EXCEPTION 'Enter the exchange rate of % for this purchase.', v_currency;
        END IF;
    END IF;

    IF coalesce(p_discount, 0) < 0 THEN RAISE EXCEPTION 'Discount cannot be negative.'; END IF;
    IF jsonb_typeof(coalesce(p_lines, '[]'::jsonb)) <> 'array' THEN RAISE EXCEPTION 'Lines must be a JSON array.'; END IF;
    IF p_supplier_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier % not found.', p_supplier_id;
    END IF;
    -- A draft can wait for its supplier; a posted purchase adds to what we owe that supplier, so it needs one
    IF p_post AND p_supplier_id IS NULL THEN RAISE EXCEPTION 'Choose the supplier before posting.'; END IF;
    IF p_supplier_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM suppliers WHERE supplier_id = p_supplier_id AND is_active)
       AND p_supplier_id IS DISTINCT FROM (SELECT supplier_id FROM purchase_invoices WHERE purchase_id = p_purchase_id) THEN
        RAISE EXCEPTION 'This supplier is inactive. Activate it in Contacts > Suppliers first.';
    END IF;

    -- Header
    IF v_id IS NULL THEN
        v_rate := fn_current_rate();
        IF v_rate IS NULL THEN RAISE EXCEPTION 'Set the exchange rate before entering purchases.'; END IF;

        INSERT INTO purchase_invoices (supplier_id, supplier_invoice_no, invoice_date, exchange_rate,
                                       discount, notes, status, user_id, currency, currency_rate, doc_type, return_of_id)
        VALUES (p_supplier_id, nullif(trim(p_supplier_invoice_no), ''), coalesce(p_invoice_date, CURRENT_DATE), v_rate,
                coalesce(p_discount, 0), nullif(trim(p_notes), ''), 0, p_user_id, v_currency, v_cur_rate, v_doc_type, v_return_of)
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
            notes = nullif(trim(p_notes), ''),
            currency = v_currency,
            currency_rate = v_cur_rate,
            doc_type = v_doc_type
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
    UPDATE purchase_invoices
    SET sub_total = v_sub, total = v_sub - coalesce(p_discount, 0),
        total_lbp = round((v_sub - coalesce(p_discount, 0)) * v_cur_rate, 2)
    WHERE purchase_id = v_id;

    IF p_post THEN
        PERFORM fn_post_purchase(v_id, p_user_id);
    END IF;

    RETURN v_id;
END $$;

-- ---------------------------------------------------------------------------
-- Posts a draft purchase: stock goes up, the average cost is updated, the invoice gets its
-- number and the supplier balance goes up. Amounts on the invoice are in its currency;
-- costs and the supplier balance are in the primary currency (converted with currency_rate).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_post_purchase(p_purchase_id integer, p_user_id integer)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    v_inv       purchase_invoices%ROWTYPE;
    v_sub       numeric;
    v_total     numeric;
    v_total_lbp numeric;
    v_ratio     numeric;
    v_no        text;
    v_bad       text;
BEGIN
    SELECT * INTO v_inv FROM purchase_invoices WHERE purchase_id = p_purchase_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Purchase invoice % not found.', p_purchase_id; END IF;
    IF v_inv.doc_type = 2 THEN RETURN fn_post_purchase_return(p_purchase_id, p_user_id); END IF;
    IF v_inv.status <> 0 THEN RAISE EXCEPTION 'This purchase is already posted or cancelled.'; END IF;
    IF NOT EXISTS (SELECT 1 FROM purchase_lines WHERE purchase_id = p_purchase_id) THEN
        RAISE EXCEPTION 'The purchase has no lines.';
    END IF;

    -- Item and pack size always come from the packaging, never from the screen
    UPDATE purchase_lines l SET product_id = pk.product_id, factor = pk.factor
    FROM product_packaging pk
    WHERE pk.packaging_id = l.packaging_id AND l.purchase_id = p_purchase_id
      AND (l.product_id <> pk.product_id OR l.factor <> pk.factor);

    SELECT p.name INTO v_bad
    FROM purchase_lines l JOIN products p ON p.product_id = l.product_id
    JOIN product_packaging pk ON pk.packaging_id = l.packaging_id
    WHERE l.purchase_id = p_purchase_id AND (p.product_type <> 1 OR NOT pk.can_purchase)
    LIMIT 1;
    IF v_bad IS NOT NULL THEN RAISE EXCEPTION '"%" cannot be purchased with this packaging.', v_bad; END IF;

    SELECT p.name INTO v_bad
    FROM purchase_lines l JOIN products p ON p.product_id = l.product_id
    WHERE l.purchase_id = p_purchase_id AND NOT p.is_weighted AND l.base_qty <> trunc(l.base_qty)
    LIMIT 1;
    IF v_bad IS NOT NULL THEN RAISE EXCEPTION '"%" is bought in whole units only.', v_bad; END IF;

    -- Recalculate the totals (never trust what the screen sent)
    UPDATE purchase_lines SET line_total = round(quantity * unit_cost, 2) WHERE purchase_id = p_purchase_id;
    SELECT sum(line_total) INTO v_sub FROM purchase_lines WHERE purchase_id = p_purchase_id;
    v_total := v_sub - v_inv.discount;
    IF v_total < 0 THEN RAISE EXCEPTION 'The discount is bigger than the invoice.'; END IF;
    v_ratio := CASE WHEN v_sub = 0 THEN 1 ELSE v_total / v_sub END;   -- spreads the invoice discount on the cost
    v_total_lbp := round(v_total * v_inv.currency_rate, 2);

    -- Incoming quantity and cost per base unit in the primary currency, per product
    IF to_regclass('pg_temp._in') IS NOT NULL THEN DROP TABLE pg_temp._in; END IF;
    CREATE TEMP TABLE _in ON COMMIT DROP AS
    SELECT product_id,
           sum(base_qty) AS qty,
           sum(line_total) * v_ratio * v_inv.currency_rate / sum(base_qty) AS cost
    FROM purchase_lines WHERE purchase_id = p_purchase_id
    GROUP BY product_id;

    PERFORM 1 FROM products WHERE product_id IN (SELECT product_id FROM _in) ORDER BY product_id FOR UPDATE;

    UPDATE products p
    SET avg_cost  = CASE WHEN p.stock_qty <= 0 THEN i.cost
                         ELSE (p.stock_qty * p.avg_cost + i.qty * i.cost) / (p.stock_qty + i.qty) END,
        stock_qty = p.stock_qty + i.qty,
        updated_at = now()
    FROM _in i WHERE p.product_id = i.product_id;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id)
    SELECT i.product_id, 1, i.qty, i.cost, p.stock_qty, 1, p_purchase_id, p_user_id
    FROM _in i JOIN products p ON p.product_id = i.product_id;

    v_no := coalesce(v_inv.purchase_no, fn_next_doc_no('P'));

    UPDATE purchase_invoices
    SET purchase_no = v_no, sub_total = v_sub, total = v_total, total_lbp = v_total_lbp, status = 1, posted_at = now()
    WHERE purchase_id = p_purchase_id;

    UPDATE suppliers SET balance_lbp = balance_lbp + v_total_lbp WHERE supplier_id = v_inv.supplier_id;

    RETURN v_no;
END $$;

-- ---------------------------------------------------------------------------
-- Posts a draft return to the supplier (called by fn_post_purchase): the opposite of a purchase.
--   - the stock goes down (refused when there is not enough, unless allow_negative_stock = '1');
--   - the average cost takes the returned goods out at their cost: (stock x avg - qty x cost) / (stock - qty),
--     kept as it was when that does not make sense (nothing left, or below 0);
--   - stock ledger: movement 4 (return to supplier), ref_type 1 (purchase document);
--   - what we owe the supplier goes down by the return's total (in the primary currency);
--   - it gets its number PR-... and can no longer be changed.
-- A return of a purchase (return_of_id) may only take its items, at most what was bought on it less earlier returns.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_post_purchase_return(p_purchase_id integer, p_user_id integer)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    v_inv       purchase_invoices%ROWTYPE;
    v_sub       numeric;
    v_total     numeric;
    v_total_lbp numeric;
    v_ratio     numeric;
    v_no        text;
    v_bad       text;
BEGIN
    SELECT * INTO v_inv FROM purchase_invoices WHERE purchase_id = p_purchase_id AND doc_type = 2 FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Purchase return % not found.', p_purchase_id; END IF;
    IF v_inv.status <> 0 THEN RAISE EXCEPTION 'This return is already posted or cancelled.'; END IF;
    IF v_inv.supplier_id IS NULL THEN RAISE EXCEPTION 'Choose the supplier the goods go back to.'; END IF;
    IF NOT EXISTS (SELECT 1 FROM purchase_lines WHERE purchase_id = p_purchase_id) THEN
        RAISE EXCEPTION 'The return has no lines.';
    END IF;

    UPDATE purchase_lines l SET product_id = pk.product_id, factor = pk.factor
    FROM product_packaging pk
    WHERE pk.packaging_id = l.packaging_id AND l.purchase_id = p_purchase_id
      AND (l.product_id <> pk.product_id OR l.factor <> pk.factor);

    SELECT p.name INTO v_bad
    FROM purchase_lines l JOIN products p ON p.product_id = l.product_id
    WHERE l.purchase_id = p_purchase_id AND p.product_type <> 1
    LIMIT 1;
    IF v_bad IS NOT NULL THEN RAISE EXCEPTION '"%" is not a stock item.', v_bad; END IF;

    SELECT p.name INTO v_bad
    FROM purchase_lines l JOIN products p ON p.product_id = l.product_id
    WHERE l.purchase_id = p_purchase_id AND NOT p.is_weighted AND l.base_qty <> trunc(l.base_qty)
    LIMIT 1;
    IF v_bad IS NOT NULL THEN RAISE EXCEPTION '"%" goes back in whole units only.', v_bad; END IF;

    -- A return of a purchase: only its items, and not more than is left of them (other returns of it wait: lock it)
    IF v_inv.return_of_id IS NOT NULL THEN
        PERFORM 1 FROM purchase_invoices WHERE purchase_id = v_inv.return_of_id FOR UPDATE;
        SELECT fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text)
               || CASE WHEN coalesce(b.qty, 0) = 0 THEN ' was not on that purchase.'
                       ELSE ': only ' || trim_scale(b.qty - coalesce(r.qty, 0)) || ' left to return.' END
        INTO v_bad
        FROM (SELECT packaging_id, sum(quantity) AS qty FROM purchase_lines WHERE purchase_id = p_purchase_id GROUP BY packaging_id) n
        JOIN product_packaging pk ON pk.packaging_id = n.packaging_id
        JOIN products p ON p.product_id = pk.product_id
        LEFT JOIN (SELECT packaging_id, sum(quantity) AS qty FROM purchase_lines WHERE purchase_id = v_inv.return_of_id
                   GROUP BY packaging_id) b ON b.packaging_id = n.packaging_id
        LEFT JOIN (SELECT l.packaging_id, sum(l.quantity) AS qty
                   FROM purchase_invoices r JOIN purchase_lines l ON l.purchase_id = r.purchase_id
                   WHERE r.return_of_id = v_inv.return_of_id AND r.doc_type = 2 AND r.status = 1
                   GROUP BY l.packaging_id) r ON r.packaging_id = n.packaging_id
        WHERE n.qty > coalesce(b.qty, 0) - coalesce(r.qty, 0)
        LIMIT 1;
        IF v_bad IS NOT NULL THEN RAISE EXCEPTION '%', v_bad; END IF;
    END IF;

    -- Totals (never trust what the screen sent)
    UPDATE purchase_lines SET line_total = round(quantity * unit_cost, 2) WHERE purchase_id = p_purchase_id;
    SELECT sum(line_total) INTO v_sub FROM purchase_lines WHERE purchase_id = p_purchase_id;
    v_total := v_sub - v_inv.discount;
    IF v_total < 0 THEN RAISE EXCEPTION 'The discount is bigger than the return.'; END IF;
    v_ratio := CASE WHEN v_sub = 0 THEN 1 ELSE v_total / v_sub END;
    v_total_lbp := round(v_total * v_inv.currency_rate, 2);

    -- Outgoing quantity and cost per base unit in the primary currency, per product
    IF to_regclass('pg_temp._out') IS NOT NULL THEN DROP TABLE pg_temp._out; END IF;
    CREATE TEMP TABLE _out ON COMMIT DROP AS
    SELECT product_id,
           sum(base_qty) AS qty,
           sum(line_total) * v_ratio * v_inv.currency_rate / sum(base_qty) AS cost
    FROM purchase_lines WHERE purchase_id = p_purchase_id
    GROUP BY product_id;

    PERFORM 1 FROM products WHERE product_id IN (SELECT product_id FROM _out) ORDER BY product_id FOR UPDATE;

    IF fn_setting('allow_negative_stock', '1') <> '1' THEN
        SELECT fn_item_description(p.name, p.size, p.color, NULL) || ': only ' || trim_scale(p.stock_qty) || ' ' || p.base_unit || ' in stock'
        INTO v_bad
        FROM _out o JOIN products p ON p.product_id = o.product_id
        WHERE p.stock_qty < o.qty LIMIT 1;
        IF v_bad IS NOT NULL THEN RAISE EXCEPTION '%', v_bad; END IF;
    END IF;

    UPDATE products p
    SET avg_cost  = CASE WHEN p.stock_qty - o.qty > 0 AND p.stock_qty * p.avg_cost - o.qty * o.cost > 0
                         THEN (p.stock_qty * p.avg_cost - o.qty * o.cost) / (p.stock_qty - o.qty)
                         ELSE p.avg_cost END,
        stock_qty = p.stock_qty - o.qty,
        updated_at = now()
    FROM _out o WHERE p.product_id = o.product_id;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id)
    SELECT o.product_id, 4, -o.qty, o.cost, p.stock_qty, 1, p_purchase_id, p_user_id
    FROM _out o JOIN products p ON p.product_id = o.product_id;

    v_no := coalesce(v_inv.purchase_no, fn_next_doc_no('PR'));

    UPDATE purchase_invoices
    SET purchase_no = v_no, sub_total = v_sub, total = v_total, total_lbp = v_total_lbp, status = 1, posted_at = now()
    WHERE purchase_id = p_purchase_id;

    UPDATE suppliers SET balance_lbp = balance_lbp - v_total_lbp WHERE supplier_id = v_inv.supplier_id;

    RETURN v_no;
END $$;

-- ---------------------------------------------------------------------------
-- AI invoice reading: the items that best match one line read from a supplier's invoice photo or a voice note.
-- p_text = the item as written / said ("pepsi bix", "سيدرز قديم"), p_text_alt = the same name in the other script
-- ("Cedars Old") or NULL: the better of the two counts. p_packaging = the packaging read (box, pack, bottle)
-- or NULL, p_barcode = a barcode printed on the line or NULL.
-- Fuzzy text match (pg_trgm) on normalized text (fn_text_normalize: "أحمر" = "احمر"), so spelling mistakes still
-- match; an exact barcode always wins (score 1).
-- score 0..1: about 0.5 and more is a good match. Only packagings that can be purchased.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_purchase_match_packaging(text, text, text, integer);
DROP FUNCTION IF EXISTS public.fn_purchase_match_packaging(text, text, text, text, integer);
CREATE OR REPLACE FUNCTION public.fn_purchase_match_packaging(
    p_text text, p_packaging text DEFAULT NULL, p_barcode text DEFAULT NULL, p_limit integer DEFAULT 5,
    p_text_alt text DEFAULT NULL)
RETURNS TABLE (
    packaging_id integer, product_id integer, item_name text, packaging_name varchar,
    barcode varchar, factor numeric, base_unit varchar, is_weighted boolean, last_cost numeric, score numeric)
LANGUAGE sql STABLE
AS $$
    WITH input AS (
        SELECT fn_text_normalize(coalesce(nullif(trim(p_text), ''), '?')) AS t,
               fn_text_normalize(coalesce(nullif(trim(p_text_alt), ''), '?')) AS t2,
               lower(nullif(trim(p_packaging), '')) AS pk,
               nullif(trim(p_barcode), '') AS bc
    ),
    names AS (
        SELECT pk.packaging_id, p.product_id, pk.name AS pack_name, pk.barcode, pk.factor, pk.is_default,
               p.base_unit, p.is_weighted, p.avg_cost,
               fn_item_description(p.name::text, p.size::text, p.color::text, NULL) AS item_name,
               fn_text_normalize(p.name) AS n_name,
               fn_text_normalize(p.name || ' ' || coalesce(p.size, '') || ' ' || pk.name) AS n_full,
               fn_text_normalize(p.name || ' ' || pk.name) AS n_pack
        FROM product_packaging pk
        JOIN products p ON p.product_id = pk.product_id
        WHERE pk.is_active AND pk.can_purchase AND p.is_active AND p.product_type = 1
    ),
    candidates AS (
        SELECT n.packaging_id, n.product_id, n.item_name,
               n.pack_name AS packaging_name, n.barcode, n.factor, n.base_unit, n.is_weighted,
               round(n.avg_cost * n.factor, 2) AS last_cost,
               -- name similarity counts for 85%, the packaging read on the invoice (box / pack / bottle...) for 15%,
               -- so the right packaging of the right item scores highest
               CASE WHEN i.bc IS NOT NULL AND n.barcode = i.bc THEN 1.0
                    ELSE least(0.99,
                         0.85 * greatest(similarity(i.t, n.n_name), word_similarity(i.t, n.n_full), similarity(i.t, n.n_pack),
                                         similarity(i.t2, n.n_name), word_similarity(i.t2, n.n_full), similarity(i.t2, n.n_pack))
                         + CASE WHEN i.pk IS NULL THEN (CASE WHEN n.is_default THEN 0.10 ELSE 0.05 END)
                                WHEN lower(n.pack_name) = i.pk OR similarity(lower(n.pack_name), i.pk) > 0.4 THEN 0.15
                                -- "pack", "carton", "case", "bp"... on the invoice = a packaging of several units
                                WHEN i.pk IN ('pack', 'packet', 'carton', 'case', 'box', 'bx', 'bp', 'dozen', 'tray', 'bundle', 'crate')
                                     AND n.factor > 1 THEN 0.12
                                ELSE 0 END)
               END::numeric(5,3) AS score
        FROM names n
        CROSS JOIN input i
    )
    SELECT c.packaging_id, c.product_id, c.item_name, c.packaging_name, c.barcode, c.factor, c.base_unit,
           c.is_weighted, c.last_cost, greatest(c.score, 0)
    FROM candidates c
    WHERE c.score > 0.15
    ORDER BY c.score DESC, c.item_name, c.factor
    LIMIT greatest(coalesce(p_limit, 5), 1);
$$;
