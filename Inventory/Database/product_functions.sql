-- Product / category functions used by the Inventory app (ProductRepository, CategoryRepository).
-- Safe to re-run: every function is CREATE OR REPLACE.
-- Apply:  psql -h localhost -U postgres -d Pos -f product_functions.sql

-- ---------------------------------------------------------------------------
-- Barcodes: other ways the same label can be written, for a scan that does not match exactly
-- (used by fn_purchase_find_packaging and fn_pos_find_item; the cashier's memory list does the same, PosItemCache).
--   - without its last digit, when that digit is the right check digit (EAN-13 / EAN-8 / UPC-A, GS1):
--     the item was saved without it, the scanner sends it;
--   - without a leading 0 (EAN-13 "0" + UPC-A) and with one (the other way round).
-- Only digits, 7 or more; anything else has no other way.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_barcode_check_digit(p_digits text)
RETURNS integer
LANGUAGE sql IMMUTABLE
AS $$
    -- GS1: from the right, the digits are weighted 3, 1, 3, 1...
    SELECT (10 - sum(substr(reverse(p_digits), i, 1)::int * CASE WHEN i % 2 = 1 THEN 3 ELSE 1 END) % 10) % 10
    FROM generate_series(1, length(p_digits)) i;
$$;

CREATE OR REPLACE FUNCTION public.fn_barcode_variants(p_code text)
RETURNS text[]
LANGUAGE sql IMMUTABLE
AS $$
    SELECT CASE WHEN p_code ~ '^\d{7,}$' THEN
        array_remove(ARRAY[
            CASE WHEN right(p_code, 1)::int = fn_barcode_check_digit(left(p_code, -1)) THEN left(p_code, -1) END,
            CASE WHEN p_code LIKE '0%' THEN substr(p_code, 2) END,
            '0' || p_code], NULL)
    ELSE '{}'::text[] END;
$$;

-- ---------------------------------------------------------------------------
-- Categories (a tree: parent_id points to the parent category, NULL = top level)
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_category_get_all();

-- Every category with its full path ("Shampoo > Sunsilk"), depth (0 = top level)
-- and the number of items directly in it. Ordered so parents come before children.
CREATE OR REPLACE FUNCTION public.fn_category_get_all()
RETURNS TABLE (category_id integer, name varchar, parent_id integer, path text, level integer, item_count bigint)
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

-- Adds a category under p_parent_id (NULL = top level); returns the new category_id.
CREATE OR REPLACE FUNCTION public.fn_category_add(p_name text, p_parent_id integer)
RETURNS integer
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

-- Renames and/or moves a category. A category cannot be moved under itself or one of its children.
CREATE OR REPLACE FUNCTION public.fn_category_update(p_category_id integer, p_name text, p_parent_id integer)
RETURNS void
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

-- Deletes an empty category. Refuses when it still has sub-categories or items.
CREATE OR REPLACE FUNCTION public.fn_category_delete(p_category_id integer)
RETURNS void
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

-- Finds a category by its path ("Shampoo > Sunsilk"), creating any missing level.
-- Names match case-insensitively. Returns the category_id of the last level (NULL for an empty path).
CREATE OR REPLACE FUNCTION public.fn_category_ensure_path(p_path text)
RETURNS integer
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

-- ---------------------------------------------------------------------------
-- Products: read (sale_price / barcode come from the default packaging)
-- Dropped first so the returned columns can change on re-run.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_product_get_by_id(integer);
DROP FUNCTION IF EXISTS public.fn_product_get_all();
DROP FUNCTION IF EXISTS public.fn_product_get_all(integer, integer);
DROP FUNCTION IF EXISTS public.fn_product_get_all(integer, integer, integer);
DROP FUNCTION IF EXISTS public.fn_product_get_all(integer, integer, integer, text);
DROP FUNCTION IF EXISTS public.fn_product_get_all(integer, integer, integer, text, text);

-- One page of products ordered by name. Every row carries total_count (all matching
-- products, not just this page) so the caller gets the page and the total in one call.
-- total_count must stay the LAST column: BaseRepository.GetPagedAsync splits on it.
-- p_category_id: NULL = all items, 0 = items without a category,
--                otherwise that category and all of its sub-categories.
-- p_search:      NULL/blank = no search. Otherwise matches items whose name contains every
--                word (any order, case-insensitive) or that have a packaging whose barcode
--                equals or starts with the text. Exact barcode matches are listed first.
CREATE OR REPLACE FUNCTION public.fn_product_get_all(
    p_page        integer DEFAULT 1,
    p_page_size   integer DEFAULT 50,
    p_category_id integer DEFAULT NULL,
    p_search      text    DEFAULT NULL,
    p_stock       text    DEFAULT NULL)   -- NULL = all, 'in' = above the reorder level, 'low' = 0 < stock <= reorder level, 'out' = 0 or less
RETURNS TABLE (
    product_id integer, parent_id integer, name varchar, category_id integer, category_name varchar,
    product_type smallint, size varchar, color varchar, base_unit varchar, is_weighted boolean,
    avg_cost numeric, stock_qty numeric, reorder_level numeric, sale_price numeric, barcode varchar,
    is_active boolean, created_at timestamptz, updated_at timestamptz,
    total_count bigint)
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
    ORDER BY (v_search IS NOT NULL AND EXISTS (SELECT 1 FROM product_packaging b
                                              WHERE b.product_id = p.product_id AND b.barcode = v_search)) DESC,
             p.name, p.product_id          -- product_id keeps the order stable between pages
    LIMIT p_page_size
    OFFSET (p_page - 1) * p_page_size;
END $$;

CREATE OR REPLACE FUNCTION public.fn_product_get_by_id(p_product_id integer)
RETURNS TABLE (
    product_id integer, parent_id integer, name varchar, category_id integer, category_name varchar,
    product_type smallint, size varchar, color varchar, base_unit varchar, is_weighted boolean,
    avg_cost numeric, stock_qty numeric, reorder_level numeric, sale_price numeric, barcode varchar,
    is_active boolean, created_at timestamptz, updated_at timestamptz)
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

-- ---------------------------------------------------------------------------
-- Packagings: the ways one item is bought and sold. Stock is always counted in the
-- item's base unit; factor = how many base units one packaging contains.
--   e.g. Cola (base unit "can"):  can  factor 1  barcode A  price 50,000
--                                 box  factor 24 barcode B  price 1,100,000
-- Buying 2 boxes adds 48 cans to stock.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_product_get_packagings(integer);

-- Active packagings of one item, default first then by size.
CREATE OR REPLACE FUNCTION public.fn_product_get_packagings(p_product_id integer)
RETURNS TABLE (
    packaging_id integer, name varchar, factor numeric, barcode varchar, sale_price numeric,
    is_default boolean, can_sell boolean, can_purchase boolean)
LANGUAGE sql STABLE
AS $$
    SELECT pk.packaging_id, pk.name, pk.factor, pk.barcode, pk.sale_price,
           pk.is_default, pk.can_sell, pk.can_purchase
    FROM product_packaging pk
    WHERE pk.product_id = p_product_id AND pk.is_active
    ORDER BY pk.is_default DESC, pk.factor, pk.packaging_id;
$$;

-- Replaces the packagings of one item with p_packagings, a JSON array of
--   { "packaging_id" (null = new), "name", "factor", "barcode", "sale_price",
--     "is_default", "can_sell", "can_purchase" }
-- Exactly one must be the default. Packagings left out are deleted, or deactivated
-- when purchases or sales already use them (so that history stays intact).
-- A new row with the same name as a removed one brings that one back.
CREATE OR REPLACE FUNCTION public.fn_product_save_packagings(p_product_id integer, p_packagings jsonb)
RETURNS void
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

-- ---------------------------------------------------------------------------
-- Products: add
-- Creates the product, its packagings and, for standard items, the opening
-- stock through fn_add_opening_stock.
-- p_packagings (JSON, see fn_product_save_packagings): when NULL, one default
-- packaging is created from the base unit, p_sale_price and p_barcode (used by import).
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_product_add(text, integer, smallint, text, text, text, boolean, numeric, numeric, boolean, numeric, text, numeric, integer);
DROP FUNCTION IF EXISTS public.fn_product_add(text, integer, smallint, text, text, text, boolean, numeric, numeric, boolean, numeric, text, numeric, integer, jsonb);

CREATE OR REPLACE FUNCTION public.fn_product_add(
    p_name          text,
    p_category_id   integer,
    p_product_type  smallint,
    p_size          text,
    p_color         text,
    p_base_unit     text,
    p_is_weighted   boolean,
    p_avg_cost      numeric,
    p_reorder_level numeric,
    p_is_active     boolean,
    p_sale_price    numeric,
    p_barcode       text,
    p_opening_qty   numeric,
    p_user_id       integer,
    p_packagings    jsonb DEFAULT NULL)
RETURNS integer
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

-- ---------------------------------------------------------------------------
-- Products: update
-- Stock and average cost are not edited here; they change only through
-- stock functions (opening stock, purchases, adjustments, sales).
-- p_packagings (JSON, see fn_product_save_packagings): when NULL, only the default
-- packaging's p_sale_price and p_barcode are updated.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_product_update(integer, text, integer, smallint, text, text, text, boolean, numeric, boolean, numeric, text);
DROP FUNCTION IF EXISTS public.fn_product_update(integer, text, integer, smallint, text, text, text, boolean, numeric, boolean, numeric, text, jsonb);

CREATE OR REPLACE FUNCTION public.fn_product_update(
    p_product_id    integer,
    p_name          text,
    p_category_id   integer,
    p_product_type  smallint,
    p_size          text,
    p_color         text,
    p_base_unit     text,
    p_is_weighted   boolean,
    p_reorder_level numeric,
    p_is_active     boolean,
    p_sale_price    numeric,
    p_barcode       text,
    p_packagings    jsonb DEFAULT NULL)
RETURNS void
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

-- ---------------------------------------------------------------------------
-- Products: delete
-- Items with history (stock movements, sales, purchases, variants, bundles...)
-- cannot be removed without breaking that history, so they are deactivated.
-- Returns true when the row was deleted, false when it was deactivated.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_product_delete(p_product_id integer)
RETURNS boolean
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

-- ---------------------------------------------------------------------------
-- Products: import
-- p_rows is a JSON array of objects:
--   { "row_number", "name", "category", "product_type", "size", "color", "base_unit",
--     "is_weighted", "avg_cost", "sale_price", "barcode", "opening_qty", "reorder_level", "is_active" }
-- Each row goes through fn_product_add. "category" may be a path ("Shampoo > Sunsilk");
-- each level is matched by name (case-insensitive) and created when missing
-- (fn_category_ensure_path). All or nothing: the first bad row
-- stops the import, rolls everything back and reports its row number.
-- Returns the number of imported products.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_product_import(p_rows jsonb, p_user_id integer)
RETURNS integer
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

-- ---------------------------------------------------------------------------
-- Switches an item on or off (the Active toggle of the item list). Nothing else changes.
-- ---------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_product_set_active(integer, boolean);
CREATE OR REPLACE FUNCTION public.fn_product_set_active(p_product_id integer, p_is_active boolean)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE products SET is_active = p_is_active, updated_at = now() WHERE product_id = p_product_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Item % not found.', p_product_id; END IF;
END $$;
