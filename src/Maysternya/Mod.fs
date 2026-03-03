namespace TeaDriven.Maysternya

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mod =
    open System
    open System.IO
    open System.IO.Compression

    open SimpleExec

    open TeaDriven.Maysternya.Domain
    open TeaDriven.Maysternya.Localization
    open TeaDriven.Maysternya.Prelude
    open TeaDriven.Maysternya.SiiUnit

    [<SiiUnit("package_version_info")>]
    type PackageVersionInfo () =
        [<SiiAttribute("package_name")>]
        member val PackageName: string = null with get, private set

        [<SiiAttribute("compatible_versions")>]
        member val CompatibleVersions: string array = null with get, private set

        [<SiiAttribute("informational")>]
        member val Informational: bool = false with get, private set

    [<SiiUnit("mod_package")>]
    type ModPackage () =
        [<SiiAttribute("package_version")>]
        member val PackageVersion: string = null with get, set

        [<SiiAttribute("display_name")>]
        member val DisplayName: string = null with get, set

        [<SiiAttribute("author")>]
        member val Author: string = null with get, set

        [<SiiAttribute("category")>]
        member val Category: string array = null with get, set

        [<SiiAttribute("icon")>]
        member val Icon: string = null with get, set

        [<SiiAttribute("description_file")>]
        member val DescriptionFile: string = null with get, set

        [<SiiAttribute("dlc_dependencies")>]
        member val DlcDependencies: string array = null with get, set

        [<SiiAttribute("mp_mod_optional")>]
        member val MpModOptional: bool = false with get, private set

    type private FileOperation<'T> =
        | Success of 'T
        | Failure of string

    type private PackagePresence =
        | Accessible of ModPackage * string
        | Archive of string
        | NotFound of string

    let private siiParsingOptions =
        SiiParsingOptions.IncludeNamelessClasses ||| SiiParsingOptions.AllowExtraContentAfterEnd

    let readVersions log modPath =
        try
            let versionsFilePath = Path.Combine(modPath, Constants.FileNames.VersionsSii)
            let versionsFileContent = File.ReadAllText(versionsFilePath)

            let document = SiiDocument(typeof<PackageVersionInfo>)
            document.Load(versionsFileContent, siiParsingOptions) |> ignore

            document.Definitions.Keys
            |> Seq.map document.GetDefinition<PackageVersionInfo>
            |> Seq.toList
            |> Some
        with ex ->
            log Error ReadMods (String.Format(locString Log.ErrorReadingVersionsFrom_Format, modPath, ex.Message))
            None

    let determineRelevantPackage (allPackages: PackageVersionInfo list) =
        let contentPackages = allPackages |> List.filter (_.Informational >> not)

        contentPackages
        |> List.tryFind (_.CompatibleVersions >> isNull)
        |> function
            | Some package -> package, NotVersionLocked
            | None ->
                let package, version =
                    contentPackages
                    |> List.map (fun package -> package, Array.max package.CompatibleVersions)
                    |> List.maxBy snd

                package, SpecificVersion version

    let readManifestContents (manifestContents: string) =
        let document = SiiDocument(typeof<ModPackage>)
        document.Load(manifestContents.Trim(), siiParsingOptions) |> ignore

        document.GetDefinition<ModPackage>(Seq.head document.Definitions.Keys)

    let private loadFileFromFileSystem packagePath fileName =
        async {
            let filePath = Path.Combine(packagePath, fileName)
            return! File.ReadAllTextAsync(filePath) |> Async.AwaitTask
        }

    let private loadFileFromZipArchive archivePath fileName =
        async {
            try
                use! archive = ZipFile.OpenReadAsync(archivePath) |> Async.AwaitTask
                let manifestEntry = archive.GetEntry(fileName)
                use! manifestStream = manifestEntry.OpenAsync() |> Async.AwaitTask
                use reader = new StreamReader(manifestStream)
                let! contents = reader.ReadToEndAsync() |> Async.AwaitTask

                return Success contents
            with ex -> return Failure archivePath
        }

    let private extractFileFromHashFsArchive extractorPath tempPath archivePath fileName =
        async {
            let parameters = $"\"{archivePath}\" -p=/{fileName} -d={tempPath} -q"
            do! Command.RunAsync(extractorPath, parameters, noEcho=true, createNoWindow=true) |> Async.AwaitTask

            return Path.Combine(tempPath, fileName)
        }

    let private loadFileFromHashFsArchive extractorPath archivePath fileName =
        async {
            try
                let tempPath = FileSystem.getTempDirectory Constants.Application.Application
                let! path = extractFileFromHashFsArchive extractorPath tempPath archivePath fileName
                let contents = File.ReadAllText(path)

                return Success contents
            with ex -> return Failure archivePath
        }

    let private readManifest log extractorPath modPath packageName =
        async {
            try
                let packagePath = Path.Combine(modPath, packageName)
                let manifestFileName = Constants.FileNames.ManifestSii

                let! manifestContents =
                    async {
                        if Directory.Exists packagePath
                        then
                            let loadFile = loadFileFromFileSystem packagePath
                            let! contents = loadFile manifestFileName

                            let makeSuccess loadFile fileName =
                                async {
                                    let! file = loadFile fileName
                                    return Success file
                                }

                            return (contents, makeSuccess loadFile) |> Success |> Some
                        else
                            let archiveExtensions = [ ".zip"; ".scs" ]

                            let archivePath =
                                archiveExtensions
                                |> List.tryPick
                                    (fun extension ->
                                        let archivePath = packagePath + extension

                                        if File.Exists archivePath
                                        then Some archivePath
                                        else None)

                            match archivePath with
                            | Some archivePath ->
                                let loadFile = loadFileFromZipArchive archivePath
                                let! manifestFile = loadFile manifestFileName

                                match manifestFile with
                                | Success contents -> return Success (contents, loadFile) |> Some
                                | Failure archivePath ->
                                    match extractorPath with
                                    | Some extractorPath ->
                                        let loadFile = loadFileFromHashFsArchive extractorPath archivePath
                                        let! contents = loadFile manifestFileName

                                        match contents with
                                        | Success contents -> return Success (contents, loadFile) |> Some
                                        | Failure archivePath -> return Failure archivePath |> Some
                                    | None -> return Failure archivePath |> Some
                            | None -> return None
                    }

                match manifestContents with
                | Some manifestContents ->
                    match manifestContents with
                    | Success (contents, loadFile) ->
                        let modPackage = readManifestContents contents

                        let! description =
                            async {
                                if not <| String.IsNullOrWhiteSpace modPackage.DescriptionFile
                                then
                                    let! descriptionFile = loadFile modPackage.DescriptionFile
                                    return Some descriptionFile
                                else return None
                            }

                        let descriptionText =
                            description
                            |> Option.map
                                (function
                                    | Success description -> description
                                    | Failure _ -> "")
                            |> Option.defaultValue ""

                        return Accessible (modPackage, descriptionText) |> Some
                    | Failure archivePath -> return Archive archivePath |> Some
                | None -> return NotFound packageName |> Some
            with ex ->
                log Error ReadMods (String.Format(locString Log.ErrorReadingManifestFrom_Format, modPath, ex.Message))
                return None
        }

    let readMetadata log extractorPath modPath packageName =
        async {
            let! packageData = readManifest log extractorPath modPath packageName

            return
                packageData
                |> Option.map
                    (function
                    | Accessible (modPackage, description) ->
                        let displayName, source =
                            if String.IsNullOrWhiteSpace modPackage.DisplayName
                            then
                                let firstLineOfDescription =
                                    if String.IsNullOrWhiteSpace description
                                    then None
                                    else
                                        use reader = new StringReader(description)
                                        reader.ReadLine() |> Some

                                firstLineOfDescription
                                |> Option.map (asFst DescriptionFile)
                                |> Option.defaultValue (locString Loc.NoDisplayName, Unavailable)
                            else modPackage.DisplayName, Package

                        {|
                            DisplayName = displayName
                            DisplayNameSource = source
                            Author = modPackage.Author
                            ModVersion = modPackage.PackageVersion
                            Description = description
                        |}
                    | Archive path ->
                        {|
                            DisplayName = String.Format(locString Loc.MetadataIn_Format, Path.GetFileName path)
                            DisplayNameSource = Unavailable
                            Author = ""
                            ModVersion = ""
                            Description = ""
                        |}
                    | NotFound packageName ->
                        {|
                            DisplayName = String.Format(locString Loc.PackageNotFound_Format, packageName)
                            DisplayNameSource = Unavailable
                            Author = ""
                            ModVersion = ""
                            Description = ""
                        |})
        }

    let convertPackage (packageVersionInfo: PackageVersionInfo) =
        {
            Name = packageVersionInfo.PackageName
            CompatibleVersions =
                if isNull packageVersionInfo.CompatibleVersions
                then []
                else Array.toList packageVersionInfo.CompatibleVersions
            Informational = packageVersionInfo.Informational
        }

    let readMod log extractorPath (modPath: string) =
        log Diagnostic ReadMods (String.Format(locString Log.ReadingMod_Format, modPath))

        async {
            try
                let packages = readVersions log modPath

                match packages with
                | Some packages ->
                    let relevantPackage, compatibleVersion = determineRelevantPackage packages
                    let! metadata = readMetadata log extractorPath modPath relevantPackage.PackageName

                    match metadata with
                    | Some metadata ->
                        let modId = Path.GetFileName modPath

                        return
                            {
                                Id = modId
                                Path = modPath
                                Name = metadata.DisplayName
                                DisplayNameSource = metadata.DisplayNameSource
                                Author = metadata.Author
                                Version = metadata.ModVersion
                                Description = metadata.Description
                                HighestCompatibleGameVersion = compatibleVersion
                                RelevantPackageName = relevantPackage.PackageName
                                AllPackages = packages |> List.map convertPackage
                            }
                            |> Some
                    | None -> return None
                | None -> return None
            with ex ->
                log Error ReadMods (String.Format(locString Log.ErrorReadingMod_Format, modPath, ex.Message))

                return None
        }

    let readMods log extractorPath modsPath =
        modsPath
        |> Directory.GetDirectories
        |> List.ofArray
        |> List.map (readMod log extractorPath)
        |> Async.Sequential

    let writeVersions (packages: Package list) =
        let stringBuilder = System.Text.StringBuilder()

        stringBuilder
            .AppendLine("SiiNunit")
            .AppendLine("{")
        |> ignore

        for package in packages do
            stringBuilder
                .AppendLine($"package_version_info : .{package.Name.Replace('_', '.')}")
                .AppendLine("{")
                .AppendLine($"\tpackage_name: \"{package.Name}\"")
            |> ignore

            for version in package.CompatibleVersions do
                stringBuilder.AppendLine($"\tcompatible_versions[]: \"{version}\"") |> ignore

            if package.Informational
            then stringBuilder.AppendLine("\tinformational: true") |> ignore

            stringBuilder
                .AppendLine("}")
                .AppendLine()
            |> ignore

        stringBuilder.AppendLine("}") |> ignore

        stringBuilder.ToString()

    let removeVersionRestriction modPath relevantPackage (allPackages: Package list) =
        let packagesToWrite =
            allPackages
            |> List.map
                (fun package ->
                    if package.Name = relevantPackage
                    then { package with CompatibleVersions = [] }
                    else package)

        let versionsFileContent = writeVersions packagesToWrite

        let versionsFilePath = Path.Combine(modPath, Constants.FileNames.VersionsSii)
        File.WriteAllText(versionsFilePath, versionsFileContent)

    let parseGameVersion (versionFileContents: string) =
        let gameVersionRegex =
            System.Text.RegularExpressions.Regex(
                "version: \"(?<version>.*)\"",
                System.Text.RegularExpressions.RegexOptions.Compiled)

        let ``match`` = gameVersionRegex.Match(versionFileContents)
        ``match``.Groups["version"].Value

    let readGameVersion extractorPath steamPath gamePath =
        async {
            let fullGamePath = Path.Combine(steamPath, Constants.Paths.GamesPath, gamePath)

            if Directory.Exists fullGamePath
            then
                let versionFilePath = Path.Combine(fullGamePath, Constants.FileNames.VersionScs)
                let! versionFile = loadFileFromHashFsArchive extractorPath versionFilePath Constants.FileNames.VersionSii

                match versionFile with
                | Success versionFileContents ->
                    let gameVersion = parseGameVersion versionFileContents |> Version
                    return Some gameVersion
                | Failure _ -> return None
            else return None
        }

    let determineVersionCompatibility compatibleVersion (gameVersion: Version option): VersionCompatibility =
        match compatibleVersion, gameVersion with
        | NotVersionLocked, _ -> Unrestricted
        | SpecificVersion _, None -> Indeterminate
        | SpecificVersion compatibleVersion, Some gameVersion ->
            let compatible = compatibleVersion.Replace(".*", ".")
            let gameVersionString = gameVersion.ToString()

            if gameVersionString.ToString().StartsWith(compatible[..gameVersionString.Length - 1])
            then Allowed
            else Incompatible
