namespace Maysternya.Tests

open Xunit
open Swensen.Unquote

module GetPackageContentsTest =
    open Maysternya
    open Maysternya.Domain

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
        let actualResult = Logic.getPackageContents input

        // Assert
        expectedPackageName =! (actualResult[Constants.PackageFileKeys.PackageName] |> Seq.head)
        expectedInformational =! (actualResult[Constants.PackageFileKeys.Informational] |> Seq.head)
        expectedCompatibleVersions =! (actualResult[Constants.PackageFileKeys.CompatibleVersions] |> Seq.toList)

module ParsePackageVersionInfoTests =
    open Maysternya
    open Maysternya.Domain

    [<Theory>]
    [<InlineData("""
package_version_info : .universal
{
	package_name: "v3_4"
}""", false)>]
    [<InlineData("""
package_version_info : .universal
{
	package_name: "v3_4"
    informational: false
}""", false)>]
    [<InlineData("""
package_version_info : .universal
{
	package_name: "v3_4"
    informational: true
}""", true)>]
    let ``Package without versions returns correct result`` (input: string, expectedInformational: bool) =
        // Arrange
        let expectedResult =
            {
                PackageName = "v3_4"
                CompatibleVersions = []
                IsInformational = expectedInformational
            }

        // Act
        let actualResult = Logic.parsePackageVersionInfo input

        // Assert
        expectedResult =! actualResult

    [<Theory>]
    [<InlineData("""
package_version_info : .universal
{
	package_name: "v3_4"
	compatible_versions[]: "1.48.*"
	compatible_versions[]: "1.49.*"
}""", false)>]
    let ``Package with versions returns correct result`` (input: string, expectedInformational: bool) =
        // Arrange
        let expectedResult =
            {
                PackageName = "v3_4"
                CompatibleVersions = ["1.49.*"; "1.48.*"]
                IsInformational = expectedInformational
            }

        // Act
        let actualResult = Logic.parsePackageVersionInfo input

        // Assert
        expectedResult =! actualResult
