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

    [<Fact>]
    let ``Package string with comments is returned`` () =
        // Arrange
        let input =
            """SiiNunit
{
# ".package_name" does not matter as the dot at the beginning of the file means that this unit is anonymous.
# Please keep this form to not make any conflicts with other mod packages (name collisions).
mod_package : .package_name {

    # Package version can be any string with any length.
    package_version: "2.7"

    # Display name can be any string with any length.
    display_name: "Freightliner Argosy v2.7"

    # Author can be any string with any length.
    author: "Harven, Lucasi, H.Trucker, odd_fellow"

    # Categories is an array of strings.
    category[]: "truck"

    # Icon inside the root directory of the mod.
    icon: "argosy.jpg"

    # Description file inside the root directory of the mod.
    description_file: "descr.txt"
}
}"""

        let expectedResult =
            """mod_package : .package_name {

    # Package version can be any string with any length.
    package_version: "2.7"

    # Display name can be any string with any length.
    display_name: "Freightliner Argosy v2.7"

    # Author can be any string with any length.
    author: "Harven, Lucasi, H.Trucker, odd_fellow"

    # Categories is an array of strings.
    category[]: "truck"

    # Icon inside the root directory of the mod.
    icon: "argosy.jpg"

    # Description file inside the root directory of the mod.
    description_file: "descr.txt"
}"""

        // Act
        let actualResult = Metadata.getModPackageString input

        // Assert
        expectedResult =! actualResult
