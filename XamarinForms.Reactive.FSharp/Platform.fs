namespace XamarinForms.Reactive.FSharp

open Xamarin.Forms

open Splat
open System.Reflection

type IPlatform = 
    abstract member GetMainPage: unit -> NavigationPage
    abstract member RegisterDependencies: dependencyResolver:IMutableDependencyResolver -> unit
    abstract member GetLocalFilePath: fileName:string -> string
    abstract member AppDomainAssemblies: Assembly[]
