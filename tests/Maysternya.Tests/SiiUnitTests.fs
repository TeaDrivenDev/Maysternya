namespace TeaDriven.Maysternya.Tests

module SiiUnitTests =
    open Xunit

    open TeaDriven.Maysternya
    open TeaDriven.Maysternya.SiiUnit

    type ModPackage = Mod.ModPackage
    type PackageVersionInfo = Mod.PackageVersionInfo

    [<Theory>]
    [<InlineData("""
SiiNunit
{
package_version_info : .info.compatible.versions
{
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .154
{
    package_name: "154"
    compatible_versions[]: "1.54.*"
    compatible_versions[]: "1.55.*"
    compatible_versions[]: "1.56.*"
}

package_version_info : .157
{
    package_name: "157"
    compatible_versions[]: "1.57.*"
}
}""")>]
    [<InlineData("""
SiiNunit
{
package_version_info : .info.compatible.versions {
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .154
{
    package_name: "154"
    compatible_versions[]: "1.54.*"
    compatible_versions[]: "1.55.*"
    compatible_versions[]: "1.56.*"
}

package_version_info : .157
{
    package_name: "157"
    compatible_versions[]: "1.57.*"
}
}""")>]
    let ``Versions file is parsed correctly`` (input: string) =
        // Arrange
        let expectedPackageName1 = "compatibility_info"
        let expectedCompatibleVersions1 = Unchecked.defaultof<string array>
        let expectedInformational1 = true

        let expectedPackageName2 = "154"
        let expectedCompatibleVersions2 = [| "1.54.*"; "1.55.*"; "1.56.*" |]
        let expectedInformational2 = false

        let expectedPackageName3 = "157"
        let expectedCompatibleVersions3 = [| "1.57.*" |]
        let expectedInformational3 = false

        let document = SiiDocument(typeof<PackageVersionInfo>)

        // Act
        document.Load(input, includeNamelessClasses=true) |> ignore
        let keys = document.Definitions.Keys |> Seq.toList

        // Assert
        let definition = document.GetDefinition<PackageVersionInfo>(keys[0])
        Assert.Equal(expectedPackageName1, definition.PackageName)
        Assert.Equal<string array>(expectedCompatibleVersions1, definition.CompatibleVersions)
        Assert.Equal(expectedInformational1, definition.Informational)

        let definition = document.GetDefinition<PackageVersionInfo>(keys[1])
        Assert.Equal(expectedPackageName2, definition.PackageName)
        Assert.Equal<string array>(expectedCompatibleVersions2, definition.CompatibleVersions)
        Assert.Equal(expectedInformational2, definition.Informational)

        let definition = document.GetDefinition<PackageVersionInfo>(keys[2])
        Assert.Equal(expectedPackageName3, definition.PackageName)
        Assert.Equal<string array>(expectedCompatibleVersions3, definition.CompatibleVersions)
        Assert.Equal(expectedInformational3, definition.Informational)

    [<Fact>]
    let ``Manifest file is parsed correctly`` () =
        // Arrange
        let input = """
SiiNunit
{
# ".package_name" does not matter as the dot at the beginning of the file means that this unit is anonymous.
# Please keep this form to not make any conflicts with other mod packages (name collisions).
mod_package : .package_name
{

    # Package version can be any string with any length.
    package_version: "V1.0.3.0"

    # Display name can be any string with any length.
    display_name: "Some Things"

    # Author can be any string with any length.
    author: "Autheur"

    # Categories is an array of strings.
    category[]: "tuning_parts"
    category[]: "truck"

    # Icon inside the root directory of the mod.
    icon: "mod_icon.jpg"

    # Description file inside the root directory of the mod.
    description_file: "mod_description.txt"

    dlc_dependencies[]: "dlc_kenworth_t680"
    dlc_dependencies[]: "dlc_kenworth_w900"

    mp_mod_optional: true
}
}
"""

        let expectedPackageVersion = "V1.0.3.0"
        let expectedDisplayName = "Some Things"
        let expectedAuthor = "Autheur"
        let expectedCategory = [| "tuning_parts"; "truck" |]
        let expectedIcon = "mod_icon.jpg"
        let expectedDescriptionFile = "mod_description.txt"
        let expectedDlcDependencies = [| "dlc_kenworth_t680"; "dlc_kenworth_w900" |]

        let document = SiiDocument(typeof<ModPackage>)

        // Act
        document.Load(input, includeNamelessClasses=true) |> ignore
        let keys = document.Definitions.Keys |> Seq.toList

        // Assert
        let definition = document.GetDefinition<ModPackage>(keys[0])
        Assert.Equal(expectedPackageVersion, definition.PackageVersion)
        Assert.Equal(expectedDisplayName, definition.DisplayName)
        Assert.Equal(expectedAuthor, definition.Author)
        Assert.Equal<string array>(expectedCategory, definition.Category)
        Assert.Equal(expectedIcon, definition.Icon)
        Assert.Equal(expectedDescriptionFile, definition.DescriptionFile)
        Assert.Equal<string array>(expectedDlcDependencies, definition.DlcDependencies)
