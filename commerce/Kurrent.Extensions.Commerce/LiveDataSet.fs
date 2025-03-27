namespace Kurrent.Extensions.Commerce

open System
open System.ComponentModel
open System.IO
open System.IO.Compression
open System.Text.Json
open System.Text.Json.Serialization
open Bogus
open EventStore.Client
open FSharp.Control
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Serialization.SystemTextJson
open Spectre.Console.Cli
open Spectre.Console.Cli.AsyncCommandExtensions

module LiveDataSet =
    type Settings() =
        inherit CommandSettings()

        [<Description("Configuration file")>]
        [<CommandOption("-c|--configuration")>]
        [<DefaultValue("")>]
        member val ConfigurationFile = "" with get, set

        [<Description("Connection string to the target database")>]
        [<CommandOption("--connection-string")>]
        [<DefaultValue("esdb://localhost:2113?tls=false")>]
        member val ConnectionString = "esdb://localhost:2113?tls=false" with get, set

    [<Description("Generate and seeds a live dataset according to the configuration")>]
    type Command(logger: ILogger) =
        inherit AsyncCommand<Settings>()

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

                do! ProductCatalogBuilder.build faker configuration logger
                
                let client_settings = EventStoreClientSettings.Create settings.ConnectionString
                client_settings.OperationOptions.ThrowOnAppendFailure <- false
                use client = new EventStoreClient(client_settings)

                do! LiveShoppingSimulator.simulate client faker configuration logger
                
                return 0
            }
