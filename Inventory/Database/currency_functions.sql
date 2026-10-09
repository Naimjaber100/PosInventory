-- Currencies and exchange rates (Settings > Currencies & exchange rates).
--
-- One PRIMARY currency: every price, total and balance is stored in it (the columns named *_lbp hold
-- primary-currency amounts). It can only change while there are no sales, purchases or payments yet.
-- Other active currencies can be taken / given back at the cashier at their latest rate:
--   rate = how much of the primary currency 1 unit of that currency is worth (1 USD = 89,500 LBP).
-- Each new rate is a row in exchange_rates (history: when, who). fn_currency_rate(code) gives the latest.
-- The "second" currency (first active non-primary by sort order, USD today) is the one shown next to
-- totals on the cashier screen; fn_current_rate() is its rate (kept for the older functions).
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f currency_functions.sql   (then pos_functions.sql)

CREATE TABLE IF NOT EXISTS public.currencies (
    code       char(3) PRIMARY KEY CONSTRAINT currencies_code_check CHECK (code ~ '^[A-Z]{3}$'),
    name       varchar(50) NOT NULL,
    symbol     varchar(8) NOT NULL,
    decimals   smallint NOT NULL DEFAULT 2 CONSTRAINT currencies_decimals_check CHECK (decimals BETWEEN 0 AND 4),
    is_primary boolean NOT NULL DEFAULT false,
    is_active  boolean NOT NULL DEFAULT true,
    sort_order integer NOT NULL DEFAULT 0,
    CONSTRAINT currencies_primary_active CHECK (NOT is_primary OR is_active)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_currencies_one_primary ON public.currencies (is_primary) WHERE is_primary;

-- The two currencies the system used until now
INSERT INTO public.currencies (code, name, symbol, decimals, is_primary, sort_order) VALUES
    ('LBP', 'Lebanese pound', 'LBP', 0, true, 0),
    ('USD', 'US dollar', '$', 2, false, 1)
ON CONFLICT (code) DO NOTHING;

-- exchange_rates: one row per new rate of a currency (it only held the USD rate before)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'exchange_rates' AND column_name = 'lbp_per_usd') THEN
        ALTER TABLE public.exchange_rates RENAME COLUMN lbp_per_usd TO rate;
    END IF;
END $$;
ALTER TABLE public.exchange_rates ADD COLUMN IF NOT EXISTS currency_code char(3) REFERENCES currencies(code);
UPDATE public.exchange_rates SET currency_code = 'USD' WHERE currency_code IS NULL;
ALTER TABLE public.exchange_rates ALTER COLUMN currency_code SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_exchange_rates_currency ON public.exchange_rates (currency_code, effective_at DESC, rate_id DESC);

-- Payments may be in any currency of the list (was: LBP or USD only)
ALTER TABLE public.sales_payments    DROP CONSTRAINT IF EXISTS sales_payments_currency_check;
ALTER TABLE public.supplier_payments DROP CONSTRAINT IF EXISTS supplier_payments_currency_check;
ALTER TABLE public.customer_payments DROP CONSTRAINT IF EXISTS customer_payments_currency_check;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'sales_payments_currency_fkey') THEN
        ALTER TABLE public.sales_payments ADD CONSTRAINT sales_payments_currency_fkey FOREIGN KEY (currency) REFERENCES currencies(code);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'supplier_payments_currency_fkey') THEN
        ALTER TABLE public.supplier_payments ADD CONSTRAINT supplier_payments_currency_fkey FOREIGN KEY (currency) REFERENCES currencies(code);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'customer_payments_currency_fkey') THEN
        ALTER TABLE public.customer_payments ADD CONSTRAINT customer_payments_currency_fkey FOREIGN KEY (currency) REFERENCES currencies(code);
    END IF;
END $$;

DROP FUNCTION IF EXISTS public.fn_currency_get_all();
DROP FUNCTION IF EXISTS public.fn_currency_save(text, text, text, smallint, boolean, integer);
DROP FUNCTION IF EXISTS public.fn_currency_set_primary(text);
DROP FUNCTION IF EXISTS public.fn_currency_set_rate(text, numeric, integer);
DROP FUNCTION IF EXISTS public.fn_currency_rates(text, integer);

-- ---------------------------------------------------------------------------
-- Rates
-- ---------------------------------------------------------------------------

-- Latest rate of a currency in the primary currency: 1 for the primary, NULL when no rate is set
CREATE OR REPLACE FUNCTION public.fn_currency_rate(p_code text)
RETURNS numeric
LANGUAGE sql STABLE
AS $$
    SELECT CASE WHEN c.is_primary THEN 1::numeric
                ELSE (SELECT r.rate FROM exchange_rates r WHERE r.currency_code = c.code
                      ORDER BY r.effective_at DESC, r.rate_id DESC LIMIT 1) END
    FROM currencies c WHERE c.code = upper(p_code);
$$;

-- Rate of the second currency (shown next to totals; stored on invoices as exchange_rate).
-- 1 when the system has no other active currency; NULL when it has one without a rate.
CREATE OR REPLACE FUNCTION public.fn_current_rate()
RETURNS numeric
LANGUAGE sql STABLE
AS $$
    SELECT coalesce((SELECT fn_currency_rate(c.code) FROM currencies c
                     WHERE c.is_active AND NOT c.is_primary ORDER BY c.sort_order, c.code LIMIT 1),
                    CASE WHEN EXISTS (SELECT 1 FROM currencies c WHERE c.is_active AND NOT c.is_primary) THEN NULL ELSE 1 END);
