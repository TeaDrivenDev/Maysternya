namespace TeaDriven.Maysternya.ViewModels

open System

open Avalonia.Platform.Storage
open ReactiveElmish

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain

open App

type ModViewModel(modData: Mod, gameVersion: Version option) as this =
    inherit ReactiveElmishViewModel()

    member _.Id = modData.Id
    member _.Name = modData.Name
    member _.DisplayNameSource = modData.DisplayNameSource
    member _.Author = modData.Author
    member _.Description = modData.Description
    member _.Version = modData.Version
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
        store.Dispatch(RemoveVersionRestriction (this.ModPath, this.RelevantPackageName, this.AllPackages))

type MainWindowViewModel(
    folderPicker: Services.FolderPickerService,
    filePicker: Services.FilePickerService) as this =
    inherit ReactiveElmishViewModel()

    // TODO Probably move these elsewhere
    let workshopDirectoryButNoModsMessage = "Workshop directory found, but no ETS2 or ATS mod directories"
    let noWorkshopDirectoryMessage = "Workshop directory not found"

    let ets2NoVersionMessage = "ETS2"
    let ets2VersionMessage = "ETS2 (version {0})"
    let noEts2ModsDirectoryFoundMessage = "ETS2 mod directory not found"

    let atsNoVersionMessage = "ATS"
    let atsVersionMessage = "ATS (version {0})"
    let noAtsModsDirectoryFoundMessage = "ATS mod directory not found"

    let extractorInfoMessage =
        "The HashFS extractor is used to get metadata (name, author, description, version) for mods "
        + "packaged as HashFS, as well as the installed game versions. Using it is optional, but "
        + "recommended; only the display of some mods and the game versions will be affected if it "
        + "is missing."
        + Environment.NewLine
        + Environment.NewLine
        + "If using the extractor, version 2026-02-15 or newer is required; using an older version "
        + "will cause the application to not work at all."

    let selectedGameVersion model =
        match model.SelectedGame with
        | Ets2 -> model.Ets2Version
        | Ats -> model.AtsVersion
        | NoGame -> None

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
                then noWorkshopDirectoryMessage
                elif not model.Ets2ModsDirectory.PathExists && not model.AtsModsDirectory.PathExists
                then workshopDirectoryButNoModsMessage
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
                    |> Option.map (fun version -> String.Format(ets2VersionMessage, version))
                    |> Option.defaultValue ets2NoVersionMessage
                else noEts2ModsDirectoryFoundMessage)

    member this.IsEts2ModsDirectoryFound =
        this.Bind(store, _.Ets2ModsDirectory.PathExists)

    member this.AtsModsDirectoryMessage =
        this.Bind(
            store,
            fun model ->
                if model.AtsModsDirectory.PathExists
                then
                    model.AtsVersion
                    |> Option.map (fun version -> String.Format(atsVersionMessage, version))
                    |> Option.defaultValue atsNoVersionMessage
                else noAtsModsDirectoryFoundMessage)

    member this.IsAtsModsDirectoryFound =
        this.Bind(store, _.AtsModsDirectory.PathExists)

    member this.SelectedGame = this.Bind(store, _.SelectedGame)

    member this.Mods =
        this.Bind(
            store,
            fun model ->
                model.Mods
                |> List.map (fun modData -> new ModViewModel(modData, selectedGameVersion model)))

    member this.ExtractorInfoMessage = extractorInfoMessage

    member this.SelectSteamDirectory() =
        task {
            let! path = folderPicker.TryPickFolder()
            return store.Dispatch(UpdateSteamDirectory path)
        }

    member this.SelectHashFsExtractor() =
        task {
            let options =
                FilePickerOpenOptions(
                    Title = "Select extractor executable",
                    FileTypeFilter =
                        [
                            FilePickerFileType("Extractor", Patterns = ["extractor.exe"])
                            FilePickerFileTypes.All
                        ])

            let! path = filePicker.TryPickFile(Some options)
            return store.Dispatch(UpdateHashFsExtractorPath path)
        }

    member this.SetSelectedGame(selectedGame: SelectedGame) =
        store.Dispatch(SelectGame selectedGame)

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
