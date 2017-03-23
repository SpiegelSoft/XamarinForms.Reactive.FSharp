﻿namespace XamarinForms.Reactive.Samples.Droid

open System.IO
open System

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps

open ReactiveUI

open Android.Content.PM
open Android.Content
open Android.Runtime
open Android.Widget
open Android.Views
open Android.App
open Android.OS

open XamarinForms.Reactive.Samples.Shared
open XamarinForms.Reactive.FSharp

open ReactiveUI.XamForms

open Xamarin.Forms

type XamarinForms = Xamarin.Forms.Forms

type DroidPlatform() =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface IPlatform with
        member __.GetMainPage() = new RoutedViewHost() :> Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName

[<Activity (Label = SharedConfiguration.AppName, MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsApplicationActivity()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        XamarinForms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let application = new App<IPlatform>(new DroidPlatform() :> IPlatform, new UiContext(this), new SharedConfiguration.Configuration(), createDashboardViewModel)
        this.LoadApplication application



