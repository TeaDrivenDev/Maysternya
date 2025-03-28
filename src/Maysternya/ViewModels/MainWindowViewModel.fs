namespace TeaDriven.Maysternya.ViewModels

open ReactiveElmish

open TeaDriven.Maysternya

type MainWindowViewModel(folderPicker: Services.FolderPickerService) =
    inherit ReactiveElmishViewModel()

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
