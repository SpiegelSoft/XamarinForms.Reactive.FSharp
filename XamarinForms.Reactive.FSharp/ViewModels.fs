namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
open System.Threading
open System

open ReactiveUI

module NullableArguments =
    let toNullable<'a> arg =
        match arg with
        | Some value -> value
        | None -> Unchecked.defaultof<'a>

module Modal =
    type AlertMessage = { Title: string; Message: string; Accept: string }
    let noMessage = { Title = String.Empty; Message = String.Empty; Accept = String.Empty }

open ExpressionConversion
open Modal

[<AbstractClass>]
type ReactiveViewModel() as this =
    inherit ReactiveObject()
    let mutable message = noMessage
    let uiContext = SynchronizationContext.Current
    member __.SyncContext with get() = uiContext
    member __.Message 
        with get() = message 
        and set(value) =
            this.RaiseAndSetIfChanged(&message, value, "Message") |> ignore
            if message <> noMessage then this.RaiseAndSetIfChanged(&message, noMessage, "Message") |> ignore
    member val MessageSent = this.WhenAnyValue(toLinq <@ fun vm -> vm.Message @>).ObserveOn(RxApp.MainThreadScheduler).Where(fun m -> m <> noMessage) with get
    abstract member SubscribeToCommands: unit -> unit
    abstract member UnsubscribeFromCommands: unit -> unit
