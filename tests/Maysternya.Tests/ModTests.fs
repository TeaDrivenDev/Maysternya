namespace TeaDriven.Maysternya.Tests

module ModTests =
    open Xunit

    open TeaDriven.Maysternya
    open TeaDriven.Maysternya.SiiUnit

    type PackageVersionInfo = Mod.PackageVersionInfo

    [<Theory>]
    [<InlineData("""
SiiNunit
{
package_version_info : .universal
{
 package_name: "universal"
}
}""")>]
    [<InlineData("""
SiiNunit
{
package_version_info : .info.compatible.versions
{
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .155
{
	package_name: "155"
	compatible_versions[]: "1.55.*"
	compatible_versions[]: "1.56.*"
}

package_version_info : .universal
{
	package_name: "universal"
}
}""")>]
    let ``Unrestricted, non-informational package is returned`` (versions: string) =
        // Arrange
        let expectedPackageName = "universal"
        let expectedHighestCompatibleVersion = Domain.NotVersionLocked

        let document = SiiDocument(typeof<PackageVersionInfo>)
        document.Load(versions, SiiParsingOptions.IncludeNamelessClasses) |> ignore
        let keys = document.Definitions.Keys |> Seq.toList
        let packages = keys |> List.map document.GetDefinition<PackageVersionInfo>

        // Act
        let relevantPackage, highestCompatibleVersion = Mod.determineRelevantPackage packages

        // Assert
        Assert.Equal(expectedPackageName, relevantPackage.PackageName)
        Assert.Equal(expectedHighestCompatibleVersion, highestCompatibleVersion)

    [<Fact>]
    let ``Non-informational package with highest compatible version is returned`` () =
        // Arrange
        let versions = """
SiiNunit
{
package_version_info : .info.compatible.versions
{
    package_name: "compatibility_info"
    informational: true
}

package_version_info : .155
{
	package_name: "155"
	compatible_versions[]: "1.55.*"
	compatible_versions[]: "1.56.*"
}

package_version_info : .157
{
	package_name: "157"
	compatible_versions[]: "1.57.*"
}

package_version_info : .154
{
	package_name: "154"
	compatible_versions[]: "1.54.*"
}
}"""

        let expectedPackageName = "157"
        let expectedHighestCompatibleVersion = Domain.SpecificVersion "1.57.*"

        let document = SiiDocument(typeof<PackageVersionInfo>)
        document.Load(versions, SiiParsingOptions.IncludeNamelessClasses) |> ignore
        let keys = document.Definitions.Keys |> Seq.toList
        let packages = keys |> List.map document.GetDefinition<PackageVersionInfo>

        // Act
        let relevantPackage, highestCompatibleVersion = Mod.determineRelevantPackage packages

        // Assert
        Assert.Equal(expectedPackageName, relevantPackage.PackageName)
        Assert.Equal(expectedHighestCompatibleVersion, highestCompatibleVersion)

    [<Fact>]
    let ``Game version is parsed correctly from version file`` () =
        // Arrange
        let input = """
SiiNunit
{
fs_pack_set : _nameless.1738.ca80 {
 application: eut2
 version: "1.58.1.2"
 platforms: 1
 platforms[0]: pc
 creation_timestamp: 1770930287
 pack_verifiers: 200
 pack_verifiers[0]: 055AA77D4F5BB09A8D4EA7EDE66AC5DD9952713E943928524445B8B2682574F8
 pack_verifiers[1]: 060142DABDC9ED97F11E2E2B6E33064C80B10F126A7DFA66C7E9B88D0116A954
}

}"""

        let expectedGameVersion = "1.58.1.2"

        // Act
        let actualGameVersion = Mod.parseGameVersion input

        // Assert
        Assert.Equal(expectedGameVersion, actualGameVersion)
