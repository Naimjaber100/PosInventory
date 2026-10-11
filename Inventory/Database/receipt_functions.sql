-- Printed receipts (cashier, 80 mm printer): everything one receipt needs in a single call, so printing
-- right after a sale costs one quick database trip. Also used to reprint an old invoice (Sales F7).
-- Safe to re-run.
-- Apply:  psql -h localhost -U postgres -d Pos -f receipt_functions.sql

DROP FUNCTION IF EXISTS public.fn_sale_receipt(integer);

-- One sale or return as JSON:
-- { company: {name, address, mobile, email, vat_number},
--   invoice_no, invoice_type (1 sale, 2 return), invoice_date, cashier, customer,
--   sub_total, line_discounts, discount, total, change_lbp, usd_rate, primary_code,
--   lines:    [{ item_name, quantity, unit_price, discount, line_total }],
--   payments: [{ method (1 cash, 2 card, 3 account), currency, amount, amount_lbp }] }
-- NULL when the sale does not exist.
CREATE OR REPLACE FUNCTION public.fn_sale_receipt(p_sale_id integer)
RETURNS jsonb
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
        'sub_total', si.sub_total,
        'line_discounts', (SELECT coalesce(sum(l.discount), 0) FROM sales_lines l WHERE l.sale_id = si.sale_id),
        'discount', si.discount,
        'total', si.total,
        'change_lbp', si.change_lbp,
        'usd_rate', si.exchange_rate,
        'primary_code', (SELECT code FROM currencies WHERE is_primary),
        'lines', coalesce((SELECT jsonb_agg(jsonb_build_object(
                                'item_name', l.description, 'quantity', l.quantity, 'unit_price', l.unit_price,
                                'discount', l.discount, 'line_total', round(l.quantity * l.unit_price - l.discount, 2))
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
