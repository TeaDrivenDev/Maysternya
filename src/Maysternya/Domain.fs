namespace Maysternya.Domain

[<RequireQualifiedAccess>]
module Constants =
    module Games =
        [<Literal>]
        let Ets2 = "ETS 2"

        [<Literal>]
        let Ats = "ATS"

    module Paths =
        [<Literal>]
        let WorkshopContentPath = @"steamapps\workshop\content"

    module SteamIds =
        [<Literal>]
        let Ets2 = "227300"

        [<Literal>]
        let Ats = "270880"

    module FileNames =
        [<Literal>]
        let VersionsSii = "versions.sii"

        [<Literal>]
        let ManifestSii = "manifest.sii"

    module PackageFileKeys =
        [<Literal>]
        let SiiNunit = "SiiNunit"

        [<Literal>]
        let DisplayName = "display_name"

        [<Literal>]
        let PackageVersionInfo = "package_version_info"

        [<Literal>]
        let PackageName = "package_name"

        [<Literal>]
        let CompatibleVersions = "compatible_versions[]"

        [<Literal>]
        let Informational = "informational"

type ConfiguredDirectory =
    {
        Path: string
        PathExists: bool
    } with
    static member Empty =
        {
            Path = ""
            PathExists = false
        }

type CompatibleVersion =
    | SpecificVersion of string
    | NotVersionLocked

type PackageVersionInfo =
    {
        Id: string
        PackageName: string
        CompatibleVersions: string list
        IsInformational: bool
        OtherValues: string list
    }

type Mod =
    {
        Id: string
        Path: string
        Name: string
        HighestCompatibleVersion: CompatibleVersion
    }
