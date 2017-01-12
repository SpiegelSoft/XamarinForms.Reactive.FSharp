namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
open System

open ReactiveUI

open Xamarin.Forms

open Modal
open Themes

type ContentView<'TViewModel when 'TViewModel :> ReactiveViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ContentView()
    let mutable viewModel = Unchecked.defaultof<'TViewModel>
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    member __.ViewModel  with get() = viewModel and set(value: 'TViewModel) = viewModel <- value
    interface IViewFor<'TViewModel> with member __.ViewModel with get() = this.ViewModel and set(value) = this.ViewModel <- value
    interface IViewFor with member __.ViewModel with get() = (this :> IViewFor<'TViewModel>).ViewModel :> obj and set(value: obj) = (this :> IViewFor<'TViewModel>).ViewModel <- (value :?> 'TViewModel)

[<AbstractClass>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> ReactiveViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ContentPage()
    let messageReceived (message: AlertMessage) = this.DisplayAlert(message.Title, message.Message, message.Accept) |> ignore
    let mutable viewModel, listener = Unchecked.defaultof<'TViewModel>, Observable.Never<AlertMessage>().Subscribe(messageReceived)
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    member __.ViewModel with get() = viewModel and set(value: 'TViewModel) = listener.Dispose(); viewModel <- value; listener <- value.MessageSent.Subscribe(messageReceived)
    abstract member CreateContent: unit -> View
    interface IViewFor<'TViewModel> with member __.ViewModel with get() = this.ViewModel and set(value) = this.ViewModel <- value
    interface IViewFor with member __.ViewModel with get() = (this :> IViewFor<'TViewModel>).ViewModel :> obj and set(value: obj) = (this :> IViewFor<'TViewModel>).ViewModel <- (value :?> 'TViewModel)
    interface IDisposable with member __.Dispose() = listener.Dispose()
    override __.OnAppearing() =
        base.OnAppearing()
        match box this.Content with
        | null -> this.Content <- this.CreateContent()
        | _ -> this |> ignore

