-- Starting data of a NEW Pos database (Setup-Server.ps1 runs it once, after schema.sql and Database\*.sql;
-- then fix_sequences.sql moves the id counters past these rows).
-- psql variables, given by Setup-Server.ps1:
--   admin_hash = PasswordHasher.Hash(the manager's password)   company = the shop's name   usd_rate = 1 USD in LBP (0 = later)
-- Safe to re-run: rows that are already there are kept.

-- The first manager (logs in with the password only; can add the other users in Settings > Users)
INSERT INTO public.users (user_id, user_name, password_hash, full_name, role, must_change_password, is_active)
OVERRIDING SYSTEM VALUE
VALUES (1, 'Administrator', :'admin_hash', 'Administrator', 1, false, true)
ON CONFLICT (user_id) DO NOTHING;

-- The customer of every cash sale (sales without a client use customer 1)
INSERT INTO public.customers (customer_id, name, balance_lbp, is_active)
OVERRIDING SYSTEM VALUE
VALUES (1, 'Walk-in customer', 0, true)
ON CONFLICT (customer_id) DO NOTHING;

-- The cash register of the shop
INSERT INTO public.registers (register_id, name, is_active)
OVERRIDING SYSTEM VALUE
VALUES (1, 'Register 1', true)
ON CONFLICT (register_id) DO NOTHING;

-- The company (name, address, logo... are completed in Settings > Company)
INSERT INTO public.company (company_id, name)
OVERRIDING SYSTEM VALUE
VALUES (1, :'company')
ON CONFLICT (company_id) DO NOTHING;

-- Currencies: LBP is the primary one, USD the second one (more in Settings > Currencies)
INSERT INTO public.currencies (code, name, symbol, decimals, is_primary, is_active, sort_order) VALUES
    ('LBP', 'Lebanese pound', 'LBP', 0, true,  true, 0),
    ('USD', 'US dollar',      '$',   2, false, true, 1)
ON CONFLICT (code) DO NOTHING;

-- Today's dollar rate, when given (sales and purchases need one; also set in Settings > Currencies)
INSERT INTO public.exchange_rates (rate, effective_at, user_id, currency_code)
SELECT :usd_rate, now(), 1, 'USD'
WHERE :usd_rate > 0 AND NOT EXISTS (SELECT 1 FROM public.exchange_rates WHERE currency_code = 'USD');

-- Settings the program reads
INSERT INTO public.settings (key, value) VALUES
    ('receipt_footer', 'Thank you for shopping with us'),
    ('allow_negative_stock', '1')
ON CONFLICT (key) DO NOTHING;
