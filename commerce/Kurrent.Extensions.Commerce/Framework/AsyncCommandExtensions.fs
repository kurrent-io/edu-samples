namespace Spectre.Console.Cli

open System.Text.Json
open Microsoft.Extensions.Logging
open Minerals.StringCases

module AsyncCommandExtensions =
    type AsyncCommand<'Settings when 'Settings :> CommandSettings> with
        member this.Describe(settings: 'Settings, logger: ILogger) =
            logger.LogInformation(
                "Executing command '{Command}' with settings {Settings}",
                this.GetType().DeclaringType.Name.ToKebabCase(),
                JsonSerializer.Serialize(settings)
            )
