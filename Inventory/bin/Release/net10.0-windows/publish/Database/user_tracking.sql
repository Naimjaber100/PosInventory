-- Who did what: every table is linked to the user who changed it.
--
-- The app tells the database who is logged in: before each call BaseRepository runs
--   set_config('app.user_id', '<id>', false)        (read back with fn_app_user_id()).
-- Triggers then fill the user columns, so every save path is covered (also future functions):
--   * master data (items, packagings, categories, suppliers, customers, registers, POS buttons, settings, users):
--       created_by / created_at on insert, updated_by / updated_at on a real change
--   * documents that already store their creator in user_id (purchases, sales, stock adjustments, shifts):
--       updated_by / updated_at; purchases also posted_by when they are posted
--   * deletes of any of them: a row in audit_log (who, when, which table, the deleted row as JSON)
-- Rows that existed before this script keep created_by = NULL (unknown).
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f user_tracking.sql

-- The logged-in user of this connection (NULL when the app did not say)
CREATE OR REPLACE FUNCTION public.fn_app_user_id()
RETURNS integer
LANGUAGE sql STABLE
AS $$
    SELECT CASE WHEN current_setting('app.user_id', true) ~ '^\d+$' THEN current_setting('app.user_id', true)::integer END;
$$;

-- ---------------------------------------------------------------------------
-- Trigger functions
-- ---------------------------------------------------------------------------

-- Master data: created_by on insert; updated_by / updated_at when something really changed.
-- Trigger arguments = columns whose changes do not count (e.g. stock_qty changed by a sale).
CREATE OR REPLACE FUNCTION public.fn_stamp_created_updated()
RETURNS trigger
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

-- Documents (creator already in user_id): updated_by / updated_at; purchases also posted_by
CREATE OR REPLACE FUNCTION public.fn_stamp_updated()
RETURNS trigger
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

-- Deletes: one audit_log row with the deleted row. Trigger argument = the key column.
-- Held sales are not logged: finishing or re-holding one deletes the held copy on purpose.
CREATE OR REPLACE FUNCTION public.fn_audit_delete()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_key text := to_jsonb(OLD) ->> TG_ARGV[0];
BEGIN
    IF TG_TABLE_NAME = 'sales_invoices' AND (to_jsonb(OLD) ->> 'status') = '0' THEN RETURN OLD; END IF;
    INSERT INTO audit_log (user_id, action, table_name, record_id, details)
    VALUES (fn_app_user_id(), 'delete', TG_TABLE_NAME,
            CASE WHEN v_key ~ '^\d+$' THEN v_key::integer END,
            to_jsonb(OLD));
    RETURN OLD;
END $$;

-- ---------------------------------------------------------------------------
-- Columns and triggers
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    t record;
BEGIN
    -- Master data: table, key column, changes that do not count
    FOR t IN SELECT * FROM (VALUES
        ('products',          'product_id',   ARRAY['stock_qty', 'avg_cost']),
        ('product_packaging', 'packaging_id', ARRAY[]::text[]),
        ('categories',        'category_id',  ARRAY[]::text[]),
        ('suppliers',         'supplier_id',  ARRAY[]::text[]),
        ('customers',         'customer_id',  ARRAY[]::text[]),
        ('registers',         'register_id',  ARRAY[]::text[]),
        ('pos_quick_groups',  'group_id',     ARRAY[]::text[]),
        ('pos_quick_buttons', 'button_id',    ARRAY[]::text[]),
        ('settings',          'key',          ARRAY[]::text[]),
        ('company',           'company_id',   ARRAY[]::text[]),
        ('users',             'user_id',      ARRAY[]::text[])
    ) AS x(tbl, key_col, ignored)
    LOOP
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS created_by integer REFERENCES users(user_id)', t.tbl);
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS created_at timestamptz', t.tbl);   -- old rows: unknown
        EXECUTE format('ALTER TABLE public.%I ALTER COLUMN created_at SET DEFAULT now()', t.tbl);
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS updated_by integer REFERENCES users(user_id)', t.tbl);
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS updated_at timestamptz', t.tbl);

        EXECUTE format('DROP TRIGGER IF EXISTS trg_%s_stamp ON public.%I', t.tbl, t.tbl);
        EXECUTE format('CREATE TRIGGER trg_%s_stamp BEFORE INSERT OR UPDATE ON public.%I FOR EACH ROW EXECUTE FUNCTION fn_stamp_created_updated(%s)',
                       t.tbl, t.tbl, coalesce((SELECT string_agg(quote_literal(c), ', ') FROM unnest(t.ignored) c), ''));
        EXECUTE format('DROP TRIGGER IF EXISTS trg_%s_audit_delete ON public.%I', t.tbl, t.tbl);
        EXECUTE format('CREATE TRIGGER trg_%s_audit_delete AFTER DELETE ON public.%I FOR EACH ROW EXECUTE FUNCTION fn_audit_delete(%L)',
                       t.tbl, t.tbl, t.key_col);
    END LOOP;

    -- Documents: table, key column, changes that do not count (supplier payments change paid_lbp)
    FOR t IN SELECT * FROM (VALUES
        ('purchase_invoices', 'purchase_id',   ARRAY['paid_lbp']),
        ('sales_invoices',    'sale_id',       ARRAY[]::text[]),
        ('stock_adjustments', 'adjustment_id', ARRAY[]::text[]),
        ('shifts',            'shift_id',      ARRAY[]::text[])
    ) AS x(tbl, key_col, ignored)
    LOOP
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS updated_by integer REFERENCES users(user_id)', t.tbl);
        EXECUTE format('ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS updated_at timestamptz', t.tbl);

        EXECUTE format('DROP TRIGGER IF EXISTS trg_%s_stamp ON public.%I', t.tbl, t.tbl);
        EXECUTE format('CREATE TRIGGER trg_%s_stamp BEFORE UPDATE ON public.%I FOR EACH ROW EXECUTE FUNCTION fn_stamp_updated(%s)',
                       t.tbl, t.tbl, coalesce((SELECT string_agg(quote_literal(c), ', ') FROM unnest(t.ignored) c), ''));
        EXECUTE format('DROP TRIGGER IF EXISTS trg_%s_audit_delete ON public.%I', t.tbl, t.tbl);
        EXECUTE format('CREATE TRIGGER trg_%s_audit_delete AFTER DELETE ON public.%I FOR EACH ROW EXECUTE FUNCTION fn_audit_delete(%L)',
                       t.tbl, t.tbl, t.key_col);
    END LOOP;

    ALTER TABLE public.purchase_invoices ADD COLUMN IF NOT EXISTS posted_by integer REFERENCES users(user_id);
END $$;

-- Purchases posted before tracking: they were posted by their creator (the only user then was admin)
ALTER TABLE public.purchase_invoices DISABLE TRIGGER trg_purchase_invoices_stamp;
UPDATE public.purchase_invoices SET posted_by = user_id WHERE status = 1 AND posted_by IS NULL;
ALTER TABLE public.purchase_invoices ENABLE TRIGGER trg_purchase_invoices_stamp;

-- Reports per user (sales by user / shift later) go through these
CREATE INDEX IF NOT EXISTS ix_sales_user_date ON public.sales_invoices (user_id, invoice_date);
CREATE INDEX IF NOT EXISTS ix_purchase_user ON public.purchase_invoices (user_id);
CREATE INDEX IF NOT EXISTS ix_audit_log_table ON public.audit_log (table_name, record_id);
