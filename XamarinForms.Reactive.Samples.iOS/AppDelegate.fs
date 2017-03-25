namespace XamarinForms.Reactive.Samples.iOS

open Xamarin.Forms.Platform.iOS

open System.IO
open System

open ReactiveUI

open UIKit

open Foundation

open XamarinForms.Reactive.Samples.Common
open XamarinForms.Reactive.FSharp

type XamarinForms = Xamarin.Forms.Forms

type IosPlatform() =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface IPlatform with
        member __.GetMainPage() = new ReactiveUI.XamForms.RoutedViewHost() :> Xamarin.Forms.Page
        member __.RegisterDependencies _ _ = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit FormsApplicationDelegate ()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.FinishedLaunching (app, options) =
        XamarinForms.Init()
        this.LoadApplication(new App<IPlatform>(new IosPlatform() :> IPlatform, new UiContext(this), createDashboardViewModel))
        base.FinishedLaunching(app, options)