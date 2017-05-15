namespace XamarinForms.Reactive.FSharp.Android

open XamarinForms.Reactive.FSharp

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

type MapSearchBarRenderer() =
    inherit SearchBarRenderer()

module XrfAndroid =
    [<assembly: ExportRendererAttribute (typeof<MapSearchBar>, typeof<MapSearchBarRenderer>)>] do ()
    let Init() = 0 |> ignore

