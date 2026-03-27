using System.Data.SqlClient;

namespace Turfirm.Infrastructure
{
    public static class Db
    {
        public const string MasterConnection = @"Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master";
        public const string AppConnection = @"Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=DBTurfirma";

        public static SqlConnection Open(string cs)
        {
            var connection = new SqlConnection(cs);
            connection.Open();
            return connection;
        }
    }
}
