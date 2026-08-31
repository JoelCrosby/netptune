using System.Data;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

// Base for repositories that reach the database through Dapper rather than EF.
public abstract class ReadOnlyRepository
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected ReadOnlyRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    protected IDbConnection StartConnection()
    {
        return ConnectionFactory.StartConnection();
    }
}
