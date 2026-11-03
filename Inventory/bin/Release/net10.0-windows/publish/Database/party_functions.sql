-- Clients (customers table) and suppliers (suppliers table): two separate tables, one page each
-- (menu Clients & Suppliers). balance_lbp is changed by sales / purchases / payments, never typed in.
-- Delete: one that has sales, purchases or payments is deactivated instead (its history stays).
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f party_functions.sql

ALTER TABLE public.customers ADD COLUMN IF NOT EXISTS address varchar(250);
ALTER TABLE public.customers ADD COLUMN IF NOT EXISTS email   varchar(150);
ALTER TABLE public.customers ADD COLUMN IF NOT EXISTS notes   varchar(500);

ALTER TABLE public.suppliers ADD COLUMN IF NOT EXISTS email      varchar(150);
ALTER TABLE public.suppliers ADD COLUMN IF NOT EXISTS vat_number varchar(50);
ALTER TABLE public.suppliers ADD COLUMN IF NOT EXISTS notes      varchar(500);

CREATE INDEX IF NOT EXISTS ix_customers_name ON public.customers USING gin (name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_suppliers_name ON public.suppliers USING gin (name gin_trgm_ops);

DROP FUNCTION IF EXISTS public.fn_customer_get_all(text, boolean);
DROP FUNCTION IF EXISTS public.fn_customer_save(integer, text, text, text, text, numeric, text, boolean);
DROP FUNCTION IF EXISTS public.fn_customer_delete(integer);
DROP FUNCTION IF EXISTS public.fn_supplier_get_all(text, boolean);
DROP FUNCTION IF EXISTS public.fn_supplier_save(integer, text, text, text, text, text, text, boolean);
DROP FUNCTION IF EXISTS public.fn_supplier_delete(integer);

-- ===========================================================================
-- Clients
-- ===========================================================================

-- Clients by name / phone / email (blank = all), active first, by name. p_include_inactive: also the inactive ones.
CREATE OR REPLACE FUNCTION public.fn_customer_get_all(p_search text DEFAULT NULL, p_include_inactive boolean DEFAULT false)
RETURNS TABLE (customer_id integer, name varchar, phone varchar, address varchar, email varchar,
               balance_lbp numeric, credit_limit_lbp numeric, notes varchar, is_active boolean)
LANGUAGE sql STABLE
AS $$
    SELECT c.customer_id, c.name, c.phone, c.address, c.email, c.balance_lbp, c.credit_limit_lbp, c.notes, c.is_active
    FROM customers c
    WHERE (p_include_inactive OR c.is_active)
      AND (nullif(trim(p_search), '') IS NULL
           OR c.name ILIKE '%' || trim(p_search) || '%'
           OR c.phone ILIKE '%' || trim(p_search) || '%'
           OR c.email ILIKE '%' || trim(p_search) || '%')
    ORDER BY NOT c.is_active, lower(c.name), c.customer_id;
$$;

-- Adds (p_customer_id null) or updates a client; returns its id. p_credit_limit null = no limit.
CREATE OR REPLACE FUNCTION public.fn_customer_save(
    p_customer_id integer, p_name text, p_phone text, p_address text, p_email text,
    p_credit_limit numeric, p_notes text, p_is_active boolean DEFAULT true)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id integer;
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF p_credit_limit < 0 THEN RAISE EXCEPTION 'The credit limit cannot be negative.'; END IF;
    IF nullif(trim(p_email), '') IS NOT NULL AND trim(p_email) !~ '^[^@\s]+@[^@\s]+\.[^@\s]+$' THEN
        RAISE EXCEPTION 'The email address is not valid.';
    END IF;

    IF p_customer_id IS NULL THEN
        INSERT INTO customers (name, phone, address, email, credit_limit_lbp, notes, is_active)
        VALUES (trim(p_name), nullif(trim(p_phone), ''), nullif(trim(p_address), ''), nullif(trim(p_email), ''),
                p_credit_limit, nullif(trim(p_notes), ''), coalesce(p_is_active, true))
        RETURNING customer_id INTO v_id;
    ELSE
        UPDATE customers
        SET name = trim(p_name), phone = nullif(trim(p_phone), ''), address = nullif(trim(p_address), ''),
            email = nullif(trim(p_email), ''), credit_limit_lbp = p_credit_limit, notes = nullif(trim(p_notes), ''),
            is_active = coalesce(p_is_active, is_active)
        WHERE customer_id = p_customer_id
        RETURNING customer_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'Client % does not exist.', p_customer_id; END IF;
    END IF;
    RETURN v_id;
END $$;

-- Deletes a client; one with sales or payments is deactivated instead. Returns 'deleted' or 'deactivated'.
CREATE OR REPLACE FUNCTION public.fn_customer_delete(p_customer_id integer)
RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    BEGIN
        DELETE FROM customers WHERE customer_id = p_customer_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'Client % does not exist.', p_customer_id; END IF;
        RETURN 'deleted';
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE customers SET is_active = false WHERE customer_id = p_customer_id;
        RETURN 'deactivated';
    END;
END $$;

-- ===========================================================================
-- Suppliers
-- ===========================================================================

CREATE OR REPLACE FUNCTION public.fn_supplier_get_all(p_search text DEFAULT NULL, p_include_inactive boolean DEFAULT false)
RETURNS TABLE (supplier_id integer, name varchar, phone varchar, address varchar, email varchar,
               balance_lbp numeric, vat_number varchar, notes varchar, is_active boolean)
LANGUAGE sql STABLE
AS $$
    SELECT s.supplier_id, s.name, s.phone, s.address, s.email, s.balance_lbp, s.vat_number, s.notes, s.is_active
    FROM suppliers s
    WHERE (p_include_inactive OR s.is_active)
      AND (nullif(trim(p_search), '') IS NULL
           OR s.name ILIKE '%' || trim(p_search) || '%'
           OR s.phone ILIKE '%' || trim(p_search) || '%'
           OR s.email ILIKE '%' || trim(p_search) || '%'
           OR s.vat_number ILIKE '%' || trim(p_search) || '%')
    ORDER BY NOT s.is_active, lower(s.name), s.supplier_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_supplier_save(
    p_supplier_id integer, p_name text, p_phone text, p_address text, p_email text,
    p_vat_number text, p_notes text, p_is_active boolean DEFAULT true)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id integer;
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF nullif(trim(p_email), '') IS NOT NULL AND trim(p_email) !~ '^[^@\s]+@[^@\s]+\.[^@\s]+$' THEN
        RAISE EXCEPTION 'The email address is not valid.';
    END IF;

    IF p_supplier_id IS NULL THEN
        INSERT INTO suppliers (name, phone, address, email, vat_number, notes, is_active)
        VALUES (trim(p_name), nullif(trim(p_phone), ''), nullif(trim(p_address), ''), nullif(trim(p_email), ''),
                nullif(trim(p_vat_number), ''), nullif(trim(p_notes), ''), coalesce(p_is_active, true))
        RETURNING supplier_id INTO v_id;
    ELSE
        UPDATE suppliers
        SET name = trim(p_name), phone = nullif(trim(p_phone), ''), address = nullif(trim(p_address), ''),
            email = nullif(trim(p_email), ''), vat_number = nullif(trim(p_vat_number), ''), notes = nullif(trim(p_notes), ''),
            is_active = coalesce(p_is_active, is_active)
        WHERE supplier_id = p_supplier_id
        RETURNING supplier_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'Supplier % does not exist.', p_supplier_id; END IF;
    END IF;
    RETURN v_id;
END $$;

-- Deletes a supplier; one with purchases or payments is deactivated instead. Returns 'deleted' or 'deactivated'.
CREATE OR REPLACE FUNCTION public.fn_supplier_delete(p_supplier_id integer)
RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    BEGIN
        DELETE FROM suppliers WHERE supplier_id = p_supplier_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'Supplier % does not exist.', p_supplier_id; END IF;
        RETURN 'deleted';
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE suppliers SET is_active = false WHERE supplier_id = p_supplier_id;
        RETURN 'deactivated';
    END;
END $$;
