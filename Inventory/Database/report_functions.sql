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

DROP FUNCTION IF EXISTS public.fn_report_sales_by_invoice(date, date, integer);

-- ---------------------------------------------------------------------------
-- 3. Sales by invoice: one row per sale and return invoice from p_from to p_to, in time order.
-- p_user_id NULL = every cashier. A return has negative amounts, so the columns add up to the net.
--   gross     = quantity x price before any discount
--   discounts = line discounts + invoice discount
--   total     = what the invoice came to (what the customer paid / got back)
--   cost      = cost of the items (average cost when sold)
--   profit    = total - cost
--   payments  = how it was paid, e.g. "USD 10.00 + LBP 305,000"
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_sales_by_invoice(p_from date, p_to date, p_user_id integer DEFAULT NULL)
RETURNS TABLE (sale_id integer, invoice_no varchar, invoice_type smallint, invoice_date timestamptz,
               cashier varchar, customer varchar, item_count bigint, quantity numeric,
               gross numeric, discounts numeric, total numeric, cost numeric, profit numeric, payments text)
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

DROP FUNCTION IF EXISTS public.fn_report_sales_unpaid(date, date, integer);

-- ---------------------------------------------------------------------------
-- 4. Sales and unpaid: one row per day from p_from to p_to (days without sales too).
-- p_user_id NULL = every cashier. The unpaid amounts are those of the invoices of that day, as they are now
-- (a client who pays later lowers the unpaid of the day of the sale; Database/customer_payment_functions.sql).
--   net_sales    = sales - returns
--   on_account   = what was left unpaid when the sales were made (Unpaid F3)
--   unpaid       = what is still unpaid of it now
--   unpaid_count = invoices that still have something unpaid
-- The report shows the difference: collected = net_sales - unpaid.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_sales_unpaid(p_from date, p_to date, p_user_id integer DEFAULT NULL)
RETURNS TABLE (day date, sale_count bigint, sales numeric, returns numeric, net_sales numeric,
               on_account numeric, unpaid numeric, unpaid_count bigint)
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

DROP FUNCTION IF EXISTS public.fn_report_unpaid_by_customer();

-- ---------------------------------------------------------------------------
-- 5. Unpaid by client: one row per client who still owes something, as it is now, biggest amount first.
--   invoice_count = invoices that still have something unpaid
--   oldest_unpaid = date of the oldest of them (the next payment settles that one first)
--   unpaid        = what the client still owes (customers.balance_lbp)
--   last_payment  = when the client last paid something
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_unpaid_by_customer()
RETURNS TABLE (customer_id integer, name varchar, phone varchar, invoice_count bigint, oldest_unpaid timestamptz,
               unpaid numeric, credit_limit_lbp numeric, last_payment timestamptz)
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

DROP FUNCTION IF EXISTS public.fn_report_customer_statement(date, date, integer);

-- ---------------------------------------------------------------------------
-- 6. Client statement: the unpaid sales and the payments of a client (p_customer_id NULL = every client)
-- from p_from to p_to, in time order.
--   entry_type 0 = what was owed before p_from (one row, only when it is not 0)
--              1 = a sale left unpaid: debit = what was left unpaid when it was made;
--                  due_lbp = what is still unpaid of it now (0 = paid since)
--              2 = a payment: credit = what was paid; details = how and which invoices it settled
-- The report adds the running balance (debit - credit).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_customer_statement(p_from date, p_to date, p_customer_id integer DEFAULT NULL)
RETURNS TABLE (entry_type smallint, entry_date timestamptz, customer_id integer, customer varchar, reference text,
               details text, cashier varchar, debit numeric, credit numeric, due_lbp numeric)
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

