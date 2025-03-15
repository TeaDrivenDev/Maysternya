namespace Maysternya.Tests

open Xunit
open Swensen.Unquote

module ParsePackageVersionInfoTests =
    open Maysternya
    open Maysternya.Domain

    [<Fact>]
    let ``Returns default`` () =
        // Arrange
        let input = System.Guid.NewGuid().ToString()

        // Act
        let actualResult = Logic.parsePackageVersionInfo input

        // Assert
        test <@ Unchecked.defaultof<PackageVersionInfo> = actualResult @>
