using System.Data;
using Dapper;
using Npgsql;
using Inventory.Models;

namespace Inventory.Repositories
{
    public abstract class BaseRepository
    {
        private readonly string _connectionString;

        static BaseRepository()
        {
            // product_id -> ProductId, avg_cost -> AvgCost, ...
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        protected BaseRepository(string? connectionString = null)
        {
            _connectionString = WithoutGssEncryption(connectionString ?? AppConfig.ConnectionString
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json."));
        }

        private static string? _lastGiven, _lastUsed;

        // Npgsql first tries Kerberos (GSS) encryption on every new connection. On a computer that is in a Windows
        // domain this waits for the domain controller: each new connection took many seconds, or timed out, and the
        // screens stayed empty. The database here does not use Kerberos, so it is switched off unless appsettings.json
        // sets "GSS Encryption Mode" itself.
        private static string WithoutGssEncryption(string connectionString)
        {
            if (connectionString == _lastGiven && _lastUsed != null) return _lastUsed;
            var used = connectionString;
            if (!connectionString.Contains("GSS", StringComparison.OrdinalIgnoreCase))
                used = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
            (_lastGiven, _lastUsed) = (connectionString, used);
            return used;
        }

        /// <summary>
        /// A connection of its own that stays open, e.g. to LISTEN for database notifications (PosItemCache).
        /// Not opened yet. Keepalive: a broken network is noticed within a minute.
        /// </summary>
        internal static NpgsqlConnection NewLongLivedConnection()
        {
            var connectionString = WithoutGssEncryption(AppConfig.ConnectionString
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json."));
            return new NpgsqlConnection(new NpgsqlConnectionStringBuilder(connectionString) { KeepAlive = 30, Pooling = false }.ConnectionString);
        }

        // An open connection that knows who is logged in: database triggers record this user
        // as created_by / updated_by / posted_by and in audit_log (Database/user_tracking.sql).
        private async Task<NpgsqlConnection> OpenAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SELECT set_config('app.user_id', @id, false)",
                new { id = AppSession.CurrentUser?.UserId.ToString() ?? string.Empty });
            return connection;
        }

        /// <summary>Calls a function and returns its rows.</summary>
        protected async Task<IEnumerable<T>> GetListAsync<T>(string function, DynamicParameters? parameters = null)
        {
            await using var connection = await OpenAsync();
            return await connection.QueryAsync<T>(BuildCall(function, parameters), parameters);
        }

        /// <summary>
        /// Calls a paged function (p_page, p_page_size) whose rows end with a total_count column.
        /// </summary>
        protected async Task<PagedResult<T>> GetPagedAsync<T>(string function, int page, int pageSize, DynamicParameters? parameters = null)
        {
            parameters ??= new DynamicParameters();
            parameters.Add("p_page", page);
            parameters.Add("p_page_size", pageSize);

            await using var connection = await OpenAsync();
            var rows = (await connection.QueryAsync<T, long, (T Item, long Total)>(
                BuildCall(function, parameters), (item, total) => (item, total), parameters, splitOn: "total_count")).ToList();

            return new PagedResult<T>
            {
                Items = rows.Select(r => r.Item).ToList(),
                TotalCount = rows.Count > 0 ? rows[0].Total : 0,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <summary>Calls a function and returns its single row or scalar value (default when no row).</summary>
        protected async Task<T?> GetSingleAsync<T>(string function, DynamicParameters? parameters = null)
        {
            await using var connection = await OpenAsync();
            return await connection.QuerySingleOrDefaultAsync<T>(BuildCall(function, parameters), parameters);
        }

        /// <summary>Calls a function that returns nothing.</summary>
        protected async Task ExecuteAsync(string function, DynamicParameters? parameters = null)
        {
            await using var connection = await OpenAsync();
            await connection.ExecuteAsync(BuildCall(function, parameters), parameters);
        }

        // SELECT * FROM fn_name(p_a => @p_a, p_b => @p_b)
        // Named notation, so parameter names must match the function's argument names; order does not matter.
        private static string BuildCall(string function, DynamicParameters? parameters)
        {
            var args = parameters == null
                ? string.Empty
                : string.Join(", ", parameters.ParameterNames.Select(name => $"{name} => @{name}"));
            return $"SELECT * FROM {function}({args})";
        }
    }
}
