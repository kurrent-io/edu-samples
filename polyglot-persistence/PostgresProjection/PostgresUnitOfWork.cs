namespace PostgresProjection;

public class PostgresUnitOfWork : IDisposable
{
    private readonly PostgresDataAccess _postgres;
    public PostgresUnitOfWork(PostgresDataAccess postgres)
    {
        _postgres = postgres;
        postgres.BeginTransaction();
    }

    public void Commit()
    {
        _postgres.Commit();
    }
        
    public void Dispose()
    {
        _postgres.Rollback();
    }
}