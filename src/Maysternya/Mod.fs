namespace TeaDriven.Maysternya

[<RequireQualifiedAccess>]
module Mod =
    open System.IO
    open System.IO.Compression

    open TeaDriven.Maysternya.Domain
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

    type FileContents =
        | Success of string
        | Failure of string

    type PackagePresence =
        | Accessible of ModPackage
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

    let loadManifestFromArchive archivePath =
        try
            use archive = ZipFile.OpenRead(archivePath)
            let manifestEntry = archive.GetEntry(Constants.FileNames.ManifestSii)
            use manifestStream = manifestEntry.Open()
            use reader = new StreamReader(manifestStream)
            reader.ReadToEnd() |> Success
        with ex -> Failure archivePath

    let readManifest modPath packageName =
        let packagePath = Path.Combine(modPath, packageName)

        let manifestContents =
            if Directory.Exists packagePath
            then
                let manifestPath = Path.Combine(packagePath, Constants.FileNames.ManifestSii)
                File.ReadAllText(manifestPath) |> Success |> Some
            else
                let archiveExtensions = [ ".zip"; ".scs" ]

                archiveExtensions
                |> List.tryPick
                       (fun extension ->
                            let archiveFileName = packagePath + extension

                            if File.Exists archiveFileName
                            then Some archiveFileName
                            else None)
                |> Option.map loadManifestFromArchive

        manifestContents
        |> Option.map
            (function
                | Success contents -> readManifestContents contents |> Accessible
                | Failure archivePath -> Archive archivePath)
        |> Option.defaultValue (NotFound packageName)

    let readMetadata modPath packageName =
        let packageData =
            readManifest modPath packageName

        match packageData with
        | Accessible modPackage ->
            {|
                DisplayName =
                    modPackage.DisplayName
                    |> Option.ofObj
                    |> Option.defaultValue "[No display name]"
                Author = modPackage.Author
                ModVersion = modPackage.PackageVersion
            |}
        | Archive path ->
            {|
                DisplayName = $"[Metadata in {Path.GetFileName path}]"
                Author = ""
                ModVersion = ""
            |}
        | NotFound packageName ->
            {|
                DisplayName = $"[Package {packageName} not found]"
                Author = ""
                ModVersion = ""
            |}

    let readMod (modPath: string) =
        let relevantPackage, compatibleVersion =
            modPath |> readVersions |> determineRelevantPackage

        let metadata = readMetadata modPath relevantPackage.PackageName

        let modId = Path.GetFileName modPath

        {
            Id = modId
            Path = modPath
            Name = metadata.DisplayName
            Author = metadata.Author
            Version = metadata.ModVersion
            Description = "xd"
            HighestCompatibleGameVersion = compatibleVersion
        }

    let readMods modsPath =
        modsPath
        |> Directory.GetDirectories
        |> List.ofArray
        |> List.map readMod
