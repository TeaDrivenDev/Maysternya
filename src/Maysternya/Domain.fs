namespace TeaDriven.Maysternya.Domain

[<RequireQualifiedAccess>]
module Constants =
    module Application =
        [<Literal>]
        let Vendor = "TeaDrivenDev"

        [<Literal>]
        let Application = "Maysternya"

    module Games =
        [<Literal>]
        let Ets2 = "ETS 2"

        [<Literal>]
        let Ats = "ATS"

    module Paths =
        [<Literal>]
        let DefaultSteamPath = @"C:\Program Files (x86)\Steam"

        [<Literal>]
        let WorkshopContentPath = @"steamapps\workshop\content"

        [<Literal>]
        let GamesPath = @"steamapps\common"

        [<Literal>]
        let Ets2Game = "Euro Truck Simulator 2"

        [<Literal>]
        let AtsGame = "American Truck Simulator"

    module Urls =
        [<Literal>]
        let HashFsExtractor = "https://github.com/sk-zk/Extractor/releases"

        [<Literal>]
        let SteamPrefix = "steam://openurl/"

        [<Literal>]
        let SteamWorkshopItem = "https://steamcommunity.com/sharedfiles/filedetails/?id="

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

        [<Literal>]
        let VersionSii = "version.sii"

        [<Literal>]
        let VersionScs = "version.scs"

        [<Literal>]
        let ExtractorExe = "extractor.exe"

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

type ConfiguredFile =
    {
        Path: string
        FileExists: bool
    } with
    static member Empty =
        {
            Path = ""
            FileExists = false
        }

type LogActivity =
    | UpdateDirectoryPaths
    | UpdateExtractorPath
    | UpdateGameVersions
    | ReadMods
    | RemoveRestriction

type SelectedGame = NoGame | Ets2 | Ats

type CompatibleVersion =
    | SpecificVersion of string
    | NotVersionLocked

type VersionCompatibility = Unrestricted | Allowed | Incompatible | Indeterminate

type DisplayNameSource = Package | DescriptionFile | Unavailable

type Package =
    {
        Name: string
        CompatibleVersions: string list
        Informational: bool
    }

type Mod =
    {
        Id: string
        Path: string
        Name: string
        DisplayNameSource: DisplayNameSource
        Author: string
        Version: string
        Description: string
        HighestCompatibleGameVersion: CompatibleVersion
        RelevantPackageName: string
        AllPackages: Package list
    }

type Settings =
    {
        SteamPath: string
        HashFsExtractorPath: string
        DefaultGame: SelectedGame
    }
