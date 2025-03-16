namespace Maysternya

[<RequireQualifiedAccess>]
module Logic =
    open System.Linq
    open System.Text.RegularExpressions

    open Maysternya.Domain

    [<Literal>]
    let whitespaceOrLineBreak = @"\s|\r\n?|\n"
    let characterWhitespaceOrLineBreak = $".|{whitespaceOrLineBreak}"

    let private packageStringRegex =
        Regex(
        $@"{Constants.PackageFileKeys.PackageVersionInfo}\s?:\s?.*({whitespaceOrLineBreak})*\{{({characterWhitespaceOrLineBreak})+?\}}",
        RegexOptions.Compiled)

    let private packageValueRegex =
        Regex(
            @"^\s*(?<key>[\w\[\]]+):\s+(?<value>[^\s]*)\s+$",
            RegexOptions.Multiline ||| RegexOptions.Compiled)

    let private stringValueRegex = Regex("\"(?<value>.*)\"", RegexOptions.Compiled)

    let private getStringValue value =
        stringValueRegex.Match(value).Groups["value"].Value

    let getPackageStrings (fileString: string) =
        packageStringRegex.Matches fileString |> Seq.map _.Value |> Seq.toList

    let getPackageContents (packageString: string) =
        let matches = packageValueRegex.Matches packageString
        let contents =
            (matches |> Seq.map (fun m -> m.Groups["key"].Value, m.Groups["value"].Value))
                .ToLookup(fst, snd)

        contents

    let parsePackageVersionInfo (packageString: string) =
        let contents = getPackageContents packageString

        let packageName =
            contents[Constants.PackageFileKeys.PackageName] |> Seq.head |> getStringValue

        let informational =
            contents[Constants.PackageFileKeys.Informational]
            |> Seq.tryHead
            |> Option.map bool.Parse
            |> Option.defaultValue false

        let compatibleVersions =
            contents[Constants.PackageFileKeys.CompatibleVersions]
            |> Seq.map getStringValue
            |> Seq.sortDescending
            |> Seq.toList

        {
            PackageName = packageName
            CompatibleVersions = compatibleVersions
            IsInformational = informational
        }

    let parseVersions (versionsFileString: string) =
        versionsFileString
        |> getPackageStrings
        |> List.map parsePackageVersionInfo
