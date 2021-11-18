using System.Data;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

/// <summary>
/// Basic base repository, designed to be used only with dapper or other micro ORM
/// </summary>
public abstract class ReadOnlyRepository
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected ReadOnlyRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    /// <summary>
    /// Starts connection to the database to execute a query
    /// </summary>
    /// <returns>sql connection</returns>
    protected IDbConnection StartConnection()
    {
        return ConnectionFactory.StartConnection();
    }
}