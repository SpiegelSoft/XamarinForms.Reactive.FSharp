namespace XamarinForms.Reactive.Samples.Shared

open XamarinForms.Reactive.FSharp

open System

type Person = { Name: string; DateOfBirth: DateTime }

type ICustomPlatform = inherit IPlatform
