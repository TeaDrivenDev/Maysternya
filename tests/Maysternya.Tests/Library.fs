namespace Maysternya.Tests

open Xunit
open Swensen.Unquote

module Say =
    [<Fact>]
    let xTest () =
        test <@ 1 = 1 @>
