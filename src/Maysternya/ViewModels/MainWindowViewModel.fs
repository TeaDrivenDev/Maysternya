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

    member this.SelectSourceDirectory() = ()

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
