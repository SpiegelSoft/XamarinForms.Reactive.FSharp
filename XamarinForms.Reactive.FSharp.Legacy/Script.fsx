// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.
open Microsoft.FSharp.Quotations

#r "bin/Debug/Xamarin.Forms.Core.dll"

open Xamarin.Forms

Color.FromHex("#00529B")

let rec propertyName = function
| Patterns.Lambda(_, expr) -> propertyName expr
| Patterns.PropertyGet(_, propOrValInfo, _) -> propOrValInfo.Name
| _ -> failwith "Unexpected input"

let rec setProperty instance value = function
| Patterns.Lambda(_, expr) -> setProperty instance value expr
| Patterns.PropertyGet(_, propertyInfo, _) -> propertyInfo.SetValue(instance, value)
| _ -> failwith "Property expression expected"

type MyType =
    {
        mutable Property1: string
    }

let prop1 = <@ fun (t: MyType) -> t.Property1 @>

propertyName prop1

let instance = { Property1 = "Hello" }

let inline (.*) (x: int) = 2 * x

printf "%i" (.* 2)

setProperty instance "World" prop1

