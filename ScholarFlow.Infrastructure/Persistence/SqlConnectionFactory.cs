using System.Data;
using Microsoft.Data.SqlClient;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Infrastructure.Persistence;

public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    private readonly string _connectionString = connectionString
        ?? throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null.");

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
