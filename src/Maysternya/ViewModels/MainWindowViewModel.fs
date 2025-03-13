namespace Maysternya.ViewModels

open ReactiveElmish

open Maysternya

type MainWindowViewModel(folderPicker: Services.FolderPickerService) =
    inherit ReactiveElmishViewModel()

    static member DesignVM =
        new MainWindowViewModel(Design.stub)
