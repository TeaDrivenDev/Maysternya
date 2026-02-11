namespace TeaDriven.Maysternya.ViewModels

open ReactiveElmish

open TeaDriven.Maysternya
open TeaDriven.Maysternya.Domain

open App

type ModViewModel(modData: Mod) =
    inherit ReactiveElmishViewModel()

    member _.Id = modData.Id
    member _.Name = modData.Name
    member _.Version = modData.Version

type MainWindowViewModel(folderPicker: Services.FolderPickerService) as this =
    inherit ReactiveElmishViewModel()

    // TODO Probably move these elsewhere
    let workshopDirectoryButNoModsMessage = "Workshop directory found, but no ETS2 or ATS mod directories"
    let noWorkshopDirectoryMessage = "Workshop directory not found"

    let ets2ModsDirectoryFoundMessage = "ETS2 mod directory found"
    let noEts2ModsDirectoryFoundMessage = "ETS2 mod directory not found"

    let atsModsDirectoryFoundMessage = "ATS mod directory found"
    let noAtsModsDirectoryFoundMessage = "ATS mod directory not found"

    member this.SteamDirectory
        with get () = this.Bind(store, _.SteamDirectory.Path)
        and set value = store.Dispatch(UpdateSteamDirectory (Some value))

    member this.IsSteamDirectoryValid: bool = this.Bind(store, _.SteamDirectory.PathExists)

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
                then ets2ModsDirectoryFoundMessage
                else noEts2ModsDirectoryFoundMessage)

    member this.IsEts2ModsDirectoryFound =
        this.Bind(store, _.Ets2ModsDirectory.PathExists)

    member this.AtsModsDirectoryMessage =
        this.Bind(
            store,
            fun model ->
                if model.AtsModsDirectory.PathExists
                then atsModsDirectoryFoundMessage
                else noAtsModsDirectoryFoundMessage)

    member this.IsAtsModsDirectoryFound =
        this.Bind(store, _.AtsModsDirectory.PathExists)

    member this.SelectedGame = this.Bind(store, _.SelectedGame)

    member this.Mods =
        this.Bind(
            store,
            fun model -> model.Mods |> List.map (fun modData -> new ModViewModel(modData)))

    member this.SelectSteamDirectory() =
        task {
            let! path = folderPicker.TryPickFolder()
            return store.Dispatch(UpdateSteamDirectory path)
        }

    member this.SetSelectedGame(selectedGame: SelectedGame) =
        store.Dispatch(SelectGame selectedGame)

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
