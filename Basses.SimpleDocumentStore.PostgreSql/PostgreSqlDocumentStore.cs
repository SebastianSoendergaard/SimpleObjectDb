using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Basses.SimpleDocumentStore.PostgreSql;

public class PostgreSqlDocumentStore : IDocumentStore
{
    private readonly PostgreSqlHelper _sqlHelper;
    private readonly DocumentStoreConfiguration _configuration;

    public string ConnectionString { get; private set; }

    public PostgreSqlDocumentStore(string connectionString, DocumentStoreConfiguration configuration)
    {
        ConnectionString = connectionString;

        _sqlHelper = new PostgreSqlHelper(connectionString);
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
            new PostgreSqlParameter("id", id.ToString() ?? ""),
            new PostgreSqlParameter("data", json, NpgsqlDbType.Jsonb)
        };

        try
        {
            await _sqlHelper.ExecuteAsync(sql, parameters);
        }
        catch (PostgresException ex) when (ex.Message.Contains("23505: duplicate key value violates unique constraint"))
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
            new PostgreSqlParameter("id", id.ToString() ?? ""),
            new PostgreSqlParameter("data", json, NpgsqlDbType.Jsonb)
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
            new PostgreSqlParameter("id", id.ToString() ?? "")
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
        var sql = $"DELETE FROM {GetTableName<Tdata>()} WHERE id = @id";

        var parameters = new[]
        {
            new PostgreSqlParameter("id", id.ToString() ?? "")
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
        return GetTableName(typeof(Tdata));
    }

    private string GetTableName(Type type)
    {
        var schema = _configuration.TryGetSchema(type);
        if (schema != null)
        {
            return $"{schema}.{TableNameFromTypeName(type.Name)}";
        }
        else
        {
            return TableNameFromTypeName(type.Name);
        }
    }

    private static string TableNameFromTypeName(string typeName)
    {
        return Regex.Replace(typeName, @"(?<!_|^)([A-Z])", "_$1").ToLower();
    }

    private void CreateTablesIfNotExists()
    {
        var schemas = _configuration.Schemas.Select(x => x.Value);
        var tableNames = _configuration.IdConverters.Select(x => GetTableName(x.Key));

        try
        {
            _sqlHelper.Transaction(async (conn, tx) =>
            {
                foreach (var schema in schemas)
                {
                    var sql = $"CREATE SCHEMA IF NOT EXISTS {schema}";

                    await _sqlHelper.ExecuteAsync(sql, [], conn, tx);
                }

                foreach (var tableName in tableNames)
                {
                    var sql = $@"CREATE TABLE IF NOT EXISTS {tableName} (
                                id varchar(50), 
                                data jsonb,
                                PRIMARY KEY (id)
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
