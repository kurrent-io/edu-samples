namespace DuckDB.NET.Data

module Extensions =
    type DuckDBConnectionStringBuilder with
        member this.Clone() =
            let builder = DuckDBConnectionStringBuilder()

            for key in this.Keys do
                builder.Add(downcast key, this.Item(downcast key))

            builder
