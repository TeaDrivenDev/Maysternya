namespace TeaDriven.Maysternya.Controls

open System
open System.Collections.Generic
open System.Globalization

open Avalonia
open Avalonia.Controls
open Avalonia.Data.Converters
open Avalonia.Markup.Xaml
open Avalonia.Data

type RequirementLevel = Required | Recommended | Optional

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

    static let RequirementLevelProperty =
        AvaloniaProperty.RegisterDirect<ValidatedTextBox, RequirementLevel>(
            nameof(Unchecked.defaultof<ValidatedTextBox>.RequirementLevel),
            (fun o -> o.RequirementLevel),
            (fun o v -> o.RequirementLevel <- v))

    static let iconClassConverter =
        {
            new IMultiValueConverter with
                member this.Convert(values: IList<obj>, targetType: Type, parameter: obj, culture: CultureInfo) =
                    match values |> Seq.toList with
                    | [ :? bool as isValid; :? RequirementLevel as requirementLevel ; :? string as text ] ->
                        if isValid
                        then "Valid"
                        elif not <| String.IsNullOrWhiteSpace text
                        then "Invalid"
                        else
                            match requirementLevel with
                            | Required -> "Invalid"
                            | Recommended -> "Recommended"
                            | Optional -> "Optional"
                    | _ -> ""
        }

    let mutable text = Unchecked.defaultof<string>
    let mutable watermark = Unchecked.defaultof<string>
    let mutable isValid = false
    let mutable requirementLevel = Required

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

    member this.RequirementLevel
        with get () = requirementLevel
        and set value = this.SetAndRaise(RequirementLevelProperty, &requirementLevel, value) |> ignore

    static member IconClassConverter = iconClassConverter

// TODO Deduplicate
// https://github.com/AvaloniaUI/Avalonia/issues/2427#issuecomment-2861152275
type BindableStyleClasses() =
    static let ClassesProperty: AttachedProperty<string> =
        AvaloniaProperty.RegisterAttached<BindableStyleClasses, StyledElement, string>("Classes", defaultValue="")

    static let HandleClassesChanged (element: StyledElement) (e: AvaloniaPropertyChangedEventArgs): unit =
        let newValue = e.NewValue |> Option.ofObj |> Option.map string |> Option.defaultValue ""

        element.Classes.Clear()
        element.Classes.AddRange(newValue.Split(' '))

    static do
        ClassesProperty.Changed.AddClassHandler<StyledElement, string>(Action<_, _> HandleClassesChanged) |> ignore

    static member GetClasses(element: StyledElement) = element.GetValue(ClassesProperty)
    static member SetClasses(element: StyledElement, value: string) = element.SetValue(ClassesProperty, value) |> ignore
