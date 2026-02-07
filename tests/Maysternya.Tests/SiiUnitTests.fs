namespace TeaDriven.Maysternya.Tests

module SiiUnitTests =
    open Xunit

    open TeaDriven.Maysternya.SiiUnit

    [<SiiUnit("package_version_info")>]
    type PackageVersionInfo () =
        [<SiiAttribute("package_name")>]
        member val PackageName: string = null with get, private set

        [<SiiAttribute("compatible_versions")>]
        member val CompatibleVersions: string array = null with get, private set

        [<SiiAttribute("informational")>]
        member val Informational: bool = false with get, private set

    [<Fact>]
    let ``Versions file is parsed correctly`` () =
        // Arrange
        let input = """
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
}"""

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
