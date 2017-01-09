namespace XamarinForms.Reactive.FSharp

open Xamarin.Forms

type IPlatform = 
    abstract member GetMainPage: unit -> Page