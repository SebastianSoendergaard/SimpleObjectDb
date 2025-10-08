using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace Basses.SimpleDocumentStore.SqlServer;

public class SqlServerDocumentStore : IDocumentStore
{
    private readonly SqlServerHelper _sqlHelper;
    private readonly DocumentStoreConfiguration _configuration;

    public SqlServerDocumentStore(string connectionString, DocumentStoreConfiguration configuration)
    {
        _sqlHelper = new SqlServerHelper(connectionString);
        _configuration = configuration;

        _sqlHelper.EnsureDatabase();

        CreateTablesIfNotExists();
    }

    public async Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = _configuration.Serializer.ToJson(data);

        var sql = $"INSERT INTO {GetTableName<Tdata>()} (id, data) values(@id, @data)";

        var parameters = new[]
        {
            new SqlServerParameter("id", id.ToString() ?? ""),
            new SqlServerParameter("data", json)
        };

        try
        {
            await _sqlHelper.ExecuteAsync(sql, parameters);
        }
        catch (SqlException ex) when (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
        {
            throw new AlreadyExistException($"Id for {typeof(Tdata).FullName} already exist");
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not create object", ex);
        }
    }

    public async Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var json = _configuration.Serializer.ToJson(data);
        int affectedRows = 0;

        var sql = $"UPDATE {GetTableName<Tdata>()} SET data = @data WHERE id = @id";

        var parameters = new[]
        {
            new SqlServerParameter("id", id.ToString() ?? ""),
            new SqlServerParameter("data", json)
        };

        try
        {
            affectedRows = await _sqlHelper.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not update object", ex);
        }

        if (affectedRows == 0)
        {
            throw new NotFoundException($"Id for {typeof(Tdata).FullName} not found");
        }
    }

    public async Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"SELECT data FROM {GetTableName<Tdata>()} WHERE id = @id";

        var parameters = new[]
        {
            new SqlServerParameter("id", id.ToString() ?? "")
        };

        try
        {
            var json = await _sqlHelper.QuerySingleOrDefaultAsync(sql, parameters, reader => reader.GetString(0), cancellationToken);
            var data = string.IsNullOrEmpty(json) ? null : _configuration.Serializer.FromJson<Tdata>(json);
            return data;
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not get object", ex);
        }
    }

    public async IAsyncEnumerable<Tdata> GetAllAsync<Tdata>([EnumeratorCancellation] CancellationToken cancellationToken = default) where Tdata : class
    {
        IEnumerable<string> jsonObjects = [];
        var sql = $"SELECT data FROM {GetTableName<Tdata>()}";

        try
        {
            jsonObjects = await _sqlHelper.QueryAsync(sql, [], reader => reader.GetString(0), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not get objects", ex);
        }

        foreach (var json in jsonObjects)
        {
            var data = _configuration.Serializer.FromJson<Tdata>(json);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public async Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"DELETE FROM {GetTableName<Tdata>()} WHERE Id = @id";

        var parameters = new[]
        {
            new SqlServerParameter("id", id.ToString() ?? "")
        };

        try
        {
            await _sqlHelper.ExecuteAsync(sql, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not delete object", ex);
        }
    }

    public async Task DeleteAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class
    {
        var sql = $"DELETE FROM {GetTableName<Tdata>()}";

        try
        {
            await _sqlHelper.ExecuteAsync(sql, [], cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not delete objects", ex);
        }
    }

    private string GetTableName<Tdata>()
    {
        return typeof(Tdata).Name;
    }

    private void CreateTablesIfNotExists()
    {
        var tableNames = _configuration.IdConverters.Select(x => x.Key.Name).ToArray();

        try
        {
            _sqlHelper.Transaction(async (conn, tx) =>
            {
                foreach (var tableName in tableNames)
                {
                    var sql = $@"IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
                                CREATE TABLE dbo.{tableName} (
                                    Id varchar(50), 
                                    Data varchar(MAX),
                                    PRIMARY KEY (Id)
                                );";

                    await _sqlHelper.ExecuteAsync(sql, [], conn, tx);
                }
            })
            .GetAwaiter()
            .GetResult();
        }
        catch (Exception ex)
        {
            throw new DocumentStoreException("Could not create database tables", ex);
        }
    }
}
