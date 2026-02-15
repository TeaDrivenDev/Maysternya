namespace TeaDriven.Maysternya

open Microsoft.Extensions.DependencyInjection

open ReactiveElmish.Avalonia

open TeaDriven.Maysternya.ViewModels
open TeaDriven.Maysternya.Views

type AppCompositionRoot() =
    inherit CompositionRoot()

    let mainView = MainWindowView()

    override this.RegisterServices(services) =
        base.RegisterServices services |> ignore
        services.AddSingleton<Services.FolderPickerService>(Services.FolderPickerService(mainView)) |> ignore
        services.AddSingleton<Services.FilePickerService>(Services.FilePickerService(mainView))

    override this.RegisterViews() =
        Map [
            VM.Key<MainWindowViewModel>(), View.Singleton(mainView)
        ]

    static member val Instance = AppCompositionRoot()