$$;

-- Sets a new rate for a currency (kept as history)
CREATE OR REPLACE FUNCTION public.fn_currency_set_rate(p_code text, p_rate numeric, p_user_id integer)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM currencies WHERE code = upper(p_code)) THEN RAISE EXCEPTION 'Currency % does not exist.', p_code; END IF;
    IF EXISTS (SELECT 1 FROM currencies WHERE code = upper(p_code) AND is_primary) THEN
        RAISE EXCEPTION 'The primary currency has no rate (it is always 1).';
    END IF;
    IF p_rate IS NULL OR p_rate <= 0 THEN RAISE EXCEPTION 'The rate must be more than 0.'; END IF;
    INSERT INTO exchange_rates (currency_code, rate, user_id) VALUES (upper(p_code), round(p_rate, 2), p_user_id);
END $$;

-- The rate history of a currency, newest first
CREATE OR REPLACE FUNCTION public.fn_currency_rates(p_code text, p_limit integer DEFAULT 100)
RETURNS TABLE (effective_at timestamptz, rate numeric, set_by varchar)
LANGUAGE sql STABLE
AS $$
    SELECT r.effective_at, r.rate, u.full_name
    FROM exchange_rates r LEFT JOIN users u ON u.user_id = r.user_id
    WHERE r.currency_code = upper(p_code)
    ORDER BY r.effective_at DESC, r.rate_id DESC
    LIMIT greatest(coalesce(p_limit, 100), 1);
$$;

-- ---------------------------------------------------------------------------
-- Currencies
-- ---------------------------------------------------------------------------

-- All currencies: primary first, then by sort order; with the latest rate
CREATE OR REPLACE FUNCTION public.fn_currency_get_all()
RETURNS TABLE (code char(3), name varchar, symbol varchar, decimals smallint, is_primary boolean, is_active boolean,
               sort_order integer, rate numeric, rate_at timestamptz, rate_by varchar)
LANGUAGE sql STABLE
AS $$
    SELECT c.code, c.name, c.symbol, c.decimals, c.is_primary, c.is_active, c.sort_order,
           CASE WHEN c.is_primary THEN 1 ELSE r.rate END, r.effective_at, u.full_name
    FROM currencies c
    LEFT JOIN LATERAL (SELECT x.rate, x.effective_at, x.user_id FROM exchange_rates x
                       WHERE x.currency_code = c.code ORDER BY x.effective_at DESC, x.rate_id DESC LIMIT 1) r ON NOT c.is_primary
    LEFT JOIN users u ON u.user_id = r.user_id
    ORDER BY NOT c.is_primary, c.sort_order, c.code;
$$;

-- Adds or changes a currency (the code identifies it). The primary currency cannot be switched off.
CREATE OR REPLACE FUNCTION public.fn_currency_save(
    p_code text, p_name text, p_symbol text, p_decimals smallint, p_is_active boolean, p_sort_order integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_code text := upper(trim(p_code));
BEGIN
    IF v_code !~ '^[A-Z]{3}$' THEN RAISE EXCEPTION 'The code must be 3 letters, e.g. EUR.'; END IF;
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF nullif(trim(p_symbol), '') IS NULL THEN RAISE EXCEPTION 'The symbol is required.'; END IF;
    IF NOT coalesce(p_is_active, true) AND EXISTS (SELECT 1 FROM currencies WHERE code = v_code AND is_primary) THEN
        RAISE EXCEPTION 'The primary currency cannot be switched off.';
    END IF;

    INSERT INTO currencies (code, name, symbol, decimals, is_active, sort_order)
    VALUES (v_code, trim(p_name), trim(p_symbol), coalesce(p_decimals, 2), coalesce(p_is_active, true),
            coalesce(p_sort_order, (SELECT coalesce(max(sort_order), 0) + 1 FROM currencies)))
    ON CONFLICT (code) DO UPDATE
    SET name = EXCLUDED.name, symbol = EXCLUDED.symbol, decimals = EXCLUDED.decimals,
        is_active = EXCLUDED.is_active, sort_order = EXCLUDED.sort_order;
END $$;

-- Makes a currency the primary one: only while nothing is stored in amounts yet.
-- The old rates were against the old primary, so they are removed: set the rates again after.
CREATE OR REPLACE FUNCTION public.fn_currency_set_primary(p_code text)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_code text := upper(trim(p_code));
BEGIN
    IF NOT EXISTS (SELECT 1 FROM currencies WHERE code = v_code) THEN RAISE EXCEPTION 'Currency % does not exist.', p_code; END IF;
    IF EXISTS (SELECT 1 FROM currencies WHERE code = v_code AND is_primary) THEN RETURN; END IF;
    IF EXISTS (SELECT 1 FROM sales_invoices) OR EXISTS (SELECT 1 FROM purchase_invoices)
       OR EXISTS (SELECT 1 FROM supplier_payments) OR EXISTS (SELECT 1 FROM customer_payments) THEN
        RAISE EXCEPTION 'The primary currency cannot change: there are already sales or purchases, and all their amounts are in the current primary currency.';
    END IF;

    UPDATE currencies SET is_primary = false WHERE is_primary;
    UPDATE currencies SET is_primary = true, is_active = true, sort_order = 0 WHERE code = v_code;
    DELETE FROM exchange_rates;
END $$;
