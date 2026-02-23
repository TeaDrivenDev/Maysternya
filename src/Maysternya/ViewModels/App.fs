namespace TeaDriven.Maysternya.ViewModels

open System

open DynamicData
open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain
open TeaDriven.Maysternya.FileSystemTypes
open TeaDriven.Maysternya.Logging

module App =
    let withoutCommand model = model, Cmd.none

    let updateIfSome createUpdatedModel model value =
        value
        |> Option.map (createUpdatedModel model)
        |> Option.defaultValue model

    type Activity = UpdatePaths | UpdateGameVersions | ReadMods | RemoveRestriction

    type Model =
        {
            SteamDirectory: ConfiguredDirectory
            WorkshopDirectory: ConfiguredDirectory
            Ets2ModsDirectory: ConfiguredDirectory
            AtsModsDirectory: ConfiguredDirectory
            HashFsExtractorPath: ConfiguredFile
            Ets2Version: Version option
            AtsVersion: Version option
            SelectedGame: SelectedGame
            DefaultSelectedGame: SelectedGame
            Mods: Mod list
            LogEntries: SourceCache<LogEntry<Activity>, DateTimeOffset>
        }
        with
            static member Default =
                {
                    SteamDirectory = ConfiguredDirectory.Empty
                    WorkshopDirectory = ConfiguredDirectory.Empty
                    Ets2ModsDirectory = ConfiguredDirectory.Empty
                    AtsModsDirectory = ConfiguredDirectory.Empty
                    HashFsExtractorPath = ConfiguredFile.Empty
                    Ets2Version = None
                    AtsVersion = None
                    SelectedGame = NoGame
                    DefaultSelectedGame = NoGame
                    Mods = []
                    LogEntries = SourceCache.create _.Timestamp
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
        | InitRefreshGameVersions of Message option
        | CompleteRefreshGameVersions of CompleteRefreshGameVersionsParameters
        | InitRefreshModsList
        | CompleteRefreshModsList of Mod list
        | RemoveVersionRestriction of string * string * Package list
        | Log of LogLevel * Activity * string
        | Terminate
    and CompleteRefreshGameVersionsParameters =
        {
            Ets2Version: Version option
            AtsVersion: Version option
            NextMessage: Message option
        }

    let withLog logLevel activity message model =
         let logEntry = LogEntry<_>.create logLevel activity message

         { model with LogEntries = logEntry.addTo model.LogEntries }


    let commandAfterDirectorySelection model =
        let condition model game =
            match game with
            | Ets2 -> model.Ets2ModsDirectory.PathExists
            | Ats -> model.AtsModsDirectory.PathExists
            | NoGame -> false

        let messageAfterRefreshingGameVersions =
            match model.SelectedGame with
            | NoGame ->
                match model.DefaultSelectedGame with
                | NoGame -> None
                | game when condition model game -> Some (SelectGame game)
                | _ -> None
            | game when condition model game -> Some InitRefreshModsList
            | _ -> None

        Cmd.ofMsg (InitRefreshGameVersions messageAfterRefreshingGameVersions)

    let init () =
        let settings =
            Settings.loadSettings ()
            |> Option.defaultValue
                {
                    SteamPath = Constants.Paths.DefaultSteamPath
                    HashFsExtractorPath = ""
                    DefaultGame = NoGame
                }

        let model =
            {
                Model.Default with
                    HashFsExtractorPath = FileSystem.createConfiguredFile settings.HashFsExtractorPath
                    DefaultSelectedGame = settings.DefaultGame
            }

        model, Cmd.ofMsg (UpdateSteamDirectory (Some settings.SteamPath))

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

            model, Cmd.ofMsg (InitRefreshGameVersions (Some InitRefreshModsList))
        | SelectGame game ->
            { model with SelectedGame = game; DefaultSelectedGame = NoGame }, Cmd.ofMsg InitRefreshModsList
        | InitRefreshGameVersions nextMessage ->
            let steamDirectory = model.SteamDirectory
            let extractor = model.HashFsExtractorPath

            let readGameVersions () =
                async {
                    let! [ets2Version; atsVersion] =
                        async {
                            if steamDirectory.PathExists && extractor.FileExists
                            then
                                let versions = ResizeArray<_>()
                                for game in [Constants.Paths.Ets2Game; Constants.Paths.AtsGame] do
                                    let! version = Mod.readGameVersion extractor.Path steamDirectory.Path game
                                    versions.Add version

                                return versions |> Seq.toList
                            else return [None; None]
                        }

                    return
                        {
                            Ets2Version = ets2Version
                            AtsVersion = atsVersion
                            NextMessage = nextMessage
                        }
                }

            model, Cmd.OfAsync.perform readGameVersions () CompleteRefreshGameVersions
        | CompleteRefreshGameVersions parameters ->
            {
                model with
                    Ets2Version = parameters.Ets2Version
                    AtsVersion = parameters.AtsVersion
            }, parameters.NextMessage |> Option.map Cmd.ofMsg |> Option.defaultValue Cmd.none
        | InitRefreshModsList ->
            let readMods () =
                async {
                    let! mods =
                        async {
                            if model.SteamDirectory.PathExists
                            then
                                let modsPath =
                                    match model.SelectedGame with
                                    | Ets2 -> model.Ets2ModsDirectory.Path |> Some
                                    | Ats -> model.AtsModsDirectory.Path |> Some
                                    | NoGame -> None

                                let extractorPath =
                                    if model.HashFsExtractorPath.FileExists
                                    then Some model.HashFsExtractorPath.Path
                                    else None

                                match modsPath with
                                | Some modsPath -> return! Mod.readMods extractorPath modsPath
                                | None -> return [||]
                            else return [||]
                        }

                    return mods |> Array.toList
                }

            model, Cmd.OfAsync.perform readMods () CompleteRefreshModsList
        | CompleteRefreshModsList mods -> { model with Mods = mods } |> withoutCommand
        | RemoveVersionRestriction (modPath, relevantPackageName, allPackages) ->
            Mod.removeVersionRestriction modPath relevantPackageName allPackages

            model, Cmd.ofMsg InitRefreshModsList
        | Log (logLevel, activity, message) ->
            (model |> withLog logLevel activity message) |> withoutCommand
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
