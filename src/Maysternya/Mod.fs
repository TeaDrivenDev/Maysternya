namespace TeaDriven.Maysternya

[<RequireQualifiedAccess>]
module Mod =
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
