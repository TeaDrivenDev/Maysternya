namespace TeaDriven.Maysternya.ViewModels

open ReactiveElmish

open TeaDriven.Maysternya

open App

type MainWindowViewModel(folderPicker: Services.FolderPickerService) as this =
    inherit ReactiveElmishViewModel()

    member this.SteamDirectory
        with get () = this.Bind(store, _.SteamDirectory.Path)
        and set value = store.Dispatch(UpdateSteamDirectory (Some value))

    member this.IsSteamDirectoryValid: bool = this.Bind(store, _.SteamDirectory.PathExists)

    member this.WorkshopDirectory = this.Bind(store, _.WorkshopDirectory.Path)
    member this.IsWorkshopDirectoryValid = this.Bind(store, _.WorkshopDirectory.PathExists)

    member this.Ets2ModsDirectory = this.Bind(store, _.Ets2ModsDirectory.Path)
    member this.IsEts2ModsDirectoryValid = this.Bind(store, _.Ets2ModsDirectory.PathExists)

    member this.AtsModsDirectory = this.Bind(store, _.AtsModsDirectory.Path)
    member this.IsAtsModsDirectoryValid = this.Bind(store, _.AtsModsDirectory.PathExists)

    member this.SelectSteamDirectory() =
        task {
            let! path = folderPicker.TryPickFolder()
            return store.Dispatch(UpdateSteamDirectory path)
        }

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
