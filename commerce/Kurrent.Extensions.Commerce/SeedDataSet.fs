namespace Kurrent.Extensions.Commerce

open System
open System.Collections.Generic
open System.ComponentModel
open System.IO
open System.IO.Compression
open System.Text.Json
open System.Text.Json.Serialization
open EventStore.Client
open FSharp.Control
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Serialization.SystemTextJson
open Spectre.Console.Cli
open Spectre.Console.Cli.AsyncCommandExtensions

module SeedDataSet =
    type StreamEventRecord =
        { [<JsonPropertyName("id")>]
          Id: Guid
          [<JsonPropertyName("type")>]
          Type: string
          [<JsonPropertyName("content-type")>]
          ContentType: string
          [<JsonPropertyName("data")>]
          Data: JsonElement }

    type StreamBatchRecord =
        { [<JsonPropertyName("stream")>]
          Stream: string
          [<JsonPropertyName("events")>]
          Events: StreamEventRecord[] }

    type Settings() =
        inherit CommandSettings()

        [<Description("Path to a previously generated data set")>]
        [<CommandArgument(0, "<generated-data-set>")>]
        member val InputPath = "" with get, set

        [<Description("Connection string to the target database")>]
        [<CommandOption("--connection-string")>]
        [<DefaultValue("esdb://localhost:2113?tls=false")>]
        member val ConnectionString = "esdb://localhost:2113?tls=false" with get, set

        member this.EnsureInputPath() =
            if not (File.Exists(this.InputPath)) then
                failwith $"The input path '{this.InputPath}' does not exist."

        member this.DetectInputFormat() =
            match Path.GetExtension(this.InputPath).ToLowerInvariant() with
            | ".zip" -> Zip
            | ".json" -> Json
            | _ -> failwith $"The input path '{this.InputPath}' is neither a JSON or ZIP file."

    [<Description("Seed a dataset into a target database")>]
    type Command(logger: ILogger) =
        inherit AsyncCommand<Settings>()

        let seed (client: EventStoreClient) (events: TaskSeq<StreamBatchRecord>) =
            task {
                let revisions = Dictionary<string, StreamRevision>()

                do!
                    events
                    |> TaskSeq.iterAsync (fun record ->
                        task {
                            let expected =
                                match revisions.TryGetValue record.Stream with
                                | true, revision -> revision
                                | false, _ -> StreamRevision.None

                            let data =
                                record.Events
                                |> Array.map (fun event ->
                                    EventData(
                                        Uuid.FromGuid event.Id,
                                        event.Type,
                                        JsonSerializer.SerializeToUtf8Bytes(event.Data)
                                    ))

                            let! append_result = client.AppendToStreamAsync(record.Stream, expected, data)

                            revisions[record.Stream] <- append_result.NextExpectedStreamRevision
                        })
            }

        override this.ExecuteAsync(context: CommandContext, settings: Settings) =
            task {
                settings.EnsureInputPath()

                this.Describe(settings, logger)

                let client_settings = EventStoreClientSettings.Create settings.ConnectionString
                client_settings.OperationOptions.ThrowOnAppendFailure <- false
                use client = new EventStoreClient(client_settings)

                let options =
                    JsonFSharpOptions
                        .Default()
                        .WithUnionUntagged()
                        .WithUnionUnwrapRecordCases()
                        .ToJsonSerializerOptions()
                        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)

                options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
                options.Converters.Add(JsonStringEnumConverter(JsonNamingPolicy.CamelCase))

                match settings.DetectInputFormat() with
                | Json ->
                    use stream = File.OpenRead(settings.InputPath)

                    let data =
                        JsonSerializer.DeserializeAsyncEnumerable<StreamBatchRecord>(stream, options)

                    do! seed client data
                | Zip ->
                    use archive = new ZipArchive(File.OpenRead(settings.InputPath), ZipArchiveMode.Read)
                    let entry = archive.Entries[0]
                    use stream = entry.Open()

                    let data =
                        JsonSerializer.DeserializeAsyncEnumerable<StreamBatchRecord>(stream, options)

                    do! seed client data

                return 0
            }
