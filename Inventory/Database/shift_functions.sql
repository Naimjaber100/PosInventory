-- Shifts: one open shift per user.
--
-- A user's shift opens by itself when they open the cashier screen (or make their first sale, refund or
-- client payment): fn_shift_current. Every sale, refund and client payment is recorded in it (shift_id).
-- It stays open - also through log out / log in - until a manager ends it (End shift / End of day), typing
-- the cash counted in the drawer: fn_close_shift. The next sale of that user opens a new shift.
-- Expected cash = opening cash + cash taken - change given - cash refunds + client cash payments.
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f shift_functions.sql

-- One open shift per user (it was one per register)
DROP INDEX IF EXISTS public.ux_shifts_one_open_per_register;
CREATE UNIQUE INDEX IF NOT EXISTS ux_shifts_one_open_per_user ON public.shifts (user_id) WHERE status = 1;
CREATE INDEX IF NOT EXISTS ix_sales_invoices_shift ON public.sales_invoices (shift_id);
CREATE INDEX IF NOT EXISTS ix_customer_payments_shift ON public.customer_payments (shift_id);
CREATE INDEX IF NOT EXISTS ix_shifts_opened_at ON public.shifts (opened_at);

-- ---------------------------------------------------------------------------
-- The user's open shift; opens one when they have none. Returns its shift_id.
-- (shifts.register_id is required: the first active register is used.)
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_shift_current(p_user_id integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id  int;
    v_reg int;
BEGIN
    SELECT shift_id INTO v_id FROM shifts WHERE user_id = p_user_id AND status = 1;
    IF FOUND THEN RETURN v_id; END IF;

    SELECT register_id INTO v_reg FROM registers WHERE is_active ORDER BY register_id LIMIT 1;
    IF v_reg IS NULL THEN
        SELECT register_id INTO v_reg FROM registers ORDER BY register_id LIMIT 1;
        IF v_reg IS NULL THEN RAISE EXCEPTION 'There is no register in the registers table: add one first.'; END IF;
    END IF;

    -- Two windows of the same user at the same moment: the second one gets the first one's shift
    INSERT INTO shifts (register_id, user_id) VALUES (v_reg, p_user_id)
    ON CONFLICT (user_id) WHERE status = 1 DO NOTHING
    RETURNING shift_id INTO v_id;
    IF v_id IS NULL THEN
        SELECT shift_id INTO v_id FROM shifts WHERE user_id = p_user_id AND status = 1;
    END IF;
    RETURN v_id;
END $$;

-- The old "open a shift on a register" call now follows the per-user rule
CREATE OR REPLACE FUNCTION public.fn_open_shift(p_register_id integer, p_user_id integer, p_opening_lbp numeric DEFAULT 0, p_opening_usd numeric DEFAULT 0)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id int;
BEGIN
    IF EXISTS (SELECT 1 FROM shifts WHERE user_id = p_user_id AND status = 1) THEN
        RAISE EXCEPTION 'This user already has an open shift. End it first.';
    END IF;
    INSERT INTO shifts (register_id, user_id, opening_cash_lbp, opening_cash_usd)
    VALUES (p_register_id, p_user_id, p_opening_lbp, p_opening_usd)
    RETURNING shift_id INTO v_id;
    RETURN v_id;
END $$;

-- ---------------------------------------------------------------------------
-- One shift's figures. Open shift: worked out now; closed shift: the cash expected / counted when it closed.
-- Amounts in LBP (the drawer's LBP and USD cash apart).
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_shift_summary(integer);
CREATE FUNCTION public.fn_shift_summary(p_shift_id integer)
RETURNS TABLE (shift_id integer, user_id integer, user_name varchar, opened_at timestamptz, closed_at timestamptz,
               status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric,
               profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric,
               opening_cash_lbp numeric, opening_cash_usd numeric,
               expected_cash_lbp numeric, expected_cash_usd numeric,
               counted_cash_lbp numeric, counted_cash_usd numeric,
               difference_lbp numeric, difference_usd numeric)
LANGUAGE plpgsql STABLE
AS $$
#variable_conflict use_column
DECLARE
    v_s   shifts%ROWTYPE;
    v_lbp numeric;
    v_usd numeric;
    v_client_lbp numeric;
    v_client_usd numeric;
BEGIN
    SELECT * INTO v_s FROM shifts WHERE shifts.shift_id = p_shift_id;
    IF NOT FOUND THEN RETURN; END IF;

    SELECT coalesce(sum(amount) FILTER (WHERE currency = 'LBP'), 0), coalesce(sum(amount) FILTER (WHERE currency = 'USD'), 0)
    INTO v_client_lbp, v_client_usd
    FROM customer_payments WHERE customer_payments.shift_id = p_shift_id AND method = 1;

    IF v_s.status = 1 THEN
        SELECT v_s.opening_cash_lbp
             + coalesce(sum(CASE WHEN si.invoice_type = 1 AND sp.method = 1 AND sp.currency = 'LBP' THEN sp.amount END), 0)
             - coalesce(sum(CASE WHEN si.invoice_type = 2 AND sp.method = 1 AND sp.currency = 'LBP' THEN sp.amount END), 0),
               v_s.opening_cash_usd
             + coalesce(sum(CASE WHEN si.invoice_type = 1 AND sp.method = 1 AND sp.currency = 'USD' THEN sp.amount END), 0)
             - coalesce(sum(CASE WHEN si.invoice_type = 2 AND sp.method = 1 AND sp.currency = 'USD' THEN sp.amount END), 0)
        INTO v_lbp, v_usd
        FROM sales_invoices si JOIN sales_payments sp ON sp.sale_id = si.sale_id
        WHERE si.shift_id = p_shift_id AND si.status = 1;

        v_lbp := coalesce(v_lbp, v_s.opening_cash_lbp)
               - coalesce((SELECT sum(change_lbp) FROM sales_invoices x WHERE x.shift_id = p_shift_id AND x.status = 1), 0)
               + v_client_lbp;
        v_usd := coalesce(v_usd, v_s.opening_cash_usd) + v_client_usd;
    ELSE
        v_lbp := v_s.expected_cash_lbp;
        v_usd := v_s.expected_cash_usd;
    END IF;

    RETURN QUERY
    SELECT v_s.shift_id, v_s.user_id, u.full_name, v_s.opened_at, v_s.closed_at, v_s.status,
           coalesce(t.invoices, 0), coalesce(t.returns, 0), coalesce(t.sales, 0), coalesce(t.refunds, 0), coalesce(t.profit, 0),
           coalesce((SELECT sum(CASE WHEN x.invoice_type = 2 THEN -p.amount_lbp ELSE p.amount_lbp END)
                     FROM sales_payments p JOIN sales_invoices x ON x.sale_id = p.sale_id
                     WHERE x.shift_id = p_shift_id AND x.status = 1 AND p.method = 2), 0),
           coalesce((SELECT sum(CASE WHEN x.invoice_type = 2 THEN -p.amount_lbp ELSE p.amount_lbp END)
                     FROM sales_payments p JOIN sales_invoices x ON x.sale_id = p.sale_id
                     WHERE x.shift_id = p_shift_id AND x.status = 1 AND p.method = 3), 0),
           v_client_lbp, v_client_usd,
           v_s.opening_cash_lbp, v_s.opening_cash_usd,
           v_lbp, v_usd,
           v_s.counted_cash_lbp, v_s.counted_cash_usd,
           v_s.counted_cash_lbp - v_lbp, v_s.counted_cash_usd - v_usd
    FROM users u
    LEFT JOIN LATERAL (
        SELECT count(*) FILTER (WHERE si.invoice_type = 1)::int AS invoices,
               count(*) FILTER (WHERE si.invoice_type = 2)::int AS returns,
               sum(si.total) FILTER (WHERE si.invoice_type = 1) AS sales,
               sum(si.total) FILTER (WHERE si.invoice_type = 2) AS refunds,
               sum(CASE WHEN si.invoice_type = 2 THEN -si.profit ELSE si.profit END) AS profit
        FROM sales_invoices si
        WHERE si.shift_id = p_shift_id AND si.status = 1
    ) t ON true
    WHERE u.user_id = v_s.user_id;
END $$;

-- ---------------------------------------------------------------------------
-- The shifts list (Settings > Shifts): every open shift, and the shifts opened between two days (inclusive).
-- Open ones first, then newest first.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_shift_list(date, date);
CREATE FUNCTION public.fn_shift_list(p_from date, p_to date)
RETURNS TABLE (shift_id integer, user_id integer, user_name varchar, opened_at timestamptz, closed_at timestamptz,
               status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric,
               profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric,
               opening_cash_lbp numeric, opening_cash_usd numeric,
               expected_cash_lbp numeric, expected_cash_usd numeric,
               counted_cash_lbp numeric, counted_cash_usd numeric,
               difference_lbp numeric, difference_usd numeric)
LANGUAGE sql STABLE
AS $$
    SELECT s.*
    FROM shifts sh
    CROSS JOIN LATERAL fn_shift_summary(sh.shift_id) s
    WHERE sh.status = 1
       OR (sh.opened_at AT TIME ZONE current_setting('TimeZone'))::date BETWEEN p_from AND p_to
    ORDER BY sh.status, sh.opened_at DESC;
$$;

-- ---------------------------------------------------------------------------
-- Ends a shift: the cash counted in the drawer (LBP and USD) is kept with the cash expected.
-- Waits for a sale being saved in it (the sale holds the shift FOR SHARE). Returns the shift's figures.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_close_shift(integer, numeric, numeric);
CREATE FUNCTION public.fn_close_shift(p_shift_id integer, p_counted_lbp numeric, p_counted_usd numeric)
RETURNS TABLE (shift_id integer, user_id integer, user_name varchar, opened_at timestamptz, closed_at timestamptz,
               status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric,
               profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric,
               opening_cash_lbp numeric, opening_cash_usd numeric,
               expected_cash_lbp numeric, expected_cash_usd numeric,
               counted_cash_lbp numeric, counted_cash_usd numeric,
               difference_lbp numeric, difference_usd numeric)
LANGUAGE plpgsql
AS $$
#variable_conflict use_column
DECLARE
    v_status smallint;
    v_lbp    numeric;
    v_usd    numeric;
BEGIN
    SELECT status INTO v_status FROM shifts WHERE shifts.shift_id = p_shift_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Shift % not found.', p_shift_id; END IF;
    IF v_status <> 1 THEN RAISE EXCEPTION 'This shift is already ended.'; END IF;
    IF p_counted_lbp < 0 OR p_counted_usd < 0 THEN RAISE EXCEPTION 'The counted cash cannot be negative.'; END IF;

    SELECT s.expected_cash_lbp, s.expected_cash_usd INTO v_lbp, v_usd FROM fn_shift_summary(p_shift_id) s;

    UPDATE shifts SET closed_at = now(), status = 2,
        expected_cash_lbp = v_lbp, expected_cash_usd = v_usd,
        counted_cash_lbp = p_counted_lbp, counted_cash_usd = p_counted_usd
    WHERE shifts.shift_id = p_shift_id;

    RETURN QUERY SELECT * FROM fn_shift_summary(p_shift_id);
END $$;
