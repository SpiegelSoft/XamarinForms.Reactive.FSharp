namespace XamarinForms.Reactive.FSharp

open Splat
open System

type IPlatform = 
    abstract member RegisterDependencies: dependencyResolver:IMutableDependencyResolver -> unit
    abstract member GetLocalFilePath: fileName:string -> string
    abstract member HandleAppLinkRequest: appLinkRequestUri:Uri -> unit 
