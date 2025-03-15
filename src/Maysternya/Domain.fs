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
        let DisplayNameKey = "display_name"

        [<Literal>]
        let PackageVersionInfoKey = "package_version_info"

        [<Literal>]
        let PackageNameKey = "package_name"

        [<Literal>]
        let CompatibleVersionsKey = "compatible_versions[]"

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

type PackageVersionInfo =
    {
        Id: string
        PackageName: string
        CompatibleVersions: string list
        OtherValues: string list
    }

type Mod =
    {
        Id: string
        Name: string
        HighestCompatibleVersion: string
        PackageVersionInfo: PackageVersionInfo
    }
