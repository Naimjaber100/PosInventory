-- Unpaid (on account) sales and the payments that settle them later.
--   sales_invoices.due_lbp        = what is still unpaid of an invoice (0 = paid). Set by fn_save_sale
--                                   (Database/pos_functions.sql) to the on-account amount of the sale.
--   customer_payments             = each time a client pays (the money counts on that day).
--   customer_payment_allocations  = which invoices a payment settled, oldest invoice first.
-- Payment state of an invoice: due_lbp = on-account amount -> Unpaid, 0 -> Paid, between -> Partly paid.
-- Safe to re-run.
-- Apply (after pos_functions.sql):  psql -h localhost -U postgres -d Pos -f customer_payment_functions.sql

ALTER TABLE public.sales_invoices ADD COLUMN IF NOT EXISTS due_lbp numeric(18,2) NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS public.customer_payment_allocations (
    payment_id integer NOT NULL REFERENCES public.customer_payments(payment_id) ON DELETE CASCADE,
    sale_id    integer NOT NULL REFERENCES public.sales_invoices(sale_id),
    amount_lbp numeric(18,2) NOT NULL CHECK (amount_lbp > 0),
    PRIMARY KEY (payment_id, sale_id)
);
CREATE INDEX IF NOT EXISTS ix_customer_payment_allocations_sale ON public.customer_payment_allocations (sale_id);
CREATE INDEX IF NOT EXISTS ix_sales_due ON public.sales_invoices (customer_id, invoice_date) WHERE due_lbp > 0;

-- On-account sales saved before this script: all of the on-account amount is still due
-- (an invoice that already has a payment on it is left alone, so re-running changes nothing).
UPDATE public.sales_invoices si
SET due_lbp = a.on_account
FROM (SELECT p.sale_id, sum(p.amount_lbp) AS on_account FROM public.sales_payments p WHERE p.method = 3 GROUP BY p.sale_id) a
WHERE a.sale_id = si.sale_id AND si.status = 1 AND si.invoice_type = 1 AND si.due_lbp = 0
  AND NOT EXISTS (SELECT 1 FROM public.customer_payment_allocations x WHERE x.sale_id = si.sale_id);

DROP FUNCTION IF EXISTS public.fn_receive_customer_payment(integer, numeric, text, integer, integer, integer, text);
DROP FUNCTION IF EXISTS public.fn_customer_open_invoices(integer);
DROP FUNCTION IF EXISTS public.fn_customer_get_payments(integer);

-- ---------------------------------------------------------------------------
-- The unpaid and partly paid invoices of a client, oldest first (the order payments settle them).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_customer_open_invoices(p_customer_id integer)
RETURNS TABLE (sale_id integer, invoice_no varchar, invoice_date timestamptz, total numeric,
               on_account_lbp numeric, due_lbp numeric)
LANGUAGE sql STABLE
AS $$
    SELECT si.sale_id, si.invoice_no, si.invoice_date, si.total,
           coalesce((SELECT sum(p.amount_lbp) FROM sales_payments p WHERE p.sale_id = si.sale_id AND p.method = 3), 0),
           si.due_lbp
    FROM sales_invoices si
    WHERE si.customer_id = p_customer_id AND si.status = 1 AND si.invoice_type = 1 AND si.due_lbp > 0
    ORDER BY si.invoice_date, si.sale_id;
$$;

-- ---------------------------------------------------------------------------
-- A client pays (part of) what they owe. The money settles their invoices oldest first: an invoice is
-- paid in full before the next one gets anything; the last one touched can stay partly paid.
-- p_method: 1 cash, 2 card. p_currency: any active currency with a rate.
-- More than the client owes is refused (a rounding difference smaller than the smallest unit of the
-- currency paid in is accepted and settles everything). All or nothing.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_receive_customer_payment(
    p_customer_id integer, p_amount numeric, p_currency text, p_method integer, p_user_id integer,
    p_shift_id integer DEFAULT NULL, p_notes text DEFAULT NULL)
RETURNS TABLE (payment_id integer, paid_lbp numeric, balance_lbp numeric, invoices_paid integer, invoices_partly integer)
LANGUAGE plpgsql
AS $$
#variable_conflict use_column
DECLARE
    v_code     text := upper(trim(p_currency));
    v_usd_rate numeric := fn_current_rate();
    v_rate     numeric;
    v_decimals int;
    v_owed     numeric;
    v_lbp      numeric;
    v_left     numeric;
    v_take     numeric;
    v_id       int;
    v_paid     int := 0;
    v_partly   int := 0;
    r          record;
