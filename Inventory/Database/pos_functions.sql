-- Cashier (POS) functions used by the Inventory app (SaleRepository).
-- Saving a sale uses the existing fn_save_sale, holding uses fn_hold_sale, refunds use fn_save_sale_return.
-- Safe to re-run: functions are dropped and recreated.
-- Apply:  psql -h localhost -U postgres -d Pos -f pos_functions.sql

DROP FUNCTION IF EXISTS public.fn_pos_find_item(text, integer);
DROP FUNCTION IF EXISTS public.fn_pos_get_held_sales();
DROP FUNCTION IF EXISTS public.fn_pos_get_held_lines(integer);
DROP FUNCTION IF EXISTS public.fn_pos_delete_held(integer);
DROP FUNCTION IF EXISTS public.fn_pos_find_sales(text, integer);       -- old refund-by-invoice lookups (removed)
DROP FUNCTION IF EXISTS public.fn_pos_get_sale_lines(integer);
DROP FUNCTION IF EXISTS public.fn_audit_add(text, text, integer, jsonb);
DROP FUNCTION IF EXISTS public.fn_save_return(integer, jsonb, integer, text, integer);
DROP FUNCTION IF EXISTS public.fn_sales_by_user(integer, date, date);
DROP FUNCTION IF EXISTS public.fn_sale_get_lines(integer);

-- ---------------------------------------------------------------------------
-- Sales screen (cashier F7): finished sales and returns between two dates (inclusive), newest first.
-- p_user_id NULL = every cashier. Numbers are positive; invoice_type 2 = return (subtract it in totals).
-- payments: how it was paid / refunded, e.g. "LBP 40,000 + USD 1.00".
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_sales_by_user(p_user_id integer, p_from date, p_to date)
RETURNS TABLE (sale_id integer, invoice_no varchar, invoice_type smallint, invoice_date timestamptz, cashier varchar,
               item_count bigint, total numeric, profit numeric, payments text)
LANGUAGE sql STABLE
AS $$
    SELECT si.sale_id, si.invoice_no, si.invoice_type, si.invoice_date, u.full_name,
           (SELECT count(*) FROM sales_lines l WHERE l.sale_id = si.sale_id),
           si.total, si.profit,
           (SELECT string_agg(trim(p.currency) || ' ' || to_char(p.amount, 'FM999,999,999,990.##')
                              || CASE p.method WHEN 2 THEN ' card' WHEN 3 THEN ' account' ELSE '' END, ' + ' ORDER BY p.payment_id)
            FROM sales_payments p WHERE p.sale_id = si.sale_id)
    FROM sales_invoices si
    LEFT JOIN users u ON u.user_id = si.user_id
    WHERE si.status = 1
      AND (p_user_id IS NULL OR si.user_id = p_user_id)
      AND si.invoice_date >= p_from::timestamptz
      AND si.invoice_date <  (p_to + 1)::timestamptz
    ORDER BY si.invoice_date DESC, si.sale_id DESC;
$$;

-- The items of one sale or return
CREATE OR REPLACE FUNCTION public.fn_sale_get_lines(p_sale_id integer)
RETURNS TABLE (line_no integer, item_name varchar, quantity numeric, unit_price numeric, discount numeric,
               line_total numeric, line_profit numeric)
LANGUAGE sql STABLE
AS $$
    SELECT l.line_no, l.description, l.quantity, l.unit_price, l.discount + l.discount_share, l.line_total, l.line_profit
    FROM sales_lines l
    WHERE l.sale_id = p_sale_id
    ORDER BY l.line_no;
$$;

-- A return invoice (invoice_type 2) does not need the original sale: the cashier scans what comes back.
ALTER TABLE public.sales_invoices DROP CONSTRAINT IF EXISTS sales_invoices_check1;

-- ---------------------------------------------------------------------------
-- Return invoice from the cashier's Return mode: the items the customer brings back are scanned
-- like a sale, the money goes back (cash in any active currency with a rate, or card in the primary
-- currency) and the stock comes back in.
-- p_lines: [{ "packaging_id": 12, "quantity": 1, "unit_price": 45000, "discount": 0 }, ...]
-- p_refund_method: 1 cash, 2 card. Numbers are positive like the sale; invoice_type 2 makes it a return
-- (reports subtract it). Returns the new return invoice (number R-...). All or nothing.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_save_return(
    p_user_id integer, p_lines jsonb, p_refund_method integer DEFAULT 1, p_refund_currency text DEFAULT 'LBP',
    p_shift_id integer DEFAULT NULL)
