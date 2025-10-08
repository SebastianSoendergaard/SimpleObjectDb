using Npgsql;
using NpgsqlTypes;

namespace Basses.SimpleDocumentStore.PostgreSql;

public class PostgreSqlHelper
{
    private readonly string _connectionString;

    public PostgreSqlHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<PostgreSqlParameter> parameters, CancellationToken? cancellationToken = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await ExecuteAsync(sql, parameters, connection);
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<PostgreSqlParameter> parameters, NpgsqlConnection connection, NpgsqlTransaction? transaction = null, CancellationToken? cancellationToken = null)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new NpgsqlCommand(sql, connection, transaction);
        AddParametersToCommand(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync(cancellationToken ?? CancellationToken.None);
    }

    public async Task Transaction(Func<NpgsqlConnection, NpgsqlTransaction, Task> execute)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var transaction = connection.BeginTransaction();

        try
        {
            await execute(connection, transaction);
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, IEnumerable<PostgreSqlParameter> parameters, Func<NpgsqlDataReader, T> createResult, CancellationToken? cancellationToken = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new NpgsqlCommand(sql, connection);
        AddParametersToCommand(cmd, parameters);
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);

        List<T> results = [];
        while (reader.Read())
        {
            results.Add(createResult(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, IEnumerable<PostgreSqlParameter> parameters, Func<NpgsqlDataReader, T> createResult, CancellationToken? cancellationToken = null)
    {
        var results = await QueryAsync(sql, parameters, createResult, cancellationToken);
        return results.SingleOrDefault();
    }

    private void AddParametersToCommand(NpgsqlCommand command, IEnumerable<PostgreSqlParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Type != null)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Type.Value, parameter.Value);
            }
            else
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }
        }
    }

    public void EnsureDatabase()
    {
        var connectionProperties = _connectionString
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
            throw new DocumentStoreException("Could not create database", ex);
        }
    }
}

public record PostgreSqlParameter
{
    public PostgreSqlParameter(string name, object value)
    {
        Name = name;
        Value = value;
    }

    public PostgreSqlParameter(string name, object value, NpgsqlDbType type)
    {
        Name = name;
        Value = value;
        Type = type;
    }

    public string Name { get; }
    public object Value { get; }
    public NpgsqlDbType? Type { get; }
}
