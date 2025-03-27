namespace Kurrent.Extensions.Commerce

open System.ComponentModel
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Serialization.SystemTextJson
open Spectre.Console.Cli
open Spectre.Console.Cli.AsyncCommandExtensions

module GetDefaultConfiguration =
    type Settings() =
        inherit CommandSettings()

        [<Description("Path to the configuration output file (json)")>]
        [<CommandArgument(0, "<output-file>")>]
        member val OutputFile = "" with get, set

    [<Description("Gets the default configuration")>]
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

                logger.LogInformation("Writing output to {JsonFile}", settings.OutputFile)

                let json = JsonSerializer.Serialize(Configuration.Default, options)

                File.WriteAllText(settings.OutputFile, json)

                return 0
            }
