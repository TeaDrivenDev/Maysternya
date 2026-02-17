namespace TeaDriven.Maysternya

open Avalonia

[<AutoOpen>]
module Prelude =
    let asFst second first = first, second
    let asSnd first second = first, second

    let getResource<'T> key =
        match Application.Current.TryGetResource(key, null) with
        | true, resource -> resource :?> 'T
        | false, _ -> Unchecked.defaultof<'T>

    let locString key = getResource<string> key
