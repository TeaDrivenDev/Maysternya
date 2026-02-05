namespace TeaDriven.Maysternya.ViewModels

open System
open System.IO

open Elmish
open ReactiveElmish.Avalonia

open TeaDriven.Maysternya.Domain

module App =
    let withoutCommand model = model, Cmd.none

    let createConfiguredDirectory path =
        {
            Path = path
            PathExists = not <| String.IsNullOrWhiteSpace path && Directory.Exists path
        }

    type Model =
        {
            SteamDirectory: ConfiguredDirectory
        }

    type Message =
        | Terminate

    let init () =
        {
            SteamDirectory = ConfiguredDirectory.Empty
        }
        |> withoutCommand

    let update message model =
        match message with
        | Terminate -> model |> withoutCommand

    let subscriptions (model: Model) : Sub<Message> =
        [
        ]

    let store =
        Program.mkAvaloniaProgram init update
        |> Program.withSubscription subscriptions
        |> Program.withErrorHandler (fun (_, ex) -> printfn $"Error: %s{ex.Message}")
        |> Program.withConsoleTrace
        |> Program.mkStore
