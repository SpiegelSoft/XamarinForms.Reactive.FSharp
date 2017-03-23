namespace XamarinForms.Reactive.Samples.Shared

open XamarinForms.Reactive.FSharp

module SharedConfiguration =
    let [<Literal>] AppName = "Sample App";
    type Configuration() =
        interface IConfiguration with
            member __.MobileServiceUri = None
            member __.AppName = AppName
