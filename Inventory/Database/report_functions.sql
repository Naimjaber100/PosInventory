-- Reports (back office: Reports menu). Each report is one function: the database adds up the invoices
-- and sends back only the result rows, so a report stays fast as sales grow.
-- Amounts are in the primary currency. Days follow the database time zone (show timezone).
-- Safe to re-run: functions are dropped and recreated.
-- Apply:  psql -h localhost -U postgres -d Pos -f report_functions.sql

DROP FUNCTION IF EXISTS public.fn_report_daily_sales(date, date, integer);

-- ---------------------------------------------------------------------------
-- 1. Daily sales summary: one row per day from p_from to p_to (days without sales too).
-- p_user_id NULL = every cashier.
--   gross      = what was sold before any discount (quantity x price)
--   discounts  = line discounts + invoice discounts
--   sales      = what the sales invoices came to (gross - discounts)
--   returns    = what was refunded on return invoices
--   net_sales  = sales - returns
--   cost       = cost of what was sold - cost of what came back
--   profit     = net_sales - cost
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_daily_sales(p_from date, p_to date, p_user_id integer DEFAULT NULL)
RETURNS TABLE (day date, sale_count bigint, return_count bigint, gross numeric, discounts numeric,
               sales numeric, returns numeric, net_sales numeric, cost numeric, profit numeric)
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
    FROM generate_series(p_from, p_to, interval '1 day') d
    LEFT JOIN per_day p ON p.day = d::date
    ORDER BY d;
$$;
