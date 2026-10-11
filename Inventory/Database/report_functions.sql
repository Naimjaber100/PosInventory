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
    -- timestamp without time zone: with a time zone, the daylight-saving change (end of March) shifts
    -- every following day to 01:00 and the last day of the period would be lost
    FROM generate_series(p_from::timestamp, p_to::timestamp, interval '1 day') d
    LEFT JOIN per_day p ON p.day = d::date
    ORDER BY d;
$$;

DROP FUNCTION IF EXISTS public.fn_report_sales_by_item(date, date, integer);

-- ---------------------------------------------------------------------------
-- 2. Sales by item: one row per item sold or returned from p_from to p_to, best selling first.
-- p_user_id NULL = every cashier. Quantities are in the item's base unit (pcs, kg...), so a
-- box of 12 counts as 12. Returns are subtracted.
--   gross     = quantity x price before any discount
--   discounts = line discount + the item's share of the invoice discount
--   net_sales = what was sold - what was refunded (after discounts)
--   cost      = cost of what was sold - cost of what came back (average cost when sold)
--   profit    = net_sales - cost
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_sales_by_item(p_from date, p_to date, p_user_id integer DEFAULT NULL)
RETURNS TABLE (product_id integer, item_name text, category_name varchar, base_unit varchar,
               qty_sold numeric, qty_returned numeric, net_qty numeric,
               gross numeric, discounts numeric, net_sales numeric, cost numeric, profit numeric, invoice_count bigint)
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
