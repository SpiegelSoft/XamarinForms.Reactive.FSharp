namespace XamarinForms.Reactive.Sample.Places.Droid

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

type Resources = XamarinForms.Reactive.Sample.Places.Droid.Resource

[<Activity (Label = "XRF Places", MainLauncher = true, Icon = "@mipmap/icon")>]
    inherit FormsApplicationActivity()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        Forms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let app = new App<ICustomPlatform>(new DroidPlatform() :> ICustomPlatform, new UiContext(this), createDashboardViewModel)
        app.Init()
        base.LoadApplication app