BEGIN
    SELECT c.decimals INTO v_decimals FROM currencies c WHERE c.code = v_code AND c.is_active;
    v_rate := fn_currency_rate(v_code);
    IF v_decimals IS NULL OR v_rate IS NULL THEN RAISE EXCEPTION 'Unknown currency, or no exchange rate for it.'; END IF;
    IF p_method NOT IN (1, 2) THEN RAISE EXCEPTION 'The payment is cash or card.'; END IF;
    IF p_amount IS NULL OR p_amount <= 0 THEN RAISE EXCEPTION 'The amount must be more than 0.'; END IF;
    IF p_shift_id IS NOT NULL THEN
        PERFORM 1 FROM shifts WHERE shift_id = p_shift_id AND status = 1 FOR SHARE;
        IF NOT FOUND THEN RAISE EXCEPTION 'This shift is closed. Open a new shift first.'; END IF;
    END IF;

    -- One register at a time per client (fn_save_sale locks the same row for an unpaid sale)
    SELECT c.balance_lbp INTO v_owed FROM customers c WHERE c.customer_id = p_customer_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Customer not found.'; END IF;
    IF v_owed <= 0 THEN RAISE EXCEPTION 'This customer owes nothing.'; END IF;

    v_lbp := round(p_amount * v_rate, 2);
    IF v_lbp > v_owed THEN
        IF v_lbp - v_owed >= v_rate * power(10, -v_decimals) THEN
            RAISE EXCEPTION 'The customer owes only %.', to_char(v_owed, 'FM999,999,999,990.##');
        END IF;
        v_lbp := v_owed;   -- e.g. 4.53 USD for 405,000 LBP: the few pounds over are rounding
    END IF;

    INSERT INTO customer_payments (customer_id, shift_id, method, currency, amount, exchange_rate, amount_lbp, user_id, notes)
    VALUES (p_customer_id, p_shift_id, p_method, v_code, p_amount, coalesce(v_usd_rate, 1), v_lbp, p_user_id, nullif(trim(p_notes), ''))
    RETURNING payment_id INTO v_id;

    -- Oldest invoice first
    v_left := v_lbp;
    FOR r IN
        SELECT si.sale_id, si.due_lbp FROM sales_invoices si
        WHERE si.customer_id = p_customer_id AND si.status = 1 AND si.invoice_type = 1 AND si.due_lbp > 0
        ORDER BY si.invoice_date, si.sale_id
        FOR UPDATE
    LOOP
        EXIT WHEN v_left <= 0;
        v_take := least(r.due_lbp, v_left);
        UPDATE sales_invoices SET due_lbp = due_lbp - v_take WHERE sale_id = r.sale_id;
        INSERT INTO customer_payment_allocations (payment_id, sale_id, amount_lbp) VALUES (v_id, r.sale_id, v_take);
        v_left := v_left - v_take;
        IF v_take = r.due_lbp THEN v_paid := v_paid + 1; ELSE v_partly := v_partly + 1; END IF;
    END LOOP;

    UPDATE customers SET balance_lbp = balance_lbp - v_lbp WHERE customer_id = p_customer_id;

    RETURN QUERY SELECT v_id, v_lbp, v_owed - v_lbp, v_paid, v_partly;
END $$;

-- ---------------------------------------------------------------------------
-- The payments of a client, newest first. invoices: what each one settled, e.g. "S-2026-000051, S-2026-000052 (part)".
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_customer_get_payments(p_customer_id integer)
RETURNS TABLE (payment_id integer, payment_date timestamptz, method smallint, currency text, amount numeric,
               amount_lbp numeric, cashier varchar, invoices text)
LANGUAGE sql STABLE
AS $$
    SELECT cp.payment_id, cp.payment_date, cp.method, trim(cp.currency), cp.amount, cp.amount_lbp, u.full_name,
           (SELECT string_agg(si.invoice_no || CASE WHEN si.due_lbp > 0 THEN ' (part)' ELSE '' END, ', '
                              ORDER BY si.invoice_date, si.sale_id)
            FROM customer_payment_allocations a JOIN sales_invoices si ON si.sale_id = a.sale_id
            WHERE a.payment_id = cp.payment_id)
    FROM customer_payments cp
    LEFT JOIN users u ON u.user_id = cp.user_id
    WHERE cp.customer_id = p_customer_id
    ORDER BY cp.payment_date DESC, cp.payment_id DESC;
$$;
