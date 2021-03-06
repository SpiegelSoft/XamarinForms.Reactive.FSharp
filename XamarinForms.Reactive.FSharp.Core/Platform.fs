﻿namespace XamarinForms.Reactive.FSharp

open Xamarin.Forms

open Splat

type IPlatform = 
    abstract member RegisterDependencies: dependencyResolver:IMutableDependencyResolver -> unit
    abstract member GetLocalFilePath: fileName:string -> string
