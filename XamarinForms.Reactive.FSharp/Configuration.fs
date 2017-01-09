namespace XamarinForms.Reactive.FSharp

type IConfiguration =
    abstract member MobileServiceUri: string option
    abstract member AppName: string