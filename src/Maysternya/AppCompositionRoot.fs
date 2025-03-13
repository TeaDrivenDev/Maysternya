namespace Maysternya

open Microsoft.Extensions.DependencyInjection

open ReactiveElmish.Avalonia

open Maysternya.ViewModels
open Maysternya.Views

type AppCompositionRoot() =
    inherit CompositionRoot()

    let mainView = MainWindowView()

    override this.RegisterServices(services) =
        base.RegisterServices services |> ignore
        services.AddSingleton<Services.FolderPickerService>(Services.FolderPickerService(mainView))

    override this.RegisterViews() =
        Map [
            VM.Key<MainWindowViewModel>(), View.Singleton(mainView)
        ]
