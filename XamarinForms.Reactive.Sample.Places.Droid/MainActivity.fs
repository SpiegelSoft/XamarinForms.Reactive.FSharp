namespace XamarinForms.Reactive.Sample.Places.Droid

open System.IO
open System

open Android.Content
open Android.Widget
open Android.Views
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

open XamarinForms.Reactive.Sample.Places.Common
open XamarinForms.Reactive.FSharp

open ReactiveUI

type AndroidResource = XamarinForms.Reactive.Sample.Places.Droid.Resource

type MapSearchBarRenderer() =
    inherit SearchBarRenderer()
    override this.OnElementChanged e =
        let inflatorService = this.Context.GetSystemService(Context.LayoutInflaterService) :?> LayoutInflater 
        let containerView = inflatorService.Inflate (AndroidResource.Layout.Main, null, false)
        base.OnElementChanged(e)

[<assembly: ExportRendererAttribute (typeof<MapSearchBar>, typeof<MapSearchBarRenderer>)>] do ()

type DroidPlatform(showToast) =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface ICustomPlatform with
        member __.RegisterDependencies _ = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName
        member __.ShowToastNotification text = showToast text

[<Activity (Label = "XRF Places", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity() =
    inherit FormsApplicationActivity()
    let createDashboardViewModel() = new PlacesViewModel() :> IRoutableViewModel
    let showToast (activity: Context) (text: string) = Toast.MakeText(activity, text, ToastLength.Long).Show()
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        Forms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let app = new App<ICustomPlatform>(showToast this |> DroidPlatform, new UiContext(this), createDashboardViewModel)
        app.Init()
        base.LoadApplication app
