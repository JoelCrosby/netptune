using System.Data;

using Microsoft.Data.SqlClient;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common
{
    public abstract class DbConnectionFactory : IDbConnectionFactory
    {
        protected readonly string ConnectionString;

        protected DbConnectionFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Starts and open a database connection to the sql database
        /// </summary>
        /// <returns>Database connection to use with dapper</returns>
        public IDbConnection StartConnection()
        {
            var connection = new SqlConnection(ConnectionString);

            connection.Open();

            return connection;
        }
    }
}
