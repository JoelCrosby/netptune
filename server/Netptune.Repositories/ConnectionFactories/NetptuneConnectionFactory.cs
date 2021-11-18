using Netptune.Repositories.Common;

namespace Netptune.Repositories.ConnectionFactories;

public class NetptuneConnectionFactory : DbConnectionFactory
{
    public NetptuneConnectionFactory(string connectionString) : base(connectionString)
    {
    }
}