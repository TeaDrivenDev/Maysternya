namespace TeaDriven.Maysternya.Domain

[<RequireQualifiedAccess>]
module Constants =
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

type SelectedGame = NoGame | Ets2 | Ats

type CompatibleVersion =
    | SpecificVersion of string
    | NotVersionLocked

type Mod =
    {
        Id: string
        Path: string
        Name: string
        Version: string
        Description: string
        HighestCompatibleGameVersion: CompatibleVersion
    }
