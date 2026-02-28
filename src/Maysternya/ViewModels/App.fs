namespace TeaDriven.Maysternya.ViewModels

open System
open System.Reactive.Subjects

open DynamicData
open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain
open TeaDriven.Maysternya.FileSystemTypes
open TeaDriven.Maysternya.Localization
open TeaDriven.Maysternya.LoggingTypes

module App =
    let inline withCommand command model = model, command
    let inline withoutCommand model = model, Cmd.none

    let minLogLevelToNotify = Warning

    let mutable asyncLogEntries = Unchecked.defaultof<Subject<LogLevel * LogActivity * string>>

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
            LastRefreshConfiguration: LastRefreshConfiguration option
            LogEntries: SourceCache<LogEntry<LogActivity>, DateTimeOffset>
            Display: DisplayFeatures
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
                    LastRefreshConfiguration = None
                    LogEntries = SourceCache.create _.Timestamp
                    Display =
                        {
                            IsShowLog = false
                            MinLogLevel = Informational
                            NewLogEntryNotification = None
                        }
                }
            interface ILogTarget<LogActivity, DateTimeOffset, Model> with
                // First .LogEntries is the interface member, second is the record field.
                // This works, but is probably not an ideal way to do this.
                member this.LogEntries = this.LogEntries

                member this.UpdateLogEntries(logEntries: SourceCache<LogEntry<LogActivity>, DateTimeOffset>) =
                    { this with LogEntries = logEntries }
    and LastRefreshConfiguration =
        {
            SteamDirectory: string
            HashFsExtractorPath: string
            SelectedGame: SelectedGame
        }
    and DisplayFeatures =
        {
            IsShowLog: bool
            MinLogLevel: LogLevel
            NewLogEntryNotification: LogLevel option
        }

    let logAsync logLevel logActivity logMessage = asyncLogEntries.OnNext(logLevel, logActivity, logMessage)

    let withLog logLevel logActivity logMessage model =
        let model =
            if not model.Display.IsShowLog
                && logLevel >= model.Display.MinLogLevel
                && logLevel >= minLogLevelToNotify
            then
                {
                    model with
                        Display.NewLogEntryNotification =
                            model.Display.NewLogEntryNotification
                            |> Option.map
                                (fun notificationLevel ->
                                    if logLevel > notificationLevel then logLevel else notificationLevel)
                            |> Option.defaultValue logLevel
                            |> Some
                }
            else model

        model |> Logging.withLog logLevel logActivity logMessage

    let updatePaths model paths =
        let model =
            {
                model with
                    SteamDirectory = FileSystem.createConfiguredDirectory paths.SteamPath
                    WorkshopDirectory = FileSystem.createConfiguredDirectory paths.WorkshopContentPath
                    Ets2ModsDirectory = FileSystem.createConfiguredDirectory paths.Ets2ModsPath
                    AtsModsDirectory = FileSystem.createConfiguredDirectory paths.AtsModsPath
            }

        let logLevel, logMessage =
            if not model.SteamDirectory.PathExists
            then Warning, String.Format(locString Log.SteamDirectoryNotFound_Format, model.SteamDirectory.Path)
            elif not model.WorkshopDirectory.PathExists
            then Warning, String.Format(locString Log.WorkshopDirectoryNotFound_Format, model.SteamDirectory.Path)
            elif not model.Ets2ModsDirectory.PathExists && not model.AtsModsDirectory.PathExists
            then Warning, String.Format(locString Log.ModDirectoriesNotFound_Format, model.WorkshopDirectory.Path)
            else Informational, String.Format(locString Log.SteamPathIs_Format, model.SteamDirectory.Path)

        model |> withLog logLevel UpdateDirectoryPaths logMessage

    type Message =
        | UpdateSteamDirectory of string option
        | UpdateHashFsExtractorPath of string option
        | SelectGame of SelectedGame
        | InitRefreshGameVersions of Message option
        | CompleteRefreshGameVersions of CompleteRefreshGameVersionsParameters
        | InitRefreshModsList of force: bool
        | CompleteRefreshModsList of Mod list
        | RemoveVersionRestriction of name: string * modPath: string * relevantPackage: string * allPackages: Package list
        | Log of LogLevel * LogActivity * string
        | ToggleLog
        | ChangeMinLogLevel of LogLevel
        | Terminate
    and CompleteRefreshGameVersionsParameters =
        {
            Ets2Version: Version option
            AtsVersion: Version option
            NextMessage: Message option
        }

    let commandAfterDirectorySelection (model: Model) =
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
            | game when condition model game -> Some (InitRefreshModsList false)
            | _ -> None

        Cmd.ofMsg (InitRefreshGameVersions messageAfterRefreshingGameVersions)

    let init () =
        asyncLogEntries <- new Subject<_>()

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
            value
            |> Option.map
                (fun path ->
                    let model = path |> FileSystem.determinePaths |> updatePaths model

                    model, commandAfterDirectorySelection model)
            |> Option.defaultValue (model, Cmd.none)
        | UpdateHashFsExtractorPath value ->
            value
            |> Option.map
                (fun path ->
                    { model with HashFsExtractorPath = FileSystem.createConfiguredFile path }
                    |> withLog
                        Informational
                        UpdateExtractorPath
                        (String.Format(locString Log.ExtractorPathIs_Format, model.HashFsExtractorPath.Path))
                    |> withCommand (Cmd.ofMsg (InitRefreshGameVersions (Some (InitRefreshModsList false)))))
            |> Option.defaultValue (model, Cmd.none)
        | SelectGame game ->
            { model with SelectedGame = game; DefaultSelectedGame = NoGame }
            |> withCommand (Cmd.ofMsg (InitRefreshModsList false))
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
            let logMessage =
                [
                    parameters.Ets2Version
                    |> Option.map (fun version -> String.Format(locString Log.Ets2Version_Format, version))
                    |> Option.defaultValue (locString Log.NoEts2Version)

                    parameters.AtsVersion
                    |> Option.map (fun version -> String.Format(locString Log.AtsVersion_Format, version))
                    |> Option.defaultValue (locString Log.NoAtsVersion)
                ]
                |> String.concat "; "

            let logLevel =
                match parameters.Ets2Version, parameters.AtsVersion with
                | None, None -> Warning
                | _ -> Informational

            {
                model with
                    Ets2Version = parameters.Ets2Version
                    AtsVersion = parameters.AtsVersion
            }
            |> withLog logLevel UpdateGameVersions logMessage
            |> withCommand (parameters.NextMessage |> Option.map Cmd.ofMsg |> Option.defaultValue Cmd.none)
        | InitRefreshModsList force ->
            let refreshConfiguration =
                {
                    SteamDirectory = model.SteamDirectory.Path
                    HashFsExtractorPath = model.HashFsExtractorPath.Path
                    SelectedGame = model.SelectedGame
                }

            if
                force
                || model.LastRefreshConfiguration |> Option.map ((<>) refreshConfiguration) |> Option.defaultValue true
            then
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
                                    | Some modsPath -> return! Mod.readMods logAsync extractorPath modsPath
                                    | None -> return [||]
                                else return [||]
                            }

                        return mods |> Array.toList |> List.choose id
                    }

                { model with LastRefreshConfiguration = Some refreshConfiguration }
                |> withCommand (Cmd.OfAsync.perform readMods () CompleteRefreshModsList)
            else model, Cmd.none
        | CompleteRefreshModsList mods ->
            { model with Mods = mods }
            |> withLog
                Informational
                ReadMods
                (String.Format(locString Log.ModsRead_Format, model.SelectedGame.ToString().ToUpper(), mods.Length))
            |> withoutCommand
        | RemoveVersionRestriction (name, modPath, relevantPackageName, allPackages) ->
            let logLevel, logMessage =
                try
                    Mod.removeVersionRestriction modPath relevantPackageName allPackages
                    Informational, String.Format(locString Log.RemovedVersionRestriction_Format, name)
                with _ ->
                    Error, String.Format(locString Log.ErrorRemovingVersionRestriction_Format, name)

            model
            |> withLog logLevel RemoveRestriction logMessage
            |> withCommand (Cmd.ofMsg (InitRefreshModsList true))
        | Log (logLevel, activity, message) ->
            model |> withLog logLevel activity message |> withoutCommand
        | ToggleLog ->
            {
                model with
                    Display.IsShowLog = not model.Display.IsShowLog
                    Display.NewLogEntryNotification = None
            }
            |> withoutCommand
        | ChangeMinLogLevel minLogLevel ->
            { model with Display.MinLogLevel = minLogLevel } |> withoutCommand
        | Terminate -> model |> withoutCommand

    let subscriptions (model: Model) : Sub<Message> =
        let asyncLogEntriesSub dispatch =
            let subscription = asyncLogEntries |> Observable.subscribe (Log >> dispatch)
            FSharp.Control.Reactive.Disposable.create (fun () -> subscription.Dispose())

        [
            [ nameof(asyncLogEntriesSub) ], asyncLogEntriesSub
        ]

    let store =
        Program.mkAvaloniaProgram init update
        |> Program.withSubscription subscriptions
        |> Program.withErrorHandler (fun (_, ex) -> printfn $"Error: %s{ex.Message}")
        |> Program.withConsoleTrace
        |> Program.mkStore
