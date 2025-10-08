using System.Data;
using Microsoft.Data.SqlClient;

namespace Basses.SimpleDocumentStore.SqlServer;

public class SqlServerHelper
{
    private readonly string _connectionString;

    public SqlServerHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<SqlServerParameter> parameters, CancellationToken? cancellationToken = null)
    {
        using var connection = new SqlConnection(_connectionString);
        return await ExecuteAsync(sql, parameters, connection);
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<SqlServerParameter> parameters, SqlConnection connection, SqlTransaction? transaction = null, CancellationToken? cancellationToken = null)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new SqlCommand(sql, connection, transaction);
        AddParametersToCommand(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync(cancellationToken ?? CancellationToken.None);
    }

    public async Task Transaction(Func<SqlConnection, SqlTransaction, Task> execute)
    {
        using var connection = new SqlConnection(_connectionString);
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

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, IEnumerable<SqlServerParameter> parameters, Func<SqlDataReader, T> createResult, CancellationToken? cancellationToken = null)
    {
        using var connection = new SqlConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new SqlCommand(sql, connection);
        AddParametersToCommand(cmd, parameters);
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);

        List<T> results = [];
        while (reader.Read())
        {
            results.Add(createResult(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, IEnumerable<SqlServerParameter> parameters, Func<SqlDataReader, T> createResult, CancellationToken? cancellationToken = null)
    {
        var results = await QueryAsync(sql, parameters, createResult, cancellationToken);
        return results.SingleOrDefault();
    }

    private void AddParametersToCommand(SqlCommand command, IEnumerable<SqlServerParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
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
            throw new DocumentStoreException("Could not create database", ex);
        }
    }
}

public record SqlServerParameter
{
    public SqlServerParameter(string name, object value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public object Value { get; }
}
