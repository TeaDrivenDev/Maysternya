namespace TeaDriven.Maysternya

[<RequireQualifiedAccess>]
module Mod =
    open System
    open System.IO
    open System.IO.Compression

    open SimpleExec

    open TeaDriven.Maysternya.Domain
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

    let readVersions modPath =
        let versionsFilePath = Path.Combine(modPath, Constants.FileNames.VersionsSii)
        let versionsFileContent = File.ReadAllText(versionsFilePath)

        let document = SiiDocument(typeof<PackageVersionInfo>)
        document.Load(versionsFileContent, siiParsingOptions) |> ignore

        document.Definitions.Keys
        |> Seq.map document.GetDefinition<PackageVersionInfo>
        |> Seq.toList

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
        let filePath = Path.Combine(packagePath, fileName)
        File.ReadAllText(filePath)

    let private loadFileFromZipArchive archivePath fileName =
        try
            use archive = ZipFile.OpenRead(archivePath)
            let manifestEntry = archive.GetEntry(fileName)
            use manifestStream = manifestEntry.Open()
            use reader = new StreamReader(manifestStream)
            let contents = reader.ReadToEnd()

            Success contents
        with ex -> Failure archivePath

    let private extractFileFromHashFsArchive extractorPath tempPath archivePath fileName =
        let parameters = $"{archivePath} -p=/{fileName} -d={tempPath}"
        Command.Run(extractorPath, parameters, noEcho=true)

        Path.Combine(tempPath, fileName)

    let private loadFileFromHashFsArchive extractorPath archivePath fileName =
        try
            let tempPath = FileSystem.getTempDirectory Constants.Application.Application

            let path = extractFileFromHashFsArchive extractorPath tempPath archivePath fileName
            let contents = File.ReadAllText(path)
            File.Delete(path)

            Success contents
        with ex -> Failure archivePath

    let private readManifest extractorPath modPath packageName =
        let packagePath = Path.Combine(modPath, packageName)
        let manifestFileName = Constants.FileNames.ManifestSii

        let manifestContents =
            if Directory.Exists packagePath
            then
                let loadFile = loadFileFromFileSystem packagePath
                let contents = loadFile manifestFileName

                (contents, loadFile >> Success) |> Success |> Some
            else
                let archiveExtensions = [ ".zip"; ".scs" ]

                archiveExtensions
                |> List.tryPick
                    (fun extension ->
                        let archivePath = packagePath + extension

                        if File.Exists archivePath
                        then Some archivePath
                        else None)
                |> Option.map
                    (fun archivePath ->
                        let loadFile = loadFileFromZipArchive archivePath

                        loadFile manifestFileName
                        |> function
                            | Success contents -> Success (contents, loadFile)
                            | Failure archivePath ->
                                extractorPath
                                |> Option.map
                                    (fun extractorPath ->
                                        let loadFile = loadFileFromHashFsArchive extractorPath archivePath

                                        loadFile manifestFileName
                                        |> function
                                            | Success contents -> Success (contents, loadFile)
                                            | Failure archivePath -> Failure archivePath)
                                |> Option.defaultValue (Failure archivePath))

        manifestContents
        |> Option.map
            (function
                | Success (contents, loadFile) ->
                    let modPackage = readManifestContents contents

                    let description =
                        if not <| String.IsNullOrWhiteSpace modPackage.DescriptionFile
                        then loadFile modPackage.DescriptionFile |> Some
                        else None

                    let descriptionText =
                        description
                        |> Option.map
                            (function
                                | Success description -> description
                                | Failure _ -> "")
                        |> Option.defaultValue ""

                    Accessible (modPackage, descriptionText)

                | Failure archivePath -> Archive archivePath)
        |> Option.defaultValue (NotFound packageName)

    let readMetadata extractorPath modPath packageName =
        let packageData =
            readManifest extractorPath modPath packageName

        match packageData with
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
                    |> Option.defaultValue ("[No display name]", Unavailable)
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
                DisplayName = $"[Metadata in {Path.GetFileName path}]"
                DisplayNameSource = Unavailable
                Author = ""
                ModVersion = ""
                Description = ""
            |}
        | NotFound packageName ->
            {|
                DisplayName = $"[Package {packageName} not found]"
                DisplayNameSource = Unavailable
                Author = ""
                ModVersion = ""
                Description = ""
            |}

    let convertPackage (packageVersionInfo: PackageVersionInfo) =
        {
            Name = packageVersionInfo.PackageName
            CompatibleVersions =
                if isNull packageVersionInfo.CompatibleVersions
                then []
                else Array.toList packageVersionInfo.CompatibleVersions
            Informational = packageVersionInfo.Informational
        }

    let readMod extractorPath modPath =
        let packages = readVersions modPath
        let relevantPackage, compatibleVersion = determineRelevantPackage packages

        let metadata = readMetadata extractorPath modPath relevantPackage.PackageName

        let modId = Path.GetFileName modPath

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

    let readMods extractorPath modsPath =
        modsPath
        |> Directory.GetDirectories
        |> List.ofArray
        |> List.map (readMod extractorPath)

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
