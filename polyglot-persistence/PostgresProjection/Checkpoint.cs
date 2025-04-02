using Dapper;

namespace PostgresProjection;

public class Checkpoint
{
    public static CommandDefinition GetCreateTableCommand()
    {
        return new CommandDefinition(@"
                     CREATE TABLE IF NOT EXISTS checkpoints (
                         read_model_name TEXT PRIMARY KEY,
                         checkpoint BIGINT NOT NULL
                     )");
    }

    public static CommandDefinition GetQuery(string readModelName)
    {
        var sql = "SELECT checkpoint FROM checkpoints WHERE read_model_name = @readModelName";

        var parameters = new { readModelName };

        return new CommandDefinition(sql, parameters);
    }

    public static CommandDefinition GetUpdateCommand(string readModelName, long checkpoint)
    {
        var sql = @"
                    INSERT INTO checkpoints (read_model_name, checkpoint)
                    VALUES (@ReadModelName, @Checkpoint)
                    ON CONFLICT (read_model_name) DO UPDATE 
                    SET checkpoint = @Checkpoint";

        var parameters = new { readModelName, Checkpoint = checkpoint };

        return new CommandDefinition(sql, parameters);
    }
}