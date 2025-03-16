namespace Maysternya.Tests

open Xunit
open Swensen.Unquote

module GetPackageContentsTest =
    open Maysternya

    [<Fact>]
    let grah () =
        // Arrange
        let input = """
package_version_info : .universal
{
	package_name: "v3_4"
}"""

        let expectedResult = "\"v3_4\""

        // Act
        let actualResult = Logic.getPackageContents input

        // Assert
        expectedResult =! (actualResult[Domain.Constants.PackageFileKeys.PackageName] |> Seq.head)

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
