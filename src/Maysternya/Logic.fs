namespace Maysternya

open Maysternya.Domain

[<RequireQualifiedAccess>]
module Logic =
    let parsePackageVersionInfo (content: string) = Unchecked.defaultof<PackageVersionInfo>
