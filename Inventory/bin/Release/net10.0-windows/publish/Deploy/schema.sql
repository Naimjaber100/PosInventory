--
-- PostgreSQL database dump
--

-- Dumped from database version 15.2
-- Dumped by pg_dump version 15.2

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pg_trgm; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;


--
-- Name: fn_add_opening_stock(integer, numeric, numeric, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_add_opening_stock(p_product_id integer, p_qty numeric, p_cost_per_base numeric, p_user_id integer) RETURNS numeric
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_balance numeric;
BEGIN
    IF p_qty <= 0 THEN RAISE EXCEPTION 'Opening quantity must be more than 0.'; END IF;
    IF p_cost_per_base < 0 THEN RAISE EXCEPTION 'Cost cannot be negative.'; END IF;

    -- Weighted average only over stock that really exists (old stock <= 0 -> take the new cost)
    UPDATE products
    SET avg_cost  = CASE WHEN stock_qty <= 0 THEN p_cost_per_base
                         ELSE (stock_qty * avg_cost + p_qty * p_cost_per_base) / (stock_qty + p_qty) END,
        stock_qty = stock_qty + p_qty,
        updated_at = now()
    WHERE product_id = p_product_id AND product_type = 1
    RETURNING stock_qty INTO v_balance;

    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found or it cannot hold stock.', p_product_id; END IF;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, user_id, notes)
    VALUES (p_product_id, 6, p_qty, p_cost_per_base, v_balance, 3, p_user_id, 'Opening stock');

    RETURN v_balance;
END $$;


--
-- Name: fn_app_user_id(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_app_user_id() RETURNS integer
    LANGUAGE sql STABLE
    AS $_$
    SELECT CASE WHEN current_setting('app.user_id', true) ~ '^\d+$' THEN current_setting('app.user_id', true)::integer END;
$_$;


--
-- Name: fn_audit_add(text, text, integer, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_audit_add(p_action text, p_table_name text, p_record_id integer, p_details jsonb) RETURNS void
    LANGUAGE sql
    AS $$
    INSERT INTO audit_log (user_id, action, table_name, record_id, details)
    VALUES (fn_app_user_id(), p_action, p_table_name, p_record_id, p_details);
$$;


--
-- Name: fn_audit_delete(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_audit_delete() RETURNS trigger
    LANGUAGE plpgsql
    AS $_$
DECLARE
    v_key text := to_jsonb(OLD) ->> TG_ARGV[0];
BEGIN
    IF TG_TABLE_NAME = 'sales_invoices' AND (to_jsonb(OLD) ->> 'status') = '0' THEN RETURN OLD; END IF;
    INSERT INTO audit_log (user_id, action, table_name, record_id, details)
    VALUES (fn_app_user_id(), 'delete', TG_TABLE_NAME,
            CASE WHEN v_key ~ '^\d+$' THEN v_key::integer END,
            to_jsonb(OLD));
    RETURN OLD;
END $_$;


--
-- Name: fn_barcode_check_digit(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_barcode_check_digit(p_digits text) RETURNS integer
    LANGUAGE sql IMMUTABLE
    AS $$
    -- GS1: from the right, the digits are weighted 3, 1, 3, 1...
    SELECT (10 - sum(substr(reverse(p_digits), i, 1)::int * CASE WHEN i % 2 = 1 THEN 3 ELSE 1 END) % 10) % 10
    FROM generate_series(1, length(p_digits)) i;
$$;


--
-- Name: fn_barcode_variants(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_barcode_variants(p_code text) RETURNS text[]
    LANGUAGE sql IMMUTABLE
    AS $_$
    SELECT CASE WHEN p_code ~ '^\d{7,}$' THEN
        array_remove(ARRAY[
            CASE WHEN right(p_code, 1)::int = fn_barcode_check_digit(left(p_code, -1)) THEN left(p_code, -1) END,
            CASE WHEN p_code LIKE '0%' THEN substr(p_code, 2) END,
            '0' || p_code], NULL)
    ELSE '{}'::text[] END;
$_$;


--
-- Name: fn_category_add(text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_category_add(p_name text, p_parent_id integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id integer;
BEGIN
    IF coalesce(trim(p_name), '') = '' THEN RAISE EXCEPTION 'Category name is required.'; END IF;
    IF p_parent_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM categories WHERE category_id = p_parent_id) THEN
        RAISE EXCEPTION 'Parent category % not found.', p_parent_id;
    END IF;
    IF EXISTS (SELECT 1 FROM categories
               WHERE parent_id IS NOT DISTINCT FROM p_parent_id AND lower(name) = lower(trim(p_name))) THEN
        RAISE EXCEPTION 'A category named "%" already exists here.', trim(p_name);
    END IF;

    INSERT INTO categories (name, parent_id) VALUES (trim(p_name), p_parent_id)
    RETURNING category_id INTO v_id;
    RETURN v_id;
END $$;


--
-- Name: fn_category_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_category_delete(p_category_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_children integer;
    v_items    integer;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM categories WHERE category_id = p_category_id) THEN
        RAISE EXCEPTION 'Category % not found.', p_category_id;
    END IF;

    SELECT count(*) INTO v_children FROM categories WHERE parent_id = p_category_id;
    IF v_children > 0 THEN
        RAISE EXCEPTION 'This category has % sub-categor%. Delete or move them first.',
            v_children, CASE WHEN v_children = 1 THEN 'y' ELSE 'ies' END;
    END IF;

    SELECT count(*) INTO v_items FROM products WHERE category_id = p_category_id;
    IF v_items > 0 THEN
        RAISE EXCEPTION 'This category has % item(s). Move them to another category first.', v_items;
    END IF;

    DELETE FROM categories WHERE category_id = p_category_id;
END $$;


--
-- Name: fn_category_ensure_path(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_category_ensure_path(p_path text) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_part   text;
    v_parent integer := NULL;
    v_id     integer;
BEGIN
    FOREACH v_part IN ARRAY string_to_array(coalesce(p_path, ''), '>') LOOP
        v_part := trim(v_part);
        CONTINUE WHEN v_part = '';

        SELECT c.category_id INTO v_id
        FROM categories c
        WHERE c.parent_id IS NOT DISTINCT FROM v_parent AND lower(c.name) = lower(v_part);

        IF v_id IS NULL THEN
            INSERT INTO categories (name, parent_id) VALUES (v_part, v_parent)
            RETURNING category_id INTO v_id;
        END IF;

        v_parent := v_id;
        v_id := NULL;
    END LOOP;

    RETURN v_parent;
END $$;


--
-- Name: fn_category_get_all(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_category_get_all() RETURNS TABLE(category_id integer, name character varying, parent_id integer, path text, level integer, item_count bigint)
    LANGUAGE sql STABLE
    AS $$
    WITH RECURSIVE tree AS (
        SELECT c.category_id, c.name, c.parent_id, c.name::text AS path, 0 AS level,
               ARRAY[lower(c.name::text)] AS sort_key
        FROM categories c
        WHERE c.parent_id IS NULL
        UNION ALL
        SELECT c.category_id, c.name, c.parent_id, t.path || ' > ' || c.name, t.level + 1,
               t.sort_key || lower(c.name::text)
        FROM categories c
        JOIN tree t ON c.parent_id = t.category_id
    )
    SELECT t.category_id, t.name, t.parent_id, t.path, t.level,
           (SELECT count(*) FROM products p WHERE p.category_id = t.category_id)
    FROM tree t
    ORDER BY t.sort_key;
$$;


--
-- Name: fn_category_update(integer, text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_category_update(p_category_id integer, p_name text, p_parent_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF coalesce(trim(p_name), '') = '' THEN RAISE EXCEPTION 'Category name is required.'; END IF;
    IF NOT EXISTS (SELECT 1 FROM categories WHERE category_id = p_category_id) THEN
        RAISE EXCEPTION 'Category % not found.', p_category_id;
    END IF;
    IF p_parent_id IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM categories WHERE category_id = p_parent_id) THEN
            RAISE EXCEPTION 'Parent category % not found.', p_parent_id;
        END IF;
        IF p_parent_id IN (
            WITH RECURSIVE sub AS (
                SELECT c.category_id FROM categories c WHERE c.category_id = p_category_id
                UNION ALL
                SELECT c.category_id FROM categories c JOIN sub s ON c.parent_id = s.category_id)
            SELECT sub.category_id FROM sub) THEN
            RAISE EXCEPTION 'A category cannot be moved under itself or one of its sub-categories.';
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM categories
               WHERE parent_id IS NOT DISTINCT FROM p_parent_id AND lower(name) = lower(trim(p_name))
                 AND category_id <> p_category_id) THEN
        RAISE EXCEPTION 'A category named "%" already exists here.', trim(p_name);
    END IF;

    UPDATE categories SET name = trim(p_name), parent_id = p_parent_id
    WHERE category_id = p_category_id;
END $$;


--
-- Name: fn_close_shift(integer, numeric, numeric); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_close_shift(p_shift_id integer, p_counted_lbp numeric, p_counted_usd numeric) RETURNS TABLE(shift_id integer, user_id integer, user_name character varying, opened_at timestamp with time zone, closed_at timestamp with time zone, status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric, profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric, opening_cash_lbp numeric, opening_cash_usd numeric, expected_cash_lbp numeric, expected_cash_usd numeric, counted_cash_lbp numeric, counted_cash_usd numeric, difference_lbp numeric, difference_usd numeric)
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


--
-- Name: fn_company_get(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_company_get() RETURNS TABLE(name character varying, address character varying, email character varying, mobile character varying, vat_number character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT c.name, c.address, c.email, c.mobile, c.vat_number FROM company c WHERE c.company_id = 1;
$$;


--
-- Name: fn_company_get_logo(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_company_get_logo() RETURNS bytea
    LANGUAGE sql STABLE
    AS $$
    SELECT c.logo FROM company c WHERE c.company_id = 1;
$$;


--
-- Name: fn_company_save(text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_company_save(p_name text, p_address text, p_email text, p_mobile text, p_vat_number text) RETURNS void
    LANGUAGE plpgsql
    AS $_$
DECLARE
    v_email text := nullif(trim(p_email), '');
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The company name is required.'; END IF;
    IF v_email IS NOT NULL AND v_email !~ '^[^@\s]+@[^@\s]+\.[^@\s]+$' THEN
        RAISE EXCEPTION 'The email address is not valid.';
    END IF;

    INSERT INTO company (company_id, name, address, email, mobile, vat_number)
    VALUES (1, trim(p_name), nullif(trim(p_address), ''), v_email, nullif(trim(p_mobile), ''), nullif(trim(p_vat_number), ''))
    ON CONFLICT (company_id) DO UPDATE
    SET name = EXCLUDED.name, address = EXCLUDED.address, email = EXCLUDED.email,
        mobile = EXCLUDED.mobile, vat_number = EXCLUDED.vat_number;
END $_$;


--
-- Name: fn_company_set_logo(bytea); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_company_set_logo(p_logo bytea) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF length(p_logo) > 1048576 THEN RAISE EXCEPTION 'The logo is too big (1 MB at most).'; END IF;
    UPDATE company SET logo = p_logo WHERE company_id = 1;
    IF NOT FOUND THEN RAISE EXCEPTION 'Save the company first.'; END IF;
END $$;


--
-- Name: fn_currency_get_all(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_get_all() RETURNS TABLE(code character, name character varying, symbol character varying, decimals smallint, is_primary boolean, is_active boolean, sort_order integer, rate numeric, rate_at timestamp with time zone, rate_by character varying)
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


--
-- Name: fn_currency_rate(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_rate(p_code text) RETURNS numeric
    LANGUAGE sql STABLE
    AS $$
    SELECT CASE WHEN c.is_primary THEN 1::numeric
                ELSE (SELECT r.rate FROM exchange_rates r WHERE r.currency_code = c.code
                      ORDER BY r.effective_at DESC, r.rate_id DESC LIMIT 1) END
    FROM currencies c WHERE c.code = upper(p_code);
$$;


--
-- Name: fn_currency_rates(text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_rates(p_code text, p_limit integer DEFAULT 100) RETURNS TABLE(effective_at timestamp with time zone, rate numeric, set_by character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT r.effective_at, r.rate, u.full_name
    FROM exchange_rates r LEFT JOIN users u ON u.user_id = r.user_id
    WHERE r.currency_code = upper(p_code)
    ORDER BY r.effective_at DESC, r.rate_id DESC
    LIMIT greatest(coalesce(p_limit, 100), 1);
$$;


--
-- Name: fn_currency_save(text, text, text, smallint, boolean, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_save(p_code text, p_name text, p_symbol text, p_decimals smallint, p_is_active boolean, p_sort_order integer) RETURNS void
    LANGUAGE plpgsql
    AS $_$
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
END $_$;


--
-- Name: fn_currency_set_primary(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_set_primary(p_code text) RETURNS void
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


--
-- Name: fn_currency_set_rate(text, numeric, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_currency_set_rate(p_code text, p_rate numeric, p_user_id integer) RETURNS void
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


--
-- Name: fn_current_rate(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_current_rate() RETURNS numeric
    LANGUAGE sql STABLE
    AS $$
    SELECT coalesce((SELECT fn_currency_rate(c.code) FROM currencies c
                     WHERE c.is_active AND NOT c.is_primary ORDER BY c.sort_order, c.code LIMIT 1),
                    CASE WHEN EXISTS (SELECT 1 FROM currencies c WHERE c.is_active AND NOT c.is_primary) THEN NULL ELSE 1 END);
$$;


--
-- Name: fn_customer_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_customer_delete(p_customer_id integer) RETURNS text
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


--
-- Name: fn_customer_get_all(text, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_customer_get_all(p_search text DEFAULT NULL::text, p_include_inactive boolean DEFAULT false) RETURNS TABLE(customer_id integer, name character varying, phone character varying, address character varying, email character varying, balance_lbp numeric, credit_limit_lbp numeric, notes character varying, is_active boolean)
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


--
-- Name: fn_customer_get_payments(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_customer_get_payments(p_customer_id integer) RETURNS TABLE(payment_id integer, payment_date timestamp with time zone, method smallint, currency text, amount numeric, amount_lbp numeric, cashier character varying, invoices text)
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


--
-- Name: fn_customer_open_invoices(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_customer_open_invoices(p_customer_id integer) RETURNS TABLE(sale_id integer, invoice_no character varying, invoice_date timestamp with time zone, total numeric, on_account_lbp numeric, due_lbp numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT si.sale_id, si.invoice_no, si.invoice_date, si.total,
           coalesce((SELECT sum(p.amount_lbp) FROM sales_payments p WHERE p.sale_id = si.sale_id AND p.method = 3), 0),
           si.due_lbp
    FROM sales_invoices si
    WHERE si.customer_id = p_customer_id AND si.status = 1 AND si.invoice_type = 1 AND si.due_lbp > 0
    ORDER BY si.invoice_date, si.sale_id;
$$;


--
-- Name: fn_customer_save(integer, text, text, text, text, numeric, text, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_customer_save(p_customer_id integer, p_name text, p_phone text, p_address text, p_email text, p_credit_limit numeric, p_notes text, p_is_active boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $_$
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
END $_$;


--
-- Name: fn_hold_sale(integer, jsonb, integer, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_hold_sale(p_user_id integer, p_lines jsonb, p_shift_id integer DEFAULT NULL::integer, p_customer_id integer DEFAULT NULL::integer, p_replace_held_id integer DEFAULT NULL::integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
#variable_conflict use_column
DECLARE
    v_id    int;
    v_count int;
BEGIN
    IF p_lines IS NULL OR jsonb_array_length(p_lines) = 0 THEN RAISE EXCEPTION 'Nothing to hold.'; END IF;
    IF p_replace_held_id IS NOT NULL THEN
        DELETE FROM sales_invoices WHERE sale_id = p_replace_held_id AND status = 0;
    END IF;

    INSERT INTO sales_invoices (invoice_type, customer_id, shift_id, user_id, exchange_rate, status)
    VALUES (1, p_customer_id, p_shift_id, p_user_id, coalesce(fn_current_rate(), 1), 0)
    RETURNING sale_id INTO v_id;

    INSERT INTO sales_lines (sale_id, line_no, product_id, packaging_id, description, quantity, factor,
                             unit_price, discount, unit_cost, line_total, line_profit)
    SELECT v_id, x.ord::int, p.product_id, pk.packaging_id, fn_item_description(p.name, p.size, p.color, pk.name),
           (x.j->>'quantity')::numeric, pk.factor,
           coalesce((x.j->>'unit_price')::numeric, pk.sale_price), coalesce((x.j->>'discount')::numeric, 0), 0,
           round((x.j->>'quantity')::numeric * coalesce((x.j->>'unit_price')::numeric, pk.sale_price)
                 - coalesce((x.j->>'discount')::numeric, 0), 2), 0
    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS x(j, ord)
    JOIN product_packaging pk ON pk.packaging_id = (x.j->>'packaging_id')::int
    JOIN products p ON p.product_id = pk.product_id;

    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count <> jsonb_array_length(p_lines) THEN RAISE EXCEPTION 'An item on the held sale was not found.'; END IF;

    UPDATE sales_invoices si SET sub_total = t.s, total = t.s
    FROM (SELECT sum(line_total) AS s FROM sales_lines WHERE sale_id = v_id) t
    WHERE si.sale_id = v_id;

    RETURN v_id;
END $$;


--
-- Name: fn_item_description(text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_item_description(p_name text, p_size text, p_color text, p_packaging text) RETURNS text
    LANGUAGE sql IMMUTABLE
    AS $$
    SELECT left(p_name
        || coalesce(' · ' || p_size, '')
        || coalesce(' · ' || p_color, '')
        || CASE WHEN p_packaging IS NULL OR lower(p_packaging) IN ('piece', 'pcs') THEN '' ELSE ' · ' || p_packaging END, 200);
$$;


--
-- Name: fn_next_doc_no(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_next_doc_no(p_doc_type text) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_year int := extract(year FROM now())::int;
    v_n    int;
BEGIN
    INSERT INTO document_counters AS dc (doc_type, doc_year, last_number)
    VALUES (p_doc_type, v_year, 1)
    ON CONFLICT (doc_type, doc_year) DO UPDATE SET last_number = dc.last_number + 1
    RETURNING last_number INTO v_n;

    RETURN p_doc_type || '-' || v_year || '-' || lpad(v_n::text, 6, '0');
END $$;


--
-- Name: fn_open_shift(integer, integer, numeric, numeric); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_open_shift(p_register_id integer, p_user_id integer, p_opening_lbp numeric DEFAULT 0, p_opening_usd numeric DEFAULT 0) RETURNS integer
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


--
-- Name: fn_pay_supplier(integer, numeric, text, integer, integer, integer, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pay_supplier(p_supplier_id integer, p_amount numeric, p_currency text, p_method integer, p_user_id integer, p_purchase_id integer DEFAULT NULL::integer, p_notes text DEFAULT NULL::text) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_rate numeric := fn_current_rate();
    v_lbp  numeric;
    v_id   int;
BEGIN
    IF p_currency NOT IN ('LBP', 'USD') THEN RAISE EXCEPTION 'Currency must be LBP or USD.'; END IF;
    IF p_amount <= 0 THEN RAISE EXCEPTION 'Amount must be more than 0.'; END IF;
    IF p_currency = 'USD' AND v_rate IS NULL THEN RAISE EXCEPTION 'Set the exchange rate first.'; END IF;
    v_lbp := CASE WHEN p_currency = 'USD' THEN round(p_amount * v_rate, 2) ELSE p_amount END;

    IF p_purchase_id IS NOT NULL THEN
        UPDATE purchase_invoices SET paid_lbp = paid_lbp + v_lbp
        WHERE purchase_id = p_purchase_id AND supplier_id = p_supplier_id AND status = 1;
        IF NOT FOUND THEN RAISE EXCEPTION 'Posted purchase % not found for this supplier.', p_purchase_id; END IF;
    END IF;

    INSERT INTO supplier_payments (supplier_id, purchase_id, method, currency, amount, exchange_rate, amount_lbp, user_id, notes)
    VALUES (p_supplier_id, p_purchase_id, p_method, p_currency, p_amount, coalesce(v_rate, 1), v_lbp, p_user_id, p_notes)
    RETURNING payment_id INTO v_id;

    UPDATE suppliers SET balance_lbp = balance_lbp - v_lbp WHERE supplier_id = p_supplier_id;
    RETURN v_id;
END $$;


--
-- Name: fn_pos_delete_held(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_delete_held(p_sale_id integer) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM sales_invoices WHERE sale_id = p_sale_id AND status = 0;   -- lines cascade
    RETURN FOUND;
END $$;


--
-- Name: fn_pos_find_item(text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_find_item(p_search text, p_limit integer DEFAULT 30) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, sale_price numeric, factor numeric, is_weighted boolean, base_unit character varying, stock_qty numeric)
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

    -- The same label written another way: saved without its check digit, or with / without a leading 0
    -- (fn_barcode_variants, Database/product_functions.sql)
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
    WHERE pk.barcode = ANY (fn_barcode_variants(v_search))
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
           OR pk.barcode LIKE '%' || v_pattern || '%')   -- any part of the barcode (index: Database/purchase_functions.sql)
    ORDER BY p.name, pk.factor, pk.packaging_id
    LIMIT greatest(coalesce(p_limit, 30), 1);
END $$;


--
-- Name: fn_pos_find_scale_item(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_find_scale_item(p_scale_code text) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, sale_price numeric, factor numeric, is_weighted boolean, base_unit character varying, stock_qty numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           pk.name, pk.barcode, pk.sale_price, pk.factor, p.is_weighted, p.base_unit, p.stock_qty
    FROM products p
    JOIN product_packaging pk ON pk.product_id = p.product_id AND pk.is_active AND pk.can_sell
    WHERE p.scale_code = trim(p_scale_code) AND p.is_active AND p.product_type = 1
    ORDER BY (pk.factor = 1) DESC, pk.is_default DESC, pk.packaging_id
    LIMIT 1;
$$;


--
-- Name: fn_pos_get_held_lines(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_get_held_lines(p_sale_id integer) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, quantity numeric, unit_price numeric, discount numeric, factor numeric, is_weighted boolean, base_unit character varying, stock_qty numeric)
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


--
-- Name: fn_pos_get_held_sales(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_get_held_sales() RETURNS TABLE(sale_id integer, held_at timestamp with time zone, user_name character varying, item_count bigint, total numeric)
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


--
-- Name: fn_pos_item_list(timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_item_list(p_since timestamp with time zone DEFAULT NULL::timestamp with time zone) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, sale_price numeric, factor numeric, is_weighted boolean, base_unit character varying, stock_qty numeric, product_name character varying, scale_code character varying, sellable boolean, as_of timestamp with time zone)
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_ids integer[];
BEGIN
    -- The first load: everything that can be sold
    IF p_since IS NULL THEN
        RETURN QUERY SELECT v.*, now() FROM v_pos_items v WHERE v.sellable;
        RETURN;
    END IF;

    -- A refresh: the items whose cashier data changed (through their index)
    SELECT array_agg(DISTINCT x.id) INTO v_ids FROM (
        SELECT p.product_id AS id FROM products p WHERE p.pos_changed_at > p_since
        UNION ALL SELECT pk.product_id FROM product_packaging pk WHERE pk.pos_changed_at > p_since
    ) x;
    IF v_ids IS NULL THEN RETURN; END IF;

    RETURN QUERY SELECT v.*, now() FROM v_pos_items v WHERE v.product_id = ANY (v_ids);
END $$;


--
-- Name: fn_pos_item_stamp(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_item_stamp() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF TG_OP = 'UPDATE' AND NOT EXISTS (
        SELECT 1 FROM unnest(TG_ARGV) c WHERE to_jsonb(NEW) -> c IS DISTINCT FROM to_jsonb(OLD) -> c) THEN
        RETURN NEW;   -- saved again without a change the cashier sees
    END IF;
    NEW.pos_changed_at := now();
    PERFORM pg_notify('pos_items', 'changed');
    RETURN NEW;
END $$;


--
-- Name: fn_pos_items_notify(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_pos_items_notify() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_ids text;
BEGIN
    SELECT string_agg(o.packaging_id::text, ',') INTO v_ids FROM old_rows o;
    IF v_ids IS NOT NULL THEN
        PERFORM pg_notify('pos_items', CASE WHEN length(v_ids) > 7000 THEN 'reload' ELSE 'deleted:' || v_ids END);
    END IF;
    RETURN NULL;
END $$;


--
-- Name: fn_post_purchase(integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_post_purchase(p_purchase_id integer, p_user_id integer) RETURNS text
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

    -- Recalculate the totals (never trust what the screen sent); a line discount lowers that line's cost
    UPDATE purchase_lines SET line_total = round(quantity * unit_cost - discount, 2) WHERE purchase_id = p_purchase_id;
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


--
-- Name: fn_post_purchase_return(integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_post_purchase_return(p_purchase_id integer, p_user_id integer) RETURNS text
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
    UPDATE purchase_lines SET line_total = round(quantity * unit_cost - discount, 2) WHERE purchase_id = p_purchase_id;
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


--
-- Name: fn_post_stock_adjustment(integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_post_stock_adjustment(p_adjustment_id integer, p_user_id integer) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_status smallint;
    v_no     text;
BEGIN
    SELECT status INTO v_status FROM stock_adjustments WHERE adjustment_id = p_adjustment_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Adjustment % not found.', p_adjustment_id; END IF;
    IF v_status <> 0 THEN RAISE EXCEPTION 'This adjustment is already posted.'; END IF;

    PERFORM 1 FROM products
    WHERE product_id IN (SELECT product_id FROM stock_adjustment_lines WHERE adjustment_id = p_adjustment_id)
    ORDER BY product_id FOR UPDATE;

    -- difference = what was counted - what the system had WHEN IT WAS COUNTED (qty_before).
    -- Sales and purchases made between counting and posting are kept.
    UPDATE stock_adjustment_lines l
    SET difference = l.counted_qty - l.qty_before, unit_cost = p.avg_cost
    FROM products p WHERE p.product_id = l.product_id AND l.adjustment_id = p_adjustment_id;

    UPDATE products p SET stock_qty = p.stock_qty + l.difference, updated_at = now()
    FROM stock_adjustment_lines l
    WHERE l.product_id = p.product_id AND l.adjustment_id = p_adjustment_id AND l.difference <> 0;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id, notes)
    SELECT l.product_id, 5, l.difference, l.unit_cost, p.stock_qty, 3, p_adjustment_id, p_user_id, a.reason
    FROM stock_adjustment_lines l
    JOIN stock_adjustments a ON a.adjustment_id = l.adjustment_id
    JOIN products p ON p.product_id = l.product_id
    WHERE l.adjustment_id = p_adjustment_id AND l.difference <> 0;

    v_no := fn_next_doc_no('ADJ');
    UPDATE stock_adjustments SET status = 1, adjustment_no = v_no, posted_at = now() WHERE adjustment_id = p_adjustment_id;
    RETURN v_no;
END $$;


--
-- Name: fn_product_add(text, integer, smallint, text, text, text, boolean, numeric, numeric, boolean, numeric, text, numeric, integer, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_add(p_name text, p_category_id integer, p_product_type smallint, p_size text, p_color text, p_base_unit text, p_is_weighted boolean, p_avg_cost numeric, p_reorder_level numeric, p_is_active boolean, p_sale_price numeric, p_barcode text, p_opening_qty numeric, p_user_id integer, p_packagings jsonb DEFAULT NULL::jsonb) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id integer;
BEGIN
    IF coalesce(trim(p_name), '') = '' THEN RAISE EXCEPTION 'Name is required.'; END IF;
    IF coalesce(trim(p_base_unit), '') = '' THEN RAISE EXCEPTION 'Base unit is required.'; END IF;
    IF p_avg_cost < 0 THEN RAISE EXCEPTION 'Cost cannot be negative.'; END IF;
    IF p_sale_price < 0 THEN RAISE EXCEPTION 'Sale price cannot be negative.'; END IF;
    IF coalesce(p_opening_qty, 0) > 0 AND p_product_type <> 1 THEN
        RAISE EXCEPTION 'Only standard items can have opening stock.';
    END IF;

    INSERT INTO products (name, category_id, product_type, size, color, base_unit,
                          is_weighted, avg_cost, reorder_level, is_active)
    VALUES (trim(p_name), p_category_id, p_product_type, nullif(trim(p_size), ''), nullif(trim(p_color), ''),
            trim(p_base_unit), p_is_weighted, coalesce(p_avg_cost, 0), coalesce(p_reorder_level, 0), p_is_active)
    RETURNING product_id INTO v_id;

    IF p_packagings IS NULL THEN
        INSERT INTO product_packaging (product_id, name, factor, barcode, sale_price, is_default)
        VALUES (v_id, trim(p_base_unit), 1, nullif(trim(p_barcode), ''), coalesce(p_sale_price, 0), true);
    ELSE
        PERFORM fn_product_save_packagings(v_id, p_packagings);
    END IF;

    IF coalesce(p_opening_qty, 0) > 0 THEN
        PERFORM fn_add_opening_stock(v_id, p_opening_qty, coalesce(p_avg_cost, 0), p_user_id);
    END IF;

    RETURN v_id;
END $$;


--
-- Name: fn_product_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_delete(p_product_id integer) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM products WHERE product_id = p_product_id) THEN
        RAISE EXCEPTION 'Item % not found.', p_product_id;
    END IF;

    BEGIN
        DELETE FROM products WHERE product_id = p_product_id;  -- packaging / bundle rows cascade
        RETURN true;
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE products SET is_active = false, updated_at = now() WHERE product_id = p_product_id;
        RETURN false;
    END;
END $$;


--
-- Name: fn_product_get_all(integer, integer, integer, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_get_all(p_page integer DEFAULT 1, p_page_size integer DEFAULT 50, p_category_id integer DEFAULT NULL::integer, p_search text DEFAULT NULL::text, p_stock text DEFAULT NULL::text) RETURNS TABLE(product_id integer, parent_id integer, name character varying, category_id integer, category_name character varying, product_type smallint, size character varying, color character varying, base_unit character varying, is_weighted boolean, avg_cost numeric, stock_qty numeric, reorder_level numeric, sale_price numeric, barcode character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone, total_count bigint)
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_search  text := nullif(trim(p_search), '');
    v_pattern text;     -- the search text with LIKE wildcards escaped
    v_words   text[];   -- escaped words for the name match
BEGIN
    IF p_page IS NULL OR p_page < 1 THEN RAISE EXCEPTION 'Page must be 1 or more.'; END IF;
    IF p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 500 THEN
        RAISE EXCEPTION 'Page size must be between 1 and 500.';
    END IF;

    IF v_search IS NOT NULL THEN
        v_pattern := replace(replace(replace(v_search, '\', '\\'), '%', '\%'), '_', '\_');
        v_words   := regexp_split_to_array(v_pattern, '\s+');
    END IF;

    RETURN QUERY
    SELECT p.product_id, p.parent_id, p.name, p.category_id, c.name,
           p.product_type, p.size, p.color, p.base_unit, p.is_weighted,
           p.avg_cost, p.stock_qty, p.reorder_level, pk.sale_price, pk.barcode,
           p.is_active, p.created_at, p.updated_at,
           count(*) OVER ()
    FROM products p
    LEFT JOIN categories c         ON c.category_id = p.category_id
    LEFT JOIN product_packaging pk ON pk.product_id = p.product_id AND pk.is_default
    WHERE (p_category_id IS NULL
           OR (p_category_id = 0 AND p.category_id IS NULL)
           OR p.category_id IN (
                WITH RECURSIVE sub AS (
                    SELECT c0.category_id AS cid FROM categories c0 WHERE c0.category_id = p_category_id
                    UNION ALL
                    SELECT c1.category_id FROM categories c1 JOIN sub s ON c1.parent_id = s.cid)
                SELECT sub.cid FROM sub))
      AND (v_search IS NULL
           OR NOT EXISTS (SELECT 1 FROM unnest(v_words) w WHERE p.name NOT ILIKE '%' || w || '%')
           OR EXISTS (SELECT 1 FROM product_packaging b
                      WHERE b.product_id = p.product_id AND b.barcode LIKE v_pattern || '%'))
      -- Stock filter: only standard items hold stock (bundles / models count as "in stock")
      AND (p_stock IS NULL
           OR (p_stock = 'in'  AND (p.product_type <> 1 OR p.stock_qty > p.reorder_level))
           OR (p_stock = 'low' AND p.product_type = 1 AND p.stock_qty > 0 AND p.stock_qty <= p.reorder_level)
           OR (p_stock = 'out' AND p.product_type = 1 AND p.stock_qty <= 0))
    ORDER BY  p.product_id DESC,
             p.name, p.product_id          -- product_id keeps the order stable between pages
    LIMIT p_page_size
    OFFSET (p_page - 1) * p_page_size;
END $$;


--
-- Name: fn_product_get_by_id(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_get_by_id(p_product_id integer) RETURNS TABLE(product_id integer, parent_id integer, name character varying, category_id integer, category_name character varying, product_type smallint, size character varying, color character varying, base_unit character varying, is_weighted boolean, avg_cost numeric, stock_qty numeric, reorder_level numeric, sale_price numeric, barcode character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT p.product_id, p.parent_id, p.name, p.category_id, c.name,
           p.product_type, p.size, p.color, p.base_unit, p.is_weighted,
           p.avg_cost, p.stock_qty, p.reorder_level, pk.sale_price, pk.barcode,
           p.is_active, p.created_at, p.updated_at
    FROM products p
    LEFT JOIN categories c         ON c.category_id = p.category_id
    LEFT JOIN product_packaging pk ON pk.product_id = p.product_id AND pk.is_default
    WHERE p.product_id = p_product_id;
$$;


--
-- Name: fn_product_get_packagings(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_get_packagings(p_product_id integer) RETURNS TABLE(packaging_id integer, name character varying, factor numeric, barcode character varying, sale_price numeric, is_default boolean, can_sell boolean, can_purchase boolean)
    LANGUAGE sql STABLE
    AS $$
    SELECT pk.packaging_id, pk.name, pk.factor, pk.barcode, pk.sale_price,
           pk.is_default, pk.can_sell, pk.can_purchase
    FROM product_packaging pk
    WHERE pk.product_id = p_product_id AND pk.is_active
    ORDER BY pk.is_default DESC, pk.factor, pk.packaging_id;
$$;


--
-- Name: fn_product_get_scale_code(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_get_scale_code(p_product_id integer) RETURNS character varying
    LANGUAGE sql STABLE
    AS $$
    SELECT scale_code FROM products WHERE product_id = p_product_id;
$$;


--
-- Name: fn_product_import(jsonb, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_import(p_rows jsonb, p_user_id integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    r          jsonb;

    v_cat_id   integer;
    v_count    integer := 0;
BEGIN
    IF jsonb_typeof(p_rows) IS DISTINCT FROM 'array' THEN
        RAISE EXCEPTION 'Import data must be a JSON array.';
    END IF;

    FOR r IN SELECT value FROM jsonb_array_elements(p_rows) LOOP
        BEGIN
            v_cat_id := fn_category_ensure_path(r->>'category');

            PERFORM fn_product_add(
                r->>'name',
                v_cat_id,
                coalesce((r->>'product_type')::smallint, 1::smallint),
                r->>'size',
                r->>'color',
                coalesce(nullif(trim(r->>'base_unit'), ''), 'pcs'),
                coalesce((r->>'is_weighted')::boolean, false),
                coalesce((r->>'avg_cost')::numeric, 0),
                coalesce((r->>'reorder_level')::numeric, 0),
                coalesce((r->>'is_active')::boolean, true),
                coalesce((r->>'sale_price')::numeric, 0),
                r->>'barcode',
                coalesce((r->>'opening_qty')::numeric, 0),
                p_user_id);

            v_count := v_count + 1;
        EXCEPTION WHEN OTHERS THEN
            RAISE EXCEPTION 'Row %: %', coalesce(r->>'row_number', '?'), SQLERRM;
        END;
    END LOOP;

    RETURN v_count;
END $$;


--
-- Name: fn_product_save_packagings(integer, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_save_packagings(p_product_id integer, p_packagings jsonb) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    r       record;
    v_id    integer;
    v_keep  integer[] := '{}';
    v_bad   text;
BEGIN
    IF jsonb_typeof(p_packagings) IS DISTINCT FROM 'array' OR jsonb_array_length(p_packagings) = 0 THEN
        RAISE EXCEPTION 'Add at least one packaging.';
    END IF;

    CREATE TEMP TABLE IF NOT EXISTS _pk (
        n integer, packaging_id integer, name text, factor numeric, barcode text,
        sale_price numeric, is_default boolean, can_sell boolean, can_purchase boolean) ON COMMIT DROP;
    TRUNCATE _pk;

    INSERT INTO _pk
    SELECT e.n, (e.item->>'packaging_id')::integer, nullif(trim(e.item->>'name'), ''),
           (e.item->>'factor')::numeric, nullif(trim(e.item->>'barcode'), ''),
           coalesce((e.item->>'sale_price')::numeric, 0),
           coalesce((e.item->>'is_default')::boolean, false),
           coalesce((e.item->>'can_sell')::boolean, true),
           coalesce((e.item->>'can_purchase')::boolean, true)
    FROM jsonb_array_elements(p_packagings) WITH ORDINALITY AS e(item, n);

    -- Checks (with friendly messages)
    IF EXISTS (SELECT 1 FROM _pk WHERE name IS NULL) THEN RAISE EXCEPTION 'Every packaging needs a name.'; END IF;
    SELECT name INTO v_bad FROM _pk WHERE factor IS NULL OR factor <= 0 LIMIT 1;
    IF FOUND THEN RAISE EXCEPTION 'Packaging "%": "Contains" must be more than 0.', v_bad; END IF;
    SELECT name INTO v_bad FROM _pk WHERE sale_price < 0 LIMIT 1;
    IF FOUND THEN RAISE EXCEPTION 'Packaging "%": sale price cannot be negative.', v_bad; END IF;
    IF (SELECT count(*) FROM _pk WHERE is_default) <> 1 THEN
        RAISE EXCEPTION 'Choose exactly one default packaging.';
    END IF;
    SELECT lower(name) INTO v_bad FROM _pk GROUP BY lower(name) HAVING count(*) > 1 LIMIT 1;
    IF FOUND THEN RAISE EXCEPTION 'Packaging name "%" is used twice.', v_bad; END IF;
    SELECT barcode INTO v_bad FROM _pk WHERE barcode IS NOT NULL GROUP BY barcode HAVING count(*) > 1 LIMIT 1;
    IF FOUND THEN RAISE EXCEPTION 'Barcode % is used twice.', v_bad; END IF;
    SELECT pk.barcode INTO v_bad
    FROM _pk x JOIN product_packaging pk ON pk.barcode = x.barcode
    WHERE pk.product_id <> p_product_id LIMIT 1;
    IF FOUND THEN RAISE EXCEPTION 'Barcode % already belongs to another item.', v_bad; END IF;
    IF EXISTS (SELECT 1 FROM _pk x WHERE x.packaging_id IS NOT NULL AND NOT EXISTS
               (SELECT 1 FROM product_packaging pk WHERE pk.packaging_id = x.packaging_id AND pk.product_id = p_product_id)) THEN
        RAISE EXCEPTION 'A packaging does not belong to this item. Reopen the item and try again.';
    END IF;

    -- A new row named like an existing (possibly removed) packaging reuses it
    UPDATE _pk x SET packaging_id = pk.packaging_id
    FROM product_packaging pk
    WHERE x.packaging_id IS NULL AND pk.product_id = p_product_id AND lower(pk.name) = lower(x.name)
      AND NOT EXISTS (SELECT 1 FROM _pk y WHERE y.packaging_id = pk.packaging_id);

    -- Removed packagings: free their name/barcode/default first so the rows below cannot clash
    FOR v_id IN SELECT pk.packaging_id FROM product_packaging pk
                WHERE pk.product_id = p_product_id
                  AND pk.packaging_id NOT IN (SELECT packaging_id FROM _pk WHERE packaging_id IS NOT NULL) LOOP
        IF EXISTS (SELECT 1 FROM purchase_lines WHERE packaging_id = v_id)
           OR EXISTS (SELECT 1 FROM sales_lines WHERE packaging_id = v_id) THEN
            UPDATE product_packaging
            SET is_active = false, is_default = false, barcode = NULL,
                name = left(name, 30) || ' (old #' || packaging_id || ')'
            WHERE packaging_id = v_id;
        ELSE
            DELETE FROM product_packaging WHERE packaging_id = v_id;
        END IF;
    END LOOP;

    -- Kept packagings: clear default / barcode / name so swaps between rows work
    UPDATE product_packaging pk
    SET is_default = false, barcode = NULL, name = '~tmp' || pk.packaging_id
    WHERE pk.packaging_id IN (SELECT packaging_id FROM _pk WHERE packaging_id IS NOT NULL);

    FOR r IN SELECT * FROM _pk ORDER BY n LOOP
        IF r.packaging_id IS NULL THEN
            INSERT INTO product_packaging (product_id, name, factor, barcode, sale_price, is_default, can_sell, can_purchase, is_active)
            VALUES (p_product_id, r.name, r.factor, r.barcode, r.sale_price, r.is_default, r.can_sell, r.can_purchase, true);
        ELSE
            UPDATE product_packaging
            SET name = r.name, factor = r.factor, barcode = r.barcode, sale_price = r.sale_price,
                is_default = r.is_default, can_sell = r.can_sell, can_purchase = r.can_purchase, is_active = true
            WHERE packaging_id = r.packaging_id;
        END IF;
    END LOOP;
END $$;


--
-- Name: fn_product_set_active(integer, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_set_active(p_product_id integer, p_is_active boolean) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE products SET is_active = p_is_active, updated_at = now() WHERE product_id = p_product_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', p_product_id; END IF;
END $$;


--
-- Name: fn_product_set_scale_code(integer, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_set_scale_code(p_product_id integer, p_scale_code text) RETURNS void
    LANGUAGE plpgsql
    AS $_$
DECLARE
    v_code text := nullif(trim(p_scale_code), '');
    v_other text;
BEGIN
    IF v_code IS NOT NULL AND v_code !~ '^[0-9]+$' THEN RAISE EXCEPTION 'The scale code can only have digits.'; END IF;
    SELECT name INTO v_other FROM products WHERE scale_code = v_code AND product_id <> p_product_id;
    IF v_other IS NOT NULL THEN RAISE EXCEPTION 'Scale code % is already used by "%".', v_code, v_other; END IF;

    UPDATE products SET scale_code = v_code WHERE product_id = p_product_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', p_product_id; END IF;
END $_$;


--
-- Name: fn_product_update(integer, text, integer, smallint, text, text, text, boolean, numeric, boolean, numeric, text, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_product_update(p_product_id integer, p_name text, p_category_id integer, p_product_type smallint, p_size text, p_color text, p_base_unit text, p_is_weighted boolean, p_reorder_level numeric, p_is_active boolean, p_sale_price numeric, p_barcode text, p_packagings jsonb DEFAULT NULL::jsonb) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF coalesce(trim(p_name), '') = '' THEN RAISE EXCEPTION 'Name is required.'; END IF;
    IF coalesce(trim(p_base_unit), '') = '' THEN RAISE EXCEPTION 'Base unit is required.'; END IF;
    IF p_sale_price < 0 THEN RAISE EXCEPTION 'Sale price cannot be negative.'; END IF;

    UPDATE products
    SET name          = trim(p_name),
        category_id   = p_category_id,
        product_type  = p_product_type,
        size          = nullif(trim(p_size), ''),
        color         = nullif(trim(p_color), ''),
        base_unit     = trim(p_base_unit),
        is_weighted   = p_is_weighted,
        reorder_level = coalesce(p_reorder_level, 0),
        is_active     = p_is_active,
        updated_at    = now()
    WHERE product_id = p_product_id;

    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', p_product_id; END IF;

    IF p_packagings IS NOT NULL THEN
        PERFORM fn_product_save_packagings(p_product_id, p_packagings);
        RETURN;
    END IF;

    UPDATE product_packaging
    SET barcode    = nullif(trim(p_barcode), ''),
        sale_price = coalesce(p_sale_price, 0)
    WHERE product_id = p_product_id AND is_default;

    IF NOT FOUND THEN
        INSERT INTO product_packaging (product_id, name, factor, barcode, sale_price, is_default)
        VALUES (p_product_id, trim(p_base_unit), 1, nullif(trim(p_barcode), ''), coalesce(p_sale_price, 0), true);
    END IF;
END $$;


--
-- Name: fn_promotion_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_promotion_delete(p_promotion_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM promotions WHERE promotion_id = p_promotion_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Promotion % does not exist.', p_promotion_id; END IF;
END $$;


--
-- Name: fn_promotion_get_active(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_promotion_get_active() RETURNS TABLE(promotion_id integer, name character varying, promo_type smallint, new_price numeric, percent numeric, buy_qty integer, free_qty integer, group_price numeric, min_total numeric, packaging_ids integer[])
    LANGUAGE sql STABLE
    AS $$
    SELECT pr.promotion_id, pr.name, pr.promo_type,
           pr.new_price, pr.percent, pr.buy_qty, pr.free_qty, pr.group_price, pr.min_total,
           coalesce(CASE
               WHEN pr.promo_type = 5 THEN (
                   WITH RECURSIVE tree AS (
                       SELECT c.category_id FROM categories c WHERE c.category_id = pr.category_id
                       UNION ALL
                       SELECT c.category_id FROM categories c JOIN tree t ON c.parent_id = t.category_id)
                   SELECT array_agg(pk.packaging_id)
                   FROM product_packaging pk
                   JOIN products p ON p.product_id = pk.product_id
                   WHERE p.category_id IN (SELECT t.category_id FROM tree t))
               WHEN pr.promo_type < 5 THEN (
                   SELECT array_agg(pi.packaging_id) FROM promotion_items pi WHERE pi.promotion_id = pr.promotion_id)
           END, '{}'::integer[])
    FROM promotions pr
    WHERE pr.is_active
      AND (pr.starts_on IS NULL OR pr.starts_on <= current_date)
      AND (pr.ends_on IS NULL OR pr.ends_on >= current_date)
    ORDER BY pr.promotion_id;
$$;


--
-- Name: fn_promotion_get_all(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_promotion_get_all() RETURNS TABLE(promotion_id integer, name character varying, promo_type smallint, starts_on date, ends_on date, is_active boolean, new_price numeric, percent numeric, buy_qty integer, free_qty integer, group_price numeric, min_total numeric, category_id integer, category_name character varying, item_count bigint, first_item text)
    LANGUAGE sql STABLE
    AS $$
    SELECT pr.promotion_id, pr.name, pr.promo_type, pr.starts_on, pr.ends_on, pr.is_active,
           pr.new_price, pr.percent, pr.buy_qty, pr.free_qty, pr.group_price, pr.min_total,
           pr.category_id, c.name,
           (SELECT count(*) FROM promotion_items pi WHERE pi.promotion_id = pr.promotion_id),
           (SELECT min(fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text))
            FROM promotion_items pi
            JOIN product_packaging pk ON pk.packaging_id = pi.packaging_id
            JOIN products p ON p.product_id = pk.product_id
            WHERE pi.promotion_id = pr.promotion_id)
    FROM promotions pr
    LEFT JOIN categories c ON c.category_id = pr.category_id
    ORDER BY pr.promotion_id DESC;
$$;


--
-- Name: fn_promotion_get_items(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_promotion_get_items(p_promotion_id integer) RETURNS TABLE(packaging_id integer, item_name text, barcode character varying, sale_price numeric, is_weighted boolean)
    LANGUAGE sql STABLE
    AS $$
    SELECT pk.packaging_id, fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           pk.barcode, pk.sale_price, p.is_weighted
    FROM promotion_items pi
    JOIN product_packaging pk ON pk.packaging_id = pi.packaging_id
    JOIN products p ON p.product_id = pk.product_id
    WHERE pi.promotion_id = p_promotion_id
    ORDER BY 2, pk.packaging_id;
$$;


--
-- Name: fn_promotion_save(integer, text, integer, date, date, boolean, numeric, numeric, integer, integer, numeric, numeric, integer, integer[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_promotion_save(p_promotion_id integer, p_name text, p_promo_type integer, p_starts_on date, p_ends_on date, p_is_active boolean, p_new_price numeric, p_percent numeric, p_buy_qty integer, p_free_qty integer, p_group_price numeric, p_min_total numeric, p_category_id integer, p_packaging_ids integer[]) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id    integer;
    v_items integer[] := CASE WHEN p_promo_type BETWEEN 1 AND 4
                              THEN ARRAY(SELECT DISTINCT i FROM unnest(coalesce(p_packaging_ids, '{}')) i WHERE i IS NOT NULL)
                              ELSE '{}' END;
    v_msg   text;
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF p_promo_type IS NULL OR p_promo_type NOT BETWEEN 1 AND 6 THEN RAISE EXCEPTION 'Choose the type of promotion.'; END IF;
    IF p_ends_on < p_starts_on THEN RAISE EXCEPTION 'The last day is before the first day.'; END IF;

    -- Only what the type uses
    IF p_promo_type <> 1 THEN p_new_price := NULL; END IF;
    IF p_promo_type NOT IN (1, 5, 6) THEN p_percent := NULL; END IF;
    IF p_promo_type NOT IN (2, 3, 4) THEN p_buy_qty := NULL; END IF;
    IF p_promo_type <> 3 THEN p_free_qty := NULL; END IF;
    IF p_promo_type NOT IN (2, 4) THEN p_group_price := NULL; END IF;
    IF p_promo_type <> 6 THEN p_min_total := NULL; END IF;
    IF p_promo_type <> 5 THEN p_category_id := NULL; END IF;

    IF p_promo_type BETWEEN 1 AND 4 THEN
        IF cardinality(v_items) = 0 THEN RAISE EXCEPTION 'Add at least one item.'; END IF;
        IF EXISTS (SELECT 1 FROM unnest(v_items) i WHERE NOT EXISTS (SELECT 1 FROM product_packaging pk WHERE pk.packaging_id = i)) THEN
            RAISE EXCEPTION 'An item of the promotion no longer exists.';
        END IF;
    END IF;
    IF p_promo_type IN (2, 3, 4) THEN
        SELECT p.name INTO v_msg FROM product_packaging pk JOIN products p ON p.product_id = pk.product_id
        WHERE pk.packaging_id = ANY (v_items) AND p.is_weighted LIMIT 1;
        IF v_msg IS NOT NULL THEN
            RAISE EXCEPTION '"%" is sold by weight: it can have a price cut or a category %%, not a promotion by pieces.', v_msg;
        END IF;
    END IF;

    IF p_promo_type = 1 THEN
        IF (p_new_price IS NULL) = (p_percent IS NULL) THEN RAISE EXCEPTION 'Give the new price or the %% off (one of the two).'; END IF;
        IF p_new_price < 0 THEN RAISE EXCEPTION 'The new price cannot be negative.'; END IF;
    ELSIF p_promo_type IN (2, 4) THEN
        IF p_buy_qty IS NULL OR p_buy_qty < 2 THEN RAISE EXCEPTION 'The group must be 2 pieces or more.'; END IF;
        IF p_group_price IS NULL OR p_group_price <= 0 THEN RAISE EXCEPTION 'Give the price of the group.'; END IF;
    ELSIF p_promo_type = 3 THEN
        IF p_buy_qty IS NULL OR p_buy_qty < 1 THEN RAISE EXCEPTION 'Give how many pieces must be bought.'; END IF;
        IF p_free_qty IS NULL OR p_free_qty < 1 THEN RAISE EXCEPTION 'Give how many pieces are free.'; END IF;
    ELSIF p_promo_type = 5 THEN
        IF NOT EXISTS (SELECT 1 FROM categories WHERE category_id = p_category_id) THEN RAISE EXCEPTION 'Choose the category.'; END IF;
    ELSIF p_promo_type = 6 THEN
        IF p_min_total IS NULL OR p_min_total < 0 THEN RAISE EXCEPTION 'Give the invoice total the promotion starts from.'; END IF;
    END IF;
    IF (p_promo_type IN (5, 6) OR p_percent IS NOT NULL) AND (p_percent IS NULL OR p_percent <= 0 OR p_percent > 100) THEN
        RAISE EXCEPTION 'The %% off must be more than 0 and at most 100.';
    END IF;

    IF p_promotion_id IS NULL THEN
        INSERT INTO promotions (name, promo_type, starts_on, ends_on, is_active, new_price, percent, buy_qty, free_qty,
                                group_price, min_total, category_id)
        VALUES (trim(p_name), p_promo_type, p_starts_on, p_ends_on, coalesce(p_is_active, true), p_new_price, p_percent,
                p_buy_qty, p_free_qty, p_group_price, p_min_total, p_category_id)
        RETURNING promotion_id INTO v_id;
    ELSE
        UPDATE promotions
        SET name = trim(p_name), promo_type = p_promo_type, starts_on = p_starts_on, ends_on = p_ends_on,
            is_active = coalesce(p_is_active, is_active), new_price = p_new_price, percent = p_percent,
            buy_qty = p_buy_qty, free_qty = p_free_qty, group_price = p_group_price, min_total = p_min_total,
            category_id = p_category_id
        WHERE promotion_id = p_promotion_id
        RETURNING promotion_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'Promotion % does not exist.', p_promotion_id; END IF;
        DELETE FROM promotion_items WHERE promotion_id = v_id;
    END IF;

    INSERT INTO promotion_items (promotion_id, packaging_id) SELECT v_id, i FROM unnest(v_items) i;
    RETURN v_id;
END $$;


--
-- Name: fn_purchase_find_packaging(text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_find_packaging(p_search text, p_limit integer DEFAULT 50) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, factor numeric, base_unit character varying, is_weighted boolean, last_cost numeric)
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


--
-- Name: fn_purchase_get_all(integer, integer, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_get_all(p_page integer DEFAULT 1, p_page_size integer DEFAULT 50, p_search text DEFAULT NULL::text) RETURNS TABLE(purchase_id integer, purchase_no character varying, supplier_invoice_no character varying, supplier_id integer, supplier_name character varying, invoice_date date, total numeric, paid_lbp numeric, balance_lbp numeric, total_usd numeric, status smallint, status_text text, item_count bigint, currency text, total_lbp numeric, created_by_name character varying, posted_by_name character varying, doc_type smallint, return_of_no character varying, total_count bigint)
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


--
-- Name: fn_purchase_get_by_id(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_get_by_id(p_purchase_id integer) RETURNS TABLE(purchase_id integer, purchase_no character varying, supplier_invoice_no character varying, supplier_id integer, supplier_name character varying, invoice_date date, exchange_rate numeric, sub_total numeric, discount numeric, total numeric, paid_lbp numeric, status smallint, notes character varying, currency text, currency_rate numeric, total_lbp numeric, doc_type smallint, return_of_id integer, return_of_no character varying)
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


--
-- Name: fn_purchase_get_lines(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_get_lines(p_purchase_id integer) RETURNS TABLE(purchase_line_id integer, product_id integer, packaging_id integer, item_name text, packaging_name character varying, barcode character varying, quantity numeric, factor numeric, base_qty numeric, base_unit character varying, is_weighted boolean, unit_cost numeric, discount numeric, line_total numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT l.purchase_line_id, l.product_id, l.packaging_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, NULL),
           pk.name, pk.barcode,
           l.quantity, l.factor, l.base_qty, p.base_unit, p.is_weighted,
           l.unit_cost, l.discount, l.line_total
    FROM purchase_lines l
    JOIN products p           ON p.product_id = l.product_id
    JOIN product_packaging pk ON pk.packaging_id = l.packaging_id
    WHERE l.purchase_id = p_purchase_id
    ORDER BY l.purchase_line_id;
$$;


--
-- Name: fn_purchase_match_packaging(text, text, text, integer, text, numeric); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_match_packaging(p_text text, p_packaging text DEFAULT NULL::text, p_barcode text DEFAULT NULL::text, p_limit integer DEFAULT 5, p_text_alt text DEFAULT NULL::text, p_pack_size numeric DEFAULT NULL::numeric) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, factor numeric, base_unit character varying, is_weighted boolean, last_cost numeric, score numeric)
    LANGUAGE sql STABLE
    AS $$
    WITH input AS (
        SELECT fn_text_normalize(coalesce(nullif(trim(p_text), ''), '?')) AS t,
               fn_text_normalize(coalesce(nullif(trim(p_text_alt), ''), '?')) AS t2,
               lower(nullif(trim(p_packaging), '')) AS pk,
               nullif(trim(p_barcode), '') AS bc,
               CASE WHEN p_pack_size > 0 THEN p_pack_size END AS ps
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
    scored AS (
        SELECT n.*, i.pk, i.bc, i.ps,
               greatest(similarity(i.t, n.n_name), word_similarity(i.t, n.n_full), similarity(i.t, n.n_pack),
                        similarity(i.t2, n.n_name), word_similarity(i.t2, n.n_full), similarity(i.t2, n.n_pack)) AS name_sim
        FROM names n
        CROSS JOIN input i
    ),
    candidates AS (
        SELECT s.packaging_id, s.product_id, s.item_name,
               s.pack_name AS packaging_name, s.barcode, s.factor, s.base_unit, s.is_weighted,
               round(s.avg_cost * s.factor, 2) AS last_cost,
               -- The barcode read on the line: sure only when the name agrees too. A photo read slanted can give the
               -- barcode of the row above or below: then this item scores low and the item named on the line wins.
               -- Otherwise: name similarity counts for 85%, the packaging read on the invoice for 15%, so the right
               -- packaging of the right item scores highest: the one that contains the pack size read (24), else the
               -- one named like the packaging read (box / pack / bottle...).
               CASE WHEN s.bc IS NOT NULL AND s.barcode = s.bc
                    THEN CASE WHEN s.name_sim >= 0.3 THEN 1.0 ELSE 0.3 + s.name_sim END
                    ELSE least(0.99,
                         0.85 * s.name_sim
                         + CASE WHEN s.ps IS NOT NULL THEN (CASE WHEN s.factor = s.ps THEN 0.15
                                                                 WHEN s.ps > 1 AND s.factor > 1 THEN 0.03 ELSE 0 END)
                                WHEN s.pk IS NULL THEN (CASE WHEN s.is_default THEN 0.10 ELSE 0.05 END)
                                WHEN lower(s.pack_name) = s.pk OR similarity(lower(s.pack_name), s.pk) > 0.4 THEN 0.15
                                -- "pack", "carton", "case", "bp"... on the invoice = a packaging of several units
                                WHEN s.pk IN ('pack', 'packet', 'carton', 'case', 'box', 'bx', 'bp', 'dozen', 'tray', 'bundle', 'crate')
                                     AND s.factor > 1 THEN 0.12
                                ELSE 0 END)
               END::numeric(5,3) AS score
        FROM scored s
    )
    SELECT c.packaging_id, c.product_id, c.item_name, c.packaging_name, c.barcode, c.factor, c.base_unit,
           c.is_weighted, c.last_cost, greatest(c.score, 0)
    FROM candidates c
    WHERE c.score > 0.15
    ORDER BY c.score DESC, c.item_name, c.factor
    LIMIT greatest(coalesce(p_limit, 5), 1);
$$;


--
-- Name: fn_purchase_save(integer, integer, text, date, numeric, text, jsonb, boolean, integer, text, numeric, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_purchase_save(p_purchase_id integer, p_supplier_id integer, p_supplier_invoice_no text, p_invoice_date date, p_discount numeric, p_notes text, p_lines jsonb, p_post boolean, p_user_id integer, p_currency text DEFAULT NULL::text, p_currency_rate numeric DEFAULT NULL::numeric, p_doc_type integer DEFAULT 1, p_return_of_id integer DEFAULT NULL::integer) RETURNS integer
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
    IF EXISTS (SELECT 1 FROM jsonb_to_recordset(coalesce(p_lines, '[]'::jsonb)) AS x(quantity numeric, unit_cost numeric, discount numeric)
               WHERE coalesce(x.discount, 0) < 0 OR coalesce(x.discount, 0) > round(x.quantity * x.unit_cost, 2)) THEN
        RAISE EXCEPTION 'A line discount must be between 0 and the line amount.';
    END IF;

    INSERT INTO purchase_lines (purchase_id, product_id, packaging_id, quantity, factor, unit_cost, discount, line_total)
    SELECT v_id, pk.product_id, pk.packaging_id, x.quantity, pk.factor, x.unit_cost, x.discount,
           round(x.quantity * x.unit_cost - x.discount, 2)
    FROM (SELECT (e.item->>'packaging_id')::integer AS packaging_id,
                 (e.item->>'quantity')::numeric    AS quantity,
                 (e.item->>'unit_cost')::numeric   AS unit_cost,
                 coalesce((e.item->>'discount')::numeric, 0) AS discount,
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


--
-- Name: fn_quick_button_add_items(integer, integer[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_button_add_items(p_group_id integer, p_packaging_ids integer[]) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_start integer;
    v_count integer;
BEGIN
    IF p_group_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pos_quick_groups WHERE group_id = p_group_id) THEN
        RAISE EXCEPTION 'Tab % does not exist.', p_group_id;
    END IF;

    SELECT coalesce(max(sort_order), 0) INTO v_start
    FROM pos_quick_buttons WHERE group_id IS NOT DISTINCT FROM p_group_id;

    INSERT INTO pos_quick_buttons (group_id, packaging_id, sort_order)
    SELECT p_group_id, i.id, v_start + row_number() OVER (ORDER BY min(i.pos))
    FROM unnest(p_packaging_ids) WITH ORDINALITY AS i(id, pos)
    WHERE EXISTS (SELECT 1 FROM product_packaging pk WHERE pk.packaging_id = i.id)
      AND NOT EXISTS (SELECT 1 FROM pos_quick_buttons qb WHERE qb.packaging_id = i.id)
    GROUP BY i.id;

    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END $$;


--
-- Name: fn_quick_button_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_button_delete(p_button_id integer) RETURNS void
    LANGUAGE sql
    AS $$
    DELETE FROM pos_quick_buttons WHERE button_id = p_button_id;
$$;


--
-- Name: fn_quick_button_get_all(boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_button_get_all(p_for_cashier boolean DEFAULT true) RETURNS TABLE(button_id integer, group_id integer, caption character varying, color character varying, sort_order integer, is_active boolean, ask_price boolean, packaging_id integer, product_id integer, item_name text, packaging_name character varying, barcode character varying, sale_price numeric, factor numeric, is_weighted boolean, base_unit character varying, stock_qty numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT qb.button_id, qb.group_id, qb.caption, qb.color, qb.sort_order, qb.is_active, qb.ask_price,
           pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           pk.name, pk.barcode, pk.sale_price, pk.factor, p.is_weighted, p.base_unit,
           CASE WHEN p.product_type = 2
                THEN (SELECT min(floor(c.stock_qty / b.quantity)) FROM bundle_items b
                      JOIN products c ON c.product_id = b.component_product_id WHERE b.bundle_product_id = p.product_id)
                ELSE p.stock_qty END
    FROM pos_quick_buttons qb
    JOIN product_packaging pk  ON pk.packaging_id = qb.packaging_id
    JOIN products p            ON p.product_id = pk.product_id
    LEFT JOIN pos_quick_groups g ON g.group_id = qb.group_id
    WHERE NOT p_for_cashier
       OR (qb.is_active AND pk.is_active AND pk.can_sell AND p.is_active AND p.product_type IN (1, 2))
    ORDER BY g.sort_order NULLS LAST, g.group_id, qb.sort_order, qb.button_id;
$$;


--
-- Name: fn_quick_button_reorder(integer[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_button_reorder(p_button_ids integer[]) RETURNS void
    LANGUAGE sql
    AS $$
    UPDATE pos_quick_buttons qb
    SET sort_order = o.pos
    FROM unnest(p_button_ids) WITH ORDINALITY AS o(id, pos)
    WHERE qb.button_id = o.id;
$$;


--
-- Name: fn_quick_button_save(integer, integer, integer, text, text, boolean, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_button_save(p_button_id integer, p_group_id integer, p_packaging_id integer, p_caption text, p_color text, p_ask_price boolean DEFAULT false, p_is_active boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id integer;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM product_packaging WHERE packaging_id = p_packaging_id) THEN
        RAISE EXCEPTION 'The item packaging % does not exist.', p_packaging_id;
    END IF;
    IF p_group_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pos_quick_groups WHERE group_id = p_group_id) THEN
        RAISE EXCEPTION 'Tab % does not exist.', p_group_id;
    END IF;
    IF EXISTS (SELECT 1 FROM pos_quick_buttons
               WHERE packaging_id = p_packaging_id AND button_id IS DISTINCT FROM p_button_id) THEN
        RAISE EXCEPTION 'This item already has a button.';
    END IF;

    IF p_button_id IS NULL THEN
        INSERT INTO pos_quick_buttons (group_id, packaging_id, caption, color, sort_order, ask_price, is_active)
        VALUES (p_group_id, p_packaging_id, nullif(trim(p_caption), ''), nullif(trim(p_color), ''),
                (SELECT coalesce(max(sort_order), 0) + 1 FROM pos_quick_buttons WHERE group_id IS NOT DISTINCT FROM p_group_id),
                coalesce(p_ask_price, false), coalesce(p_is_active, true))
        RETURNING button_id INTO v_id;
    ELSE
        UPDATE pos_quick_buttons qb
        SET sort_order   = CASE WHEN qb.group_id IS NOT DISTINCT FROM p_group_id THEN qb.sort_order
                                ELSE (SELECT coalesce(max(x.sort_order), 0) + 1 FROM pos_quick_buttons x
                                      WHERE x.group_id IS NOT DISTINCT FROM p_group_id) END,
            group_id     = p_group_id,
            packaging_id = p_packaging_id,
            caption      = nullif(trim(p_caption), ''),
            color        = nullif(trim(p_color), ''),
            ask_price    = coalesce(p_ask_price, qb.ask_price),
            is_active    = coalesce(p_is_active, qb.is_active)
        WHERE qb.button_id = p_button_id
        RETURNING qb.button_id INTO v_id;
        IF v_id IS NULL THEN
            RAISE EXCEPTION 'Button % does not exist.', p_button_id;
        END IF;
    END IF;
    RETURN v_id;
END $$;


--
-- Name: fn_quick_group_delete(integer, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_group_delete(p_group_id integer, p_delete_buttons boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF p_delete_buttons THEN
        DELETE FROM pos_quick_buttons WHERE group_id = p_group_id;
    END IF;
    DELETE FROM pos_quick_groups WHERE group_id = p_group_id;   -- remaining buttons: group_id set null
END $$;


--
-- Name: fn_quick_group_get_all(boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_group_get_all(p_for_cashier boolean DEFAULT true) RETURNS TABLE(group_id integer, name character varying, color character varying, sort_order integer, is_active boolean, button_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT g.group_id, g.name, g.color, g.sort_order, g.is_active, count(b.button_id)
    FROM pos_quick_groups g
    LEFT JOIN (SELECT x.button_id, x.group_id FROM fn_quick_button_get_all(p_for_cashier) x) b ON b.group_id = g.group_id
    WHERE NOT p_for_cashier OR g.is_active
    GROUP BY g.group_id
    HAVING NOT p_for_cashier OR count(b.button_id) > 0
    ORDER BY g.sort_order, g.group_id;
$$;


--
-- Name: fn_quick_group_reorder(integer[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_group_reorder(p_group_ids integer[]) RETURNS void
    LANGUAGE sql
    AS $$
    UPDATE pos_quick_groups g
    SET sort_order = o.pos
    FROM unnest(p_group_ids) WITH ORDINALITY AS o(id, pos)
    WHERE g.group_id = o.id;
$$;


--
-- Name: fn_quick_group_save(integer, text, text, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_group_save(p_group_id integer, p_name text, p_color text, p_is_active boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_name text := nullif(trim(p_name), '');
    v_id   integer;
BEGIN
    IF v_name IS NULL THEN
        RAISE EXCEPTION 'The tab name is required.';
    END IF;
    IF EXISTS (SELECT 1 FROM pos_quick_groups
               WHERE lower(name) = lower(v_name) AND group_id IS DISTINCT FROM p_group_id) THEN
        RAISE EXCEPTION 'A tab named "%" already exists.', v_name;
    END IF;

    IF p_group_id IS NULL THEN
        INSERT INTO pos_quick_groups (name, color, sort_order, is_active)
        VALUES (v_name, nullif(trim(p_color), ''),
                (SELECT coalesce(max(sort_order), 0) + 1 FROM pos_quick_groups), coalesce(p_is_active, true))
        RETURNING group_id INTO v_id;
    ELSE
        UPDATE pos_quick_groups
        SET name = v_name, color = nullif(trim(p_color), ''), is_active = coalesce(p_is_active, is_active)
        WHERE group_id = p_group_id
        RETURNING group_id INTO v_id;
        IF v_id IS NULL THEN
            RAISE EXCEPTION 'Tab % does not exist.', p_group_id;
        END IF;
    END IF;
    RETURN v_id;
END $$;


--
-- Name: fn_quick_item_list(text, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_quick_item_list(p_search text DEFAULT NULL::text, p_category_id integer DEFAULT NULL::integer, p_limit integer DEFAULT 500) RETURNS TABLE(packaging_id integer, product_id integer, item_name text, category_id integer, barcode character varying, sale_price numeric, is_weighted boolean, base_unit character varying, button_tab character varying)
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_search  text := nullif(trim(p_search), '');
    v_pattern text;
    v_words   text[];
BEGIN
    IF v_search IS NOT NULL THEN
        v_pattern := replace(replace(replace(v_search, '\', '\\'), '%', '\%'), '_', '\_');
        v_words   := regexp_split_to_array(v_pattern, '\s+');
    END IF;

    RETURN QUERY
    WITH RECURSIVE tree AS (
        SELECT c.category_id FROM categories c WHERE c.category_id = p_category_id
        UNION ALL
        SELECT c.category_id FROM categories c JOIN tree t ON c.parent_id = t.category_id
    )
    SELECT pk.packaging_id, p.product_id,
           fn_item_description(p.name::text, p.size::text, p.color::text, pk.name::text),
           p.category_id, pk.barcode, pk.sale_price, p.is_weighted, p.base_unit,
           CASE WHEN qb.button_id IS NULL THEN NULL ELSE coalesce(g.name, '(no tab)'::varchar) END
    FROM product_packaging pk
    JOIN products p ON p.product_id = pk.product_id
    LEFT JOIN pos_quick_buttons qb ON qb.packaging_id = pk.packaging_id
    LEFT JOIN pos_quick_groups g   ON g.group_id = qb.group_id
    WHERE pk.is_active AND pk.can_sell AND p.is_active AND p.product_type IN (1, 2)
      AND (p_category_id IS NULL OR p.category_id IN (SELECT t.category_id FROM tree t))
      AND (v_search IS NULL
           OR NOT EXISTS (SELECT 1 FROM unnest(v_words) w WHERE p.name NOT ILIKE '%' || w || '%')
           OR pk.barcode LIKE v_pattern || '%')
    ORDER BY p.name, pk.factor, pk.packaging_id
    LIMIT greatest(coalesce(p_limit, 500), 1);
END $$;


--
-- Name: fn_receive_customer_payment(integer, numeric, text, integer, integer, integer, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_receive_customer_payment(p_customer_id integer, p_amount numeric, p_currency text, p_method integer, p_user_id integer, p_shift_id integer DEFAULT NULL::integer, p_notes text DEFAULT NULL::text) RETURNS TABLE(payment_id integer, paid_lbp numeric, balance_lbp numeric, invoices_paid integer, invoices_partly integer)
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


--
-- Name: fn_report_customer_statement(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_customer_statement(p_from date, p_to date, p_customer_id integer DEFAULT NULL::integer) RETURNS TABLE(entry_type smallint, entry_date timestamp with time zone, customer_id integer, customer character varying, reference text, details text, cashier character varying, debit numeric, credit numeric, due_lbp numeric)
    LANGUAGE sql STABLE
    AS $$
    WITH bounds AS (
        SELECT (p_from::timestamp AT TIME ZONE current_setting('TimeZone')) AS t_from,
               ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone')) AS t_to
    ),
    sales AS (
        -- Sales with an on-account part
        SELECT si.sale_id, si.invoice_no, si.invoice_date, si.customer_id, si.user_id, si.total, si.due_lbp,
               sum(p.amount_lbp) AS on_account
        FROM sales_invoices si
        JOIN sales_payments p ON p.sale_id = si.sale_id AND p.method = 3
        WHERE si.status = 1 AND si.invoice_type = 1 AND si.customer_id IS NOT NULL
          AND (p_customer_id IS NULL OR si.customer_id = p_customer_id)
        GROUP BY si.sale_id
    ),
    pays AS (
        SELECT cp.* FROM customer_payments cp
        WHERE p_customer_id IS NULL OR cp.customer_id = p_customer_id
    ),
    opening AS (
        SELECT coalesce((SELECT sum(s.on_account) FROM sales s, bounds b WHERE s.invoice_date < b.t_from), 0)
             - coalesce((SELECT sum(y.amount_lbp) FROM pays y, bounds b WHERE y.payment_date < b.t_from), 0) AS amount
    )
    SELECT e.entry_type, e.entry_date, e.customer_id, e.customer, e.reference, e.details, e.cashier, e.debit, e.credit, e.due_lbp
    FROM (
        SELECT 0::smallint AS entry_type, b.t_from AS entry_date, NULL::integer AS customer_id, NULL::varchar AS customer,
               'Owed before'::text AS reference, NULL::text AS details, NULL::varchar AS cashier,
               greatest(o.amount, 0) AS debit, greatest(-o.amount, 0) AS credit, NULL::numeric AS due_lbp, 0 AS ord
        FROM opening o, bounds b
        WHERE o.amount <> 0
        UNION ALL
        SELECT 1::smallint, s.invoice_date, s.customer_id, c.name, s.invoice_no::text,
               CASE WHEN s.on_account < s.total
                    THEN 'invoice ' || to_char(s.total, 'FM999,999,999,990.##') || ', the rest was paid at the sale' END,
               u.full_name, s.on_account, 0::numeric, s.due_lbp, s.sale_id
        FROM sales s
        CROSS JOIN bounds b
        JOIN customers c ON c.customer_id = s.customer_id
        LEFT JOIN users u ON u.user_id = s.user_id
        WHERE s.invoice_date >= b.t_from AND s.invoice_date < b.t_to
        UNION ALL
        SELECT 2::smallint, y.payment_date, y.customer_id, c.name, 'Payment #' || y.payment_id,
               trim(y.currency) || ' ' || to_char(y.amount, 'FM999,999,999,990.##') || CASE y.method WHEN 2 THEN ' card' ELSE ' cash' END
               || coalesce(' for ' || (SELECT string_agg(si.invoice_no, ', ' ORDER BY si.invoice_date, si.sale_id)
                                       FROM customer_payment_allocations a JOIN sales_invoices si ON si.sale_id = a.sale_id
                                       WHERE a.payment_id = y.payment_id), ''),
               u.full_name, 0::numeric, y.amount_lbp, NULL::numeric, y.payment_id
        FROM pays y
        CROSS JOIN bounds b
        JOIN customers c ON c.customer_id = y.customer_id
        LEFT JOIN users u ON u.user_id = y.user_id
        WHERE y.payment_date >= b.t_from AND y.payment_date < b.t_to
    ) e
    ORDER BY e.entry_date, e.entry_type, e.ord;
$$;


--
-- Name: fn_report_daily_sales(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_daily_sales(p_from date, p_to date, p_user_id integer DEFAULT NULL::integer) RETURNS TABLE(day date, sale_count bigint, return_count bigint, gross numeric, discounts numeric, sales numeric, returns numeric, net_sales numeric, cost numeric, profit numeric)
    LANGUAGE sql STABLE
    AS $$
    WITH inv AS (
        -- The invoices of the period (the time range uses the invoice_date index)
        SELECT si.sale_id, si.invoice_type, si.total, si.total_cost, si.profit, si.discount,
               (si.invoice_date AT TIME ZONE current_setting('TimeZone'))::date AS day
        FROM sales_invoices si
        WHERE si.status = 1
          AND si.invoice_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND si.invoice_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND (p_user_id IS NULL OR si.user_id = p_user_id)
    ),
    line_disc AS (
        SELECT l.sale_id, sum(l.discount) AS discount
        FROM sales_lines l JOIN inv ON inv.sale_id = l.sale_id AND inv.invoice_type = 1
        GROUP BY l.sale_id
    ),
    per_day AS (
        SELECT inv.day,
               count(*) FILTER (WHERE inv.invoice_type = 1) AS sale_count,
               count(*) FILTER (WHERE inv.invoice_type = 2) AS return_count,
               coalesce(sum(inv.total + inv.discount + coalesce(ld.discount, 0)) FILTER (WHERE inv.invoice_type = 1), 0) AS gross,
               coalesce(sum(inv.discount + coalesce(ld.discount, 0)) FILTER (WHERE inv.invoice_type = 1), 0) AS discounts,
               coalesce(sum(inv.total) FILTER (WHERE inv.invoice_type = 1), 0) AS sales,
               coalesce(sum(inv.total) FILTER (WHERE inv.invoice_type = 2), 0) AS returns,
               coalesce(sum(CASE WHEN inv.invoice_type = 2 THEN -inv.total_cost ELSE inv.total_cost END), 0) AS cost
        FROM inv LEFT JOIN line_disc ld ON ld.sale_id = inv.sale_id
        GROUP BY inv.day
    )
    SELECT d::date, coalesce(p.sale_count, 0), coalesce(p.return_count, 0),
           coalesce(p.gross, 0), coalesce(p.discounts, 0), coalesce(p.sales, 0), coalesce(p.returns, 0),
           coalesce(p.sales, 0) - coalesce(p.returns, 0),
           coalesce(p.cost, 0),
           coalesce(p.sales, 0) - coalesce(p.returns, 0) - coalesce(p.cost, 0)
    -- timestamp without time zone: with a time zone, the daylight-saving change (end of March) shifts
    -- every following day to 01:00 and the last day of the period would be lost
    FROM generate_series(p_from::timestamp, p_to::timestamp, interval '1 day') d
    LEFT JOIN per_day p ON p.day = d::date
    ORDER BY d;
$$;


--
-- Name: fn_report_purchases_by_invoice(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_purchases_by_invoice(p_from date, p_to date, p_supplier_id integer DEFAULT NULL::integer) RETURNS TABLE(purchase_id integer, purchase_no character varying, doc_type smallint, supplier_invoice_no character varying, invoice_date date, supplier character varying, item_count bigint, currency text, total numeric, discount_lbp numeric, total_lbp numeric, paid_lbp numeric, owed_lbp numeric, entered_by character varying, return_of character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT pi.purchase_id, pi.purchase_no, pi.doc_type, pi.supplier_invoice_no, pi.invoice_date,
           s.name,
           (SELECT count(*) FROM purchase_lines pl WHERE pl.purchase_id = pi.purchase_id),
           trim(pi.currency), pi.total * x.sign,
           round(pi.discount * pi.currency_rate, 2) * x.sign,
           pi.total_lbp * x.sign,
           CASE WHEN pi.doc_type = 1 THEN pi.paid_lbp ELSE 0 END,
           CASE WHEN pi.doc_type = 1 THEN greatest(pi.total_lbp - pi.paid_lbp, 0) ELSE 0 END,
           u.full_name,
           ro.purchase_no
    FROM purchase_invoices pi
    CROSS JOIN LATERAL (SELECT CASE WHEN pi.doc_type = 2 THEN -1 ELSE 1 END AS sign) x
    LEFT JOIN suppliers s ON s.supplier_id = pi.supplier_id
    LEFT JOIN users u ON u.user_id = pi.user_id
    LEFT JOIN purchase_invoices ro ON ro.purchase_id = pi.return_of_id
    WHERE pi.status = 1
      AND pi.invoice_date BETWEEN p_from AND p_to
      AND (p_supplier_id IS NULL OR pi.supplier_id = p_supplier_id)
    ORDER BY pi.invoice_date, pi.purchase_id;
$$;


--
-- Name: fn_report_purchases_by_item(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_purchases_by_item(p_from date, p_to date, p_supplier_id integer DEFAULT NULL::integer) RETURNS TABLE(product_id integer, item_name text, category_name character varying, base_unit character varying, qty_bought numeric, qty_returned numeric, qty numeric, cost numeric, avg_unit_cost numeric, last_unit_cost numeric, min_unit_cost numeric, max_unit_cost numeric, avg_cost_now numeric, invoice_count bigint, last_bought date, suppliers text)
    LANGUAGE sql STABLE
    AS $$
    WITH lines AS (
        SELECT pl.product_id, pi.purchase_id, pi.doc_type, pi.invoice_date, pi.supplier_id, pl.base_qty,
               pl.line_total * pi.currency_rate
                   * CASE WHEN pi.sub_total > 0 THEN pi.total / pi.sub_total ELSE 1 END AS cost
        FROM purchase_invoices pi
        JOIN purchase_lines pl ON pl.purchase_id = pi.purchase_id
        WHERE pi.status = 1
          AND pi.invoice_date BETWEEN p_from AND p_to
          AND (p_supplier_id IS NULL OR pi.supplier_id = p_supplier_id)
          AND pl.base_qty > 0
    ),
    per_item AS (
        SELECT l.product_id,
               coalesce(sum(l.base_qty) FILTER (WHERE l.doc_type = 1), 0) AS qty_bought,
               coalesce(sum(l.base_qty) FILTER (WHERE l.doc_type = 2), 0) AS qty_returned,
               sum(CASE WHEN l.doc_type = 2 THEN -l.cost ELSE l.cost END) AS cost,
               min(l.cost / l.base_qty) FILTER (WHERE l.doc_type = 1) AS min_unit_cost,
               max(l.cost / l.base_qty) FILTER (WHERE l.doc_type = 1) AS max_unit_cost,
               count(DISTINCT l.purchase_id) FILTER (WHERE l.doc_type = 1) AS invoice_count,
               max(l.invoice_date) FILTER (WHERE l.doc_type = 1) AS last_bought,
               (array_agg(l.cost / l.base_qty ORDER BY l.invoice_date DESC, l.purchase_id DESC) FILTER (WHERE l.doc_type = 1))[1] AS last_unit_cost,
               string_agg(DISTINCT s.name, ', ') AS suppliers
        FROM lines l
        LEFT JOIN suppliers s ON s.supplier_id = l.supplier_id
        GROUP BY l.product_id
    )
    SELECT p.product_id,
           p.name || coalesce(' ' || nullif(p.size, ''), '') || coalesce(' ' || nullif(p.color, ''), ''),
           c.name, p.base_unit,
           i.qty_bought, i.qty_returned, i.qty_bought - i.qty_returned,
           round(i.cost, 2),
           CASE WHEN i.qty_bought - i.qty_returned > 0 THEN round(i.cost / (i.qty_bought - i.qty_returned), 2) END,
           round(i.last_unit_cost, 2), round(i.min_unit_cost, 2), round(i.max_unit_cost, 2), round(p.avg_cost, 2),
           i.invoice_count, i.last_bought, i.suppliers
    FROM per_item i
    JOIN products p ON p.product_id = i.product_id
    LEFT JOIN categories c ON c.category_id = p.category_id
    ORDER BY i.cost DESC, p.name;
$$;


--
-- Name: fn_report_purchases_by_supplier(date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_purchases_by_supplier(p_from date, p_to date) RETURNS TABLE(supplier_id integer, supplier character varying, phone character varying, invoice_count bigint, purchases numeric, returns numeric, net numeric, discounts numeric, paid numeric, last_purchase date, owed_now numeric)
    LANGUAGE sql STABLE
    AS $$
    WITH bought AS (
        SELECT pi.supplier_id,
               count(*) FILTER (WHERE pi.doc_type = 1) AS invoice_count,
               coalesce(sum(pi.total_lbp) FILTER (WHERE pi.doc_type = 1), 0) AS purchases,
               coalesce(sum(pi.total_lbp) FILTER (WHERE pi.doc_type = 2), 0) AS returns,
               coalesce(sum(round(pi.discount * pi.currency_rate, 2)) FILTER (WHERE pi.doc_type = 1), 0) AS discounts,
               max(pi.invoice_date) FILTER (WHERE pi.doc_type = 1) AS last_purchase
        FROM purchase_invoices pi
        WHERE pi.status = 1 AND pi.invoice_date BETWEEN p_from AND p_to
        GROUP BY pi.supplier_id
    ),
    paid AS (
        SELECT sp.supplier_id, sum(sp.amount_lbp) AS paid
        FROM supplier_payments sp
        WHERE sp.payment_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND sp.payment_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
        GROUP BY sp.supplier_id
    ),
    ids AS (
        SELECT b.supplier_id FROM bought b
        UNION
        SELECT y.supplier_id FROM paid y
    )
    SELECT ids.supplier_id, coalesce(s.name, '(no supplier)'::varchar), s.phone,
           coalesce(b.invoice_count, 0), coalesce(b.purchases, 0), coalesce(b.returns, 0),
           coalesce(b.purchases, 0) - coalesce(b.returns, 0),
           coalesce(b.discounts, 0), coalesce(y.paid, 0),
           b.last_purchase, s.balance_lbp
    FROM ids
    LEFT JOIN bought b ON b.supplier_id IS NOT DISTINCT FROM ids.supplier_id
    LEFT JOIN paid y ON y.supplier_id = ids.supplier_id
    LEFT JOIN suppliers s ON s.supplier_id = ids.supplier_id
    ORDER BY coalesce(b.purchases, 0) - coalesce(b.returns, 0) DESC, s.name NULLS LAST;
$$;


--
-- Name: fn_report_sales_by_invoice(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_sales_by_invoice(p_from date, p_to date, p_user_id integer DEFAULT NULL::integer) RETURNS TABLE(sale_id integer, invoice_no character varying, invoice_type smallint, invoice_date timestamp with time zone, cashier character varying, customer character varying, item_count bigint, quantity numeric, gross numeric, discounts numeric, total numeric, cost numeric, profit numeric, payments text)
    LANGUAGE sql STABLE
    AS $$
    SELECT si.sale_id, si.invoice_no, si.invoice_type, si.invoice_date,
           u.full_name,
           CASE WHEN cu.customer_id <> 1 THEN cu.name END,   -- 1 = walk-in customer
           l.item_count,
           l.quantity * s.sign,
           round(l.gross * s.sign, 2),
           round((l.gross - si.total) * s.sign, 2),
           si.total * s.sign,
           si.total_cost * s.sign,
           si.profit * s.sign,
           (SELECT string_agg(trim(p.currency) || ' ' || to_char(p.amount, 'FM999,999,999,990.##')
                              || CASE p.method WHEN 2 THEN ' card' WHEN 3 THEN ' account' ELSE '' END, ' + ' ORDER BY p.payment_id)
            FROM sales_payments p WHERE p.sale_id = si.sale_id)
    FROM sales_invoices si
    CROSS JOIN LATERAL (SELECT CASE WHEN si.invoice_type = 2 THEN -1 ELSE 1 END AS sign) s
    CROSS JOIN LATERAL (SELECT count(*) AS item_count, coalesce(sum(sl.quantity), 0) AS quantity,
                               coalesce(sum(sl.quantity * sl.unit_price), 0) AS gross
                        FROM sales_lines sl WHERE sl.sale_id = si.sale_id) l
    LEFT JOIN users u ON u.user_id = si.user_id
    LEFT JOIN customers cu ON cu.customer_id = si.customer_id
    WHERE si.status = 1
      AND si.invoice_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
      AND si.invoice_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
      AND (p_user_id IS NULL OR si.user_id = p_user_id)
    ORDER BY si.invoice_date, si.sale_id;
$$;


--
-- Name: fn_report_sales_by_item(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_sales_by_item(p_from date, p_to date, p_user_id integer DEFAULT NULL::integer) RETURNS TABLE(product_id integer, item_name text, category_name character varying, base_unit character varying, qty_sold numeric, qty_returned numeric, net_qty numeric, gross numeric, discounts numeric, net_sales numeric, cost numeric, profit numeric, invoice_count bigint)
    LANGUAGE sql STABLE
    AS $$
    WITH lines AS (
        SELECT l.product_id, si.sale_id, si.invoice_type,
               CASE WHEN si.invoice_type = 2 THEN -1 ELSE 1 END AS sign,
               l.base_qty, l.quantity * l.unit_price AS gross, l.line_total, l.line_profit
        FROM sales_invoices si
        JOIN sales_lines l ON l.sale_id = si.sale_id
        WHERE si.status = 1
          AND si.invoice_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND si.invoice_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND (p_user_id IS NULL OR si.user_id = p_user_id)
    ),
    per_item AS (
        SELECT product_id,
               sum(base_qty) FILTER (WHERE invoice_type = 1) AS qty_sold,
               sum(base_qty) FILTER (WHERE invoice_type = 2) AS qty_returned,
               sum(gross * sign) AS gross,
               sum((gross - line_total) * sign) AS discounts,
               sum(line_total * sign) AS net_sales,
               sum((line_total - line_profit) * sign) AS cost,
               count(DISTINCT sale_id) FILTER (WHERE invoice_type = 1) AS invoice_count
        FROM lines
        GROUP BY product_id
    )
    SELECT p.product_id,
           p.name || coalesce(' ' || nullif(p.size, ''), '') || coalesce(' ' || nullif(p.color, ''), ''),
           c.name, p.base_unit,
           coalesce(i.qty_sold, 0), coalesce(i.qty_returned, 0), coalesce(i.qty_sold, 0) - coalesce(i.qty_returned, 0),
           round(i.gross, 2), round(i.discounts, 2), round(i.net_sales, 2), round(i.cost, 2),
           round(i.net_sales - i.cost, 2), i.invoice_count
    FROM per_item i
    JOIN products p ON p.product_id = i.product_id
    LEFT JOIN categories c ON c.category_id = p.category_id
    ORDER BY i.net_sales DESC, p.name;
$$;


--
-- Name: fn_report_sales_invoice_lines(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_sales_invoice_lines(p_from date, p_to date, p_user_id integer DEFAULT NULL::integer) RETURNS TABLE(sale_id integer, line_no integer, item_name character varying, quantity numeric, unit_price numeric, discount numeric, line_total numeric, line_profit numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT l.sale_id, l.line_no, l.description,
           l.quantity * s.sign, l.unit_price,
           (l.discount + l.discount_share) * s.sign,
           l.line_total * s.sign,
           l.line_profit * s.sign
    FROM sales_invoices si
    CROSS JOIN LATERAL (SELECT CASE WHEN si.invoice_type = 2 THEN -1 ELSE 1 END AS sign) s
    JOIN sales_lines l ON l.sale_id = si.sale_id
    WHERE si.status = 1
      AND si.invoice_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
      AND si.invoice_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
      AND (p_user_id IS NULL OR si.user_id = p_user_id)
    ORDER BY si.invoice_date, si.sale_id, l.line_no;
$$;


--
-- Name: fn_report_sales_unpaid(date, date, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_sales_unpaid(p_from date, p_to date, p_user_id integer DEFAULT NULL::integer) RETURNS TABLE(day date, sale_count bigint, sales numeric, returns numeric, net_sales numeric, on_account numeric, unpaid numeric, unpaid_count bigint)
    LANGUAGE sql STABLE
    AS $$
    WITH inv AS (
        SELECT si.sale_id, si.invoice_type, si.total, si.due_lbp,
               (si.invoice_date AT TIME ZONE current_setting('TimeZone'))::date AS day
        FROM sales_invoices si
        WHERE si.status = 1
          AND si.invoice_date >= (p_from::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND si.invoice_date <  ((p_to + 1)::timestamp AT TIME ZONE current_setting('TimeZone'))
          AND (p_user_id IS NULL OR si.user_id = p_user_id)
    ),
    acct AS (
        SELECT p.sale_id, sum(p.amount_lbp) AS on_account
        FROM sales_payments p JOIN inv ON inv.sale_id = p.sale_id AND inv.invoice_type = 1
        WHERE p.method = 3
        GROUP BY p.sale_id
    ),
    per_day AS (
        SELECT inv.day,
               count(*) FILTER (WHERE inv.invoice_type = 1) AS sale_count,
               coalesce(sum(inv.total) FILTER (WHERE inv.invoice_type = 1), 0) AS sales,
               coalesce(sum(inv.total) FILTER (WHERE inv.invoice_type = 2), 0) AS returns,
               coalesce(sum(a.on_account), 0) AS on_account,
               coalesce(sum(inv.due_lbp), 0) AS unpaid,
               count(*) FILTER (WHERE inv.due_lbp > 0) AS unpaid_count
        FROM inv LEFT JOIN acct a ON a.sale_id = inv.sale_id
        GROUP BY inv.day
    )
    SELECT d::date, coalesce(p.sale_count, 0), coalesce(p.sales, 0), coalesce(p.returns, 0),
           coalesce(p.sales, 0) - coalesce(p.returns, 0),
           coalesce(p.on_account, 0), coalesce(p.unpaid, 0), coalesce(p.unpaid_count, 0)
    FROM generate_series(p_from::timestamp, p_to::timestamp, interval '1 day') d
    LEFT JOIN per_day p ON p.day = d::date
    ORDER BY d;
$$;


--
-- Name: fn_report_stock_value(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_stock_value() RETURNS TABLE(product_id integer, item_name text, category_name character varying, base_unit character varying, is_active boolean, stock_qty numeric, avg_cost numeric, sale_price numeric, total_cost numeric, total_sale numeric, profit numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT p.product_id,
           p.name || coalesce(' ' || nullif(p.size, ''), '') || coalesce(' ' || nullif(p.color, ''), ''),
           c.name, p.base_unit, p.is_active,
           p.stock_qty, round(p.avg_cost, 2), round(coalesce(pr.price, 0), 2),
           round(p.stock_qty * p.avg_cost, 2),
           round(p.stock_qty * coalesce(pr.price, 0), 2),
           round(p.stock_qty * coalesce(pr.price, 0), 2) - round(p.stock_qty * p.avg_cost, 2)
    FROM products p
    LEFT JOIN categories c ON c.category_id = p.category_id
    LEFT JOIN LATERAL (
        SELECT pk.sale_price / pk.factor AS price
        FROM product_packaging pk
        WHERE pk.product_id = p.product_id AND pk.is_active AND pk.can_sell AND pk.sale_price > 0
        ORDER BY (pk.factor = 1) DESC, pk.is_default DESC, pk.factor, pk.packaging_id
        LIMIT 1) pr ON true
    WHERE p.product_type = 1 AND p.stock_qty <> 0
    ORDER BY p.stock_qty * p.avg_cost DESC, p.name;
$$;


--
-- Name: fn_report_unpaid_by_customer(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_report_unpaid_by_customer() RETURNS TABLE(customer_id integer, name character varying, phone character varying, invoice_count bigint, oldest_unpaid timestamp with time zone, unpaid numeric, credit_limit_lbp numeric, last_payment timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT c.customer_id, c.name, c.phone,
           coalesce(o.invoice_count, 0), o.oldest_unpaid,
           c.balance_lbp, c.credit_limit_lbp,
           (SELECT max(cp.payment_date) FROM customer_payments cp WHERE cp.customer_id = c.customer_id)
    FROM customers c
    LEFT JOIN LATERAL (SELECT count(*) AS invoice_count, min(si.invoice_date) AS oldest_unpaid
                       FROM sales_invoices si
                       WHERE si.customer_id = c.customer_id AND si.status = 1 AND si.invoice_type = 1 AND si.due_lbp > 0) o ON true
    WHERE c.balance_lbp > 0
    ORDER BY c.balance_lbp DESC, lower(c.name);
$$;


--
-- Name: fn_sale_get_lines(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_sale_get_lines(p_sale_id integer) RETURNS TABLE(line_no integer, item_name character varying, quantity numeric, unit_price numeric, discount numeric, line_total numeric, line_profit numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT l.line_no, l.description, l.quantity, l.unit_price, l.discount + l.discount_share, l.line_total, l.line_profit
    FROM sales_lines l
    WHERE l.sale_id = p_sale_id
    ORDER BY l.line_no;
$$;


--
-- Name: fn_sale_receipt(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_sale_receipt(p_sale_id integer) RETURNS jsonb
    LANGUAGE sql STABLE
    AS $$
    SELECT jsonb_build_object(
        'company', (SELECT jsonb_build_object('name', c.name, 'address', c.address, 'mobile', c.mobile,
                                              'email', c.email, 'vat_number', c.vat_number)
                    FROM company c ORDER BY c.company_id LIMIT 1),
        'invoice_no', si.invoice_no,
        'invoice_type', si.invoice_type,
        'invoice_date', si.invoice_date,
        'cashier', u.full_name,
        'customer', CASE WHEN cu.customer_id IS NOT NULL AND cu.customer_id <> 1 THEN cu.name END,
        'customer_phone', CASE WHEN cu.customer_id <> 1 THEN cu.phone END,
        'customer_address', CASE WHEN cu.customer_id <> 1 THEN cu.address END,
        'sub_total', si.sub_total,
        'line_discounts', (SELECT coalesce(sum(l.discount), 0) FROM sales_lines l WHERE l.sale_id = si.sale_id),
        'discount', si.discount,
        'promo_name', si.promo_name,
        'total', si.total,
        'change_lbp', si.change_lbp,
        'usd_rate', si.exchange_rate,
        'primary_code', (SELECT code FROM currencies WHERE is_primary),
        'lines', coalesce((SELECT jsonb_agg(jsonb_build_object(
                                'item_name', l.description, 'quantity', l.quantity, 'unit_price', l.unit_price,
                                'discount', l.discount, 'promo_name', l.promo_name, 'line_total', round(l.quantity * l.unit_price - l.discount, 2))
                            ORDER BY l.line_no)
                           FROM sales_lines l WHERE l.sale_id = si.sale_id), '[]'::jsonb),
        'payments', coalesce((SELECT jsonb_agg(jsonb_build_object(
                                'method', p.method, 'currency', trim(p.currency), 'amount', p.amount, 'amount_lbp', p.amount_lbp)
                            ORDER BY p.payment_id)
                           FROM sales_payments p WHERE p.sale_id = si.sale_id), '[]'::jsonb))
    FROM sales_invoices si
    LEFT JOIN users u ON u.user_id = si.user_id
    LEFT JOIN customers cu ON cu.customer_id = si.customer_id
    WHERE si.sale_id = p_sale_id;
$$;


--
-- Name: fn_sales_by_user(integer, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_sales_by_user(p_user_id integer, p_from date, p_to date) RETURNS TABLE(sale_id integer, invoice_no character varying, invoice_type smallint, invoice_date timestamp with time zone, cashier character varying, item_count bigint, total numeric, profit numeric, payments text, customer character varying, on_account_lbp numeric, due_lbp numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT si.sale_id, si.invoice_no, si.invoice_type, si.invoice_date, u.full_name,
           (SELECT count(*) FROM sales_lines l WHERE l.sale_id = si.sale_id),
           si.total, si.profit,
           (SELECT string_agg(trim(p.currency) || ' ' || to_char(p.amount, 'FM999,999,999,990.##')
                              || CASE p.method WHEN 2 THEN ' card' WHEN 3 THEN ' account' ELSE '' END, ' + ' ORDER BY p.payment_id)
            FROM sales_payments p WHERE p.sale_id = si.sale_id),
           CASE WHEN cu.customer_id <> 1 THEN cu.name END,   -- 1 = walk-in customer
           coalesce((SELECT sum(p.amount_lbp) FROM sales_payments p WHERE p.sale_id = si.sale_id AND p.method = 3), 0),
           si.due_lbp
    FROM sales_invoices si
    LEFT JOIN users u ON u.user_id = si.user_id
    LEFT JOIN customers cu ON cu.customer_id = si.customer_id
    WHERE si.status = 1
      AND (p_user_id IS NULL OR si.user_id = p_user_id)
      AND si.invoice_date >= p_from::timestamptz
      AND si.invoice_date <  (p_to + 1)::timestamptz
    ORDER BY si.invoice_date DESC, si.sale_id DESC;
$$;


--
-- Name: fn_save_return(integer, jsonb, integer, text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_save_return(p_user_id integer, p_lines jsonb, p_refund_method integer DEFAULT 1, p_refund_currency text DEFAULT 'LBP'::text, p_shift_id integer DEFAULT NULL::integer) RETURNS TABLE(return_id integer, invoice_no text, refund_lbp numeric, refund_amount numeric)
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


--
-- Name: fn_save_sale(integer, jsonb, jsonb, integer, integer, numeric, integer, numeric, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_save_sale(p_user_id integer, p_lines jsonb, p_payments jsonb, p_shift_id integer DEFAULT NULL::integer, p_customer_id integer DEFAULT NULL::integer, p_invoice_discount numeric DEFAULT 0, p_held_sale_id integer DEFAULT NULL::integer, p_promo_discount numeric DEFAULT 0, p_promo_name text DEFAULT NULL::text) RETURNS TABLE(sale_id integer, invoice_no text, total_lbp numeric, change_lbp numeric)
    LANGUAGE plpgsql
    AS $$
#variable_conflict use_column
DECLARE
    v_rate       numeric := fn_current_rate();
    v_disc       numeric := coalesce(p_invoice_discount, 0);
    v_promo      numeric := coalesce(p_promo_discount, 0);
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
           coalesce((x.j->>'promo_discount')::numeric, 0)::numeric(18,2)        AS promo_discount,
           nullif(trim(x.j->>'promo_name'), '')                   AS promo_name,
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
                                   OR discount > quantity * unit_price
                                   OR promo_discount < 0 OR promo_discount > discount) THEN
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
    IF v_disc < 0 OR v_total < 0 OR v_promo < 0 OR v_promo > v_disc THEN RAISE EXCEPTION 'The invoice discount is not valid.'; END IF;

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
                                sub_total, discount, total, total_cost, profit, change_lbp, status, due_lbp,
                                promo_discount, promo_name)
    VALUES (v_no, 1, p_customer_id, p_shift_id, p_user_id, v_rate,
            v_sub, v_disc, v_total, v_cost, v_total - v_cost, v_change, 1,
            v_on_account,   -- unpaid until the client pays (Database/customer_payment_functions.sql)
            v_promo, CASE WHEN v_promo > 0 THEN nullif(trim(p_promo_name), '') END)
    RETURNING sale_id INTO v_sale_id;

    INSERT INTO sales_lines (sale_id, line_no, product_id, packaging_id, description, quantity, factor,
                             unit_price, discount, discount_share, unit_cost, line_total, line_profit,
                             promo_discount, promo_name)
    SELECT v_sale_id, line_no, product_id, packaging_id, description, quantity, factor,
           unit_price, discount, disc_share, unit_cost,
           gross - disc_share,
           gross - disc_share - round(quantity * unit_cost, 2),
           promo_discount, CASE WHEN promo_discount > 0 THEN promo_name END
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
END $$;


--
-- Name: fn_save_sale_return(integer, integer, jsonb, integer, text, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_save_sale_return(p_original_sale_id integer, p_user_id integer, p_lines jsonb, p_refund_method integer DEFAULT 1, p_refund_currency text DEFAULT 'LBP'::text, p_shift_id integer DEFAULT NULL::integer) RETURNS TABLE(return_id integer, invoice_no text, refund_lbp numeric, refund_amount numeric)
    LANGUAGE plpgsql
    AS $$
#variable_conflict use_column
DECLARE
    v_orig   sales_invoices%ROWTYPE;
    v_rate   numeric := fn_current_rate();
    v_count  int;
    v_refund numeric;
    v_cost   numeric;
    v_amount numeric;
    v_id     int;
    v_no     text;
    v_msg    text;
BEGIN
    SELECT * INTO v_orig FROM sales_invoices WHERE sale_id = p_original_sale_id FOR UPDATE;
    IF NOT FOUND OR v_orig.invoice_type <> 1 OR v_orig.status <> 1 THEN
        RAISE EXCEPTION 'Only a completed sale can be returned.';
    END IF;
    IF p_refund_currency NOT IN ('LBP', 'USD') OR p_refund_method NOT IN (1, 2, 3) THEN
        RAISE EXCEPTION 'Refund method or currency is not valid.';
    END IF;
    -- A sale bought on account is refunded to the account, never in cash
    IF EXISTS (SELECT 1 FROM sales_payments WHERE sale_id = p_original_sale_id AND method = 3) AND p_refund_method <> 3 THEN
        RAISE EXCEPTION 'This sale was on account: the refund must go back to the customer account.';
    END IF;
    IF p_refund_method = 3 AND v_orig.customer_id IS NULL THEN
        RAISE EXCEPTION 'On-account refund needs a customer on the original sale.';
    END IF;
    IF p_shift_id IS NOT NULL THEN
        PERFORM 1 FROM shifts WHERE shift_id = p_shift_id AND status = 1 FOR SHARE;
        IF NOT FOUND THEN RAISE EXCEPTION 'This shift is closed. Open a new shift first.'; END IF;
    END IF;
    IF p_lines IS NULL OR jsonb_array_length(p_lines) = 0 THEN RAISE EXCEPTION 'Choose the items to return.'; END IF;

    IF to_regclass('pg_temp._r') IS NOT NULL THEN DROP TABLE pg_temp._r; END IF;
    CREATE TEMP TABLE _r ON COMMIT DROP AS
    SELECT x.ord::int AS line_no, sl.sale_line_id, sl.product_id, sl.packaging_id, sl.description,
           (x.j->>'quantity')::numeric(18,3) AS quantity, sl.factor, sl.unit_price, sl.unit_cost,
           sl.quantity AS sold_qty,
           coalesce((SELECT sum(rl.quantity) FROM sales_lines rl
                     JOIN sales_invoices ri ON ri.sale_id = rl.sale_id AND ri.status = 1
                     WHERE rl.return_of_line_id = sl.sale_line_id), 0) AS returned_qty,
           -- the line total already carries its share of the invoice discount
           round(sl.line_total / sl.quantity * (x.j->>'quantity')::numeric, 2) AS refund,
           p.product_type, p.is_weighted
    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS x(j, ord)
    JOIN sales_lines sl ON sl.sale_line_id = (x.j->>'sale_line_id')::int AND sl.sale_id = p_original_sale_id
    JOIN products p ON p.product_id = sl.product_id;

    SELECT count(*) INTO v_count FROM _r;
    IF v_count <> jsonb_array_length(p_lines) THEN RAISE EXCEPTION 'A returned line does not belong to this sale.'; END IF;
    IF EXISTS (SELECT 1 FROM _r WHERE quantity IS NULL OR quantity <= 0 OR quantity > sold_qty - returned_qty) THEN
        RAISE EXCEPTION 'Return quantity is more than what is left to return.';
    END IF;
    SELECT description INTO v_msg FROM _r WHERE NOT is_weighted AND quantity * factor <> trunc(quantity * factor) LIMIT 1;
    IF v_msg IS NOT NULL THEN RAISE EXCEPTION '"%" can only be returned in whole units.', v_msg; END IF;

    SELECT sum(refund), sum(round(quantity * unit_cost, 2)) INTO v_refund, v_cost FROM _r;
    v_amount := CASE WHEN p_refund_currency = 'USD' THEN round(v_refund / v_rate, 2) ELSE v_refund END;

    v_no := fn_next_doc_no('R');

    INSERT INTO sales_invoices (invoice_no, invoice_type, original_sale_id, customer_id, shift_id, user_id,
                                exchange_rate, sub_total, discount, total, total_cost, profit, status)
    VALUES (v_no, 2, p_original_sale_id, v_orig.customer_id, p_shift_id, p_user_id,
            v_rate, v_refund, 0, v_refund, v_cost, v_refund - v_cost, 1)
    RETURNING sale_id INTO v_id;

    INSERT INTO sales_lines (sale_id, line_no, product_id, packaging_id, description, quantity, factor,
                             unit_price, discount, unit_cost, line_total, line_profit, return_of_line_id)
    SELECT v_id, line_no, product_id, packaging_id, description, quantity, factor,
           unit_price, 0, unit_cost, refund, refund - round(quantity * unit_cost, 2), sale_line_id
    FROM _r ORDER BY line_no;

    INSERT INTO sales_payments (sale_id, method, currency, amount, amount_lbp)
    VALUES (v_id, p_refund_method, p_refund_currency, v_amount, v_refund);

    IF p_refund_method = 3 THEN
        UPDATE customers SET balance_lbp = balance_lbp - v_refund WHERE customer_id = v_orig.customer_id;
    END IF;

    -- Stock back IN. Bundles put back exactly the components that were sold (saved at sale time).
    IF to_regclass('pg_temp._back') IS NOT NULL THEN DROP TABLE pg_temp._back; END IF;
    CREATE TEMP TABLE _back ON COMMIT DROP AS
    SELECT product_id, sum(qty)::numeric(18,3) AS qty FROM (
        SELECT product_id, quantity * factor AS qty FROM _r WHERE product_type = 1
        UNION ALL
        SELECT c.product_id, r.quantity * c.base_qty_per_unit
        FROM _r r JOIN sales_line_components c ON c.sale_line_id = r.sale_line_id
        WHERE r.product_type = 2
    ) s GROUP BY product_id;

    PERFORM 1 FROM products WHERE product_id IN (SELECT product_id FROM _back) ORDER BY product_id FOR UPDATE;

    UPDATE products p SET stock_qty = p.stock_qty + b.qty, updated_at = now()
    FROM _back b WHERE p.product_id = b.product_id;

    INSERT INTO stock_movements (product_id, movement_type, quantity, unit_cost, balance_after, ref_type, ref_id, user_id)
    SELECT b.product_id, 3, b.qty, p.avg_cost, p.stock_qty, 2, v_id, p_user_id
    FROM _back b JOIN products p ON p.product_id = b.product_id;

    RETURN QUERY SELECT v_id, v_no, v_refund, v_amount;
END $$;


--
-- Name: fn_setting(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_setting(p_key text, p_default text DEFAULT NULL::text) RETURNS text
    LANGUAGE sql STABLE
    AS $$
    SELECT coalesce((SELECT value FROM settings WHERE key = p_key), p_default);
$$;


--
-- Name: fn_setting_set(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_setting_set(p_key text, p_value text) RETURNS void
    LANGUAGE sql
    AS $$
    INSERT INTO settings (key, value) VALUES (p_key, coalesce(p_value, ''))
    ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value;
$$;


--
-- Name: fn_shift_current(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_shift_current(p_user_id integer) RETURNS integer
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


--
-- Name: fn_shift_list(date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_shift_list(p_from date, p_to date) RETURNS TABLE(shift_id integer, user_id integer, user_name character varying, opened_at timestamp with time zone, closed_at timestamp with time zone, status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric, profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric, opening_cash_lbp numeric, opening_cash_usd numeric, expected_cash_lbp numeric, expected_cash_usd numeric, counted_cash_lbp numeric, counted_cash_usd numeric, difference_lbp numeric, difference_usd numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.*
    FROM shifts sh
    CROSS JOIN LATERAL fn_shift_summary(sh.shift_id) s
    WHERE sh.status = 1
       OR (sh.opened_at AT TIME ZONE current_setting('TimeZone'))::date BETWEEN p_from AND p_to
    ORDER BY sh.status, sh.opened_at DESC;
$$;


--
-- Name: fn_shift_summary(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_shift_summary(p_shift_id integer) RETURNS TABLE(shift_id integer, user_id integer, user_name character varying, opened_at timestamp with time zone, closed_at timestamp with time zone, status smallint, invoices integer, returns integer, sales_lbp numeric, refunds_lbp numeric, profit_lbp numeric, card_lbp numeric, on_account_lbp numeric, client_cash_lbp numeric, client_cash_usd numeric, opening_cash_lbp numeric, opening_cash_usd numeric, expected_cash_lbp numeric, expected_cash_usd numeric, counted_cash_lbp numeric, counted_cash_usd numeric, difference_lbp numeric, difference_usd numeric)
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


--
-- Name: fn_stamp_created_updated(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_stamp_created_updated() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_old jsonb;
    v_new jsonb;
BEGIN
    IF TG_OP = 'INSERT' THEN
        NEW.created_by := coalesce(NEW.created_by, fn_app_user_id());
        RETURN NEW;
    END IF;

    v_old := to_jsonb(OLD) - 'updated_by' - 'updated_at' - coalesce(TG_ARGV, '{}'::text[]);
    v_new := to_jsonb(NEW) - 'updated_by' - 'updated_at' - coalesce(TG_ARGV, '{}'::text[]);
    IF v_old = v_new THEN RETURN NEW; END IF;   -- nothing (that counts) changed

    NEW.created_by := OLD.created_by;           -- the creator never changes
    NEW.updated_by := coalesce(fn_app_user_id(), NEW.updated_by);
    NEW.updated_at := now();
    RETURN NEW;
END $$;


--
-- Name: fn_stamp_updated(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_stamp_updated() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF (to_jsonb(OLD) - 'updated_by' - 'updated_at' - coalesce(TG_ARGV, '{}'::text[]))
     = (to_jsonb(NEW) - 'updated_by' - 'updated_at' - coalesce(TG_ARGV, '{}'::text[])) THEN
        RETURN NEW;
    END IF;
    NEW.user_id := OLD.user_id;                 -- the creator never changes
    NEW.updated_by := coalesce(fn_app_user_id(), NEW.updated_by);
    NEW.updated_at := now();
    IF TG_TABLE_NAME = 'purchase_invoices' AND OLD.status = 0 AND NEW.status = 1 THEN
        NEW.posted_by := coalesce(fn_app_user_id(), NEW.posted_by);
    END IF;
    RETURN NEW;
END $$;


--
-- Name: fn_supplier_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_supplier_delete(p_supplier_id integer) RETURNS text
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


--
-- Name: fn_supplier_get_all(text, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_supplier_get_all(p_search text DEFAULT NULL::text, p_include_inactive boolean DEFAULT false) RETURNS TABLE(supplier_id integer, name character varying, phone character varying, address character varying, email character varying, balance_lbp numeric, vat_number character varying, notes character varying, is_active boolean)
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


--
-- Name: fn_supplier_save(integer, text, text, text, text, text, text, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_supplier_save(p_supplier_id integer, p_name text, p_phone text, p_address text, p_email text, p_vat_number text, p_notes text, p_is_active boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $_$
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
END $_$;


--
-- Name: fn_text_normalize(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_text_normalize(p_text text) RETURNS text
    LANGUAGE sql IMMUTABLE
    AS $$
    SELECT regexp_replace(translate(lower(coalesce(p_text, '')), 'أإآٱةىؤئ', 'اااايهيوي'), '[ً-ْـ]', '', 'g');
$$;


--
-- Name: fn_user_delete(integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_user_delete(p_user_id integer) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_result text;
BEGIN
    BEGIN
        DELETE FROM users WHERE user_id = p_user_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'User % does not exist.', p_user_id; END IF;
        v_result := 'deleted';
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE users SET is_active = false WHERE user_id = p_user_id;
        v_result := 'deactivated';
    END;

    IF NOT EXISTS (SELECT 1 FROM users WHERE role = 1 AND is_active) THEN
        RAISE EXCEPTION 'There must be at least one active Manager.';
    END IF;
    RETURN v_result;
END $$;


--
-- Name: fn_user_find_by_password(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_user_find_by_password(p_password_hash text) RETURNS TABLE(user_id integer, full_name character varying, user_name character varying, role smallint, is_active boolean, must_change_password boolean, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT u.user_id, u.full_name, u.user_name, u.role, u.is_active, u.must_change_password, u.created_at
    FROM users u
    WHERE u.is_active AND u.password_hash = p_password_hash;
$$;


--
-- Name: fn_user_get_all(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_user_get_all() RETURNS TABLE(user_id integer, full_name character varying, user_name character varying, role smallint, is_active boolean, must_change_password boolean, created_at timestamp with time zone, password_set boolean)
    LANGUAGE sql STABLE
    AS $_$
    SELECT u.user_id, u.full_name, u.user_name, u.role, u.is_active, u.must_change_password, u.created_at,
           u.password_hash LIKE 'pin$%'
    FROM users u
    ORDER BY NOT u.is_active, u.role, lower(u.full_name), u.user_id;
$_$;


--
-- Name: fn_user_save(integer, text, text, smallint, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_user_save(p_user_id integer, p_full_name text, p_password_hash text, p_role smallint, p_is_active boolean DEFAULT true) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_full_name text := nullif(trim(p_full_name), '');
    v_hash      text := nullif(p_password_hash, '');
    v_user_name text;
    v_n         integer := 1;
    v_id        integer;
BEGIN
    IF v_full_name IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF p_role NOT IN (1, 2) THEN RAISE EXCEPTION 'The role must be Manager or User.'; END IF;
    IF p_user_id IS NULL AND v_hash IS NULL THEN RAISE EXCEPTION 'A password is required for a new user.'; END IF;

    -- The password identifies the user at login, so it must not be used by another active user
    IF coalesce(p_is_active, true) AND EXISTS (
        SELECT 1 FROM users u
        WHERE u.is_active AND u.user_id IS DISTINCT FROM p_user_id
          AND u.password_hash = coalesce(v_hash, (SELECT x.password_hash FROM users x WHERE x.user_id = p_user_id))) THEN
        RAISE EXCEPTION 'This password is already used by another user. Choose a different one.';
    END IF;

    -- user_name from the name, made unique: "Rami", "Rami 2", ...
    v_user_name := left(v_full_name, 45);
    WHILE EXISTS (SELECT 1 FROM users WHERE lower(user_name) = lower(v_user_name) AND user_id IS DISTINCT FROM p_user_id) LOOP
        v_n := v_n + 1;
        v_user_name := left(v_full_name, 45) || ' ' || v_n;
    END LOOP;

    IF p_user_id IS NULL THEN
        INSERT INTO users (user_name, password_hash, full_name, role, must_change_password, is_active)
        VALUES (v_user_name, v_hash, v_full_name, p_role, false, coalesce(p_is_active, true))
        RETURNING user_id INTO v_id;
    ELSE
        UPDATE users
        SET user_name            = v_user_name,
            full_name            = v_full_name,
            role                 = p_role,
            is_active            = coalesce(p_is_active, is_active),
            password_hash        = coalesce(v_hash, password_hash),
            must_change_password = CASE WHEN v_hash IS NULL THEN must_change_password ELSE false END
        WHERE user_id = p_user_id
        RETURNING user_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'User % does not exist.', p_user_id; END IF;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM users WHERE role = 1 AND is_active) THEN
        RAISE EXCEPTION 'There must be at least one active Manager.';
    END IF;
    RETURN v_id;
END $$;


--
-- Name: trg_adjustment_line_snapshot(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.trg_adjustment_line_snapshot() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_type smallint;
BEGIN
    SELECT product_type, stock_qty INTO v_type, NEW.qty_before FROM products WHERE product_id = NEW.product_id;
    IF v_type <> 1 THEN
        RAISE EXCEPTION 'Only items with stock can be counted (not models or bundles).';
    END IF;
    RETURN NEW;
END $$;


--
-- Name: trg_bundle_items_check(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.trg_bundle_items_check() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF (SELECT product_type FROM products WHERE product_id = NEW.bundle_product_id) <> 2 THEN
        RAISE EXCEPTION 'Only a bundle can have items inside it.';
    END IF;
    IF (SELECT product_type FROM products WHERE product_id = NEW.component_product_id) <> 1 THEN
        RAISE EXCEPTION 'A bundle can only contain items with stock (choose a size/color, not the model).';
    END IF;
    RETURN NEW;
END $$;


--
-- Name: trg_company_keep(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.trg_company_keep() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    RAISE EXCEPTION 'The company record cannot be deleted. Change it instead.';
END $$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: audit_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_log (
    audit_id bigint NOT NULL,
    logged_at timestamp with time zone DEFAULT now() NOT NULL,
    user_id integer,
    action character varying(50) NOT NULL,
    table_name character varying(50),
    record_id integer,
    details jsonb
);


--
-- Name: audit_log_audit_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.audit_log ALTER COLUMN audit_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.audit_log_audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: bundle_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.bundle_items (
    bundle_product_id integer NOT NULL,
    component_product_id integer NOT NULL,
    quantity numeric(18,3) NOT NULL,
    CONSTRAINT bundle_items_check CHECK ((bundle_product_id <> component_product_id)),
    CONSTRAINT bundle_items_quantity_check CHECK ((quantity > (0)::numeric))
);


--
-- Name: categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categories (
    category_id integer NOT NULL,
    name character varying(100) NOT NULL,
    parent_id integer,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone
);


--
-- Name: categories_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.categories ALTER COLUMN category_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.categories_category_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: company; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.company (
    company_id smallint DEFAULT 1 NOT NULL,
    name character varying(150) NOT NULL,
    address character varying(300),
    email character varying(150),
    mobile character varying(50),
    vat_number character varying(50),
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    logo bytea,
    CONSTRAINT company_single_row CHECK ((company_id = 1))
);


--
-- Name: currencies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.currencies (
    code character(3) NOT NULL,
    name character varying(50) NOT NULL,
    symbol character varying(8) NOT NULL,
    decimals smallint DEFAULT 2 NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    CONSTRAINT currencies_code_check CHECK ((code ~ '^[A-Z]{3}$'::text)),
    CONSTRAINT currencies_decimals_check CHECK (((decimals >= 0) AND (decimals <= 4))),
    CONSTRAINT currencies_primary_active CHECK (((NOT is_primary) OR is_active))
);


--
-- Name: customer_payment_allocations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customer_payment_allocations (
    payment_id integer NOT NULL,
    sale_id integer NOT NULL,
    amount_lbp numeric(18,2) NOT NULL,
    CONSTRAINT customer_payment_allocations_amount_lbp_check CHECK ((amount_lbp > (0)::numeric))
);


--
-- Name: customer_payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customer_payments (
    payment_id integer NOT NULL,
    customer_id integer NOT NULL,
    shift_id integer,
    payment_date timestamp with time zone DEFAULT now() NOT NULL,
    method smallint NOT NULL,
    currency character(3) NOT NULL,
    amount numeric(18,2) NOT NULL,
    exchange_rate numeric(18,2) NOT NULL,
    amount_lbp numeric(18,2) NOT NULL,
    user_id integer NOT NULL,
    notes character varying(200),
    CONSTRAINT customer_payments_amount_check CHECK ((amount > (0)::numeric)),
    CONSTRAINT customer_payments_method_check CHECK ((method = ANY (ARRAY[1, 2, 4])))
);


--
-- Name: customer_payments_payment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.customer_payments ALTER COLUMN payment_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.customer_payments_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: customers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customers (
    customer_id integer NOT NULL,
    name character varying(150) NOT NULL,
    phone character varying(30),
    balance_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    credit_limit_lbp numeric(18,2),
    is_active boolean DEFAULT true NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    address character varying(250),
    email character varying(150),
    notes character varying(500)
);


--
-- Name: customers_customer_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.customers ALTER COLUMN customer_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.customers_customer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: document_counters; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.document_counters (
    doc_type character varying(10) NOT NULL,
    doc_year integer NOT NULL,
    last_number integer NOT NULL
);


--
-- Name: exchange_rates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.exchange_rates (
    rate_id integer NOT NULL,
    rate numeric(18,2) NOT NULL,
    effective_at timestamp with time zone DEFAULT now() NOT NULL,
    user_id integer NOT NULL,
    currency_code character(3) NOT NULL,
    CONSTRAINT exchange_rates_lbp_per_usd_check CHECK ((rate > (0)::numeric))
);


--
-- Name: exchange_rates_rate_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.exchange_rates ALTER COLUMN rate_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.exchange_rates_rate_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: pos_quick_buttons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pos_quick_buttons (
    button_id integer NOT NULL,
    packaging_id integer NOT NULL,
    caption character varying(40),
    color character varying(7),
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    group_id integer,
    ask_price boolean DEFAULT false NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    CONSTRAINT pos_quick_buttons_color_check CHECK (((color IS NULL) OR ((color)::text ~ '^#[0-9A-Fa-f]{6}$'::text)))
);


--
-- Name: pos_quick_buttons_button_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.pos_quick_buttons ALTER COLUMN button_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.pos_quick_buttons_button_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: pos_quick_groups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pos_quick_groups (
    group_id integer NOT NULL,
    name character varying(40) NOT NULL,
    color character varying(7),
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    CONSTRAINT pos_quick_groups_color_check CHECK (((color IS NULL) OR ((color)::text ~ '^#[0-9A-Fa-f]{6}$'::text)))
);


--
-- Name: pos_quick_groups_group_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.pos_quick_groups ALTER COLUMN group_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.pos_quick_groups_group_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: product_packaging; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.product_packaging (
    packaging_id integer NOT NULL,
    product_id integer NOT NULL,
    name character varying(50) NOT NULL,
    factor numeric(18,3) NOT NULL,
    barcode character varying(50),
    sale_price numeric(18,2) DEFAULT 0 NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    can_sell boolean DEFAULT true NOT NULL,
    can_purchase boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    pos_changed_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT product_packaging_factor_check CHECK ((factor > (0)::numeric)),
    CONSTRAINT product_packaging_sale_price_check CHECK ((sale_price >= (0)::numeric))
);


--
-- Name: product_packaging_packaging_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.product_packaging ALTER COLUMN packaging_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.product_packaging_packaging_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: products; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.products (
    product_id integer NOT NULL,
    parent_id integer,
    name character varying(150) NOT NULL,
    category_id integer,
    product_type smallint DEFAULT 1 NOT NULL,
    size character varying(20),
    color character varying(30),
    base_unit character varying(20) DEFAULT 'pcs'::character varying NOT NULL,
    is_weighted boolean DEFAULT false NOT NULL,
    avg_cost numeric(18,4) DEFAULT 0 NOT NULL,
    stock_qty numeric(18,3) DEFAULT 0 NOT NULL,
    reorder_level numeric(18,3) DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_by integer,
    scale_code character varying(10),
    pos_changed_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT products_check CHECK (((product_type = 1) OR (stock_qty = (0)::numeric))),
    CONSTRAINT products_product_type_check CHECK ((product_type = ANY (ARRAY[1, 2, 3]))),
    CONSTRAINT products_scale_code_check CHECK (((scale_code IS NULL) OR ((scale_code)::text ~ '^[0-9]+$'::text)))
);


--
-- Name: products_product_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.products ALTER COLUMN product_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.products_product_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: promotion_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.promotion_items (
    promotion_id integer NOT NULL,
    packaging_id integer NOT NULL
);


--
-- Name: promotions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.promotions (
    promotion_id integer NOT NULL,
    name character varying(100) NOT NULL,
    promo_type smallint NOT NULL,
    starts_on date,
    ends_on date,
    is_active boolean DEFAULT true NOT NULL,
    new_price numeric(18,2),
    percent numeric(5,2),
    buy_qty integer,
    free_qty integer,
    group_price numeric(18,2),
    min_total numeric(18,2),
    category_id integer,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT promotions_promo_type_check CHECK (((promo_type >= 1) AND (promo_type <= 6)))
);


--
-- Name: promotions_promotion_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.promotions ALTER COLUMN promotion_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.promotions_promotion_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: purchase_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.purchase_invoices (
    purchase_id integer NOT NULL,
    purchase_no character varying(30),
    supplier_invoice_no character varying(50),
    supplier_id integer,
    invoice_date date DEFAULT CURRENT_DATE NOT NULL,
    exchange_rate numeric(18,2) NOT NULL,
    sub_total numeric(18,2) DEFAULT 0 NOT NULL,
    discount numeric(18,2) DEFAULT 0 NOT NULL,
    total numeric(18,2) DEFAULT 0 NOT NULL,
    paid_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    status smallint DEFAULT 0 NOT NULL,
    user_id integer NOT NULL,
    posted_at timestamp with time zone,
    notes character varying(300),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by integer,
    updated_at timestamp with time zone,
    posted_by integer,
    currency character(3) NOT NULL,
    currency_rate numeric(18,6) DEFAULT 1 NOT NULL,
    total_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    doc_type smallint DEFAULT 1 NOT NULL,
    return_of_id integer,
    CONSTRAINT purchase_invoices_currency_rate_check CHECK ((currency_rate > (0)::numeric)),
    CONSTRAINT purchase_invoices_discount_check CHECK ((discount >= (0)::numeric)),
    CONSTRAINT purchase_invoices_doc_type_check CHECK (((doc_type = ANY (ARRAY[1, 2])) AND ((doc_type = 2) OR (return_of_id IS NULL)))),
    CONSTRAINT purchase_invoices_status_check CHECK ((status = ANY (ARRAY[0, 1, 2])))
);


--
-- Name: purchase_invoices_purchase_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.purchase_invoices ALTER COLUMN purchase_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.purchase_invoices_purchase_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: purchase_lines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.purchase_lines (
    purchase_line_id integer NOT NULL,
    purchase_id integer NOT NULL,
    product_id integer NOT NULL,
    packaging_id integer NOT NULL,
    quantity numeric(18,3) NOT NULL,
    factor numeric(18,3) NOT NULL,
    base_qty numeric(18,3) GENERATED ALWAYS AS ((quantity * factor)) STORED,
    unit_cost numeric(18,4) NOT NULL,
    line_total numeric(18,2) DEFAULT 0 NOT NULL,
    discount numeric(18,4) DEFAULT 0 NOT NULL,
    CONSTRAINT purchase_lines_discount_check CHECK ((discount >= (0)::numeric)),
    CONSTRAINT purchase_lines_factor_check CHECK ((factor > (0)::numeric)),
    CONSTRAINT purchase_lines_quantity_check CHECK ((quantity > (0)::numeric)),
    CONSTRAINT purchase_lines_unit_cost_check CHECK ((unit_cost >= (0)::numeric))
);


--
-- Name: purchase_lines_purchase_line_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.purchase_lines ALTER COLUMN purchase_line_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.purchase_lines_purchase_line_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: registers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.registers (
    register_id integer NOT NULL,
    name character varying(50) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone
);


--
-- Name: registers_register_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.registers ALTER COLUMN register_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.registers_register_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: sales_invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales_invoices (
    sale_id integer NOT NULL,
    invoice_no character varying(30),
    invoice_type smallint DEFAULT 1 NOT NULL,
    original_sale_id integer,
    customer_id integer,
    shift_id integer,
    user_id integer NOT NULL,
    invoice_date timestamp with time zone DEFAULT now() NOT NULL,
    exchange_rate numeric(18,2) NOT NULL,
    sub_total numeric(18,2) DEFAULT 0 NOT NULL,
    discount numeric(18,2) DEFAULT 0 NOT NULL,
    total numeric(18,2) DEFAULT 0 NOT NULL,
    total_cost numeric(18,2) DEFAULT 0 NOT NULL,
    profit numeric(18,2) DEFAULT 0 NOT NULL,
    change_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    status smallint DEFAULT 1 NOT NULL,
    updated_by integer,
    updated_at timestamp with time zone,
    due_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    promo_discount numeric(18,2) DEFAULT 0 NOT NULL,
    promo_name text,
    CONSTRAINT sales_invoices_check CHECK (((status = 0) OR (invoice_no IS NOT NULL))),
    CONSTRAINT sales_invoices_invoice_type_check CHECK ((invoice_type = ANY (ARRAY[1, 2]))),
    CONSTRAINT sales_invoices_status_check CHECK ((status = ANY (ARRAY[0, 1, 2])))
);


--
-- Name: sales_invoices_sale_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.sales_invoices ALTER COLUMN sale_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.sales_invoices_sale_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: sales_line_components; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales_line_components (
    sale_line_id integer NOT NULL,
    product_id integer NOT NULL,
    base_qty_per_unit numeric(18,3) NOT NULL,
    unit_cost numeric(18,4) NOT NULL
);


--
-- Name: sales_lines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales_lines (
    sale_line_id integer NOT NULL,
    sale_id integer NOT NULL,
    line_no integer NOT NULL,
    product_id integer NOT NULL,
    packaging_id integer NOT NULL,
    description character varying(200) NOT NULL,
    quantity numeric(18,3) NOT NULL,
    factor numeric(18,3) NOT NULL,
    base_qty numeric(18,3) GENERATED ALWAYS AS ((quantity * factor)) STORED,
    unit_price numeric(18,2) NOT NULL,
    discount numeric(18,2) DEFAULT 0 NOT NULL,
    discount_share numeric(18,2) DEFAULT 0 NOT NULL,
    unit_cost numeric(18,4) NOT NULL,
    line_total numeric(18,2) NOT NULL,
    line_profit numeric(18,2) NOT NULL,
    return_of_line_id integer,
    promo_discount numeric(18,2) DEFAULT 0 NOT NULL,
    promo_name text,
    CONSTRAINT sales_lines_quantity_check CHECK ((quantity > (0)::numeric))
);


--
-- Name: sales_lines_sale_line_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.sales_lines ALTER COLUMN sale_line_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.sales_lines_sale_line_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: sales_payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales_payments (
    payment_id integer NOT NULL,
    sale_id integer NOT NULL,
    method smallint NOT NULL,
    currency character(3) NOT NULL,
    amount numeric(18,2) NOT NULL,
    amount_lbp numeric(18,2) NOT NULL,
    CONSTRAINT sales_payments_amount_check CHECK ((amount > (0)::numeric)),
    CONSTRAINT sales_payments_method_check CHECK ((method = ANY (ARRAY[1, 2, 3])))
);


--
-- Name: sales_payments_payment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.sales_payments ALTER COLUMN payment_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.sales_payments_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: settings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.settings (
    key character varying(50) NOT NULL,
    value character varying(200) NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone
);


--
-- Name: shifts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.shifts (
    shift_id integer NOT NULL,
    register_id integer NOT NULL,
    user_id integer NOT NULL,
    opened_at timestamp with time zone DEFAULT now() NOT NULL,
    closed_at timestamp with time zone,
    opening_cash_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    opening_cash_usd numeric(18,2) DEFAULT 0 NOT NULL,
    expected_cash_lbp numeric(18,2),
    expected_cash_usd numeric(18,2),
    counted_cash_lbp numeric(18,2),
    counted_cash_usd numeric(18,2),
    status smallint DEFAULT 1 NOT NULL,
    updated_by integer,
    updated_at timestamp with time zone,
    CONSTRAINT shifts_status_check CHECK ((status = ANY (ARRAY[1, 2])))
);


--
-- Name: shifts_shift_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.shifts ALTER COLUMN shift_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.shifts_shift_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: stock_adjustment_lines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.stock_adjustment_lines (
    adjustment_line_id integer NOT NULL,
    adjustment_id integer NOT NULL,
    product_id integer NOT NULL,
    counted_qty numeric(18,3) NOT NULL,
    qty_before numeric(18,3),
    difference numeric(18,3),
    unit_cost numeric(18,4),
    CONSTRAINT stock_adjustment_lines_counted_qty_check CHECK ((counted_qty >= (0)::numeric))
);


--
-- Name: stock_adjustment_lines_adjustment_line_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.stock_adjustment_lines ALTER COLUMN adjustment_line_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.stock_adjustment_lines_adjustment_line_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: stock_adjustments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.stock_adjustments (
    adjustment_id integer NOT NULL,
    adjustment_no character varying(30),
    adjustment_date timestamp with time zone DEFAULT now() NOT NULL,
    reason character varying(200) NOT NULL,
    status smallint DEFAULT 0 NOT NULL,
    user_id integer NOT NULL,
    posted_at timestamp with time zone,
    updated_by integer,
    updated_at timestamp with time zone,
    CONSTRAINT stock_adjustments_status_check CHECK ((status = ANY (ARRAY[0, 1])))
);


--
-- Name: stock_adjustments_adjustment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.stock_adjustments ALTER COLUMN adjustment_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.stock_adjustments_adjustment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: stock_movements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.stock_movements (
    movement_id bigint NOT NULL,
    product_id integer NOT NULL,
    movement_date timestamp with time zone DEFAULT now() NOT NULL,
    movement_type smallint NOT NULL,
    quantity numeric(18,3) NOT NULL,
    unit_cost numeric(18,4) NOT NULL,
    balance_after numeric(18,3) NOT NULL,
    ref_type smallint,
    ref_id integer,
    user_id integer NOT NULL,
    notes character varying(200),
    CONSTRAINT stock_movements_movement_type_check CHECK (((movement_type >= 1) AND (movement_type <= 6)))
);


--
-- Name: stock_movements_movement_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.stock_movements ALTER COLUMN movement_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.stock_movements_movement_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: supplier_payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.supplier_payments (
    payment_id integer NOT NULL,
    supplier_id integer NOT NULL,
    purchase_id integer,
    payment_date timestamp with time zone DEFAULT now() NOT NULL,
    method smallint NOT NULL,
    currency character(3) NOT NULL,
    amount numeric(18,2) NOT NULL,
    exchange_rate numeric(18,2) NOT NULL,
    amount_lbp numeric(18,2) NOT NULL,
    user_id integer NOT NULL,
    notes character varying(200),
    CONSTRAINT supplier_payments_amount_check CHECK ((amount > (0)::numeric)),
    CONSTRAINT supplier_payments_method_check CHECK ((method = ANY (ARRAY[1, 2, 4])))
);


--
-- Name: supplier_payments_payment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.supplier_payments ALTER COLUMN payment_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.supplier_payments_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: suppliers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.suppliers (
    supplier_id integer NOT NULL,
    name character varying(150) NOT NULL,
    phone character varying(30),
    address character varying(250),
    balance_lbp numeric(18,2) DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_by integer,
    updated_at timestamp with time zone,
    email character varying(150),
    vat_number character varying(50),
    notes character varying(500)
);


--
-- Name: suppliers_supplier_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.suppliers ALTER COLUMN supplier_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.suppliers_supplier_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    user_id integer NOT NULL,
    user_name character varying(50) NOT NULL,
    password_hash character varying(200) NOT NULL,
    full_name character varying(100) NOT NULL,
    role smallint NOT NULL,
    must_change_password boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_by integer,
    updated_at timestamp with time zone,
    CONSTRAINT users_role_check CHECK ((role = ANY (ARRAY[1, 2, 3])))
);


--
-- Name: users_user_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.users ALTER COLUMN user_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.users_user_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: v_barcodes; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_barcodes AS
 SELECT pk.barcode,
    pk.packaging_id,
    pk.product_id,
    p.product_type,
    p.parent_id,
    public.fn_item_description((p.name)::text, (p.size)::text, (p.color)::text, (pk.name)::text) AS display_name,
    pk.name AS packaging_name,
    pk.factor,
    pk.sale_price,
    p.is_weighted,
    p.base_unit,
        CASE
            WHEN (p.product_type = 1) THEN (p.avg_cost * pk.factor)
            ELSE (b.cost * pk.factor)
        END AS cost_per_packaging,
        CASE
            WHEN (p.product_type = 1) THEN floor((p.stock_qty / pk.factor))
            ELSE floor((b.available / pk.factor))
        END AS available
   FROM ((public.product_packaging pk
     JOIN public.products p ON ((p.product_id = pk.product_id)))
     LEFT JOIN LATERAL ( SELECT sum((c.avg_cost * bi.quantity)) AS cost,
            min(floor((c.stock_qty / bi.quantity))) AS available
           FROM (public.bundle_items bi
             JOIN public.products c ON ((c.product_id = bi.component_product_id)))
          WHERE (bi.bundle_product_id = p.product_id)) b ON ((p.product_type = 2)))
  WHERE (pk.is_active AND pk.can_sell AND p.is_active);


--
-- Name: v_daily_profit; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_daily_profit AS
 SELECT ((sales_invoices.invoice_date AT TIME ZONE 'Asia/Beirut'::text))::date AS sale_day,
    count(*) FILTER (WHERE (sales_invoices.invoice_type = 1)) AS invoices,
    count(*) FILTER (WHERE (sales_invoices.invoice_type = 2)) AS returns,
    sum(
        CASE
            WHEN (sales_invoices.invoice_type = 2) THEN (- sales_invoices.total)
            ELSE sales_invoices.total
        END) AS sales_lbp,
    sum(
        CASE
            WHEN (sales_invoices.invoice_type = 2) THEN (- sales_invoices.total_cost)
            ELSE sales_invoices.total_cost
        END) AS cost_lbp,
    sum(
        CASE
            WHEN (sales_invoices.invoice_type = 2) THEN (- sales_invoices.profit)
            ELSE sales_invoices.profit
        END) AS profit_lbp
   FROM public.sales_invoices
  WHERE (sales_invoices.status = 1)
  GROUP BY (((sales_invoices.invoice_date AT TIME ZONE 'Asia/Beirut'::text))::date);


--
-- Name: v_item_list; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_item_list AS
 SELECT p.product_id,
    p.name,
    p.product_type,
    p.category_id,
    c.name AS category_name,
    p.base_unit,
    p.is_active,
        CASE p.product_type
            WHEN 3 THEN COALESCE(ch.stock, (0)::numeric)
            WHEN 2 THEN COALESCE(bu.available, (0)::numeric)
            ELSE p.stock_qty
        END AS stock_qty,
        CASE p.product_type
            WHEN 3 THEN ch.avg_cost
            WHEN 2 THEN bu.cost
            ELSE p.avg_cost
        END AS avg_cost,
        CASE p.product_type
            WHEN 3 THEN ch.min_price
            ELSE dp.sale_price
        END AS sale_price,
    COALESCE(pc.packagings, (0)::bigint) AS packaging_count,
    COALESCE(ch.variants, (0)::bigint) AS variant_count,
        CASE
            WHEN (p.product_type = 2) THEN 'Bundle'::text
            WHEN ((p.product_type = 3) AND (COALESCE(ch.stock, (0)::numeric) <= (0)::numeric)) THEN 'Out of stock'::text
            WHEN ((p.product_type = 3) AND (ch.out_count > 0)) THEN (ch.out_count || ' size(s) out'::text)
            WHEN ((p.product_type = 1) AND (p.stock_qty <= (0)::numeric)) THEN 'Out of stock'::text
            WHEN ((p.product_type = 1) AND (p.stock_qty <= p.reorder_level)) THEN 'Low stock'::text
            ELSE 'In stock'::text
        END AS stock_status
   FROM (((((public.products p
     LEFT JOIN public.categories c ON ((c.category_id = p.category_id)))
     LEFT JOIN LATERAL ( SELECT product_packaging.sale_price
           FROM public.product_packaging
          WHERE ((product_packaging.product_id = p.product_id) AND product_packaging.is_active)
          ORDER BY product_packaging.is_default DESC, product_packaging.factor
         LIMIT 1) dp ON (true))
     LEFT JOIN LATERAL ( SELECT count(*) AS packagings
           FROM public.product_packaging
          WHERE ((product_packaging.product_id = p.product_id) AND product_packaging.is_active)) pc ON (true))
     LEFT JOIN LATERAL ( SELECT count(*) AS variants,
            sum(v.stock_qty) AS stock,
                CASE
                    WHEN (sum(v.stock_qty) > (0)::numeric) THEN (sum((v.stock_qty * v.avg_cost)) / sum(v.stock_qty))
                    ELSE avg(v.avg_cost)
                END AS avg_cost,
            count(*) FILTER (WHERE (v.stock_qty <= (0)::numeric)) AS out_count,
            min(vp.sale_price) AS min_price
           FROM (public.products v
             LEFT JOIN public.product_packaging vp ON (((vp.product_id = v.product_id) AND vp.is_default)))
          WHERE ((v.parent_id = p.product_id) AND v.is_active)) ch ON ((p.product_type = 3)))
     LEFT JOIN LATERAL ( SELECT sum((c2.avg_cost * bi.quantity)) AS cost,
            min(floor((c2.stock_qty / bi.quantity))) AS available
           FROM (public.bundle_items bi
             JOIN public.products c2 ON ((c2.product_id = bi.component_product_id)))
          WHERE (bi.bundle_product_id = p.product_id)) bu ON ((p.product_type = 2)))
  WHERE (p.parent_id IS NULL);


--
-- Name: v_pos_items; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_pos_items AS
 SELECT r.packaging_id,
    r.product_id,
    public.fn_item_description((r.name)::text, (r.size)::text, (r.color)::text, (r.pack_name)::text) AS item_name,
    r.pack_name AS packaging_name,
    r.barcode,
    r.sale_price,
    r.factor,
    r.is_weighted,
    r.base_unit,
        CASE
            WHEN (r.product_type = 2) THEN ( SELECT min(floor((c.stock_qty / b.quantity))) AS min
               FROM (public.bundle_items b
                 JOIN public.products c ON ((c.product_id = b.component_product_id)))
              WHERE (b.bundle_product_id = r.product_id))
            ELSE r.stock_qty
        END AS stock_qty,
    r.name AS product_name,
        CASE
            WHEN (r.sellable AND (r.product_type = 1) AND (r.scale_code IS NOT NULL) AND (row_number() OVER (PARTITION BY r.product_id, r.sellable ORDER BY (r.factor = (1)::numeric) DESC, r.is_default DESC, r.packaging_id) = 1)) THEN r.scale_code
            ELSE NULL::character varying
        END AS scale_code,
    r.sellable
   FROM ( SELECT pk.packaging_id,
            p.product_id,
            p.name,
            p.size,
            p.color,
            pk.name AS pack_name,
            pk.barcode,
            pk.sale_price,
            pk.factor,
            pk.is_default,
            p.is_weighted,
            p.base_unit,
            p.product_type,
            p.scale_code,
            p.stock_qty,
            (pk.is_active AND pk.can_sell AND p.is_active AND (p.product_type = ANY (ARRAY[1, 2]))) AS sellable
           FROM (public.product_packaging pk
             JOIN public.products p ON ((p.product_id = pk.product_id)))) r;


--
-- Name: v_product_sales; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_product_sales AS
 SELECT ((si.invoice_date AT TIME ZONE 'Asia/Beirut'::text))::date AS sale_day,
    sl.product_id,
    sum(
        CASE
            WHEN (si.invoice_type = 2) THEN (- sl.base_qty)
            ELSE sl.base_qty
        END) AS base_qty_sold,
    sum(
        CASE
            WHEN (si.invoice_type = 2) THEN (- sl.line_total)
            ELSE sl.line_total
        END) AS sales_lbp,
    sum(
        CASE
            WHEN (si.invoice_type = 2) THEN (- sl.line_profit)
            ELSE sl.line_profit
        END) AS profit_lbp
   FROM (public.sales_lines sl
     JOIN public.sales_invoices si ON ((si.sale_id = sl.sale_id)))
  WHERE (si.status = 1)
  GROUP BY (((si.invoice_date AT TIME ZONE 'Asia/Beirut'::text))::date), sl.product_id;


--
-- Name: v_purchase_list; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_purchase_list AS
 SELECT pi.purchase_id,
    pi.purchase_no,
    pi.supplier_invoice_no,
    pi.supplier_id,
    s.name AS supplier_name,
    pi.invoice_date,
    pi.total,
    pi.paid_lbp,
        CASE
            WHEN ((pi.status = 1) AND (pi.doc_type = 1)) THEN (pi.total_lbp - pi.paid_lbp)
            ELSE NULL::numeric
        END AS balance_lbp,
        CASE
            WHEN (pi.currency = 'USD'::bpchar) THEN pi.total
            ELSE round(((pi.total * pi.currency_rate) / pi.exchange_rate), 2)
        END AS total_usd,
        CASE pi.status
            WHEN 0 THEN 'Draft'::text
            WHEN 2 THEN 'Cancelled'::text
            ELSE
            CASE
                WHEN (pi.doc_type = 2) THEN 'Returned'::text
                WHEN (pi.paid_lbp >= pi.total_lbp) THEN 'Paid'::text
                WHEN (pi.paid_lbp > (0)::numeric) THEN 'Part paid'::text
                ELSE 'Unpaid'::text
            END
        END AS status_text,
    pi.status,
    pi.currency,
    pi.currency_rate,
    round((pi.total * pi.currency_rate), 2) AS total_lbp,
    pi.doc_type,
    pi.return_of_id
   FROM (public.purchase_invoices pi
     LEFT JOIN public.suppliers s ON ((s.supplier_id = pi.supplier_id)));


--
-- Name: v_sales_list; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_sales_list AS
 SELECT si.sale_id,
    si.invoice_no,
    si.invoice_type,
    si.original_sale_id,
    si.invoice_date,
    u.full_name AS cashier,
    cu.name AS customer_name,
    si.shift_id,
    ( SELECT count(*) AS count
           FROM public.sales_lines sl
          WHERE (sl.sale_id = si.sale_id)) AS line_count,
        CASE
            WHEN (si.invoice_type = 2) THEN (- si.total)
            ELSE si.total
        END AS total_lbp,
        CASE
            WHEN (si.invoice_type = 2) THEN (- si.profit)
            ELSE si.profit
        END AS profit_lbp,
    ((
        CASE
            WHEN (si.invoice_type = 2) THEN '-1'::integer
            ELSE 1
        END)::numeric * round((si.total / si.exchange_rate), 2)) AS total_usd,
    ( SELECT string_agg(
                CASE sp.method
                    WHEN 1 THEN ('Cash '::text || (sp.currency)::text)
                    WHEN 2 THEN 'Card'::text
                    ELSE 'On account'::text
                END, ' + '::text ORDER BY sp.payment_id) AS string_agg
           FROM public.sales_payments sp
          WHERE (sp.sale_id = si.sale_id)) AS paid_by,
        CASE
            WHEN (si.status = 0) THEN 'Held'::text
            WHEN (si.status = 2) THEN 'Cancelled'::text
            WHEN (si.invoice_type = 2) THEN 'Return'::text
            WHEN (EXISTS ( SELECT 1
               FROM public.sales_payments sp
              WHERE ((sp.sale_id = si.sale_id) AND (sp.method = 3)))) THEN 'On account'::text
            ELSE 'Paid'::text
        END AS status_text,
    si.status
   FROM ((public.sales_invoices si
     JOIN public.users u ON ((u.user_id = si.user_id)))
     LEFT JOIN public.customers cu ON ((cu.customer_id = si.customer_id)));


--
-- Name: v_stock_value; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_stock_value AS
 SELECT p.product_id,
    public.fn_item_description((p.name)::text, (p.size)::text, (p.color)::text, NULL::text) AS name,
    p.base_unit,
    p.stock_qty,
    p.avg_cost,
    round((p.stock_qty * p.avg_cost), 2) AS stock_value_lbp,
    p.reorder_level,
    (p.stock_qty <= p.reorder_level) AS is_low_stock
   FROM public.products p
  WHERE ((p.product_type = 1) AND p.is_active);


--
-- Name: v_variants; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public.v_variants AS
 SELECT v.parent_id AS model_id,
    v.product_id,
    v.size,
    v.color,
    v.stock_qty,
    v.avg_cost,
    pk.barcode,
    pk.sale_price,
    pk.packaging_id
   FROM (public.products v
     LEFT JOIN public.product_packaging pk ON (((pk.product_id = v.product_id) AND pk.is_default)))
  WHERE ((v.parent_id IS NOT NULL) AND v.is_active);


--
-- Name: audit_log audit_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_pkey PRIMARY KEY (audit_id);


--
-- Name: bundle_items bundle_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundle_items
    ADD CONSTRAINT bundle_items_pkey PRIMARY KEY (bundle_product_id, component_product_id);


--
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (category_id);


--
-- Name: company company_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.company
    ADD CONSTRAINT company_pkey PRIMARY KEY (company_id);


--
-- Name: currencies currencies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.currencies
    ADD CONSTRAINT currencies_pkey PRIMARY KEY (code);


--
-- Name: customer_payment_allocations customer_payment_allocations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payment_allocations
    ADD CONSTRAINT customer_payment_allocations_pkey PRIMARY KEY (payment_id, sale_id);


--
-- Name: customer_payments customer_payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payments
    ADD CONSTRAINT customer_payments_pkey PRIMARY KEY (payment_id);


--
-- Name: customers customers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_pkey PRIMARY KEY (customer_id);


--
-- Name: document_counters document_counters_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.document_counters
    ADD CONSTRAINT document_counters_pkey PRIMARY KEY (doc_type, doc_year);


--
-- Name: exchange_rates exchange_rates_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exchange_rates
    ADD CONSTRAINT exchange_rates_pkey PRIMARY KEY (rate_id);


--
-- Name: pos_quick_buttons pos_quick_buttons_packaging_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_packaging_id_key UNIQUE (packaging_id);


--
-- Name: pos_quick_buttons pos_quick_buttons_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_pkey PRIMARY KEY (button_id);


--
-- Name: pos_quick_groups pos_quick_groups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_groups
    ADD CONSTRAINT pos_quick_groups_pkey PRIMARY KEY (group_id);


--
-- Name: product_packaging product_packaging_barcode_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_barcode_key UNIQUE (barcode);


--
-- Name: product_packaging product_packaging_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_pkey PRIMARY KEY (packaging_id);


--
-- Name: product_packaging product_packaging_product_id_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_product_id_name_key UNIQUE (product_id, name);


--
-- Name: products products_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (product_id);


--
-- Name: promotion_items promotion_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotion_items
    ADD CONSTRAINT promotion_items_pkey PRIMARY KEY (promotion_id, packaging_id);


--
-- Name: promotions promotions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT promotions_pkey PRIMARY KEY (promotion_id);


--
-- Name: purchase_invoices purchase_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_pkey PRIMARY KEY (purchase_id);


--
-- Name: purchase_invoices purchase_invoices_purchase_no_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_purchase_no_key UNIQUE (purchase_no);


--
-- Name: purchase_lines purchase_lines_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_lines
    ADD CONSTRAINT purchase_lines_pkey PRIMARY KEY (purchase_line_id);


--
-- Name: registers registers_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registers
    ADD CONSTRAINT registers_name_key UNIQUE (name);


--
-- Name: registers registers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registers
    ADD CONSTRAINT registers_pkey PRIMARY KEY (register_id);


--
-- Name: sales_invoices sales_invoices_invoice_no_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_invoice_no_key UNIQUE (invoice_no);


--
-- Name: sales_invoices sales_invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_pkey PRIMARY KEY (sale_id);


--
-- Name: sales_line_components sales_line_components_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_line_components
    ADD CONSTRAINT sales_line_components_pkey PRIMARY KEY (sale_line_id, product_id);


--
-- Name: sales_lines sales_lines_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_lines
    ADD CONSTRAINT sales_lines_pkey PRIMARY KEY (sale_line_id);


--
-- Name: sales_payments sales_payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_payments
    ADD CONSTRAINT sales_payments_pkey PRIMARY KEY (payment_id);


--
-- Name: settings settings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.settings
    ADD CONSTRAINT settings_pkey PRIMARY KEY (key);


--
-- Name: shifts shifts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.shifts
    ADD CONSTRAINT shifts_pkey PRIMARY KEY (shift_id);


--
-- Name: stock_adjustment_lines stock_adjustment_lines_adjustment_id_product_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustment_lines
    ADD CONSTRAINT stock_adjustment_lines_adjustment_id_product_id_key UNIQUE (adjustment_id, product_id);


--
-- Name: stock_adjustment_lines stock_adjustment_lines_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustment_lines
    ADD CONSTRAINT stock_adjustment_lines_pkey PRIMARY KEY (adjustment_line_id);


--
-- Name: stock_adjustments stock_adjustments_adjustment_no_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_adjustment_no_key UNIQUE (adjustment_no);


--
-- Name: stock_adjustments stock_adjustments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_pkey PRIMARY KEY (adjustment_id);


--
-- Name: stock_movements stock_movements_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_pkey PRIMARY KEY (movement_id);


--
-- Name: supplier_payments supplier_payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.supplier_payments
    ADD CONSTRAINT supplier_payments_pkey PRIMARY KEY (payment_id);


--
-- Name: suppliers suppliers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suppliers
    ADD CONSTRAINT suppliers_pkey PRIMARY KEY (supplier_id);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (user_id);


--
-- Name: users users_user_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_user_name_key UNIQUE (user_name);


--
-- Name: ix_audit_log_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_audit_log_date ON public.audit_log USING btree (logged_at);


--
-- Name: ix_audit_log_table; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_audit_log_table ON public.audit_log USING btree (table_name, record_id);


--
-- Name: ix_customer_payment_allocations_sale; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_customer_payment_allocations_sale ON public.customer_payment_allocations USING btree (sale_id);


--
-- Name: ix_customer_payments_customer; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_customer_payments_customer ON public.customer_payments USING btree (customer_id, payment_date);


--
-- Name: ix_customer_payments_shift; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_customer_payments_shift ON public.customer_payments USING btree (shift_id);


--
-- Name: ix_customers_name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_customers_name ON public.customers USING gin (name public.gin_trgm_ops);


--
-- Name: ix_customers_phone; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_customers_phone ON public.customers USING btree (phone);


--
-- Name: ix_exchange_rates_currency; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_exchange_rates_currency ON public.exchange_rates USING btree (currency_code, effective_at DESC, rate_id DESC);


--
-- Name: ix_exchange_rates_effective; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_exchange_rates_effective ON public.exchange_rates USING btree (effective_at DESC);


--
-- Name: ix_packaging_barcode_trgm; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_packaging_barcode_trgm ON public.product_packaging USING gin (barcode public.gin_trgm_ops);


--
-- Name: ix_packaging_pos_changed; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_packaging_pos_changed ON public.product_packaging USING btree (pos_changed_at);


--
-- Name: ix_packaging_product; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_packaging_product ON public.product_packaging USING btree (product_id);


--
-- Name: ix_pos_quick_buttons_group; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_pos_quick_buttons_group ON public.pos_quick_buttons USING btree (group_id);


--
-- Name: ix_products_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_products_category ON public.products USING btree (category_id);


--
-- Name: ix_products_low_stock; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_products_low_stock ON public.products USING btree (product_id) WHERE ((product_type = 1) AND (stock_qty <= reorder_level));


--
-- Name: ix_products_name_trgm; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_products_name_trgm ON public.products USING gin (name public.gin_trgm_ops);


--
-- Name: ix_products_parent; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_products_parent ON public.products USING btree (parent_id);


--
-- Name: ix_products_pos_changed; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_products_pos_changed ON public.products USING btree (pos_changed_at);


--
-- Name: ix_promotion_items_packaging; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_promotion_items_packaging ON public.promotion_items USING btree (packaging_id);


--
-- Name: ix_purchase_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_date ON public.purchase_invoices USING btree (invoice_date);


--
-- Name: ix_purchase_lines_product; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_lines_product ON public.purchase_lines USING btree (product_id);


--
-- Name: ix_purchase_lines_purchase; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_lines_purchase ON public.purchase_lines USING btree (purchase_id);


--
-- Name: ix_purchase_return_of; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_return_of ON public.purchase_invoices USING btree (return_of_id) WHERE (return_of_id IS NOT NULL);


--
-- Name: ix_purchase_supplier; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_supplier ON public.purchase_invoices USING btree (supplier_id, invoice_date);


--
-- Name: ix_purchase_user; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_purchase_user ON public.purchase_invoices USING btree (user_id);


--
-- Name: ix_sales_customer; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_customer ON public.sales_invoices USING btree (customer_id) WHERE (customer_id IS NOT NULL);


--
-- Name: ix_sales_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_date ON public.sales_invoices USING btree (invoice_date) INCLUDE (invoice_type, total, total_cost, profit, status);


--
-- Name: ix_sales_due; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_due ON public.sales_invoices USING btree (customer_id, invoice_date) WHERE (due_lbp > (0)::numeric);


--
-- Name: ix_sales_held; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_held ON public.sales_invoices USING btree (sale_id) WHERE (status = 0);


--
-- Name: ix_sales_invoices_shift; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_invoices_shift ON public.sales_invoices USING btree (shift_id);


--
-- Name: ix_sales_lines_product; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_lines_product ON public.sales_lines USING btree (product_id) INCLUDE (quantity, factor, line_total, line_profit);


--
-- Name: ix_sales_lines_return_of; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_lines_return_of ON public.sales_lines USING btree (return_of_line_id) WHERE (return_of_line_id IS NOT NULL);


--
-- Name: ix_sales_lines_sale; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_lines_sale ON public.sales_lines USING btree (sale_id);


--
-- Name: ix_sales_payments_sale; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_payments_sale ON public.sales_payments USING btree (sale_id);


--
-- Name: ix_sales_shift; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_shift ON public.sales_invoices USING btree (shift_id);


--
-- Name: ix_sales_user_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_sales_user_date ON public.sales_invoices USING btree (user_id, invoice_date);


--
-- Name: ix_shifts_opened_at; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_shifts_opened_at ON public.shifts USING btree (opened_at);


--
-- Name: ix_stock_movements_product_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_stock_movements_product_date ON public.stock_movements USING btree (product_id, movement_date);


--
-- Name: ix_stock_movements_ref; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_stock_movements_ref ON public.stock_movements USING btree (ref_type, ref_id);


--
-- Name: ix_supplier_payments_supplier; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_supplier_payments_supplier ON public.supplier_payments USING btree (supplier_id, payment_date);


--
-- Name: ix_suppliers_name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_suppliers_name ON public.suppliers USING gin (name public.gin_trgm_ops);


--
-- Name: ux_categories_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_categories_name ON public.categories USING btree (COALESCE(parent_id, 0), lower((name)::text));


--
-- Name: ux_currencies_one_primary; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_currencies_one_primary ON public.currencies USING btree (is_primary) WHERE is_primary;


--
-- Name: ux_packaging_one_default; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_packaging_one_default ON public.product_packaging USING btree (product_id) WHERE is_default;


--
-- Name: ux_pos_quick_groups_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_pos_quick_groups_name ON public.pos_quick_groups USING btree (lower((name)::text));


--
-- Name: ux_products_scale_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_products_scale_code ON public.products USING btree (scale_code) WHERE (scale_code IS NOT NULL);


--
-- Name: ux_shifts_one_open_per_user; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_shifts_one_open_per_user ON public.shifts USING btree (user_id) WHERE (status = 1);


--
-- Name: ux_users_password_hash; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_users_password_hash ON public.users USING btree (password_hash) WHERE is_active;


--
-- Name: stock_adjustment_lines adjustment_line_snapshot; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER adjustment_line_snapshot BEFORE INSERT OR UPDATE OF product_id, counted_qty ON public.stock_adjustment_lines FOR EACH ROW EXECUTE FUNCTION public.trg_adjustment_line_snapshot();


--
-- Name: bundle_items bundle_items_check; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER bundle_items_check BEFORE INSERT OR UPDATE ON public.bundle_items FOR EACH ROW EXECUTE FUNCTION public.trg_bundle_items_check();


--
-- Name: categories trg_categories_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_categories_audit_delete AFTER DELETE ON public.categories FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('category_id');


--
-- Name: categories trg_categories_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_categories_stamp BEFORE INSERT OR UPDATE ON public.categories FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: company trg_company_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_company_audit_delete AFTER DELETE ON public.company FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('company_id');


--
-- Name: company trg_company_keep; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_company_keep BEFORE DELETE OR TRUNCATE ON public.company FOR EACH STATEMENT EXECUTE FUNCTION public.trg_company_keep();


--
-- Name: company trg_company_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_company_stamp BEFORE INSERT OR UPDATE ON public.company FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: customers trg_customers_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_customers_audit_delete AFTER DELETE ON public.customers FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('customer_id');


--
-- Name: customers trg_customers_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_customers_stamp BEFORE INSERT OR UPDATE ON public.customers FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: product_packaging trg_packaging_pos_notify_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_packaging_pos_notify_delete AFTER DELETE ON public.product_packaging REFERENCING OLD TABLE AS old_rows FOR EACH STATEMENT EXECUTE FUNCTION public.fn_pos_items_notify();


--
-- Name: product_packaging trg_packaging_pos_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_packaging_pos_stamp BEFORE INSERT OR UPDATE OF product_id, name, factor, barcode, sale_price, is_default, can_sell, is_active ON public.product_packaging FOR EACH ROW EXECUTE FUNCTION public.fn_pos_item_stamp('product_id', 'name', 'factor', 'barcode', 'sale_price', 'is_default', 'can_sell', 'is_active');


--
-- Name: pos_quick_buttons trg_pos_quick_buttons_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_pos_quick_buttons_audit_delete AFTER DELETE ON public.pos_quick_buttons FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('button_id');


--
-- Name: pos_quick_buttons trg_pos_quick_buttons_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_pos_quick_buttons_stamp BEFORE INSERT OR UPDATE ON public.pos_quick_buttons FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: pos_quick_groups trg_pos_quick_groups_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_pos_quick_groups_audit_delete AFTER DELETE ON public.pos_quick_groups FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('group_id');


--
-- Name: pos_quick_groups trg_pos_quick_groups_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_pos_quick_groups_stamp BEFORE INSERT OR UPDATE ON public.pos_quick_groups FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: product_packaging trg_product_packaging_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_product_packaging_audit_delete AFTER DELETE ON public.product_packaging FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('packaging_id');


--
-- Name: product_packaging trg_product_packaging_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_product_packaging_stamp BEFORE INSERT OR UPDATE ON public.product_packaging FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: products trg_products_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_products_audit_delete AFTER DELETE ON public.products FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('product_id');


--
-- Name: products trg_products_pos_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_products_pos_stamp BEFORE INSERT OR UPDATE OF name, size, color, base_unit, is_weighted, is_active, product_type, scale_code ON public.products FOR EACH ROW EXECUTE FUNCTION public.fn_pos_item_stamp('name', 'size', 'color', 'base_unit', 'is_weighted', 'is_active', 'product_type', 'scale_code');


--
-- Name: products trg_products_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_products_stamp BEFORE INSERT OR UPDATE ON public.products FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated('stock_qty', 'avg_cost');


--
-- Name: purchase_invoices trg_purchase_invoices_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_purchase_invoices_audit_delete AFTER DELETE ON public.purchase_invoices FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('purchase_id');


--
-- Name: purchase_invoices trg_purchase_invoices_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_purchase_invoices_stamp BEFORE UPDATE ON public.purchase_invoices FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_updated('paid_lbp');


--
-- Name: registers trg_registers_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_registers_audit_delete AFTER DELETE ON public.registers FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('register_id');


--
-- Name: registers trg_registers_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_registers_stamp BEFORE INSERT OR UPDATE ON public.registers FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: sales_invoices trg_sales_invoices_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_sales_invoices_audit_delete AFTER DELETE ON public.sales_invoices FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('sale_id');


--
-- Name: sales_invoices trg_sales_invoices_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_sales_invoices_stamp BEFORE UPDATE ON public.sales_invoices FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_updated();


--
-- Name: settings trg_settings_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_settings_audit_delete AFTER DELETE ON public.settings FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('key');


--
-- Name: settings trg_settings_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_settings_stamp BEFORE INSERT OR UPDATE ON public.settings FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: shifts trg_shifts_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_shifts_audit_delete AFTER DELETE ON public.shifts FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('shift_id');


--
-- Name: shifts trg_shifts_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_shifts_stamp BEFORE UPDATE ON public.shifts FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_updated();


--
-- Name: stock_adjustments trg_stock_adjustments_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_stock_adjustments_audit_delete AFTER DELETE ON public.stock_adjustments FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('adjustment_id');


--
-- Name: stock_adjustments trg_stock_adjustments_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_stock_adjustments_stamp BEFORE UPDATE ON public.stock_adjustments FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_updated();


--
-- Name: suppliers trg_suppliers_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_suppliers_audit_delete AFTER DELETE ON public.suppliers FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('supplier_id');


--
-- Name: suppliers trg_suppliers_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_suppliers_stamp BEFORE INSERT OR UPDATE ON public.suppliers FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: users trg_users_audit_delete; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_users_audit_delete AFTER DELETE ON public.users FOR EACH ROW EXECUTE FUNCTION public.fn_audit_delete('user_id');


--
-- Name: users trg_users_stamp; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_users_stamp BEFORE INSERT OR UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION public.fn_stamp_created_updated();


--
-- Name: audit_log audit_log_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: bundle_items bundle_items_bundle_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundle_items
    ADD CONSTRAINT bundle_items_bundle_product_id_fkey FOREIGN KEY (bundle_product_id) REFERENCES public.products(product_id) ON DELETE CASCADE;


--
-- Name: bundle_items bundle_items_component_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bundle_items
    ADD CONSTRAINT bundle_items_component_product_id_fkey FOREIGN KEY (component_product_id) REFERENCES public.products(product_id);


--
-- Name: categories categories_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: categories categories_parent_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES public.categories(category_id);


--
-- Name: categories categories_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: company company_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.company
    ADD CONSTRAINT company_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: company company_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.company
    ADD CONSTRAINT company_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: customer_payment_allocations customer_payment_allocations_payment_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payment_allocations
    ADD CONSTRAINT customer_payment_allocations_payment_id_fkey FOREIGN KEY (payment_id) REFERENCES public.customer_payments(payment_id) ON DELETE CASCADE;


--
-- Name: customer_payment_allocations customer_payment_allocations_sale_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payment_allocations
    ADD CONSTRAINT customer_payment_allocations_sale_id_fkey FOREIGN KEY (sale_id) REFERENCES public.sales_invoices(sale_id);


--
-- Name: customer_payments customer_payments_currency_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payments
    ADD CONSTRAINT customer_payments_currency_fkey FOREIGN KEY (currency) REFERENCES public.currencies(code);


--
-- Name: customer_payments customer_payments_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payments
    ADD CONSTRAINT customer_payments_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id);


--
-- Name: customer_payments customer_payments_shift_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payments
    ADD CONSTRAINT customer_payments_shift_id_fkey FOREIGN KEY (shift_id) REFERENCES public.shifts(shift_id);


--
-- Name: customer_payments customer_payments_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customer_payments
    ADD CONSTRAINT customer_payments_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: customers customers_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: customers customers_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: exchange_rates exchange_rates_currency_code_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exchange_rates
    ADD CONSTRAINT exchange_rates_currency_code_fkey FOREIGN KEY (currency_code) REFERENCES public.currencies(code);


--
-- Name: exchange_rates exchange_rates_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exchange_rates
    ADD CONSTRAINT exchange_rates_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: pos_quick_buttons pos_quick_buttons_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: pos_quick_buttons pos_quick_buttons_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_group_id_fkey FOREIGN KEY (group_id) REFERENCES public.pos_quick_groups(group_id) ON DELETE SET NULL;


--
-- Name: pos_quick_buttons pos_quick_buttons_packaging_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_packaging_id_fkey FOREIGN KEY (packaging_id) REFERENCES public.product_packaging(packaging_id) ON DELETE CASCADE;


--
-- Name: pos_quick_buttons pos_quick_buttons_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_buttons
    ADD CONSTRAINT pos_quick_buttons_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: pos_quick_groups pos_quick_groups_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_groups
    ADD CONSTRAINT pos_quick_groups_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: pos_quick_groups pos_quick_groups_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pos_quick_groups
    ADD CONSTRAINT pos_quick_groups_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: product_packaging product_packaging_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: product_packaging product_packaging_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id) ON DELETE CASCADE;


--
-- Name: product_packaging product_packaging_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.product_packaging
    ADD CONSTRAINT product_packaging_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: products products_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(category_id);


--
-- Name: products products_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: products products_parent_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES public.products(product_id);


--
-- Name: products products_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: promotion_items promotion_items_packaging_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotion_items
    ADD CONSTRAINT promotion_items_packaging_id_fkey FOREIGN KEY (packaging_id) REFERENCES public.product_packaging(packaging_id) ON DELETE CASCADE;


--
-- Name: promotion_items promotion_items_promotion_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotion_items
    ADD CONSTRAINT promotion_items_promotion_id_fkey FOREIGN KEY (promotion_id) REFERENCES public.promotions(promotion_id) ON DELETE CASCADE;


--
-- Name: promotions promotions_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT promotions_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(category_id) ON DELETE CASCADE;


--
-- Name: purchase_invoices purchase_invoices_currency_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_currency_fkey FOREIGN KEY (currency) REFERENCES public.currencies(code);


--
-- Name: purchase_invoices purchase_invoices_posted_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_posted_by_fkey FOREIGN KEY (posted_by) REFERENCES public.users(user_id);


--
-- Name: purchase_invoices purchase_invoices_return_of_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_return_of_id_fkey FOREIGN KEY (return_of_id) REFERENCES public.purchase_invoices(purchase_id);


--
-- Name: purchase_invoices purchase_invoices_supplier_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_supplier_id_fkey FOREIGN KEY (supplier_id) REFERENCES public.suppliers(supplier_id);


--
-- Name: purchase_invoices purchase_invoices_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: purchase_invoices purchase_invoices_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_invoices
    ADD CONSTRAINT purchase_invoices_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: purchase_lines purchase_lines_packaging_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_lines
    ADD CONSTRAINT purchase_lines_packaging_id_fkey FOREIGN KEY (packaging_id) REFERENCES public.product_packaging(packaging_id);


--
-- Name: purchase_lines purchase_lines_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_lines
    ADD CONSTRAINT purchase_lines_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id);


--
-- Name: purchase_lines purchase_lines_purchase_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.purchase_lines
    ADD CONSTRAINT purchase_lines_purchase_id_fkey FOREIGN KEY (purchase_id) REFERENCES public.purchase_invoices(purchase_id) ON DELETE CASCADE;


--
-- Name: registers registers_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registers
    ADD CONSTRAINT registers_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: registers registers_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.registers
    ADD CONSTRAINT registers_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: sales_invoices sales_invoices_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id);


--
-- Name: sales_invoices sales_invoices_original_sale_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_original_sale_id_fkey FOREIGN KEY (original_sale_id) REFERENCES public.sales_invoices(sale_id);


--
-- Name: sales_invoices sales_invoices_shift_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_shift_id_fkey FOREIGN KEY (shift_id) REFERENCES public.shifts(shift_id);


--
-- Name: sales_invoices sales_invoices_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: sales_invoices sales_invoices_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_invoices
    ADD CONSTRAINT sales_invoices_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: sales_line_components sales_line_components_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_line_components
    ADD CONSTRAINT sales_line_components_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id);


--
-- Name: sales_line_components sales_line_components_sale_line_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_line_components
    ADD CONSTRAINT sales_line_components_sale_line_id_fkey FOREIGN KEY (sale_line_id) REFERENCES public.sales_lines(sale_line_id) ON DELETE CASCADE;


--
-- Name: sales_lines sales_lines_packaging_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_lines
    ADD CONSTRAINT sales_lines_packaging_id_fkey FOREIGN KEY (packaging_id) REFERENCES public.product_packaging(packaging_id);


--
-- Name: sales_lines sales_lines_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_lines
    ADD CONSTRAINT sales_lines_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id);


--
-- Name: sales_lines sales_lines_return_of_line_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_lines
    ADD CONSTRAINT sales_lines_return_of_line_id_fkey FOREIGN KEY (return_of_line_id) REFERENCES public.sales_lines(sale_line_id);


--
-- Name: sales_lines sales_lines_sale_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_lines
    ADD CONSTRAINT sales_lines_sale_id_fkey FOREIGN KEY (sale_id) REFERENCES public.sales_invoices(sale_id) ON DELETE CASCADE;


--
-- Name: sales_payments sales_payments_currency_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_payments
    ADD CONSTRAINT sales_payments_currency_fkey FOREIGN KEY (currency) REFERENCES public.currencies(code);


--
-- Name: sales_payments sales_payments_sale_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales_payments
    ADD CONSTRAINT sales_payments_sale_id_fkey FOREIGN KEY (sale_id) REFERENCES public.sales_invoices(sale_id) ON DELETE CASCADE;


--
-- Name: settings settings_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.settings
    ADD CONSTRAINT settings_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: settings settings_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.settings
    ADD CONSTRAINT settings_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: shifts shifts_register_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.shifts
    ADD CONSTRAINT shifts_register_id_fkey FOREIGN KEY (register_id) REFERENCES public.registers(register_id);


--
-- Name: shifts shifts_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.shifts
    ADD CONSTRAINT shifts_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: shifts shifts_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.shifts
    ADD CONSTRAINT shifts_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: stock_adjustment_lines stock_adjustment_lines_adjustment_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustment_lines
    ADD CONSTRAINT stock_adjustment_lines_adjustment_id_fkey FOREIGN KEY (adjustment_id) REFERENCES public.stock_adjustments(adjustment_id) ON DELETE CASCADE;


--
-- Name: stock_adjustment_lines stock_adjustment_lines_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustment_lines
    ADD CONSTRAINT stock_adjustment_lines_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id);


--
-- Name: stock_adjustments stock_adjustments_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: stock_adjustments stock_adjustments_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: stock_movements stock_movements_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(product_id);


--
-- Name: stock_movements stock_movements_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: supplier_payments supplier_payments_currency_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.supplier_payments
    ADD CONSTRAINT supplier_payments_currency_fkey FOREIGN KEY (currency) REFERENCES public.currencies(code);


--
-- Name: supplier_payments supplier_payments_purchase_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.supplier_payments
    ADD CONSTRAINT supplier_payments_purchase_id_fkey FOREIGN KEY (purchase_id) REFERENCES public.purchase_invoices(purchase_id);


--
-- Name: supplier_payments supplier_payments_supplier_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.supplier_payments
    ADD CONSTRAINT supplier_payments_supplier_id_fkey FOREIGN KEY (supplier_id) REFERENCES public.suppliers(supplier_id);


--
-- Name: supplier_payments supplier_payments_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.supplier_payments
    ADD CONSTRAINT supplier_payments_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: suppliers suppliers_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suppliers
    ADD CONSTRAINT suppliers_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: suppliers suppliers_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.suppliers
    ADD CONSTRAINT suppliers_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- Name: users users_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: users users_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public.users(user_id);


--
-- PostgreSQL database dump complete
--

