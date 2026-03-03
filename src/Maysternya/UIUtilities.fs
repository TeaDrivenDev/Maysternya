namespace TeaDriven.Maysternya.UIUtilities

open System
open System.Collections.Generic
open System.Globalization

open Microsoft.FSharp.Reflection

open Avalonia
open Avalonia.Data.Converters
open Avalonia.Markup.Xaml

open TeaDriven.Maysternya

type BytesToMegabytesConverter() =
    static member Instance = BytesToMegabytesConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            match value with
            | :? int64 as size -> (System.Convert.ToDouble size) / (1024. * 1024.)
            | _ -> 0.
            :> obj

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            raise (NotSupportedException())

type XamlComparisonValueCollection() = inherit List<obj>()

type XamlComparisonValue() as this =
    member val Value: obj = null with get, set

    override _.GetHashCode() = hash this.Value

    override _.Equals(other) =
        match other with
        | :? XamlComparisonValue as otherWrapper -> this.Value.Equals(otherWrapper.Value)
        | _ -> this.Value.Equals(other)

type ValueEqualsParameterConverter(inverted: bool) =
    static member IsEqual = ValueEqualsParameterConverter(inverted = false) :> IValueConverter
    static member IsNotEqual = ValueEqualsParameterConverter(inverted = true) :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            match parameter with
            | :? XamlComparisonValueCollection as collection ->
                collection
                |> Seq.exists _.Equals(value)
                |> ((<>) inverted)
                |> box
            | _ -> (value = parameter) <> inverted

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            raise (NotSupportedException())

type ValuesEqualMultiConverter() =
    static member Instance = ValuesEqualMultiConverter() :> IMultiValueConverter

    interface IMultiValueConverter with
        member this.Convert(values: IList<obj>, targetType: Type, parameter: obj, culture: CultureInfo) =
            if values.Count > 1
            then values[0] = values[1]
            else false

// https://www.fssnip.net/7VM/title/Getting-a-sequence-of-all-union-cases-in-discriminated-union
type UnionCaseItemsSourceExtension<'T>() =
    inherit MarkupExtension()

    override this.ProvideValue(serviceProvider) =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Seq.map (fun x -> FSharpValue.MakeUnion(x, Array.zeroCreate(x.GetFields().Length)) :?> 'T)
        |> box

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

type LogLevelToStyleClassConverter() =
    let classForLogLevel logLevel =
        match logLevel with
        | Diagnostic -> "LogDiagnostic"
        | Warning -> "LogWarning"
        | Error -> "LogError"
        | _ -> ""

    static member Instance = LogLevelToStyleClassConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            match value with
            | :? Option<LogLevel> as maybe -> maybe |> Option.map classForLogLevel |> Option.defaultValue "" |> box
            | :? LogLevel as logLevel -> classForLogLevel logLevel
            | _ -> ""

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            raise (NotSupportedException())

type OptionToBooleanConverter<'T>() =
    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            match value with
            | :? Option<'T> as option -> option.IsSome
            | _ -> false

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            raise (NotSupportedException())

type UrlExtension(url: string) =
    inherit MarkupExtension()

    [<MarkupExtensionDefaultOption>]
    member val Url = url with get, set

    override this.ProvideValue(serviceProvider) = Uri this.Url
