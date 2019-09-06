using System.Data;
using System.Data.SqlClient;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Starts and open a database connection to the sql database
        /// </summary>
        /// <returns>Database connection to use with dapper</returns>
        public IDbConnection StartConnection()
        {
            var connection = new SqlConnection(_connectionString);

            connection.Open();

            return connection;
        }
    }
}