RETURNS TABLE (return_id integer, invoice_no text, refund_lbp numeric, refund_amount numeric)
LANGUAGE plpgsql
AS $$
#variable_conflict use_column
DECLARE
    v_rate   numeric := fn_current_rate();
    v_count  int;
    v_refund numeric;
    v_cost   numeric;
    v_amount numeric;
    v_msg    text;
    v_id     int;
    v_no     text;
BEGIN
    IF v_rate IS NULL THEN RAISE EXCEPTION 'Set the exchange rate first.'; END IF;
    IF p_refund_method NOT IN (1, 2)
       OR NOT EXISTS (SELECT 1 FROM currencies c WHERE c.code = upper(p_refund_currency) AND c.is_active)
       OR fn_currency_rate(p_refund_currency) IS NULL
       OR (p_refund_method = 2 AND NOT EXISTS (SELECT 1 FROM currencies c WHERE c.code = upper(p_refund_currency) AND c.is_primary)) THEN
        RAISE EXCEPTION 'Refund method or currency is not valid (card refunds are in the primary currency).';
    END IF;
    IF p_lines IS NULL OR jsonb_typeof(p_lines) <> 'array' OR jsonb_array_length(p_lines) = 0 THEN
        RAISE EXCEPTION 'Scan the items that come back first.';
    END IF;
    IF p_shift_id IS NOT NULL THEN
        PERFORM 1 FROM shifts WHERE shift_id = p_shift_id AND status = 1 FOR SHARE;
        IF NOT FOUND THEN RAISE EXCEPTION 'This shift is closed. Open a new shift first.'; END IF;
    END IF;

    -- 1. Lines with item data (an item no longer sold can still come back)
    IF to_regclass('pg_temp._rl') IS NOT NULL THEN DROP TABLE pg_temp._rl; END IF;
    CREATE TEMP TABLE _rl ON COMMIT DROP AS
    SELECT x.ord::int AS line_no, pk.packaging_id, p.product_id, p.product_type, p.is_weighted,
           fn_item_description(p.name, p.size, p.color, pk.name) AS description,
           (x.j->>'quantity')::numeric(18,3) AS quantity, pk.factor,
           coalesce((x.j->>'unit_price')::numeric, pk.sale_price)::numeric(18,2) AS unit_price,
           coalesce((x.j->>'discount')::numeric, 0)::numeric(18,2) AS discount,
           0::numeric(18,4) AS unit_cost,
           0::numeric(18,2) AS refund
    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS x(j, ord)
    JOIN product_packaging pk ON pk.packaging_id = (x.j->>'packaging_id')::int
    JOIN products p ON p.product_id = pk.product_id AND p.product_type IN (1, 2);

    SELECT count(*) INTO v_count FROM _rl;
    IF v_count <> jsonb_array_length(p_lines) THEN RAISE EXCEPTION 'An item on the return was not found.'; END IF;
    IF EXISTS (SELECT 1 FROM _rl WHERE quantity IS NULL OR quantity <= 0 OR unit_price < 0 OR discount < 0
                                   OR discount > quantity * unit_price) THEN
        RAISE EXCEPTION 'Check quantities, prices and discounts on the return.';
    END IF;
    SELECT description INTO v_msg FROM _rl WHERE NOT is_weighted AND quantity <> trunc(quantity) LIMIT 1;
    IF v_msg IS NOT NULL THEN RAISE EXCEPTION '"%" comes back in whole units only.', v_msg; END IF;

    -- 2. Stock back in per real item (bundles -> their components)
    IF to_regclass('pg_temp._rback') IS NOT NULL THEN DROP TABLE pg_temp._rback; END IF;
    CREATE TEMP TABLE _rback ON COMMIT DROP AS
    SELECT product_id, sum(qty)::numeric(18,3) AS qty FROM (
        SELECT product_id, quantity * factor AS qty FROM _rl WHERE product_type = 1
        UNION ALL
        SELECT b.component_product_id, l.quantity * l.factor * b.quantity
        FROM _rl l JOIN bundle_items b ON b.bundle_product_id = l.product_id
        WHERE l.product_type = 2
    ) s GROUP BY product_id;

    PERFORM 1 FROM products WHERE product_id IN (SELECT product_id FROM _rback) ORDER BY product_id FOR UPDATE;

    -- 3. Cost now (average cost x factor), refund per line
    UPDATE _rl l SET unit_cost = p.avg_cost * l.factor
    FROM products p WHERE p.product_id = l.product_id AND l.product_type = 1;
    UPDATE _rl l SET unit_cost = l.factor * coalesce((
        SELECT sum(c.avg_cost * b.quantity) FROM bundle_items b
        JOIN products c ON c.product_id = b.component_product_id
        WHERE b.bundle_product_id = l.product_id), 0)
    WHERE l.product_type = 2;
    UPDATE _rl SET refund = round(quantity * unit_price - discount, 2);

    SELECT sum(refund), sum(round(quantity * unit_cost, 2)) INTO v_refund, v_cost FROM _rl;
    IF v_refund <= 0 THEN RAISE EXCEPTION 'There is nothing to refund.'; END IF;
    v_amount := round(v_refund / fn_currency_rate(p_refund_currency),
                      (SELECT c.decimals FROM currencies c WHERE c.code = upper(p_refund_currency)));

    -- 4. Save
    v_no := fn_next_doc_no('R');

    INSERT INTO sales_invoices (invoice_no, invoice_type, shift_id, user_id, exchange_rate,
                                sub_total, discount, total, total_cost, profit, status)
    VALUES (v_no, 2, p_shift_id, p_user_id, v_rate, v_refund, 0, v_refund, v_cost, v_refund - v_cost, 1)
    RETURNING sale_id INTO v_id;

    INSERT INTO sales_lines (sale_id, line_no, product_id, packaging_id, description, quantity, factor,
                             unit_price, discount, unit_cost, line_total, line_profit)
    SELECT v_id, line_no, product_id, packaging_id, description, quantity, factor,
           unit_price, discount, unit_cost, refund, refund - round(quantity * unit_cost, 2)
    FROM _rl ORDER BY line_no;

    INSERT INTO sales_line_components (sale_line_id, product_id, base_qty_per_unit, unit_cost)
    SELECT sl.sale_line_id, b.component_product_id, l.factor * b.quantity, c.avg_cost
    FROM _rl l
    JOIN sales_lines sl ON sl.sale_id = v_id AND sl.line_no = l.line_no
    JOIN bundle_items b ON b.bundle_product_id = l.product_id
    JOIN products c ON c.product_id = b.component_product_id
    WHERE l.product_type = 2;

    INSERT INTO sales_payments (sale_id, method, currency, amount, amount_lbp)
    VALUES (v_id, p_refund_method, upper(p_refund_currency), v_amount, v_refund);

    -- 5. Stock IN + ledger (movement 3 = return)
    UPDATE products p SET stock_qty = p.stock_qty + b.qty, updated_at = now()
    FROM _rback b WHERE p.product_id = b.product_id;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id)
    SELECT b.product_id, 3, b.qty, p.avg_cost, p.stock_qty, 2, v_id, p_user_id
    FROM _rback b JOIN products p ON p.product_id = b.product_id;

    RETURN QUERY SELECT v_id, v_no, v_refund, v_amount;
