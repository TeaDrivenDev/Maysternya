namespace TeaDriven.Maysternya.Services

open Avalonia.Controls
open Avalonia.Platform.Storage

type FolderPickerService(mainWindow: Window) =
    member this.OpenFolderPicker() =
        mainWindow.StorageProvider.OpenFolderPickerAsync(FolderPickerOpenOptions())

    member this.TryPickFolder() =
        task {
            let! files = this.OpenFolderPicker()

            return
                files
                |> Seq.tryHead
                |> Option.map _.Path.LocalPath
        }

type FilePickerService(mainWindow: Window) =
    member this.OpenFilePicker() =
        mainWindow.StorageProvider.OpenFilePickerAsync(FilePickerOpenOptions())

    member this.TryPickFile() =
        task {
            let! files = this.OpenFilePicker()

            return
                files
                |> Seq.tryHead
                |> Option.map _.Path.LocalPath
        }
