using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Inventory.Repositories
{
    // Sends a value as a PostgreSQL jsonb parameter (keys in snake_case), e.g.
    // parameters.Add("p_rows", new JsonbParameter(rows));
    public class JsonbParameter : SqlMapper.ICustomQueryParameter
    {
        private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        private readonly string _json;

        public JsonbParameter(object value)
        {
            _json = JsonSerializer.Serialize(value, Options);
        }

        public void AddParameter(IDbCommand command, string name)
        {
            command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = _json });
        }
    }
}
