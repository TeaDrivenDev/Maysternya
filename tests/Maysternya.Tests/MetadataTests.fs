namespace TeaDriven.Maysternya.Tests

open Swensen.Unquote
open Xunit

module GetModPackageStringTests =
    open TeaDriven.Maysternya.Logic

    [<Fact>]
    let ``Simple package string without comments is returned`` () =
        // Arrange
        let input =
            """SiiNunit
{
mod_package : .package_name
{
	package_version: "1.1"
	display_name: "Kenworth W990 edited by Harven"
	author: "Harven, Frank Peru, Yekko Yek, Kriechbaum"
	category[]: "truck"
	icon: "icon.jpg"
	description_file: "mod_description.txt"
	dlc_dependencies[]: "dlc_kenworth_t680"
	dlc_dependencies[]: "dlc_kenworth_w900"
}
}"""

        let expectedResult =
            """mod_package : .package_name
{
	package_version: "1.1"
	display_name: "Kenworth W990 edited by Harven"
	author: "Harven, Frank Peru, Yekko Yek, Kriechbaum"
	category[]: "truck"
	icon: "icon.jpg"
	description_file: "mod_description.txt"
	dlc_dependencies[]: "dlc_kenworth_t680"
	dlc_dependencies[]: "dlc_kenworth_w900"
}"""

        // Act
        let actualResult = Metadata.getModPackageString input

        // Assert
        expectedResult =! actualResult
