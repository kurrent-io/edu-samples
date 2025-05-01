using Dapper;
using Npgsql;

namespace PersistentSubscriptionOrderProcessor
{
    public class PostgresDataAccess : IDisposable
    {
        private readonly NpgsqlConnection _connection;

        public PostgresDataAccess(NpgsqlConnection connection)
        {
            _connection = connection;
            _connection.Open();
        }

        public void Execute(string sql, object? param)
        {
            _connection.Execute(sql, param);
        }


        public void Execute(string sql)
        {
            _connection.Execute(sql);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}