-- ===========================================================================
-- Purchase reports (7 to 9). Only posted documents (status 1); drafts and cancelled ones are left out.
-- Returns to the supplier (doc_type 2) count against the purchases: they are negative (by invoice) or subtracted.
-- A purchase is entered in its supplier's currency: amounts are converted to the primary currency with the
-- rate kept on the invoice (currency_rate). The invoice discount is shared over the lines by their amount,
-- so the cost of the items adds up to the invoices. Days are purchase_invoices.invoice_date (the invoice's date).
-- ===========================================================================

DROP FUNCTION IF EXISTS public.fn_report_purchases_by_invoice(date, date, integer);

-- ---------------------------------------------------------------------------
-- 7. Purchases by invoice: one row per posted purchase and return from p_from to p_to, in date order.
-- p_supplier_id NULL = every supplier. A return (doc_type 2) has negative amounts, so the columns add up to the net.
--   total         = the invoice total in its own currency (currency)
--   discount_lbp  = the invoice discount, in the primary currency
--   total_lbp     = the invoice total in the primary currency (what we owe the supplier for it)
--   paid_lbp      = what was paid of it; owed_lbp = what is still owed of it (0 on a return)
--   return_of     = for a return, the purchase the goods came on
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_purchases_by_invoice(p_from date, p_to date, p_supplier_id integer DEFAULT NULL)
RETURNS TABLE (purchase_id integer, purchase_no varchar, doc_type smallint, supplier_invoice_no varchar, invoice_date date,
               supplier varchar, item_count bigint, currency text, total numeric,
               discount_lbp numeric, total_lbp numeric, paid_lbp numeric, owed_lbp numeric, entered_by varchar,
               return_of varchar)
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

DROP FUNCTION IF EXISTS public.fn_report_purchases_by_item(date, date, integer);

-- ---------------------------------------------------------------------------
-- 8. Purchases by item: one row per item bought or sent back from p_from to p_to, biggest net cost first.
-- p_supplier_id NULL = every supplier. Quantities are in the item's base unit (a box of 12 counts as 12);
-- unit costs are per base unit, in the primary currency, after the invoice discount.
--   qty_bought / qty_returned / qty = bought, sent back to the supplier, the difference
--   cost          = what the item cost on the purchases less what the returns gave back
--   avg_unit_cost = cost / qty;  last_unit_cost = on its latest purchase of the period
--   min / max     = the lowest and highest unit cost it was bought at in the period (purchases only)
--   avg_cost_now  = the item's average cost now (products.avg_cost), to compare
--   suppliers     = who it was bought from
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_purchases_by_item(p_from date, p_to date, p_supplier_id integer DEFAULT NULL)
RETURNS TABLE (product_id integer, item_name text, category_name varchar, base_unit varchar,
               qty_bought numeric, qty_returned numeric, qty numeric, cost numeric, avg_unit_cost numeric, last_unit_cost numeric,
               min_unit_cost numeric, max_unit_cost numeric, avg_cost_now numeric,
               invoice_count bigint, last_bought date, suppliers text)
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

DROP FUNCTION IF EXISTS public.fn_report_purchases_by_supplier(date, date);

-- ---------------------------------------------------------------------------
-- 9. Purchases by supplier: one row per supplier with purchases, returns or payments from p_from to p_to,
-- biggest net purchases first. Purchases posted without a supplier are one row (supplier_id NULL).
--   purchases    = what was bought from them in the period (primary currency)
--   returns      = what was sent back to them in the period
--   net          = purchases - returns
--   paid         = what we paid them in the period (supplier_payments)
--   owed_now     = what we owe them now, for all time (suppliers.balance_lbp)
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_purchases_by_supplier(p_from date, p_to date)
RETURNS TABLE (supplier_id integer, supplier varchar, phone varchar, invoice_count bigint, purchases numeric,
               returns numeric, net numeric, discounts numeric, paid numeric, last_purchase date, owed_now numeric)
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
DROP FUNCTION IF EXISTS public.fn_report_stock_value();

-- ---------------------------------------------------------------------------
-- 10. Stock value: what the stock costs and what it would sell for, now, one row per stock item that has stock
-- (above or below zero), biggest cost first. Bundles have no stock of their own and are left out.
--   stock_qty   = in the item's base unit (pcs, kg...)
--   avg_cost    = average cost of one base unit (products.avg_cost, updated by purchases)
--   sale_price  = price of one base unit: the single-unit packaging (factor 1) that can be sold, else the default
--                 packaging (its price / its pack size), else the smallest one; 0 = no price yet
--   total_cost  = stock_qty x avg_cost;  total_sale = stock_qty x sale_price;  profit = total_sale - total_cost
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_report_stock_value()
RETURNS TABLE (product_id integer, item_name text, category_name varchar, base_unit varchar, is_active boolean,
               stock_qty numeric, avg_cost numeric, sale_price numeric, total_cost numeric, total_sale numeric, profit numeric)
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
