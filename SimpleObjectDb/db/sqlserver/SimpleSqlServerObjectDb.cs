using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using SimpleFileDatabase;

namespace SimpleObjectDb.db.file;

public class SimpleSqlServerObjectDb : ISimpleObjectDb
{
    private readonly string _connectionString;
    private readonly SimpleObjectDbConfiguration _configuration;

    public SimpleSqlServerObjectDb(string connectionString, SimpleObjectDbConfiguration configuration)
    {
        _connectionString = connectionString;
        _configuration = configuration;
    }

    public async Task CreateAsync<Tdata>(Tdata data) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        string sql = $"INSERT INTO {GetTableName<Tdata>()} (Id, Data) values(@id, @data)";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("data", json);
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch(Exception ex) 
        {
            throw new SimpleFileDbException("Could not create object", ex);
        }
    }

    public async Task UpdateAsync<Tdata>(Tdata data) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        string sql = $"UPDATE {GetTableName<Tdata>()} SET Data = @data WHERE Id = @id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("data", json);
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
        string sql = $"SELECT Data FROM {GetTableName<Tdata>()} WHERE Id = @id";
        string json = "";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
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
        List<string> jsonObjects = new List<string>();
        string sql = $"SELECT Data FROM {GetTableName<Tdata>()}";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var cmd = new SqlCommand(sql, connection);
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
        string sql = $"DELETE FROM {GetTableName<Tdata>()} WHERE Id = @id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }

    private string GetTableName<Tdata>()
    {
        return typeof(Tdata).Name;
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

        CreateTablesIfNotExists(connectionString, configuration.IdConverters.Select(x => x.Key.Name).ToArray());
    }
    
    private static void CreateDatabaseIfNotExists(string connectionString, string databaseName)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var sql = $@"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{databaseName}')
                         CREATE DATABASE [{databaseName}]";
            var cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
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
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            foreach (var tableName in tableNames)
            {
                string sql = $@"IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
                                CREATE TABLE dbo.{tableName} (
                                    Id varchar(50), 
                                    Data varchar(MAX),
                                    PRIMARY KEY (Id)
                                );";
                var cmd = new SqlCommand(sql, connection);
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
