-- Every id counter goes past the highest id of its table, so the next sale, item, user... gets a free id.
-- Needed after rows were added with their ids (seed.sql, data copied from another database, a data-only restore):
-- otherwise saving fails with "duplicate key value violates unique constraint ..._pkey".
-- Safe to re-run; only moves counters forward. Setup-Server.ps1 runs it on every install and upgrade.
DO $$
DECLARE
    r record;
    v_max bigint;
BEGIN
    FOR r IN
        SELECT format('%I.%I', n.nspname, c.relname) AS tbl, a.attname AS col,
               pg_get_serial_sequence(format('%I.%I', n.nspname, c.relname), a.attname) AS seq
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
        WHERE c.relkind = 'r' AND n.nspname = 'public'
          AND pg_get_serial_sequence(format('%I.%I', n.nspname, c.relname), a.attname) IS NOT NULL
    LOOP
        EXECUTE format('SELECT max(%I) FROM %s', r.col, r.tbl) INTO v_max;
        IF v_max IS NOT NULL THEN PERFORM setval(r.seq, v_max, true); END IF;
    END LOOP;
END $$;
