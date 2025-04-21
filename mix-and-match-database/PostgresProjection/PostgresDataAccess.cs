using Dapper;
using Npgsql;

namespace PostgresProjection
{
    public class PostgresDataAccess : IDisposable
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

        public T? QueryFirstOrDefault<T>(CommandDefinition command)
        {
            return _connection.QueryFirstOrDefault<T>(command);
        }

        public T? QueryFirstOrDefault<T>(string sql)
        {
            return _connection.QueryFirstOrDefault<T>(sql);
        }


        public void Execute(CommandDefinition command)
        {
            _connection.Execute(command);
        }

        public void Execute(string sql, object? param)
        {
            _connection.Execute(sql, param);
        }


        public void Execute(IEnumerable<CommandDefinition>? commands)
        {
            if (commands != null)
                foreach (var command in commands)
                {
                    _connection.Execute(command);
                }
        }
        
        public void Execute(string sql)
        {
            _connection.Execute(sql);
        }

        public void Dispose()
        {
            _connection.Dispose();
            _transaction?.Dispose();
        }
    }
}