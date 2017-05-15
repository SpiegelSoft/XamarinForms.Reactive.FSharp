namespace XamarinForms.Reactive.Sample.Places.Common

open XamarinForms.Reactive.FSharp

type ICustomPlatform = 
    inherit IPlatform
    abstract member ShowToastNotification: text:string -> unit


