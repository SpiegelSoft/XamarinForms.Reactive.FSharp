﻿namespace XamarinForms.Reactive.Sample.HelloWorld.Common

open XamarinForms.Reactive.FSharp

open System

type Person = { Name: string; DateOfBirth: DateTime }

type ICustomPlatform = inherit IPlatform
