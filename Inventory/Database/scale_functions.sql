-- Scale products: items weighed (or priced) on a label scale. The scale prints a barcode with the item's
-- scale code (PLU) and the weight or the price in it; the cashier reads it (ScaleBarcode.cs, settings in
-- appsettings.json "ScaleBarcode") and finds the item here by its scale code.
-- products.scale_code: NULL = not a scale product; digits only, unique.
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f scale_functions.sql

ALTER TABLE public.products ADD COLUMN IF NOT EXISTS scale_code varchar(10);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'products_scale_code_check') THEN
        ALTER TABLE public.products ADD CONSTRAINT products_scale_code_check CHECK (scale_code IS NULL OR scale_code ~ '^[0-9]+$');
    END IF;
END $$;
CREATE UNIQUE INDEX IF NOT EXISTS ux_products_scale_code ON public.products (scale_code) WHERE scale_code IS NOT NULL;

DROP FUNCTION IF EXISTS public.fn_product_get_scale_code(integer);
DROP FUNCTION IF EXISTS public.fn_product_set_scale_code(integer, text);
DROP FUNCTION IF EXISTS public.fn_pos_find_scale_item(text);

-- The scale code of an item (NULL = not a scale product)
CREATE OR REPLACE FUNCTION public.fn_product_get_scale_code(p_product_id integer)
RETURNS varchar
LANGUAGE sql STABLE
AS $$
    SELECT scale_code FROM products WHERE product_id = p_product_id;
$$;

-- Makes an item a scale product with this code, or not a scale product (blank / NULL)
CREATE OR REPLACE FUNCTION public.fn_product_set_scale_code(p_product_id integer, p_scale_code text)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_code text := nullif(trim(p_scale_code), '');
    v_other text;
BEGIN
    IF v_code IS NOT NULL AND v_code !~ '^[0-9]+$' THEN RAISE EXCEPTION 'The scale code can only have digits.'; END IF;
    SELECT name INTO v_other FROM products WHERE scale_code = v_code AND product_id <> p_product_id;
    IF v_other IS NOT NULL THEN RAISE EXCEPTION 'Scale code % is already used by "%".', v_code, v_other; END IF;

    UPDATE products SET scale_code = v_code WHERE product_id = p_product_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', p_product_id; END IF;
END $$;

-- The cashier: the item of a scale label (same columns as fn_pos_find_item).
-- It is sold in its base unit (the packaging that contains 1), else its default packaging.
CREATE OR REPLACE FUNCTION public.fn_pos_find_scale_item(p_scale_code text)
RETURNS TABLE (
    packaging_id integer, product_id integer, item_name text, packaging_name varchar, barcode varchar,
    sale_price numeric, factor numeric, is_weighted boolean, base_unit varchar, stock_qty numeric)
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

