namespace Maysternya.Tests

open Xunit
open Swensen.Unquote

module GetPackageStringsTests =
    open Maysternya

    [<Fact>]
    let ``File is split correctly into package strings`` () =
        // Arrange
        let input = """
SiiNunit
{
package_version_info : .info.compatible.versions {
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .150
{
	package_name: "150"
	compatible_versions[]: "1.50.*"
}

package_version_info : .151
{
	package_name: "151"
	compatible_versions[]: "1.51.*"
	compatible_versions[]: "1.52.*"
	compatible_versions[]: "1.53.*"
	compatible_versions[]: "1.54.*"
}
}"""

        let expectedResult =
            [
                """package_version_info : .info.compatible.versions {
    package_name: "compatibility_info"
    informational: true
}"""
                """package_version_info : .150
{
	package_name: "150"
	compatible_versions[]: "1.50.*"
}"""

                """package_version_info : .151
{
	package_name: "151"
	compatible_versions[]: "1.51.*"
	compatible_versions[]: "1.52.*"
	compatible_versions[]: "1.53.*"
	compatible_versions[]: "1.54.*"
}"""
            ]

        // Act
        let actualResult = Logic.getPackageStrings input

        // Assert
        expectedResult =! actualResult

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

module ParseVersionsTests =
    open Maysternya
    open Maysternya.Domain

    [<Fact>]
    let ``Package versions are parsed correctly from file`` () =
        // Arrange
        let input = """
SiiNunit
{
package_version_info : .info.compatible.versions
{
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .150
{
	package_name: "150"
	compatible_versions[]: "1.50.*"
}

package_version_info : .151
{
	package_name: "151"
	compatible_versions[]: "1.51.*"
	compatible_versions[]: "1.52.*"
	compatible_versions[]: "1.53.*"
	compatible_versions[]: "1.54.*"
}
}"""

        let expectedResult =
            [
                {
                    PackageName = "compatibility_info"
                    CompatibleVersions = []
                    IsInformational = true
                }

                {
                    PackageName = "150"
                    CompatibleVersions = [ "1.50.*" ]
                    IsInformational = false
                }

                {
                    PackageName = "151"
                    CompatibleVersions = [ "1.54.*"; "1.53.*"; "1.52.*"; "1.51.*" ]
                    IsInformational = false
                }
            ]

        // Act
        let actualResult = Logic.parseVersions input

        // Assert
        expectedResult =! actualResult
