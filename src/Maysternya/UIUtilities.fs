namespace TeaDriven.Maysternya.UIUtilities

open System
open System.Collections.Generic
open System.Globalization

open Avalonia.Data.Converters

type BytesToMegabytesConverter() =
    static member Instance = BytesToMegabytesConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            match value with
            | :? int64 as size -> (System.Convert.ToDouble size) / (1024. * 1024.)
            | _ -> 0.
            :> obj

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            raise (NotSupportedException())

type XamlValueCollection() = inherit List<obj>()

type XamlWrapper() as this =
    member val Item: obj = null with get, set

    override _.GetHashCode() = hash this.Item

    override _.Equals(other) =
        match other with
        | :? XamlWrapper as otherWrapper -> this.Item.Equals(otherWrapper.Item)
        | _ -> this.Item.Equals(other)

type ValueEqualsParameterConverter() =
    static member Instance = ValueEqualsParameterConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            match parameter with
            | :? XamlValueCollection as collection ->
                collection
                |> Seq.exists _.Equals(value)
                |> box
            | _ -> value = parameter

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            raise (NotSupportedException())

type ValuesEqualMultiConverter() =
    static member Instance = ValuesEqualMultiConverter() :> IMultiValueConverter

    interface IMultiValueConverter with
        member this.Convert(values: IList<obj>, targetType: Type, parameter: obj, culture: CultureInfo) =
            if values.Count > 1
            then values[0] = values[1]
            else false

type EmptyStringToBoolConverter(inverted: bool) =
    static member IsEmpty = EmptyStringToBoolConverter(inverted=false) :> IValueConverter
    static member IsNotEmpty = EmptyStringToBoolConverter(inverted=true) :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            let inverted = System.Convert.ToBoolean(parameter)

            match value with
            | :? string as s -> String.IsNullOrWhiteSpace(s) = inverted
            | _ -> not inverted

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: CultureInfo) =
            raise (NotSupportedException())
