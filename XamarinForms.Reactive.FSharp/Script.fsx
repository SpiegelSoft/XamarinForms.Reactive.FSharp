// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

open Microsoft.FSharp.Quotations

let rec propertyName = function
| Patterns.Lambda(_, expr) -> propertyName expr
| Patterns.PropertyGet(_, propOrValInfo, _) -> propOrValInfo.Name
| _ -> failwith "Unexpected input"

type MyType =
    {
        Property1: string
    }

let prop1 = <@ fun (t: MyType) -> t.Property1 @>

propertyName prop1

