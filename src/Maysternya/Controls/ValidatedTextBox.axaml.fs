namespace TeaDriven.Maysternya.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Data

type ValidatedTextBox () as this =
    inherit UserControl ()

    static let TextProperty =
        AvaloniaProperty.RegisterDirect<ValidatedTextBox, string>(
            nameof(Unchecked.defaultof<ValidatedTextBox>.Text),
            (fun o -> o.Text),
            (fun o v -> o.Text <- v),
            defaultBindingMode = BindingMode.TwoWay)

    static let WatermarkProperty =
        AvaloniaProperty.RegisterDirect<ValidatedTextBox, string>(
            nameof(Unchecked.defaultof<ValidatedTextBox>.Watermark),
            (fun o -> o.Watermark),
            (fun o v -> o.Watermark <- v))

    static let IsValidProperty =
        AvaloniaProperty.RegisterDirect<ValidatedTextBox, bool>(
            nameof(Unchecked.defaultof<ValidatedTextBox>.IsValid),
            (fun o -> o.IsValid),
            (fun o v -> o.IsValid <- v))

    let mutable text = Unchecked.defaultof<string>
    let mutable watermark = Unchecked.defaultof<string>
    let mutable isValid = false

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member this.Text
        with get () = text
        and set value = this.SetAndRaise(TextProperty, &text, value) |> ignore

    member this.Watermark
        with get () = watermark
        and set value = this.SetAndRaise(WatermarkProperty, &watermark, value) |> ignore

    member this.IsValid
        with get () = isValid
        and set value = this.SetAndRaise(IsValidProperty, &isValid, value) |> ignore
