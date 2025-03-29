namespace TeaDriven.Maysternya

module Logic =
    open System.Text.RegularExpressions

    open TeaDriven.Maysternya.Domain

    [<Literal>]
    let private whitespaceOrLineBreak = @"\s|\r\n?|\n"
    let private characterWhitespaceOrLineBreak = $".|{whitespaceOrLineBreak}"

    let private blockStringRegex =
        $@"\s?:\s?.*({whitespaceOrLineBreak})*\{{({characterWhitespaceOrLineBreak})+?\}}"

    let private packageValueRegex =
        Regex(
            @"^\s*(?<key>[\w\[\]]+):\s+(?<value>[^\s]*)\s+$",
            RegexOptions.Multiline ||| RegexOptions.Compiled)

    let private stringValueRegex = Regex("\"(?<value>.*)\"", RegexOptions.Compiled)

    let private getStringValue value =
        stringValueRegex.Match(value).Groups["value"].Value

    [<RequireQualifiedAccess>]
    module Package =
        open System.Linq

        let private packageStringRegex =
            Regex(
                Constants.PackageFileKeys.PackageVersionInfo + blockStringRegex,
                RegexOptions.Compiled)

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

        let determineRelevantPackages allPackages =
            let noSpecificCompatibleVersions = _.CompatibleVersions >> List.isEmpty

            let informationalPackages, contentPackages =
                allPackages |> List.partition _.IsInformational

            let contentPackagesWithoutCompatibleVersions, contentPackagesWithCompatibleVersions =
                contentPackages |> List.partition noSpecificCompatibleVersions

            let relevantContentPackage, highestCompatibleVersion =
                contentPackagesWithoutCompatibleVersions
                |> List.tryHead
                |> Option.map (fun package -> Some package, NotVersionLocked)
                |> Option.defaultWith
                    (fun () ->
                        contentPackagesWithCompatibleVersions
                        |> function
                            | [] -> None, NotVersionLocked
                            | packages ->
                                let package, latestVersion =
                                    packages
                                    |> List.map
                                        (fun package -> package, (package.CompatibleVersions |> List.max))
                                    |> List.maxBy snd

                                Some package, SpecificVersion latestVersion)

            let relevantInformationalPackage =
                informationalPackages
                |> List.tryFind noSpecificCompatibleVersions
                |> Option.orElse relevantContentPackage

            {
                MetadataPackage = relevantInformationalPackage
                ContentPackage = relevantContentPackage
                HighestCompatibleVersion = highestCompatibleVersion
            }
