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

        Directory.CreateDirectory(_connectionString);
    }

    public async Task CreateAsync<Tdata>(Tdata data) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);

        try
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            string sql = "INSERT INTO Objects (Id, Type, Data) values(@id, @type, @data)";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("type", typeof(Tdata).Name);
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

        try
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            string sql = "UPDATE Objects SET Data = @data WHERE Type = @type AND Id = @id";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("type", typeof(Tdata).Name);
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
        try
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            string sql = "SELECT Data FROM Objects WHERE Type = @type AND Id = @id";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("type", typeof(Tdata).Name);
            var reader = await cmd.ExecuteReaderAsync();
            string json = "";
            while (reader.Read())
            {
                json = reader.GetString(0);
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

        try
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            string sql = "SELECT Data FROM Objects WHERE Type = @type";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("type", typeof(Tdata).Name);
            var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var json = reader.GetString(0);
                jsonObjects.Add(json);
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
        try
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            string sql = "DELETE FROM Objects WHERE Type = @type AND Id = @id";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("type", typeof(Tdata).Name);
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleFileDbException("Could not update object", ex);
        }
    }

    public static void CreateIfNotExist(string connectionString)
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

        CreateTablesIfNotExists(connectionString, "Objects");
    }
    
    private static void CreateDatabaseIfNotExists(string connectionString, string databaseName)
    {
        try
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = $"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{databaseName}')\r\nCREATE DATABASE [{databaseName}]";
            SqlCommand cmd = new SqlCommand(sql, connection);
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
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            foreach (string tableName in tableNames)
            {
                string sql = $"IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL\r\nCREATE TABLE dbo.{tableName} (Id varchar(50), Type varchar(100), Data varchar(MAX));";
                SqlCommand cmd = new SqlCommand(sql, connection);
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
