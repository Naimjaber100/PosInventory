-- Users of the system (Settings > Users) for the Inventory app (UserRepository).
-- Roles: 1 = Manager, 2 = User.
-- Users log in with a password only (like a PIN), so the password identifies the user:
-- every active user has a different password. The app hashes it (PBKDF2, see PasswordHasher) and only the hash
-- is stored; the same password always gives the same hash, so the user is found by it.
-- user_name is filled from the name (it is shown on held sales); it is not typed anywhere.
-- There must always be at least one active Manager.
-- Safe to re-run: functions are dropped and recreated.
-- Apply:  psql -h localhost -U postgres -d Pos -f user_functions.sql

-- No two active users with the same password
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_password_hash ON public.users (password_hash) WHERE is_active;

DROP FUNCTION IF EXISTS public.fn_user_get_all();
DROP FUNCTION IF EXISTS public.fn_user_save(integer, text, text, text, smallint, boolean);
DROP FUNCTION IF EXISTS public.fn_user_save(integer, text, text, smallint, boolean);
DROP FUNCTION IF EXISTS public.fn_user_delete(integer);
DROP FUNCTION IF EXISTS public.fn_user_find_by_password(text);

-- All users: active first, managers first, then by name.
-- password_set = false when the user has no usable password yet (or one saved before password-only login).
CREATE OR REPLACE FUNCTION public.fn_user_get_all()
RETURNS TABLE (user_id integer, full_name varchar, user_name varchar, role smallint, is_active boolean,
               must_change_password boolean, created_at timestamptz, password_set boolean)
LANGUAGE sql STABLE
AS $$
    SELECT u.user_id, u.full_name, u.user_name, u.role, u.is_active, u.must_change_password, u.created_at,
           u.password_hash LIKE 'pin$%'
    FROM users u
    ORDER BY NOT u.is_active, u.role, lower(u.full_name), u.user_id;
$$;

-- Adds (p_user_id null) or updates a user; returns its id.
-- p_password_hash null = keep the current password (required for a new user).
CREATE OR REPLACE FUNCTION public.fn_user_save(
    p_user_id integer, p_full_name text, p_password_hash text, p_role smallint, p_is_active boolean DEFAULT true)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_full_name text := nullif(trim(p_full_name), '');
    v_hash      text := nullif(p_password_hash, '');
    v_user_name text;
    v_n         integer := 1;
    v_id        integer;
BEGIN
    IF v_full_name IS NULL THEN RAISE EXCEPTION 'The name is required.'; END IF;
    IF p_role NOT IN (1, 2) THEN RAISE EXCEPTION 'The role must be Manager or User.'; END IF;
    IF p_user_id IS NULL AND v_hash IS NULL THEN RAISE EXCEPTION 'A password is required for a new user.'; END IF;

    -- The password identifies the user at login, so it must not be used by another active user
    IF coalesce(p_is_active, true) AND EXISTS (
        SELECT 1 FROM users u
        WHERE u.is_active AND u.user_id IS DISTINCT FROM p_user_id
          AND u.password_hash = coalesce(v_hash, (SELECT x.password_hash FROM users x WHERE x.user_id = p_user_id))) THEN
        RAISE EXCEPTION 'This password is already used by another user. Choose a different one.';
    END IF;

    -- user_name from the name, made unique: "Rami", "Rami 2", ...
    v_user_name := left(v_full_name, 45);
    WHILE EXISTS (SELECT 1 FROM users WHERE lower(user_name) = lower(v_user_name) AND user_id IS DISTINCT FROM p_user_id) LOOP
        v_n := v_n + 1;
        v_user_name := left(v_full_name, 45) || ' ' || v_n;
    END LOOP;

    IF p_user_id IS NULL THEN
        INSERT INTO users (user_name, password_hash, full_name, role, must_change_password, is_active)
        VALUES (v_user_name, v_hash, v_full_name, p_role, false, coalesce(p_is_active, true))
        RETURNING user_id INTO v_id;
    ELSE
        UPDATE users
        SET user_name            = v_user_name,
            full_name            = v_full_name,
            role                 = p_role,
            is_active            = coalesce(p_is_active, is_active),
            password_hash        = coalesce(v_hash, password_hash),
            must_change_password = CASE WHEN v_hash IS NULL THEN must_change_password ELSE false END
        WHERE user_id = p_user_id
        RETURNING user_id INTO v_id;
        IF v_id IS NULL THEN RAISE EXCEPTION 'User % does not exist.', p_user_id; END IF;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM users WHERE role = 1 AND is_active) THEN
        RAISE EXCEPTION 'There must be at least one active Manager.';
    END IF;
    RETURN v_id;
END $$;

-- Login: the active user with this password hash (no row = wrong password).
CREATE OR REPLACE FUNCTION public.fn_user_find_by_password(p_password_hash text)
RETURNS TABLE (user_id integer, full_name varchar, user_name varchar, role smallint, is_active boolean,
               must_change_password boolean, created_at timestamptz)
LANGUAGE sql STABLE
AS $$
    SELECT u.user_id, u.full_name, u.user_name, u.role, u.is_active, u.must_change_password, u.created_at
    FROM users u
    WHERE u.is_active AND u.password_hash = p_password_hash;
$$;

-- Deletes a user. A user with activity (sales, purchases, stock moves...) is deactivated instead.
-- Returns 'deleted' or 'deactivated'.
CREATE OR REPLACE FUNCTION public.fn_user_delete(p_user_id integer)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    v_result text;
BEGIN
    BEGIN
        DELETE FROM users WHERE user_id = p_user_id;
        IF NOT FOUND THEN RAISE EXCEPTION 'User % does not exist.', p_user_id; END IF;
        v_result := 'deleted';
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE users SET is_active = false WHERE user_id = p_user_id;
        v_result := 'deactivated';
    END;

    IF NOT EXISTS (SELECT 1 FROM users WHERE role = 1 AND is_active) THEN
        RAISE EXCEPTION 'There must be at least one active Manager.';
    END IF;
    RETURN v_result;
END $$;
