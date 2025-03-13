namespace Maysternya.Domain

[<RequireQualifiedAccess>]
module Constants =
    [<Literal>]
    let WorkshopContentPath = @"steamapps\workshop\content"

    [<Literal>]
    let Ets2Id = "227300"

    [<Literal>]
    let AtsId = "270880"

    [<Literal>]
    let VersionsSii = "versions.sii"

    [<Literal>]
    let ManifestSii = "manifest.sii"

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
