namespace TeaDriven.Maysternya.ViewModels

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

    type Model =
        {
            SteamDirectory: ConfiguredDirectory
            WorkshopDirectory: ConfiguredDirectory
            Ets2ModsDirectory: ConfiguredDirectory
            AtsModsDirectory: ConfiguredDirectory
            HashFsExtractorPath: ConfiguredFile
            SelectedGame: SelectedGame
            DefaultSelectedGame: SelectedGame
            Mods: Mod list
        }
        with
            static member Default =
                {
                    SteamDirectory = ConfiguredDirectory.Empty
                    WorkshopDirectory = ConfiguredDirectory.Empty
                    Ets2ModsDirectory = ConfiguredDirectory.Empty
                    AtsModsDirectory = ConfiguredDirectory.Empty
                    HashFsExtractorPath = ConfiguredFile.Empty
                    SelectedGame = NoGame
                    DefaultSelectedGame = NoGame
                    Mods = []
                }

    let updatePaths model paths =
        {
            model with
                SteamDirectory = FileSystem.createConfiguredDirectory paths.SteamPath
                WorkshopDirectory = FileSystem.createConfiguredDirectory paths.WorkshopContentPath
                Ets2ModsDirectory = FileSystem.createConfiguredDirectory paths.Ets2ModsPath
                AtsModsDirectory = FileSystem.createConfiguredDirectory paths.AtsModsPath
        }

    type Message =
        | UpdateSteamDirectory of string option
        | UpdateHashFsExtractorPath of string option
        | SelectGame of SelectedGame
        | RefreshModsList
        | RemoveVersionRestriction of string * string * Package list
        | Terminate

    let commandAfterDirectorySelection model =
        match model.DefaultSelectedGame with
        | Ets2 as game when model.Ets2ModsDirectory.PathExists -> SelectGame game |> Cmd.ofMsg
        | Ats as game when model.AtsModsDirectory.PathExists -> SelectGame game |> Cmd.ofMsg
        | _ -> Cmd.none

    let init () =
        let settings =
            Settings.loadSettings ()
            |> Option.defaultValue
                {
                    SteamPath = Constants.Paths.DefaultSteamPath
                    DefaultGame = NoGame
                }

        let model =
            settings.SteamPath
            |> FileSystem.determinePaths
            |> updatePaths { Model.Default with DefaultSelectedGame = settings.DefaultGame }

        model, commandAfterDirectorySelection model

    let update message model =
        match message with
        | UpdateSteamDirectory value ->
            let model =
                (model, value)
                ||> updateIfSome
                        (fun model path -> path |> FileSystem.determinePaths |> updatePaths model)

            model, commandAfterDirectorySelection model
        | UpdateHashFsExtractorPath value ->
            let model =
                (model, value)
                ||> updateIfSome
                        (fun model path ->
                            {
                                model with HashFsExtractorPath = FileSystem.createConfiguredFile path
                            })

            model |> withoutCommand
        | SelectGame game ->
            { model with SelectedGame = game; DefaultSelectedGame = NoGame }, Cmd.ofMsg RefreshModsList
        | RefreshModsList ->
            let modsPath =
                match model.SelectedGame with
                | Ets2 -> model.Ets2ModsDirectory.Path |> Some
                | Ats -> model.AtsModsDirectory.Path |> Some
                | NoGame -> None

            let mods =
                modsPath
                |> Option.map Mod.readMods
                |> Option.defaultValue []

            { model with Mods = mods } |> withoutCommand
        | RemoveVersionRestriction (modPath, relevantPackageName, allPackages) ->
            Mod.removeVersionRestriction modPath relevantPackageName allPackages

            model, Cmd.ofMsg RefreshModsList
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
