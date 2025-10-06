using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Basses.SimpleDocumentStore.SqlServer;

public class SqlServerDocumentStore : IDocumentStore
{
    private readonly string _connectionString;
    private readonly DocumentStoreConfiguration _configuration;

    public SqlServerDocumentStore(string connectionString, DocumentStoreConfiguration configuration)
    {
        _connectionString = connectionString;
        _configuration = configuration;

        CreateIfNotExist(_connectionString, _configuration);
    }

    public async Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        var sql = $"INSERT INTO {GetTableName<Tdata>()} (Id, Data) values(@id, @data)";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("data", json);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (SqlException ex) when (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
        {
            throw new AlreadyExistException($"Id for {typeof(Tdata).FullName} already exist");
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not create object", ex);
        }
    }

    public async Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = JsonSerializer.Serialize(data);
        var sql = $"UPDATE {GetTableName<Tdata>()} SET Data = @data WHERE Id = @id";
        int affectedRows = 0;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            cmd.Parameters.AddWithValue("data", json);
            affectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not update object", ex);
        }

        if (affectedRows == 0)
        {
            throw new NotFoundException($"Id for {typeof(Tdata).FullName} not found");
        }
    }

    public async Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"SELECT Data FROM {GetTableName<Tdata>()} WHERE Id = @id";
        var json = "";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
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
            throw new SimpleDocumentStoreException("Could not get object", ex);
        }
    }

    public async IAsyncEnumerable<Tdata> GetAllAsync<Tdata>([EnumeratorCancellation] CancellationToken cancellationToken = default) where Tdata : class
    {
        var jsonObjects = new List<string>();
        var sql = $"SELECT Data FROM {GetTableName<Tdata>()}";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
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
            throw new SimpleDocumentStoreException("Could not get objects", ex);
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

    public async Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"DELETE FROM {GetTableName<Tdata>()} WHERE Id = @id";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not delete object", ex);
        }
    }

    public async Task DeleteAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"DELETE FROM {GetTableName<Tdata>()}";

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not delete objects", ex);
        }
    }

    private string GetTableName<Tdata>()
    {
        return typeof(Tdata).Name;
    }

    private static void CreateIfNotExist(string connectionString, DocumentStoreConfiguration configuration)
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
            using var cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not create database", ex);
        }
    }

    private static void CreateTablesIfNotExists(string connectionString, params string[] tableNames)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var tableName in tableNames)
            {
                var sql = $@"IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
                                CREATE TABLE dbo.{tableName} (
                                    Id varchar(50), 
                                    Data varchar(MAX),
                                    PRIMARY KEY (Id)
                                );";
                using var cmd = new SqlCommand(sql, connection, transaction);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new SimpleDocumentStoreException("Could not create database tables", ex);
        }
    }
}
