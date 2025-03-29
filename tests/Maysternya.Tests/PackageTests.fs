namespace TeaDriven.Maysternya.Tests

open Swensen.Unquote
open Xunit

module GetPackageStringsTests =
    open TeaDriven.Maysternya.Parsing

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
        let actualResult = Package.getPackageStrings input

        // Assert
        expectedResult =! actualResult

module GetPackageContentsTests =
    open TeaDriven.Maysternya.Domain
    open TeaDriven.Maysternya.Parsing

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
        let actualResult = Package.getPackageContents input

        // Assert
        expectedPackageName =! (actualResult[Constants.PackageFileKeys.PackageName] |> Seq.head)
        expectedInformational =! (actualResult[Constants.PackageFileKeys.Informational] |> Seq.head)
        expectedCompatibleVersions =! (actualResult[Constants.PackageFileKeys.CompatibleVersions] |> Seq.toList)

module ParsePackageVersionInfoTests =
    open TeaDriven.Maysternya.Domain
    open TeaDriven.Maysternya.Parsing

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
        let actualResult = Package.parsePackageVersionInfo input

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
        let actualResult = Package.parsePackageVersionInfo input

        // Assert
        expectedResult =! actualResult

module ParseVersionsTests =
    open TeaDriven.Maysternya.Domain
    open TeaDriven.Maysternya.Parsing

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
        let actualResult = Package.parseVersions input

        // Assert
        expectedResult =! actualResult

module DetermineRelevantPackagesTests =
    open TeaDriven.Maysternya.Domain
    open TeaDriven.Maysternya.Parsing

    [<Fact>]
    let ``Simple non informational package is returned for metadata and content`` () =
        // Arrange
        let package =
            {
                PackageName = "package"
                CompatibleVersions = []
                IsInformational = false
            }

        let expectedResult =
            {
                MetadataPackage = Some package
                ContentPackage = Some package
                HighestCompatibleVersion = NotVersionLocked
            }

        // Act
        let actualResult = Package.determineRelevantPackages [package]

        // Assert
        expectedResult =! actualResult

    [<Fact>]
    let ``Informational and content packages are returned for metadata and content`` () =
        // Arrange
        let informationalPackage =
            {
                PackageName = "informational"
                CompatibleVersions = []
                IsInformational = true
            }

        let contentPackage =
            {
                PackageName = "content"
                CompatibleVersions = []
                IsInformational = false
            }

        let expectedResult =
            {
                MetadataPackage = Some informationalPackage
                ContentPackage = Some contentPackage
                HighestCompatibleVersion = NotVersionLocked
            }

        // Act
        let actualResult = Package.determineRelevantPackages [informationalPackage; contentPackage]

        // Assert
        expectedResult =! actualResult

    [<Fact>]
    let ``Packages without compatible versions are returned as relevant`` () =
        // Arrange
        let informationalPackageOld =
            {
                PackageName = "informationalOld"
                CompatibleVersions = [ "1.34.*" ]
                IsInformational = true
            }

        let informationalPackageNew =
            {
                PackageName = "informationalNew"
                CompatibleVersions = []
                IsInformational = true
            }

        let contentPackageOld1 =
            {
                PackageName = "contentOld1"
                CompatibleVersions = [ "1.50.*" ]
                IsInformational = false
            }

        let contentPackageOld2 =
            {
                PackageName = "contentOld2"
                CompatibleVersions = [ "1.49.*" ]
                IsInformational = false
            }

        let contentPackageNew =
            {
                PackageName = "contentNew"
                CompatibleVersions = []
                IsInformational = false
            }

        let expectedResult =
            {
                MetadataPackage = Some informationalPackageNew
                ContentPackage = Some contentPackageNew
                HighestCompatibleVersion = NotVersionLocked
            }

        // Act
        let actualResult =
            Package.determineRelevantPackages
                [
                    informationalPackageOld
                    informationalPackageNew
                    contentPackageOld1
                    contentPackageNew
                    contentPackageOld2
                ]

        // Assert
        expectedResult =! actualResult

    [<Fact>]
    let ``Content package with highest compatible version is returned if none without versions are present`` () =
        // Arrange
        let latestVersion = "1.53.*"

        let contentPackage1 =
            {
                PackageName = "content1"
                CompatibleVersions = [ "1.50.*"; "1.48.*" ]
                IsInformational = false
            }

        let contentPackageNewest =
            {
                PackageName = "contentNewest"
                CompatibleVersions = [ "1.49.*"; latestVersion ]
                IsInformational = false
            }

        let contentPackage2 =
            {
                PackageName = "content2"
                CompatibleVersions = [ "1.51.*"; "1.52.*" ]
                IsInformational = false
            }

        let expectedResult =
            {
                MetadataPackage = Some contentPackageNewest
                ContentPackage = Some contentPackageNewest
                HighestCompatibleVersion = SpecificVersion latestVersion
            }

        // Act
        let actualResult =
            Package.determineRelevantPackages
                [ contentPackage1; contentPackageNewest; contentPackage2 ]

        // Assert
        expectedResult =! actualResult
