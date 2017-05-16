namespace XamarinForms.Reactive.FSharp.Android

open XamarinForms.Reactive.FSharp

open Android.Content
open Android.Views

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

type AndroidResource = XamarinForms.Reactive.FSharp.Android.Resource

type MapSearchBarRenderer() =
    inherit SearchBarRenderer()
    override this.OnElementChanged e =
        let inflatorService = this.Context.GetSystemService(Context.LayoutInflaterService) :?> LayoutInflater 
        let containerView = inflatorService.Inflate (AndroidResource.Layout.Main, null, false)
        base.OnElementChanged(e)

module XrfAndroid =
    [<assembly: ExportRendererAttribute (typeof<MapSearchBar>, typeof<MapSearchBarRenderer>)>] do ()
    let Init() = 0 |> ignore
