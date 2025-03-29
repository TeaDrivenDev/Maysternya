namespace TeaDriven.Maysternya.Tests

open Swensen.Unquote
open Xunit

module GetPackageContentsTests =
    open TeaDriven.Maysternya
    open TeaDriven.Maysternya.Domain

    [<Fact>]
    let ``Package contents are parsed correctly`` () =
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