-- ===========================================================================
-- Label scales the items are sent to (Settings > Label scales, ScalesForm)
-- ===========================================================================
-- scales: one row per scale in the shop. driver = which model / way of sending (Scales/ScaleDrivers.cs);
--         the connection columns used depend on the driver (a file, an IP address and port, or a COM port).
-- scale_items: the items loaded in a scale, with their hot key (the key on the scale's keyboard, 1, 2, ...).
--         The PLU is the item's scale_code (the same code the cashier reads in the label barcode); the price
--         is the price the cashier uses for a scale label (fn_pos_find_scale_item). sent_* = what was last sent,
--         so the screen shows the items whose name, PLU, price or hot key changed since.

CREATE TABLE IF NOT EXISTS public.scales (
    scale_id    serial PRIMARY KEY,
    name        varchar(60) NOT NULL,
    driver      varchar(30) NOT NULL,
    host        varchar(100),
    port        integer,
    serial_port varchar(20),
    baud_rate   integer,
    file_path   varchar(260),
    is_active   boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS public.scale_items (
    scale_id     integer NOT NULL REFERENCES public.scales (scale_id) ON DELETE CASCADE,
    product_id   integer NOT NULL REFERENCES public.products (product_id) ON DELETE CASCADE,
    hot_key      integer CHECK (hot_key IS NULL OR hot_key > 0),
    sent_name    text,
    sent_code    varchar(10),
    sent_price   numeric,
    sent_hot_key integer,
    sent_at      timestamptz,
    PRIMARY KEY (scale_id, product_id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_scale_items_hot_key ON public.scale_items (scale_id, hot_key) WHERE hot_key IS NOT NULL;

DROP FUNCTION IF EXISTS public.fn_scale_get_all();
DROP FUNCTION IF EXISTS public.fn_scale_save(integer, text, text, text, integer, text, integer, text, boolean);
DROP FUNCTION IF EXISTS public.fn_scale_delete(integer);
DROP FUNCTION IF EXISTS public.fn_scale_item_list(integer);
DROP FUNCTION IF EXISTS public.fn_scale_item_add(integer, integer[], integer);
DROP FUNCTION IF EXISTS public.fn_scale_item_save(integer, integer, text, integer);
DROP FUNCTION IF EXISTS public.fn_scale_item_remove(integer, integer[]);
DROP FUNCTION IF EXISTS public.fn_scale_item_mark_sent(integer, jsonb);

CREATE OR REPLACE FUNCTION public.fn_scale_get_all()
RETURNS TABLE (
    scale_id integer, name varchar, driver varchar, host varchar, port integer, serial_port varchar,
    baud_rate integer, file_path varchar, is_active boolean, item_count bigint)
LANGUAGE sql STABLE
AS $$
    SELECT s.scale_id, s.name, s.driver, s.host, s.port, s.serial_port, s.baud_rate, s.file_path, s.is_active,
           (SELECT count(*) FROM scale_items si WHERE si.scale_id = s.scale_id)
    FROM scales s
    ORDER BY s.name, s.scale_id;
$$;

-- Adds (p_scale_id NULL) or updates a scale; returns its scale_id
CREATE OR REPLACE FUNCTION public.fn_scale_save(
    p_scale_id integer, p_name text, p_driver text, p_host text, p_port integer, p_serial_port text,
    p_baud_rate integer, p_file_path text, p_is_active boolean)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id integer;
BEGIN
    IF nullif(trim(p_name), '') IS NULL THEN RAISE EXCEPTION 'Type a name for the scale.'; END IF;
    IF nullif(trim(p_driver), '') IS NULL THEN RAISE EXCEPTION 'Choose the scale model.'; END IF;

    IF p_scale_id IS NULL THEN
        INSERT INTO scales (name, driver, host, port, serial_port, baud_rate, file_path, is_active)
        VALUES (trim(p_name), trim(p_driver), nullif(trim(p_host), ''), p_port, nullif(trim(p_serial_port), ''),
                p_baud_rate, nullif(trim(p_file_path), ''), coalesce(p_is_active, true))
        RETURNING scale_id INTO v_id;
    ELSE
        UPDATE scales SET name = trim(p_name), driver = trim(p_driver), host = nullif(trim(p_host), ''), port = p_port,
                          serial_port = nullif(trim(p_serial_port), ''), baud_rate = p_baud_rate,
                          file_path = nullif(trim(p_file_path), ''), is_active = coalesce(p_is_active, true)
        WHERE scale_id = p_scale_id
        RETURNING scale_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'Scale % not found.', p_scale_id; END IF;
    END IF;
    RETURN v_id;
END $$;

-- Deletes the scale and its item list (the items keep their PLU)
CREATE OR REPLACE FUNCTION public.fn_scale_delete(p_scale_id integer)
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM scales WHERE scale_id = p_scale_id;
$$;

-- The items of a scale: hot key order, then PLU.
-- status: New (never sent), Changed (name, PLU, price or hot key differ from what was sent), Sent.
CREATE OR REPLACE FUNCTION public.fn_scale_item_list(p_scale_id integer)
RETURNS TABLE (
    product_id integer, item_name text, scale_code varchar, hot_key integer, price numeric,
    is_weighted boolean, base_unit varchar, is_active boolean, sent_at timestamptz, status text)
LANGUAGE sql STABLE
AS $$
    SELECT si.product_id, d.item_name, p.scale_code, si.hot_key, pk.sale_price, p.is_weighted, p.base_unit,
           p.is_active, si.sent_at,
           CASE WHEN si.sent_at IS NULL THEN 'New'
                WHEN si.sent_name IS DISTINCT FROM d.item_name OR si.sent_code IS DISTINCT FROM p.scale_code
                  OR si.sent_price IS DISTINCT FROM pk.sale_price OR si.sent_hot_key IS DISTINCT FROM si.hot_key THEN 'Changed'
                ELSE 'Sent' END
    FROM scale_items si
    JOIN products p ON p.product_id = si.product_id
    CROSS JOIN LATERAL (SELECT fn_item_description(p.name::text, p.size::text, p.color::text, NULL) AS item_name) d
    LEFT JOIN LATERAL (
        SELECT x.sale_price FROM product_packaging x
        WHERE x.product_id = p.product_id AND x.is_active AND x.can_sell
        ORDER BY (x.factor = 1) DESC, x.is_default DESC, x.packaging_id
        LIMIT 1) pk ON true
    WHERE si.scale_id = p_scale_id
    ORDER BY si.hot_key NULLS LAST, p.scale_code, d.item_name;
$$;

-- Puts items on a scale. An item without a PLU gets the next free one (p_code_length digits, zero-padded
-- like ScaleBarcode.NormalizeCode). Items already on the scale are left as they are. Returns how many were added.
CREATE OR REPLACE FUNCTION public.fn_scale_item_add(p_scale_id integer, p_product_ids integer[], p_code_length integer)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_id    integer;
    v_type  smallint;
    v_name  text;
    v_code  varchar;
    v_next  bigint;
    v_count integer := 0;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM scales WHERE scale_id = p_scale_id) THEN RAISE EXCEPTION 'Scale % not found.', p_scale_id; END IF;

    FOREACH v_id IN ARRAY coalesce(p_product_ids, '{}') LOOP
        CONTINUE WHEN EXISTS (SELECT 1 FROM scale_items WHERE scale_id = p_scale_id AND product_id = v_id);

        SELECT product_type, name, scale_code INTO v_type, v_name, v_code FROM products WHERE product_id = v_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', v_id; END IF;
        IF v_type <> 1 THEN RAISE EXCEPTION '"%": only a standard item can be on a scale.', v_name; END IF;

        IF v_code IS NULL THEN
            SELECT coalesce(max(scale_code::bigint), 0) + 1 INTO v_next
            FROM products WHERE scale_code IS NOT NULL AND length(scale_code) <= p_code_length;
            IF length(v_next::text) > p_code_length THEN
                RAISE EXCEPTION 'No free PLU of % digits is left for "%".', p_code_length, v_name;
            END IF;
            PERFORM fn_product_set_scale_code(v_id, lpad(v_next::text, p_code_length, '0'));
        END IF;

        INSERT INTO scale_items (scale_id, product_id) VALUES (p_scale_id, v_id);
        v_count := v_count + 1;
    END LOOP;
    RETURN v_count;
END $$;

-- Changes the PLU (on the item: the same for every scale and for the cashier) and the hot key on this scale
CREATE OR REPLACE FUNCTION public.fn_scale_item_save(p_scale_id integer, p_product_id integer, p_scale_code text, p_hot_key integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_other text;
BEGIN
    IF nullif(trim(p_scale_code), '') IS NULL THEN RAISE EXCEPTION 'An item on a scale needs a PLU.'; END IF;
    IF p_hot_key IS NOT NULL AND p_hot_key <= 0 THEN RAISE EXCEPTION 'The hot key is a number from 1.'; END IF;
    IF NOT EXISTS (SELECT 1 FROM scale_items WHERE scale_id = p_scale_id AND product_id = p_product_id) THEN
        RAISE EXCEPTION 'The item is not on this scale.';
    END IF;

    SELECT fn_item_description(p.name::text, p.size::text, p.color::text, NULL) INTO v_other
    FROM scale_items si JOIN products p ON p.product_id = si.product_id
    WHERE si.scale_id = p_scale_id AND si.hot_key = p_hot_key AND si.product_id <> p_product_id;
    IF v_other IS NOT NULL THEN RAISE EXCEPTION 'Hot key % is already used by "%".', p_hot_key, v_other; END IF;

    PERFORM fn_product_set_scale_code(p_product_id, p_scale_code);
    UPDATE scale_items SET hot_key = p_hot_key WHERE scale_id = p_scale_id AND product_id = p_product_id;
END $$;

-- Takes items off the scale's list (the item keeps its PLU)
CREATE OR REPLACE FUNCTION public.fn_scale_item_remove(p_scale_id integer, p_product_ids integer[])
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM scale_items WHERE scale_id = p_scale_id AND product_id = ANY (p_product_ids);
$$;

-- Remembers what was sent: p_items = [{product_id, name, code, price, hot_key}]
CREATE OR REPLACE FUNCTION public.fn_scale_item_mark_sent(p_scale_id integer, p_items jsonb)
RETURNS void
LANGUAGE sql
AS $$
    UPDATE scale_items si
    SET sent_name = j.name, sent_code = j.code, sent_price = j.price, sent_hot_key = j.hot_key, sent_at = now()
    FROM jsonb_to_recordset(p_items) AS j (product_id integer, name text, code varchar, price numeric, hot_key integer)
    WHERE si.scale_id = p_scale_id AND si.product_id = j.product_id;
$$;
