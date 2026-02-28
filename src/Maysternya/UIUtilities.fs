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

type XamlValueCollection() = inherit List<obj>()

type XamlWrapper() as this =
    member val Item: obj = null with get, set

    override _.GetHashCode() = hash this.Item

    override _.Equals(other) =
        match other with
        | :? XamlWrapper as otherWrapper -> this.Item.Equals(otherWrapper.Item)
        | _ -> this.Item.Equals(other)

type ValueEqualsParameterConverter(inverted: bool) =
    static member IsEqual = ValueEqualsParameterConverter(inverted = false) :> IValueConverter
    static member IsNotEqual = ValueEqualsParameterConverter(inverted = true) :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo): obj =
            match parameter with
            | :? XamlValueCollection as collection ->
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
        | Diagnostic -> "Diagnostic"
        | Warning -> "Warning"
        | Error -> "Error"
        | _ -> ""

    static member Instance = LogLevelToStyleClassConverter()

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            match value with
            | :? Option<LogLevel> as maybe -> maybe |> Option.map classForLogLevel |> Option.defaultValue "" |> box
            | :? LogLevel as logLevel -> classForLogLevel logLevel
            | _ -> ""

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            raise (NotSupportedException())
