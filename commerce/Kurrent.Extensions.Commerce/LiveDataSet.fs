namespace Kurrent.Extensions.Commerce

open System
open System.Collections.Concurrent
open System.ComponentModel
open System.IO
open System.IO.Compression
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open Bogus
open EventStore.Client
open FSharp.Control
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Serialization.SystemTextJson
open NodaTime.Testing
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

        let append
            (client: EventStoreClient)
            (options: JsonSerializerOptions)
            (revisions: ConcurrentDictionary<StreamName, StreamRevision>)
            (stream: StreamName)
            (events: Shopping.Event array)
            =
            task {
                let expected =
                    match revisions.TryGetValue stream with
                    | true, revision -> revision
                    | false, _ -> StreamRevision.None

                let data =
                    events
                    |> Array.map (fun event ->
                        EventData(
                            Uuid.NewUuid(),
                            event.ToEventType(),
                            ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(event, options))
                        ))

                let! append_result = client.AppendToStreamAsync(StreamName.toString stream, expected, data)
                revisions[stream] <- append_result.NextExpectedStreamRevision
            }

        override this.ExecuteAsync(_, settings) =
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

                let clock = SystemClock.Instance

                let faker = Faker()

                do! ProductCatalogBuilder.build faker configuration logger

                let client_settings = EventStoreClientSettings.Create settings.ConnectionString
                client_settings.OperationOptions.ThrowOnAppendFailure <- false
                use client = new EventStoreClient(client_settings)

                let revisions = ConcurrentDictionary<StreamName, StreamRevision>()

                let cart_count =
                    faker.Random.Int(configuration.Shopping.CartCount.Minimum, configuration.Shopping.CartCount.Maximum)

                logger.LogInformation("Generating {CartCount} carts", cart_count)

                let concurrent_cart_count =
                    faker.Random.Int(
                        configuration.Shopping.ConcurrentCartCount.Minimum,
                        configuration.Shopping.ConcurrentCartCount.Maximum
                    )

                logger.LogInformation("With {ConcurrencyCount} carts concurrently", concurrent_cart_count)

                let concurrency_options =
                    ParallelOptions(MaxDegreeOfParallelism = concurrent_cart_count)

                let initial_delay_in_seconds =
                    int (
                        (double configuration.Shopping.CartActionCount.Minimum)
                        * configuration.Shopping.TimeBetweenCartActions.Minimum.TotalSeconds
                    )

                let simulator: ISimulator<Shopping.Event> = ShoppingJourneySimulator(faker)

                do!
                    Parallel.ForAsync(
                        0,
                        cart_count,
                        concurrency_options,
                        fun (cart: int) (ct: CancellationToken) ->
                            task {
                                let initial_delay =
                                    Duration.FromSeconds(double (faker.Random.Int(0, initial_delay_in_seconds)))

                                do!
                                    simulator.Simulate
                                        (FakeClock(clock.GetCurrentInstant().Plus(initial_delay)))
                                        configuration
                                    |> TaskSeq.iterAsync (fun (stream, event) ->
                                        task {
                                            let until =
                                                Instant.FromDateTimeOffset(
                                                    match event with
                                                    | Shopping.VisitorStartedShopping e -> e.At
                                                    | Shopping.CartShopperGotIdentified e -> e.At
                                                    | Shopping.CustomerStartedShopping e -> e.At
                                                    | Shopping.ItemGotAddedToCart e -> e.At
                                                    | Shopping.ItemGotRemovedFromCart e -> e.At
                                                    | Shopping.CartGotCheckedOut e -> e.At
                                                    | Shopping.CartGotAbandoned e -> e.At
                                                    | Shopping.CheckoutStarted e -> e.At
                                                    | Shopping.ShippingInformationCollected e -> e.At
                                                    | Shopping.ShippingMethodSelected e -> e.At
                                                    | Shopping.ShippingCostCalculated e -> e.At
                                                    | Shopping.BillingInformationCollected e -> e.At
                                                    | Shopping.BillingInformationCopiedFromShippingInformation e ->
                                                        e.At
                                                    | Shopping.PaymentMethodSelected e -> e.At
                                                    | Shopping.CheckoutCompleted e -> e.At
                                                    | Shopping.OrderPlaced e -> e.At
                                                )

                                            if until > clock.GetCurrentInstant() then
                                                do! Task.Delay((until - clock.GetCurrentInstant()).ToTimeSpan(), ct)

                                            do! append client options revisions stream [| event |]
                                        })
                            }
                            |> ValueTask
                    )

                return 0
            }
