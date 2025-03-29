namespace TeaDriven.Maysternya.Tests

open Swensen.Unquote
open Xunit

module PackageValueRegexTests =
    open TeaDriven.Maysternya

    [<Theory>]
    [<InlineData("""	package_version: "1.0" """, true)>]
    [<InlineData("""        display_name: "Advanced SCS Traffic" """, true)>]
    [<InlineData("""        dlc_dependencies[]: "dlc_north" """, true)>]
    [<InlineData("""        # compatible_versions[]: "1.37" # Mod is compatible with 1.19.X..""", false)>]
    let ``Package value lines are matched correctly`` (input: string, expectedIsMatch: bool) =
        // Arrange

        // Act
        let ``match`` = Parsing.packageValueRegex.Match input
        let actualIsMatch = ``match``.Success

        // Assert
        expectedIsMatch =! actualIsMatch

module GetPackageContentsTests =
    open TeaDriven.Maysternya
    open TeaDriven.Maysternya.Domain

    [<Fact>]
    let ``Package contents without comments are parsed correctly`` () =
        // Arrange
        let input = """
package_version_info : .universal
{
	package_name: "v3_4"
    informational: false
	compatible_versions[]: "1.48.*"
	compatible_versions[]: "1.49.*"
}"""

        let expectedPackageName = "\"v3_4\""
        let expectedInformational = "false"
        let expectedCompatibleVersions = ["\"1.48.*\""; "\"1.49.*\""]

        // Act
        let actualResult = Parsing.getPackageContents input

        // Assert
        expectedPackageName =! (actualResult[Constants.PackageFileKeys.PackageName] |> Seq.head)
        expectedInformational =! (actualResult[Constants.PackageFileKeys.Informational] |> Seq.head)
        expectedCompatibleVersions =! (actualResult[Constants.PackageFileKeys.CompatibleVersions] |> Seq.toList)

    [<Fact>]
    let ``Package contents with comments are parsed correctly`` () =
        // Arrange
        let input =
            """mod_package : .package_name
{
        # Package version can be any string with any length.
        package_version: "1.0"

        # Display name can be any string with any length.
        display_name: "Advanced SCS Traffic"

        # compatible_versions[]: "1.37" # Mod is compatible with 1.19.X..

        dlc_dependencies[]: "dlc_north"
        dlc_dependencies[]: "dlc_east"

        mp_mod_optional: true
}"""

        let expectedVersion = "1.0"
        let expectedDisplayName = "Advanced SCS Traffic"
        let expectedDlcDependencies = [ "dlc_north"; "dlc_east" ]
        let expectedMpModOptional = "true"

        // Act
        let actualResult = Parsing.getPackageContents input

        // Assert
        expectedVersion =! (actualResult[Constants.ManifestFileKeys.PackageVersion] |> Seq.head |> Parsing.getStringValue)
        expectedDisplayName =! (actualResult[Constants.ManifestFileKeys.DisplayName] |> Seq.head |> Parsing.getStringValue)
        expectedDlcDependencies
            =! (actualResult[Constants.ManifestFileKeys.DlcDependencies] |> Seq.map Parsing.getStringValue |> Seq.toList)
        expectedMpModOptional =! (actualResult[Constants.ManifestFileKeys.MpModOptional] |> Seq.head)
        test <@ Seq.isEmpty actualResult[Constants.PackageFileKeys.CompatibleVersions] @>
