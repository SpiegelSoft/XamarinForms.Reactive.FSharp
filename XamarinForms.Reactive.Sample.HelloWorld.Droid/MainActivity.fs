namespace XamarinForms.Reactive.Samples.Droid

open System.IO
open System

open Xamarin.Forms.Platform.Android

open ReactiveUI

open Android.Content.PM
open Android.App

open XamarinForms.Reactive.Sample.HelloWorld.Common
open XamarinForms.Reactive.FSharp

open Xamarin.Forms
open XamarinForms.Reactive.FSharp.Android

type DroidPlatform() =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface ICustomPlatform with
        member __.RegisterDependencies _ = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName

[<assembly: ExportRendererAttribute (typeof<TabbedPage>, typeof<TabbedPageRenderer>)>] do ()

[<Activity (Label = "XRF Hello World", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsApplicationActivity()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        Forms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let app = new App<ICustomPlatform>(new DroidPlatform() :> ICustomPlatform, new UiContext(this), createDashboardViewModel)
        app.Init()
        XrfAndroid.Init()
        base.LoadApplication app
