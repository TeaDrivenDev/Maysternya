namespace TeaDriven.Maysternya.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml

open TeaDriven.Maysternya.Domain
open TeaDriven.Maysternya.ViewModels

type MainWindowView () as this =
    inherit Window ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)

    member this.Window_OnClosing(sender: obj, e: WindowClosingEventArgs) =
        (this.DataContext :?> MainWindowViewModel).Shutdown()

    member this.SelectedGame_OnIsCheckedChanged(sender: obj, e: RoutedEventArgs) =
        match sender with
        | :? RadioButton as rb when rb.IsChecked.HasValue && rb.IsChecked.Value ->
            match rb.Tag with
            | :? SelectedGame as selectedGame ->
                (this.DataContext :?> MainWindowViewModel).SetSelectedGame(selectedGame)
            | _ -> ()
        | _ -> ()
