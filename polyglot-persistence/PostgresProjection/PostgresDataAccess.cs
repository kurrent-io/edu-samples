using Dapper;
using Npgsql;

namespace PostgresProjection
{
    public class PostgresDataAccess
    {
        private readonly NpgsqlConnection _connection;
        private NpgsqlTransaction? _transaction;

        public PostgresDataAccess(NpgsqlConnection connection)
        {
            _connection = connection;
            _connection.Open();
        }

        // Transaction management methods
        public void BeginTransaction()
        {
            _transaction = _connection.BeginTransaction();
        }
        public void Commit()
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Execute(string sql)
        {
            _connection.Execute(sql, transaction: _transaction);
        }

        public void Execute(string sql, object @params)
        {
            _connection.Execute(sql, @params, transaction: _transaction);
        }

        public T QueryFirstOrDefault<T>(string sql, object param)
        {
            return _connection.QueryFirstOrDefault<T>(sql, param, transaction: _transaction);
        }
    }
}