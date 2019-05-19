namespace XamarinForms.Reactive.FSharp

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open ReactiveUI

module NullableArguments =
    let toNullable<'a> arg =
        match arg with
        | Some value -> value
        | None -> Unchecked.defaultof<'a>

module Modal =
    type AlertMessage = { Title: string; Message: string; Acknowledge: string }
    type Confirmation = { Title: string; Message: string; Accept: string; Decline: string }

open Modal

[<AbstractClass>]
type PageViewModel() =
    inherit ReactiveObject()
    let mutable displayAlertCommand: ReactiveCommand<AlertMessage, Reactive.Unit> option = None
    let mutable confirmCommand: ReactiveCommand<Confirmation, bool> option = None
    let disposables = new CompositeDisposable()
    member val Disposables = disposables
    member __.DisplayAlertMessage(alertMessage) = match displayAlertCommand with | Some command -> command.Execute(alertMessage) | None -> Observable.Never<Reactive.Unit>()
    member __.DisplayConfirmation(confirmation) = match confirmCommand with | Some command -> command.Execute(confirmation) | None -> Observable.Never<bool>()
    member internal __.DisplayAlertCommand with get() = displayAlertCommand and set(value) = displayAlertCommand <- value
    member internal __.ConfirmCommand with get() = confirmCommand and set(value) = confirmCommand <- value
    abstract member InitialiseAsync: unit -> Async<unit>
    default __.InitialiseAsync() = async { () }
    member __.TearDown() = disposables.Clear()
