namespace Kurrent.Extensions.Commerce

open System
open System.ComponentModel
open System.IO
open System.IO.Compression
open System.Text.Json
open System.Text.Json.Serialization
open Bogus
open FSharp.Control
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Serialization.SystemTextJson
open NodaTime.Testing
open Spectre.Console.Cli
open Spectre.Console.Cli.AsyncCommandExtensions

module GenerateDataSet =
    type private EventDataRecord =
        { Id: Guid
          Type: string
          ContentType: string
          Data: JsonElement }

    type private StreamEventRecord =
        { Stream: StreamName
          Event: EventDataRecord
          DataLength: int64 }

    type private StreamBatchRecord =
        { Stream: StreamName
          Events: EventDataRecord[]
          DataLength: int64 }

    [<Literal>]
    let private max_append_size = 1_000_000

    let private batch (stream: TaskSeq<StreamEventRecord>) =
        taskSeq {
            use enumerator = stream.GetAsyncEnumerator()
            let! moved = enumerator.MoveNextAsync()

            if moved then
                let mutable current_stream = enumerator.Current.Stream
                let mutable batch_size = enumerator.Current.DataLength
                let batch = ResizeArray<EventDataRecord>()
                batch.Add enumerator.Current.Event

                while! enumerator.MoveNextAsync() do
                    // Still the same stream
                    if enumerator.Current.Stream = current_stream then
                        // Keep growing the batch until we're over the max append size
                        if batch_size + enumerator.Current.DataLength < max_append_size then
                            batch.Add enumerator.Current.Event
                            batch_size <- batch_size + enumerator.Current.DataLength
                        else
                            yield
                                { Stream = current_stream
                                  Events = batch.ToArray()
                                  DataLength = batch_size }

                            batch.Clear()
                            batch.Add enumerator.Current.Event
                            batch_size <- enumerator.Current.DataLength
                    else
                        yield
                            { Stream = current_stream
                              Events = batch.ToArray()
                              DataLength = batch_size }

                        batch.Clear()
                        batch.Add enumerator.Current.Event
                        batch_size <- enumerator.Current.DataLength
                        current_stream <- enumerator.Current.Stream

                // Make sure we yield any residue
                if batch.Count > 0 then
                    yield
                        { Stream = current_stream
                          Events = batch.ToArray()
                          DataLength = batch_size }
        }

    type Settings() =
        inherit CommandSettings()

        [<Description("Configuration file")>]
        [<CommandOption("-c|--configuration")>]
        [<DefaultValue("")>]
        member val ConfigurationFile = "" with get, set

        [<Description("Output file with .zip or .json extension")>]
        [<CommandOption("-o|--output")>]
        [<DefaultValue("commerce-data-set.zip")>]
        member val OutputFile = "commerce-data-set.zip" with get, set

        member this.DetectOutputFormat() =
            match Path.GetExtension(this.OutputFile).ToLowerInvariant() with
            | ".json" -> Json
            | ".zip" -> Zip
            | _ -> failwith $"The output file '{this.OutputFile}' is neither a JSON or ZIP file."

    [<Description("Generate a dataset according to the configuration")>]
    type Command(logger: ILogger) =
        inherit AsyncCommand<Settings>()

        let write_output (writer: Utf8JsonWriter) (output: TaskSeq<StreamBatchRecord>) =
            task {
                writer.WriteStartArray()

                do!
                    output
                    |> TaskSeq.iter (fun record ->
                        writer.WriteStartObject()
                        writer.WriteString("stream", StreamName.toString record.Stream)
                        writer.WritePropertyName("events")
                        writer.WriteStartArray()

                        record.Events
                        |> Array.iter (fun event ->
                            writer.WriteStartObject()
                            writer.WriteString("id", event.Id.ToString())
                            writer.WriteString("type", event.Type)
                            writer.WriteString("content-type", event.ContentType)
                            writer.WritePropertyName("data")
                            event.Data.WriteTo writer
                            //skipping metadata for now
                            writer.WriteEndObject())

                        writer.WriteEndArray()
                        writer.WriteEndObject())

                writer.WriteEndArray()
                writer.Flush()
            }

        override this.ExecuteAsync(context, settings) =
            task {
                this.Describe(settings, logger)

                let options =
                    JsonFSharpOptions
                        .Default()
                        .WithUnionUntagged()
                        .WithUnionUnwrapRecordCases()
                        .ToJsonSerializerOptions()
                        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)

                options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
                options.Converters.Add(JsonStringEnumConverter(JsonNamingPolicy.CamelCase))

                let configuration: Configuration =
                    match settings.ConfigurationFile with
                    | "" -> Configuration.Default
                    | file ->
                        if File.Exists file then
                            let json = JsonDocument.Parse(File.ReadAllText file)
                            JsonSerializer.Deserialize<Configuration>(json.RootElement, options)
                        else
                            failwith $"The configuration file '{file}' does not exist."

                let faker = Faker()
                
                let simulator : ISimulator<Shopping.Event> = ShoppingJourneySimulator(faker)

                do! ProductCatalogBuilder.build faker configuration logger

                let output =
                    taskSeq {
                        let cart_count = faker.Random.Int(
                            configuration.Shopping.CartCount.Minimum,
                            configuration.Shopping.CartCount.Maximum)
                        
                        logger.LogInformation("Generating {CartCount} carts", cart_count)
                        
                        let time_between_carts =
                            Duration.FromTicks(
                                (configuration.Shopping.ShoppingPeriod.To -
                                configuration.Shopping.ShoppingPeriod.From).TotalTicks / double cart_count)
                            
                        logger.LogInformation("Time between carts is {Days} {Hours}:{Minutes}:{Seconds}", time_between_carts.Days, time_between_carts.Hours, time_between_carts.Minutes, time_between_carts.Seconds)
                        
                        let clock = FakeClock(configuration.Shopping.ShoppingPeriod.From)
                        for _ in 1 .. cart_count do
                            yield! (simulator.Simulate (FakeClock(clock.GetCurrentInstant())) configuration)
                            clock.Advance(time_between_carts)
                    }
                    |> TaskSeq.map (fun (stream, event) ->
                        let encoded = JsonSerializer.SerializeToUtf8Bytes(event, options)
                        let json = JsonDocument.Parse(encoded)

                        { Stream = stream
                          Event =
                            { Id = Guid.NewGuid()
                              Type = event.ToEventType()
                              ContentType = "application/json"
                              Data = json.RootElement }
                          DataLength = encoded.Length })
                    |> batch

                match settings.DetectOutputFormat() with
                | Zip ->
                    logger.LogInformation("Writing output to {ZipFile}", settings.OutputFile)

                    use zip_file =
                        new ZipArchive(File.Create(settings.OutputFile), ZipArchiveMode.Create, false)

                    use entry_stream = zip_file.CreateEntry("data.json").Open()
                    use writer = new Utf8JsonWriter(entry_stream, JsonWriterOptions(Indented = true))
                    do! write_output writer output
                | Json ->
                    logger.LogInformation("Writing output to {JsonFile}", settings.OutputFile)
                    use output_file = File.Create(settings.OutputFile)
                    use writer = new Utf8JsonWriter(output_file, JsonWriterOptions(Indented = true))
                    do! write_output writer output

                return 0
            }
