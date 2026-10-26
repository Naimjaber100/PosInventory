-- The database user the program logs in with (Setup-Server.ps1): it can read and change the shop's data and run
-- the functions, but it cannot drop the database, change its structure or the server's settings. The "postgres"
-- account stays for installing and upgrading only.
-- psql variables: app_user (pos_app), app_password, db (Pos). Safe to re-run: the password is set again.

SELECT format('CREATE ROLE %I LOGIN', :'app_user')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_user') \gexec

ALTER ROLE :"app_user" LOGIN PASSWORD :'app_password';

GRANT CONNECT, TEMPORARY ON DATABASE :"db" TO :"app_user";   -- TEMPORARY: posting a purchase uses temp tables
GRANT USAGE ON SCHEMA public TO :"app_user";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"app_user";
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO :"app_user";
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO :"app_user";

-- Tables, counters and functions that later upgrades add (created by postgres) get the same rights
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO :"app_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO :"app_user";
