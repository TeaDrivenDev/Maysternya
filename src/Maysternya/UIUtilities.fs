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

type ValueEqualsParameterConverter() =
    static member Instance = ValueEqualsParameterConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            value = parameter

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            raise (NotSupportedException())

type ValuesEqualMultiConverter() =
    static member Instance = ValuesEqualMultiConverter() :> IMultiValueConverter

    interface IMultiValueConverter with
        member this.Convert(values: IList<obj>, targetType: Type, parameter: obj, culture: CultureInfo) =
            if values.Count > 1
            then values[0] = values[1]
            else false
