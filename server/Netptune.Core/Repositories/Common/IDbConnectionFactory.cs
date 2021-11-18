using System.Data;

namespace Netptune.Core.Repositories.Common;

public interface IDbConnectionFactory
{
    IDbConnection StartConnection();
}