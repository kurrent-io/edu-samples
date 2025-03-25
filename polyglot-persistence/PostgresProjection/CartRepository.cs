namespace PostgresProjection;

public class CartRepository
{
    private const string ReadModelName = "carts";
    private readonly PostgresDataAccess _postgres;
    public CartRepository(PostgresDataAccess postgres)
    {
        _postgres = postgres;

        CreateTablesIfNotExist();
    }

    private void CreateTablesIfNotExist()
    {
        _postgres.Execute(@"
                    CREATE TABLE IF NOT EXISTS carts (
                        cart_id TEXT PRIMARY KEY,
                        customer_id TEXT NULL,
                        status TEXT NOT NULL DEFAULT 'STARTED',
                        created_at TIMESTAMP NOT NULL,
                        updated_at TIMESTAMP NOT NULL
                    )");

        _postgres.Execute(@"
                    CREATE TABLE IF NOT EXISTS checkpoints (
                        read_model_name TEXT PRIMARY KEY,
                        checkpoint BIGINT NOT NULL
                    )");
    }
        
    public long? GetCartCheckpoint()
    {
        return _postgres.QueryFirstOrDefault<long?>(
            "SELECT checkpoint FROM checkpoints WHERE read_model_name = @ReadModelName",
            new { ReadModelName });
    }

    public void UpdateCartCheckpoint(long checkpoint)
    {
        _postgres.Execute(@"
                    INSERT INTO checkpoints (read_model_name, checkpoint)
                    VALUES (@ReadModelName, @Checkpoint)
                    ON CONFLICT (read_model_name) DO UPDATE 
                    SET checkpoint = @Checkpoint",
            new { ReadModelName, Checkpoint = checkpoint });
    }

    public void UpdateReadModel(Event e)
    {
        var sql = e.ProjectToSqlStatement();

        _postgres.Execute(sql.Text, sql.Params);
    }

}