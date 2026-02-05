namespace TeaDriven.Maysternya.ViewModels

open System
open System.IO

open Elmish
open ReactiveElmish.Avalonia

open TeaDriven.Maysternya.Domain

module App =
    let withoutCommand model = model, Cmd.none

    let updateIfSome createUpdatedModel model value =
        value
        |> Option.map (createUpdatedModel model)
        |> Option.defaultValue model

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
        | UpdateSteamDirectory of string option
        | Terminate

    let init () =
        {
            SteamDirectory = createConfiguredDirectory Constants.Paths.DefaultSteamPath
        }
        |> withoutCommand

    let update message model =
        match message with
        | UpdateSteamDirectory value ->
            (model, value)
            ||> updateIfSome
                (fun model path -> { model with SteamDirectory = createConfiguredDirectory path })
            |> withoutCommand
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
