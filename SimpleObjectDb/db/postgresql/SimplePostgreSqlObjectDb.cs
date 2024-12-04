using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
using SimpleFileDatabase;

namespace SimpleObjectDb.db.postgresql;

public class SimplePostgreSqlObjectDb : ISimpleObjectDb
{
    private readonly string _connectionString;
    private readonly SimpleObjectDbConfiguration _configuration;

    public SimplePostgreSqlObjectDb(string connectionString, SimpleObjectDbConfiguration configuration)
    {
        _connectionString = connectionString;
        _configuration = configuration;
    }

    public async Task CreateAsync<Tdata>(Tdata data) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        var sql = $"INSERT INTO {GetTableName<Tdata>()} (id, data) values(@id, @data)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = connection;
            cmd.Parameters.AddWithValue("id", id.ToString() ?? "");
            cmd.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, json);
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not create object", ex);
        }
    }

    public async Task UpdateAsync<Tdata>(Tdata data) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        var sql = $"UPDATE {GetTableName<Tdata>()} SET data = @data WHERE id = @id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = connection;
            cmd.Parameters.AddWithValue("id", id.ToString() ?? "");
            cmd.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, json);
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }

    public async Task<Tdata?> GetByIdAsync<Tdata>(object id) where Tdata : class
    {
        var sql = $"SELECT data FROM {GetTableName<Tdata>()} WHERE id = @id";
        var json = "";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = connection;
            cmd.Parameters.AddWithValue("id", id.ToString() ?? "");
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    json = reader.GetString(0);
                }
            }
            connection.Close();
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var data = JsonSerializer.Deserialize<Tdata>(json);
            return data;
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not get object", ex);
        }
    }

    public async IAsyncEnumerable<Tdata> GetAllAsync<Tdata>() where Tdata : class
    {
        List<string> jsonObjects = new();
        var sql = $"SELECT data FROM {GetTableName<Tdata>()}";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var json = reader.GetString(0);
                    jsonObjects.Add(json);
                }
            }
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not get objects", ex);
        }

        foreach (var json in jsonObjects)
        {
            var data = JsonSerializer.Deserialize<Tdata>(json);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public async Task DeleteByIdAsync<Tdata>(object id) where Tdata : class
    {
        var sql = $"DELETE FROM {GetTableName<Tdata>()} WHERE id = @id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = connection;
            cmd.Parameters.AddWithValue("id", id.ToString() ?? "");
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }

    private static string GetTableName<Tdata>()
    {
        return TableNameFromTypeName(typeof(Tdata).Name);
    }

    private static string TableNameFromTypeName(string typeName)
    {
        return Regex.Replace(typeName, @"(?<!_|^)([A-Z])", "_$1").ToLower();
    }

    public static void CreateIfNotExist(string connectionString, SimpleObjectDbConfiguration configuration)
    {
        var connectionProperties = connectionString
            .Split(';')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x =>
            {
                var values = x.Split('=');
                return new { Key = values[0], Value = values[1] };
            });

        var connectionPropertiesWithoutDatabase = connectionProperties
            .Where(x => !x.Key.StartsWith("database", StringComparison.OrdinalIgnoreCase));

        var connectionStringWithoutDatabase = string.Join(';', connectionPropertiesWithoutDatabase.Select(x => $"{x.Key}={x.Value}"));

        var databaseName = connectionProperties
            .Where(x => x.Key.StartsWith("database", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .Single();

        CreateDatabaseIfNotExists(connectionStringWithoutDatabase, databaseName);

        CreateTablesIfNotExists(connectionString, configuration.IdConverters.Select(x => TableNameFromTypeName(x.Key.Name)).ToArray());
    }

    private static void CreateDatabaseIfNotExists(string connectionString, string databaseName)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql1 = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{databaseName}'";
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = connection;
            var tableCount = (long)(cmd1.ExecuteScalar() ?? 0);
            if (tableCount == 0)
            {
                var sql2 = $"CREATE DATABASE {databaseName}";
                using var cmd2 = new NpgsqlCommand(sql2);
                cmd2.Connection = connection;
                cmd2.ExecuteNonQuery();
            }
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }

    private static void CreateTablesIfNotExists(string connectionString, params string[] tableNames)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            foreach (var tableName in tableNames)
            {
                var sql = $@"CREATE TABLE IF NOT EXISTS public.{tableName} (
                                id varchar(50), 
                                data jsonb,
                                PRIMARY KEY (Id)
                            );";
                using var cmd = new NpgsqlCommand(sql);
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }
}