END $$;

-- Adds a row to audit_log for the logged-in user (fn_app_user_id, Database/user_tracking.sql),
-- e.g. a refund approved by a manager.
CREATE OR REPLACE FUNCTION public.fn_audit_add(p_action text, p_table_name text, p_record_id integer, p_details jsonb)
RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO audit_log (user_id, action, table_name, record_id, details)
    VALUES (fn_app_user_id(), p_action, p_table_name, p_record_id, p_details);
$$;

-- ---------------------------------------------------------------------------
-- Packagings that can be sold (active, can_sell, standard items and bundles).
-- An exact barcode match returns just that packaging (scanner); otherwise every
-- word must appear in the item name, or the barcode starts with the text.
-- stock_qty is in base units (for bundles: how many can be made from the components).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_pos_find_item(p_search text, p_limit integer DEFAULT 30)
RETURNS TABLE (
    packaging_id integer, product_id integer, item_name text, packaging_name varchar, barcode varchar,
    sale_price numeric, factor numeric, is_weighted boolean, base_unit varchar, stock_qty numeric)
LANGUAGE plpgsql STABLE
AS $$
DECLARE
    v_search  text := nullif(trim(p_search), '');
    v_pattern text;
    v_words   text[];
BEGIN
    IF v_search IS NULL THEN RETURN; END IF;

    RETURN QUERY
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           pk.name, pk.barcode, pk.sale_price, pk.factor, p.is_weighted, p.base_unit,
           CASE WHEN p.product_type = 2
                THEN (SELECT min(floor(c.stock_qty / b.quantity)) FROM bundle_items b
                      JOIN products c ON c.product_id = b.component_product_id WHERE b.bundle_product_id = p.product_id)
                ELSE p.stock_qty END
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.barcode = v_search
      AND pk.is_active AND pk.can_sell AND p.is_active AND p.product_type IN (1, 2);
    IF FOUND THEN RETURN; END IF;

    v_pattern := replace(replace(replace(v_search, '\', '\\'), '%', '\%'), '_', '\_');
    v_words   := regexp_split_to_array(v_pattern, '\s+');

    RETURN QUERY
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           pk.name, pk.barcode, pk.sale_price, pk.factor, p.is_weighted, p.base_unit,
           CASE WHEN p.product_type = 2
                THEN (SELECT min(floor(c.stock_qty / b.quantity)) FROM bundle_items b
                      JOIN products c ON c.product_id = b.component_product_id WHERE b.bundle_product_id = p.product_id)
                ELSE p.stock_qty END
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    WHERE pk.is_active AND pk.can_sell AND p.is_active AND p.product_type IN (1, 2)
      AND (NOT EXISTS (SELECT 1 FROM unnest(v_words) w WHERE p.name NOT ILIKE '%' || w || '%')
           OR pk.barcode LIKE v_pattern || '%')
    ORDER BY p.name, pk.factor, pk.packaging_id
    LIMIT greatest(coalesce(p_limit, 30), 1);
END $$;

-- ---------------------------------------------------------------------------
-- Held (parked) sales, newest first. A held sale is a sales_invoices row with status 0.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_pos_get_held_sales()
RETURNS TABLE (sale_id integer, held_at timestamptz, user_name varchar, item_count bigint, total numeric)
LANGUAGE sql STABLE
AS $$
    SELECT si.sale_id, si.invoice_date, u.user_name,
           (SELECT count(*) FROM sales_lines l WHERE l.sale_id = si.sale_id),
           si.total
    FROM sales_invoices si
    LEFT JOIN users u ON u.user_id = si.user_id
    WHERE si.status = 0
    ORDER BY si.invoice_date DESC, si.sale_id DESC;
$$;

-- The lines of one held sale, with the current item data (for recalling it to the screen).
CREATE OR REPLACE FUNCTION public.fn_pos_get_held_lines(p_sale_id integer)
RETURNS TABLE (
    packaging_id integer, product_id integer, item_name text, packaging_name varchar, barcode varchar,
    quantity numeric, unit_price numeric, discount numeric, factor numeric, is_weighted boolean,
    base_unit varchar, stock_qty numeric)
LANGUAGE sql STABLE
AS $$
    SELECT l.packaging_id, l.product_id, l.description::text, pk.name, pk.barcode,
           l.quantity, l.unit_price, l.discount, pk.factor, p.is_weighted, p.base_unit, p.stock_qty
    FROM sales_lines l
    JOIN sales_invoices si    ON si.sale_id = l.sale_id AND si.status = 0
    JOIN product_packaging pk ON pk.packaging_id = l.packaging_id
    JOIN products p           ON p.product_id = l.product_id
    WHERE l.sale_id = p_sale_id
    ORDER BY l.line_no;
$$;

-- Discards a held sale. Returns false when it was already finished or deleted.
CREATE OR REPLACE FUNCTION public.fn_pos_delete_held(p_sale_id integer)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM sales_invoices WHERE sale_id = p_sale_id AND status = 0;   -- lines cascade
    RETURN FOUND;
END $$;

-- ---------------------------------------------------------------------------
-- Saves and finishes a sale (it was created in the database before; kept here since the multi-currency
-- change). Payments can be in any active currency of the list: each one is converted with
-- fn_currency_rate (Database/currency_functions.sql). All or nothing.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_save_sale(p_user_id integer, p_lines jsonb, p_payments jsonb, p_shift_id integer DEFAULT NULL::integer, p_customer_id integer DEFAULT NULL::integer, p_invoice_discount numeric DEFAULT 0, p_held_sale_id integer DEFAULT NULL::integer)
 RETURNS TABLE(sale_id integer, invoice_no text, total_lbp numeric, change_lbp numeric)
 LANGUAGE plpgsql
AS $function$
#variable_conflict use_column
DECLARE
    v_rate       numeric := fn_current_rate();
    v_disc       numeric := coalesce(p_invoice_discount, 0);
    v_count      int;
    v_sub        numeric;
    v_cost       numeric;
    v_total      numeric;
    v_paid       numeric;
    v_cash       numeric;
    v_on_account numeric;
    v_change     numeric;
    v_msg        text;
    v_sale_id    int;
    v_no         text;
BEGIN
    IF v_rate IS NULL THEN RAISE EXCEPTION 'Set the exchange rate first.'; END IF;
    IF p_lines IS NULL OR jsonb_typeof(p_lines) <> 'array' OR jsonb_array_length(p_lines) = 0 THEN
        RAISE EXCEPTION 'The invoice has no items.';
    END IF;
    p_payments := coalesce(p_payments, '[]'::jsonb);

    -- The shift must be open. FOR SHARE: closing the shift waits until this sale is saved.
    IF p_shift_id IS NOT NULL THEN
        PERFORM 1 FROM shifts WHERE shift_id = p_shift_id AND status = 1 FOR SHARE;
        IF NOT FOUND THEN RAISE EXCEPTION 'This shift is closed. Open a new shift first.'; END IF;
    END IF;

    -- 1. Lines with item data
    IF to_regclass('pg_temp._l') IS NOT NULL THEN DROP TABLE pg_temp._l; END IF;
    CREATE TEMP TABLE _l ON COMMIT DROP AS
    SELECT x.ord::int                                             AS line_no,
           pk.packaging_id, p.product_id, p.product_type, p.is_weighted,
           fn_item_description(p.name, p.size, p.color, pk.name)  AS description,
           (x.j->>'quantity')::numeric(18,3)                      AS quantity,
           pk.factor,
           coalesce((x.j->>'unit_price')::numeric, pk.sale_price)::numeric(18,2) AS unit_price,
           coalesce((x.j->>'discount')::numeric, 0)::numeric(18,2)              AS discount,
           0::numeric(18,4)                                       AS unit_cost,
           0::numeric(18,2)                                       AS gross,         -- qty x price - line discount
           0::numeric(18,2)                                       AS disc_share     -- share of the invoice discount
    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS x(j, ord)
    JOIN product_packaging pk ON pk.packaging_id = (x.j->>'packaging_id')::int AND pk.is_active AND pk.can_sell
    JOIN products p ON p.product_id = pk.product_id AND p.is_active AND p.product_type IN (1, 2);

    SELECT count(*) INTO v_count FROM _l;
    IF v_count <> jsonb_array_length(p_lines) THEN
        RAISE EXCEPTION 'An item on the invoice cannot be sold (not found or not active).';
    END IF;
    IF EXISTS (SELECT 1 FROM _l WHERE quantity IS NULL OR quantity <= 0 OR unit_price < 0 OR discount < 0
                                   OR discount > quantity * unit_price) THEN
        RAISE EXCEPTION 'Check quantities, prices and discounts on the invoice.';
    END IF;
    SELECT description INTO v_msg FROM _l WHERE NOT is_weighted AND quantity <> trunc(quantity) LIMIT 1;
    IF v_msg IS NOT NULL THEN RAISE EXCEPTION '"%" is sold in whole units only.', v_msg; END IF;
    SELECT description INTO v_msg FROM _l l
    WHERE product_type = 2 AND NOT EXISTS (SELECT 1 FROM bundle_items b WHERE b.bundle_product_id = l.product_id) LIMIT 1;
    IF v_msg IS NOT NULL THEN RAISE EXCEPTION 'Bundle "%" has no items inside it.', v_msg; END IF;

    -- 2. Stock needed per real item (bundles -> their components)
    IF to_regclass('pg_temp._need') IS NOT NULL THEN DROP TABLE pg_temp._need; END IF;
    CREATE TEMP TABLE _need ON COMMIT DROP AS
    SELECT product_id, sum(qty)::numeric(18,3) AS qty FROM (
        SELECT product_id, quantity * factor AS qty FROM _l WHERE product_type = 1
        UNION ALL
        SELECT b.component_product_id, l.quantity * l.factor * b.quantity
        FROM _l l JOIN bundle_items b ON b.bundle_product_id = l.product_id
        WHERE l.product_type = 2
    ) s GROUP BY product_id;

    -- 3. Lock those items (in id order -> no deadlocks between registers) and check stock
    PERFORM 1 FROM products WHERE product_id IN (SELECT product_id FROM _need) ORDER BY product_id FOR UPDATE;

    IF fn_setting('allow_negative_stock', '1') <> '1' THEN
        SELECT fn_item_description(p.name, p.size, p.color, NULL) || ': only ' || trim_scale(p.stock_qty) || ' ' || p.base_unit || ' in stock'
        INTO v_msg
        FROM _need n JOIN products p ON p.product_id = n.product_id
        WHERE p.stock_qty < n.qty LIMIT 1;
        IF v_msg IS NOT NULL THEN RAISE EXCEPTION '%', v_msg; END IF;
    END IF;

    -- 4. Cost of each line, frozen now (average cost x factor)
    UPDATE _l l SET unit_cost = p.avg_cost * l.factor
    FROM products p WHERE p.product_id = l.product_id AND l.product_type = 1;

    UPDATE _l l SET unit_cost = l.factor * coalesce((
        SELECT sum(c.avg_cost * b.quantity) FROM bundle_items b
        JOIN products c ON c.product_id = b.component_product_id
        WHERE b.bundle_product_id = l.product_id), 0)
    WHERE l.product_type = 2;

    -- 5. Totals. The invoice discount is shared over the lines so that per-item
    --    sales and profit reports add up exactly to the invoice.
    UPDATE _l SET gross = round(quantity * unit_price - discount, 2);
    SELECT sum(gross), sum(round(quantity * unit_cost, 2)) INTO v_sub, v_cost FROM _l;
    v_total := v_sub - v_disc;
    IF v_disc < 0 OR v_total < 0 THEN RAISE EXCEPTION 'The invoice discount is not valid.'; END IF;

    IF v_disc > 0 THEN
        UPDATE _l SET disc_share = round(v_disc * gross / v_sub, 2);
        UPDATE _l SET disc_share = disc_share + (v_disc - (SELECT sum(disc_share) FROM _l))
        WHERE line_no = (SELECT line_no FROM _l ORDER BY gross DESC, line_no LIMIT 1);
    END IF;

    -- 6. Payments
    IF jsonb_typeof(p_payments) <> 'array' OR EXISTS (
        SELECT 1 FROM jsonb_array_elements(p_payments) AS y(v)
        WHERE (v->>'method')::int NOT IN (1, 2, 3)
           OR NOT EXISTS (SELECT 1 FROM currencies c WHERE c.code = v->>'currency' AND c.is_active)
           OR fn_currency_rate(v->>'currency') IS NULL
           OR coalesce((v->>'amount')::numeric, 0) <= 0) THEN
        RAISE EXCEPTION 'A payment line is not valid (unknown currency, or no exchange rate for it).';
    END IF;

    SELECT coalesce(sum(lbp), 0),
           coalesce(sum(lbp) FILTER (WHERE method = 1), 0),
           coalesce(sum(lbp) FILTER (WHERE method = 3), 0)
    INTO v_paid, v_cash, v_on_account
    FROM (SELECT (v->>'method')::int AS method,
                 round((v->>'amount')::numeric * fn_currency_rate(v->>'currency'), 2) AS lbp
          FROM jsonb_array_elements(p_payments) AS y(v)) pay;

    IF v_paid < v_total THEN
        RAISE EXCEPTION 'Paid % is less than the total %.',
            to_char(v_paid, 'FM999,999,999,990'), to_char(v_total, 'FM999,999,999,990');
    END IF;
    v_change := v_paid - v_total;
    -- Change comes out of the cash drawer only: card + on account may not be more than the total
    IF v_change > v_cash THEN
        RAISE EXCEPTION 'Card and on-account amounts are more than the total. Only cash can give change.';
    END IF;
    IF v_on_account > 0 THEN
        IF p_customer_id IS NULL THEN RAISE EXCEPTION 'Choose a customer for an on-account sale.'; END IF;
        PERFORM 1 FROM customers WHERE customer_id = p_customer_id AND is_active FOR UPDATE;   -- one register at a time
        IF NOT FOUND THEN RAISE EXCEPTION 'Customer not found.'; END IF;
        SELECT name || ': credit limit reached' INTO v_msg FROM customers
        WHERE customer_id = p_customer_id AND credit_limit_lbp IS NOT NULL
          AND balance_lbp + v_on_account > credit_limit_lbp;
        IF v_msg IS NOT NULL THEN RAISE EXCEPTION '%', v_msg; END IF;
    END IF;

    -- 7. Save (a recalled held sale is replaced by the real one, only once)
    IF p_held_sale_id IS NOT NULL THEN
        DELETE FROM sales_invoices WHERE sale_id = p_held_sale_id AND status = 0;
        IF NOT FOUND THEN RAISE EXCEPTION 'This held sale was already finished on another register.'; END IF;
    END IF;

    v_no := fn_next_doc_no('S');

    INSERT INTO sales_invoices (invoice_no, invoice_type, customer_id, shift_id, user_id, exchange_rate,
                                sub_total, discount, total, total_cost, profit, change_lbp, status)
    VALUES (v_no, 1, p_customer_id, p_shift_id, p_user_id, v_rate,
            v_sub, v_disc, v_total, v_cost, v_total - v_cost, v_change, 1)
    RETURNING sale_id INTO v_sale_id;

    INSERT INTO sales_lines (sale_id, line_no, product_id, packaging_id, description, quantity, factor,
                             unit_price, discount, discount_share, unit_cost, line_total, line_profit)
    SELECT v_sale_id, line_no, product_id, packaging_id, description, quantity, factor,
           unit_price, discount, disc_share, unit_cost,
           gross - disc_share,
           gross - disc_share - round(quantity * unit_cost, 2)
    FROM _l ORDER BY line_no;

    -- Remember what was inside each bundle, so a return puts back exactly that
    INSERT INTO sales_line_components (sale_line_id, product_id, base_qty_per_unit, unit_cost)
    SELECT sl.sale_line_id, b.component_product_id, l.factor * b.quantity, c.avg_cost
    FROM _l l
    JOIN sales_lines sl ON sl.sale_id = v_sale_id AND sl.line_no = l.line_no
    JOIN bundle_items b ON b.bundle_product_id = l.product_id
    JOIN products c ON c.product_id = b.component_product_id
    WHERE l.product_type = 2;

    INSERT INTO sales_payments (sale_id, method, currency, amount, amount_lbp)
    SELECT v_sale_id, (v->>'method')::smallint, v->>'currency', (v->>'amount')::numeric,
           round((v->>'amount')::numeric * fn_currency_rate(v->>'currency'), 2)
    FROM jsonb_array_elements(p_payments) AS y(v);

    -- 8. Stock OUT + ledger
    UPDATE products p SET stock_qty = p.stock_qty - n.qty, updated_at = now()
    FROM _need n WHERE p.product_id = n.product_id;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id)
    SELECT n.product_id, 2, -n.qty, p.avg_cost, p.stock_qty, 2, v_sale_id, p_user_id
    FROM _need n JOIN products p ON p.product_id = n.product_id;

    IF v_on_account > 0 THEN
        UPDATE customers SET balance_lbp = balance_lbp + v_on_account WHERE customer_id = p_customer_id;
    END IF;

    RETURN QUERY SELECT v_sale_id, v_no, v_total, v_change;
END $function$

;
