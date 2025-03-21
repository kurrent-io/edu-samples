namespace Spectre.Console.Cli

open System.ComponentModel
open System.Reflection
open Minerals.StringCases

module ConfiguratorExtensions =
    type IConfigurator with
        member this.AddCommand<'Command when 'Command :> ICommand and 'Command: not struct>() =
            this
                .AddCommand<'Command>(typeof<'Command>.DeclaringType.Name.ToKebabCase())
                .WithDescription(typeof<'Command>.GetCustomAttribute<DescriptionAttribute>().Description)
            |> ignore

            this
