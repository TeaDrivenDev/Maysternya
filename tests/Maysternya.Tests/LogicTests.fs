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

    [<Fact>]
    let ``Package with only name returns correct result`` () =
        // Arrange
        let input = """
package_version_info : .universal
{
	package_name: "v3_4"
}"""

        let expectedResult =
            {
                PackageName = "v3_4"
                CompatibleVersions = []
                IsInformational = false
            }

        // Act
        let actualResult = Logic.parsePackageVersionInfo input

        // Assert
        expectedResult =! actualResult
