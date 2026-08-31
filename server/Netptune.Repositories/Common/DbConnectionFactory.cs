using System.Data;

using Netptune.Core.Repositories.Common;

using Npgsql;

namespace Netptune.Repositories.Common;

public abstract class DbConnectionFactory : IDbConnectionFactory
{
    protected readonly string ConnectionString;

    protected DbConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public IDbConnection StartConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);

        connection.Open();

        return connection;
    }
}
