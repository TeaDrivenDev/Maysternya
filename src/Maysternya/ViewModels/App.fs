namespace TeaDriven.Maysternya.ViewModels

open System
open System.IO

open Elmish
open ReactiveElmish.Avalonia

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain
open TeaDriven.Maysternya.FileSystemTypes

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
            WorkshopDirectory: ConfiguredDirectory
            Ets2ModsDirectory: ConfiguredDirectory
            AtsModsDirectory: ConfiguredDirectory
        }
        with
            static member Default =
                {
                    SteamDirectory = ConfiguredDirectory.Empty
                    WorkshopDirectory = ConfiguredDirectory.Empty
                    Ets2ModsDirectory = ConfiguredDirectory.Empty
                    AtsModsDirectory = ConfiguredDirectory.Empty
                }

    let updatePaths model paths =
        {
            model with
                SteamDirectory = createConfiguredDirectory paths.SteamPath
                WorkshopDirectory = createConfiguredDirectory paths.WorkshopContentPath
                Ets2ModsDirectory = createConfiguredDirectory paths.Ets2ModsPath
                AtsModsDirectory = createConfiguredDirectory paths.AtsModsPath
        }

    type Message =
        | UpdateSteamDirectory of string option
        | Terminate

    let init () =
        Constants.Paths.DefaultSteamPath
        |> FileSystem.determinePaths
        |> updatePaths Model.Default
        |> withoutCommand

    let update message model =
        match message with
        | UpdateSteamDirectory value ->
            (model, value)
            ||> updateIfSome
                (fun model path -> path |> FileSystem.determinePaths |> updatePaths model)
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
