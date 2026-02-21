open System.IO
open System.Text
open System.Xml.Linq

let resourceFile = Path.Combine(__SOURCE_DIRECTORY__, "Localization/en-US.axaml")
let destinationFile = Path.Combine(__SOURCE_DIRECTORY__, "Localization.fs")

let getLocKeys xaml =
    let systemNamespaceString = "clr-namespace:System;assembly=System.Runtime"
    let stringElementName = "String"

    let xamlNamespaceString = "http://schemas.microsoft.com/winfx/2006/xaml"
    let keyAttributeName = "Key"

    let document = XDocument.Parse(xaml)

    let systemNamespace = XNamespace.Get systemNamespaceString
    let qualifiedStringElementName = systemNamespace + stringElementName

    let xamlNamespace = XNamespace.Get xamlNamespaceString
    let qualifiedKeyAttributeName = xamlNamespace + keyAttributeName

    document.Root.Descendants(qualifiedStringElementName)
    |> Seq.map (fun (element: XElement) -> element.Attribute(qualifiedKeyAttributeName).Value)
    |> Seq.toList

let updateLocalization resources destination =
    let locKeys = resources |> File.ReadAllText |> getLocKeys

    let sb = StringBuilder()
    sb.AppendLine("namespace TeaDriven.Maysternya.Localization") |> ignore

    let locKeyGroups =
        locKeys
        |> List.map (fun (locKey: string) -> (locKey.Split([| '.' |], 2), locKey))
        |> List.groupBy (fst >> Array.head)

    for groupKey, groupItems in locKeyGroups do
        sb
            .AppendLine()
            .AppendLine($"module {groupKey} =")
        |> ignore

        for split, locKey in groupItems do
            let key = split[1]
            let valueName = key.Replace('.', '_')
            sb.AppendLine($@"    let {valueName} = ""{locKey}""") |> ignore

    File.WriteAllText(destination, sb.ToString())

updateLocalization resourceFile destinationFile
