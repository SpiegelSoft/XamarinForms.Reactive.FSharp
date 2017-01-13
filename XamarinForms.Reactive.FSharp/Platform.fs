namespace XamarinForms.Reactive.FSharp

open Xamarin.Forms

open Splat

type IPlatform = 
    abstract member GetMainPage: unit -> Page
    abstract member RegisterDependencies: IMutableDependencyResolver -> unit