namespace Kurrent.Extensions.Commerce

open System
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open Kurrent.Extensions.Commerce.Framework
open EventStore.Client
open FSharp.Control
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Console
open NodaTime
open Spectre.Console
open Spectre.Console.Cli
open Spectre.Console.Cli.ConfiguratorExtensions

module Program =
    type private TypeResolver(provider: IServiceProvider) =
        interface ITypeResolver with
            member this.Resolve(clrType: Type) = provider.GetService(clrType)

    type private TypeRegistrar(builder: IServiceCollection) =
        interface ITypeRegistrar with
            member this.Build() =
                TypeResolver(builder.BuildServiceProvider()) :> ITypeResolver

            member this.Register(service: Type, implementation: Type) =
                builder.AddSingleton(service, implementation) |> ignore

            member this.RegisterInstance(service: Type, implementation: obj) =
                builder.AddSingleton(service, implementation) |> ignore

            member this.RegisterLazy(service: Type, func: Func<obj>) =
                builder.AddSingleton(service, fun _ -> func.Invoke()) |> ignore

    [<EntryPoint>]
    let main args =
        let services = ServiceCollection()

        services.AddLogging(fun c ->
            c.AddSimpleConsole(fun o ->
                o.IncludeScopes <- false
                o.SingleLine <- true
                o.TimestampFormat <- "HH:mm:ss "
                o.ColorBehavior <- LoggerColorBehavior.Enabled
                o.UseUtcTimestamp <- true)
            |> ignore)
        |> ignore

        services.AddSingleton<ILogger>(fun sp -> sp.GetRequiredService<ILoggerFactory>().CreateLogger("edb-commerce"))
        |> ignore

        let app = CommandApp(TypeRegistrar(services))

        app.Configure(fun config ->
            config
                .SetApplicationName("edb-commerce")
                .SetApplicationVersion(Assembly.GetEntryAssembly().GetName().Version.ToString())
                .PropagateExceptions()
                // Note that commands:
                // - must live in an F# module
                // - the name of the F# module will become the command name in kebab case (e.g. name-of-the-module)
                // - must be attributed with [<Description("...")>] for the description to be picked up
                // Please add commands in the order you want them to appear in the help text (mostly alphabetical)
                .AddCommand<GenerateDataSet.Command>()
                .AddCommand<SeedDataSet.Command>()
            |> ignore)

        task {
            try
                return! app.RunAsync(args)
            with error ->
                AnsiConsole.WriteException(error, ExceptionFormats.ShortenEverything)
                return -1
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

// task {
//     let settings =
//         EventStoreClientSettings.Create "esdb://admin:changeit@localhost:2113?tls=true&tlsVerifyCert=false"
//
//     settings.OperationOptions.ThrowOnAppendFailure <- false
//     use client = new EventStoreClient(settings)
//
//     let options =
//         JsonFSharpOptions.Default().WithUnionUntagged().WithUnionUnwrapRecordCases().ToJsonSerializerOptions()
//
//     options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
//
//     let configuration: ShoppingSimulator.Configuration =
//         { ShoppingPeriod = { From = Instant.FromUtc(2020, 1, 1, 0, 0, 0); To = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow) }
//           CartCount = 1000
//           CartActionCount = { Minimum = 1; Maximum = 10 }
//           TimeBetweenCartActions =
//             { Minimum = Duration.FromSeconds 5.0
//               Maximum = Duration.FromMinutes 15.0 }
//           AbandonCartAfterTime = Duration.FromHours 1.0 }
//
//     //let clock = SystemClock.Instance
//
//     do!
//         ShoppingSimulator.simulate configuration
//         |> TaskSeq.map (fun (stream, event) ->
//             stream,
//             EventData(
//                 Uuid.NewUuid(),
//                 event.ToEventName(),
//                 ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(event, options))
//             ))
//         |> TaskSeq.batch
//         |> KurrentDB.seed client
//
//     return 0
// }
// |> Async.AwaitTask
// |> Async.RunSynchronously
