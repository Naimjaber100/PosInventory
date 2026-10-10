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
