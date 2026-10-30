-- The company (Settings > Company): name, address, email, mobile number, VAT number, logo, TVA settings.
-- Exactly one record: company_id is always 1, it is created here and cannot be deleted.
-- The name is shown on the login screen, the cashier screen and the back office.
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f company_functions.sql
-- (then user_tracking.sql again, to record who changes it)

CREATE TABLE IF NOT EXISTS public.company (
    company_id smallint PRIMARY KEY DEFAULT 1 CONSTRAINT company_single_row CHECK (company_id = 1),
    name       varchar(150) NOT NULL,
    address    varchar(300),
    email      varchar(150),
    mobile     varchar(50),
    vat_number varchar(50)
);

-- The one record, from the old settings (store_name, store_phone) the first time
INSERT INTO public.company (company_id, name, mobile)
VALUES (1,
        coalesce(nullif(trim((SELECT value FROM settings WHERE key = 'store_name')), ''), 'My Store'),
        nullif(trim((SELECT value FROM settings WHERE key = 'store_phone')), ''))
ON CONFLICT (company_id) DO NOTHING;

-- The logo printed at the top of the receipt (Settings > Company): a PNG, already made small and grey by the
-- app (CompanyLogo.cs), so it is a few KB. NULL = no logo. Read on its own (fn_company_get_logo), not with
-- fn_company_get, which every screen calls for the name.
ALTER TABLE public.company ADD COLUMN IF NOT EXISTS logo bytea;

-- TVA (Settings > Company): whether the store charges it, whether the sale prices typed for the items already
-- include it, and the rate a new item starts with (each item has its own rate, products.vat_rate).
ALTER TABLE public.company ADD COLUMN IF NOT EXISTS vat_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE public.company ADD COLUMN IF NOT EXISTS prices_include_vat boolean NOT NULL DEFAULT true;
ALTER TABLE public.company ADD COLUMN IF NOT EXISTS vat_rate numeric(5,2) NOT NULL DEFAULT 11
    CONSTRAINT company_vat_rate_check CHECK (vat_rate BETWEEN 0 AND 100);

-- It cannot be deleted
CREATE OR REPLACE FUNCTION public.trg_company_keep()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'The company record cannot be deleted. Change it instead.';
END $$;

DROP TRIGGER IF EXISTS trg_company_keep ON public.company;
CREATE TRIGGER trg_company_keep BEFORE DELETE OR TRUNCATE ON public.company
    FOR EACH STATEMENT EXECUTE FUNCTION trg_company_keep();

DROP FUNCTION IF EXISTS public.fn_company_get();
DROP FUNCTION IF EXISTS public.fn_company_save(text, text, text, text, text);
DROP FUNCTION IF EXISTS public.fn_company_save(text, text, text, text, text, boolean, boolean, numeric);
DROP FUNCTION IF EXISTS public.fn_company_get_logo();
DROP FUNCTION IF EXISTS public.fn_company_set_logo(bytea);

-- The logo (NULL = none)
CREATE OR REPLACE FUNCTION public.fn_company_get_logo()
RETURNS bytea
LANGUAGE sql STABLE
AS $$
    SELECT c.logo FROM company c WHERE c.company_id = 1;
$$;

-- Sets the logo; NULL removes it. At most 1 MB (the app sends a few KB).
CREATE OR REPLACE FUNCTION public.fn_company_set_logo(p_logo bytea)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    IF length(p_logo) > 1048576 THEN RAISE EXCEPTION 'The logo is too big (1 MB at most).'; END IF;
    UPDATE company SET logo = p_logo WHERE company_id = 1;
    IF NOT FOUND THEN RAISE EXCEPTION 'Save the company first.'; END IF;
END $$;

-- The company record
CREATE OR REPLACE FUNCTION public.fn_company_get()
RETURNS TABLE (name varchar, address varchar, email varchar, mobile varchar, vat_number varchar,
               vat_enabled boolean, prices_include_vat boolean, vat_rate numeric)
LANGUAGE sql STABLE
AS $$
    SELECT c.name, c.address, c.email, c.mobile, c.vat_number, c.vat_enabled, c.prices_include_vat, c.vat_rate
    FROM company c WHERE c.company_id = 1;
$$;

-- Saves the company record (blank = empty)
CREATE OR REPLACE FUNCTION public.fn_company_save(
    p_name text, p_address text, p_email text, p_mobile text, p_vat_number text,
    p_vat_enabled boolean, p_prices_include_vat boolean, p_vat_rate numeric)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_email text := nullif(trim(p_email), '');
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The company name is required.'; END IF;
    IF v_email IS NOT NULL AND v_email !~ '^[^@\s]+@[^@\s]+\.[^@\s]+$' THEN
        RAISE EXCEPTION 'The email address is not valid.';
    END IF;
    IF p_vat_rate IS NULL OR p_vat_rate NOT BETWEEN 0 AND 100 THEN
        RAISE EXCEPTION 'The TVA rate must be between 0 and 100.';
    END IF;

    INSERT INTO company (company_id, name, address, email, mobile, vat_number, vat_enabled, prices_include_vat, vat_rate)
    VALUES (1, trim(p_name), nullif(trim(p_address), ''), v_email, nullif(trim(p_mobile), ''), nullif(trim(p_vat_number), ''),
            coalesce(p_vat_enabled, false), coalesce(p_prices_include_vat, true), p_vat_rate)
    ON CONFLICT (company_id) DO UPDATE
    SET name = EXCLUDED.name, address = EXCLUDED.address, email = EXCLUDED.email,
        mobile = EXCLUDED.mobile, vat_number = EXCLUDED.vat_number, vat_enabled = EXCLUDED.vat_enabled,
        prices_include_vat = EXCLUDED.prices_include_vat, vat_rate = EXCLUDED.vat_rate;
END $$;
