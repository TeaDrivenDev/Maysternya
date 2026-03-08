namespace TeaDriven.Maysternya.ViewModels

open System
open System.Collections.Generic
open System.Reactive.Linq
open System.Reflection

open Avalonia.Platform.Storage
open DynamicData
open ReactiveElmish
open ReactiveUI

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain
open TeaDriven.Maysternya.Localization
open TeaDriven.Maysternya.LoggingTypes

open App

type ModViewModel(modData: Mod, gameVersion: Version option) as this =
    inherit ReactiveElmishViewModel()

    member _.Id = modData.Id
    member _.Name = modData.Name
    member _.DisplayNameSource = modData.DisplayNameSource
    member _.Author = modData.Author
    member _.Description = modData.Description
    member _.Version = modData.Version
    member _.Categories = modData.Categories |> String.concat ", "
    member _.HighestCompatibleGameVersion =
        match modData.HighestCompatibleGameVersion with
        | SpecificVersion version -> version
        | NotVersionLocked -> ""
    member _.ModPath = modData.Path
    member _.RelevantPackageName = modData.RelevantPackageName
    member _.AllPackages = modData.AllPackages
    member _.VersionCompatibility =
        Mod.determineVersionCompatibility modData.HighestCompatibleGameVersion gameVersion

    member _.RemoveVersionRestriction() =
        {
            ModId = this.Id
            Name = this.Name
            ModPath = this.ModPath
            RelevantPackageName = this.RelevantPackageName
            AllPackages = this.AllPackages
        }
        |> RemoveVersionRestriction
        |> store.Dispatch

    member _.OpenModDirectory() = FileSystem.openDirectoryInFileManager this.ModPath

type MainWindowViewModel(
    folderPicker: Services.FolderPickerService,
    filePicker: Services.FilePickerService) as this =
    inherit ReactiveElmishViewModel()

    let mutable mods = Unchecked.defaultof<_>
    let mutable logEntries = Unchecked.defaultof<_>

    let selectedGameVersion (model: Model) =
        match model.SelectedGame with
        | Ets2 -> model.Ets2Version
        | Ats -> model.AtsVersion
        | NoGame -> None

    let byMinLogLevel: IObservable<Func<LogEntryViewModel<_>, bool>> =
        this
            .WhenAnyValue(_.MinLogLevel)
            .Select(fun logLevel -> Func<_, _>(fun (entry: LogEntryViewModel<_>) -> entry.LogLevel >= logLevel))

    do
        store.Model.Mods
            .Connect()
            .Transform(fun ``mod`` -> new ModViewModel(``mod``, selectedGameVersion store.Model))
            .SortAndBind(
                &mods,
                Comparer.Create(fun (x: ModViewModel) (y: ModViewModel) -> String.Compare(x.Id, y.Id)))
            .DisposeMany()
            .Subscribe()
        |> this.AddDisposable

        store.Model.LogEntries
            .Connect()
            .TransformImmutable(fun logEntry -> new LogEntryViewModel<_>(logEntry))
            .Filter(byMinLogLevel)
            .Bind(&logEntries)
            .DisposeMany()
            .Subscribe()
        |> this.AddDisposable

    member _.WindowTitle =
        let version = Assembly.GetExecutingAssembly().GetName().Version
        let components =
            match version.Build, version.Revision with
            | 0, 0 -> 2
            | _, 0 -> 3
            | _, _ -> 4

        $"{locString Loc.WindowTitle} v{version.ToString(components)}"

    member this.SteamDirectory
        with get () = this.Bind(store, _.SteamDirectory.Path)
        and set value = store.Dispatch(UpdateSteamDirectory (Some value))

    member this.IsSteamDirectoryValid: bool = this.Bind(store, _.SteamDirectory.PathExists)

    member this.HashFsExtractorPath
        with get () = this.Bind(store, _.HashFsExtractorPath.Path)
        and set value = store.Dispatch(UpdateHashFsExtractorPath(Some value))

    member this.IsHashFsExtractorPathValid = this.Bind(store, _.HashFsExtractorPath.FileExists)

    member this.WorkshopDirectoryMessage =
        this.Bind(
            store,
            fun model ->
                if not model.WorkshopDirectory.PathExists
                then locString Loc.WorkshopDirectoryNotFound
                elif not model.Ets2ModsDirectory.PathExists && not model.AtsModsDirectory.PathExists
                then locString Loc.WorkshopButNoModDirectories
                else "")

    member this.IsShowWorkshopDirectoryMessage =
        this.Bind(store, fun model -> this.WorkshopDirectoryMessage <> "")

    member this.Ets2ModsDirectoryMessage =
        this.Bind(
            store,
            fun model ->
                if model.Ets2ModsDirectory.PathExists
                then
                    model.Ets2Version
                    |> Option.map (fun version -> String.Format(locString Loc.Ets2Version_Format, version))
                    |> Option.defaultValue (locString Loc.Ets2)
                else locString Loc.Ets2ModsDirectoryNotFound)

    member this.IsEts2ModsDirectoryFound =
        this.Bind(store, _.Ets2ModsDirectory.PathExists)

    member this.AtsModsDirectoryMessage =
        this.Bind(
            store,
            fun model ->
                if model.AtsModsDirectory.PathExists
                then
                    model.AtsVersion
                    |> Option.map (fun version -> String.Format(locString Loc.AtsVersion_Format, version))
                    |> Option.defaultValue (locString Loc.Ats)
                else locString Loc.AtsModsDirectoryNotFound)

    member this.IsAtsModsDirectoryFound =
        this.Bind(store, _.AtsModsDirectory.PathExists)

    member this.SelectedGame = this.Bind(store, _.SelectedGame)

    member this.Mods = mods

    member this.IsShowLog = this.Bind(store, _.Display.IsShowLog)

    member this.LogEntries = logEntries

    member this.MinLogLevel
        with get (): LogLevel = this.Bind(store, _.Display.MinLogLevel)
        and set value = store.Dispatch(ChangeMinLogLevel value)

    member this.NewLogEntryNotification =
        this.Bind(store, _.Display.NewLogEntryNotification)

    member this.SelectSteamDirectory() =
        task {
            let! path = folderPicker.TryPickFolder()
            return store.Dispatch(UpdateSteamDirectory path)
        }

    member this.SelectHashFsExtractor() =
        task {
            let options =
                FilePickerOpenOptions(
                    Title = locString Loc.SelectExtractorExecutable,
                    FileTypeFilter =
                        [
                            FilePickerFileType(locString Loc.Extractor, Patterns = ["extractor.exe"])
                            FilePickerFileTypes.All
                        ])

            let! path = filePicker.TryPickFile(Some options)
            store.Dispatch(UpdateHashFsExtractorPath path)
        }

    member this.SetSelectedGame(selectedGame: SelectedGame) =
        store.Dispatch(SelectGame selectedGame)

    member this.RefreshModsList() =
        store.Dispatch(InitRefreshModsList true)

    member this.ToggleIsShowLog() = store.Dispatch(ToggleLog)

    member this.Shutdown() =
        let settings =
            {
                SteamPath = this.SteamDirectory
                HashFsExtractorPath = this.HashFsExtractorPath
                DefaultGame = this.SelectedGame
            }

        Settings.saveSettings settings

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
