using Npgsql;
using System.Data;

namespace Shared.Database;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        // Ensure lis schema is in search path
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET search_path TO lis, public;";
        cmd.ExecuteNonQuery();
        return connection;
    }
}
