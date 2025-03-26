namespace Kurrent.Extensions.Commerce.Framework

open System
open System.Collections.Generic
open System.IO
open FSharp.Control
open DuckDB.NET.Data
open DuckDB.NET.Data.Extensions
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Abstractions

module Sql =
    type SqlProps =
        private
            { Builder: DuckDBConnectionStringBuilder
              Logger: ILogger }

    let private sanitize (text: string) =
        let sanitized =
            text.ReplaceLineEndings(" ").Split(' ', StringSplitOptions.RemoveEmptyEntries)

        String.Join(" ", sanitized)

    let private getAvailableMemoryInGigabytes () =
        float (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes) / (1024.0 ** 3)

    let connect (memory_percentage: int) (cpu_percentage: int) =
        let max_memory =
            int (getAvailableMemoryInGigabytes () * (float memory_percentage / 100.0))

        let max_cpu =
            int (float Environment.ProcessorCount * (float cpu_percentage / 100.0))

        let builder = DuckDBConnectionStringBuilder(DataSource = ":memory:")
        builder.Add("threads", max_cpu)
        builder.Add("memory_limit", $"{max_memory}GB")

        { Builder = builder
          Logger = NullLogger.Instance }

    let connect_with_defaults () = connect 50 50

    let log (logger: ILogger) (props: SqlProps) = { props with Logger = logger }

    let query text read (props: SqlProps) =
        taskSeq {
            let db = props.Builder.Clone()
            let temporary_db = Path.GetTempFileName()
            File.Delete(temporary_db) // DuckDB expects a database file and an empty file is not a database file according to DuckDB
            db.DataSource <- temporary_db
            use connection = new DuckDBConnection(db.ConnectionString)

            props.Logger.LogInformation(
                "Executing SQL query \"{Query}\" on connection \"{ConnectionString}\"",
                sanitize (text),
                db.ConnectionString
            )

            try
                do! connection.OpenAsync()
                use command = connection.CreateCommand()
                command.CommandText <- text
                command.UseStreamingMode <- true
                use reader = command.ExecuteReader()

                if not reader.IsClosed then
                    while reader.Read() do
                        yield read reader

                do! connection.CloseAsync()
            finally
                if File.Exists(db.DataSource) then
                    File.Delete(db.DataSource) // Clean up the temporary database file
        }

    let query_single text read (props: SqlProps) =
        query text read props |> TaskSeq.exactlyOne

    let parameterized_query text read (parameters: IReadOnlyDictionary<string, obj>) (props: SqlProps) =
        taskSeq {
            let db = props.Builder.Clone()
            let temporary_db = Path.GetTempFileName()
            File.Delete(temporary_db) // DuckDB expects a database file and an empty file is not a database file according to DuckDB
            db.DataSource <- temporary_db
            use connection = new DuckDBConnection(db.ConnectionString)

            props.Logger.LogInformation(
                "Executing parameterized SQL query \"{Query}\" on connection \"{ConnectionString}\"",
                sanitize (text),
                db.ConnectionString
            )

            try
                do! connection.OpenAsync()
                use command = connection.CreateCommand()
                command.CommandText <- text
                command.UseStreamingMode <- true

                for parameter in parameters do
                    command.Parameters.Add(DuckDBParameter(parameter.Key, parameter.Value))
                    |> ignore

                use reader = command.ExecuteReader()

                if not reader.IsClosed then
                    while reader.Read() do
                        yield read reader

                do! connection.CloseAsync()
            finally
                if File.Exists(db.DataSource) then
                    File.Delete(db.DataSource) // Clean up the temporary database file
        }

    let query_scalar text (props: SqlProps) =
        task {
            let db = props.Builder.Clone()
            let temporary_db = Path.GetTempFileName()
            File.Delete(temporary_db) // DuckDB expects a database file and an empty file is not a database file according to DuckDB
            db.DataSource <- temporary_db
            use connection = new DuckDBConnection(db.ConnectionString)

            props.Logger.LogInformation(
                "Executing scalar SQL query \"{Query}\" on connection \"{ConnectionString}\"",
                sanitize (text),
                db.ConnectionString
            )

            try
                do! connection.OpenAsync()
                use command = connection.CreateCommand()
                command.CommandText <- text
                command.UseStreamingMode <- true
                let! result = command.ExecuteScalarAsync()
                do! connection.CloseAsync()
                return result
            finally
                if File.Exists(db.DataSource) then
                    File.Delete(db.DataSource) // Clean up the temporary database file
        }
