namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
open System

open ReactiveUI

open Xamarin.Forms

open Modal

open Themes
open System.Collections.Generic

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
    let maps = new Dictionary<Guid, GeographicMap>()
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    let descendantAdded = 
        let processElement _ (eventArgs:ElementEventArgs) =
            match eventArgs.Element with
            | :? GeographicMap as map -> maps.Add (map.Id, map)
            | _ -> eventArgs |> ignore
        new EventHandler<ElementEventArgs> (processElement)
    let descendantRemoved = 
        let processElement _ (eventArgs:ElementEventArgs) =
            match eventArgs.Element with
            | :? GeographicMap as map -> map.Close(); maps.Remove map.Id |> ignore
            | _ -> eventArgs |> ignore
        new EventHandler<ElementEventArgs> (processElement)
    member __.ViewModel with get() = viewModel and set(value: 'TViewModel) = listener.Dispose(); viewModel <- value; listener <- value.MessageSent.Subscribe(messageReceived)
    abstract member CreateContent: unit -> View
    interface IViewFor<'TViewModel> with member __.ViewModel with get() = this.ViewModel and set(value) = this.ViewModel <- value
    interface IViewFor with member __.ViewModel with get() = (this :> IViewFor<'TViewModel>).ViewModel :> obj and set(value: obj) = (this :> IViewFor<'TViewModel>).ViewModel <- (value :?> 'TViewModel)
    override __.OnAppearing() = 
        base.OnAppearing()
        this.DescendantAdded.AddHandler descendantAdded
        this.DescendantRemoved.AddHandler descendantRemoved
        this.ViewModel.SubscribeToCommands()
        match box this.Content with | null -> this.Content <- this.CreateContent() | _ -> this |> ignore
    override __.OnDisappearing() = 
        listener.Dispose()
        this.ViewModel.UnsubscribeFromCommands()
        this.DescendantRemoved.RemoveHandler descendantRemoved
        this.DescendantAdded.RemoveHandler descendantAdded
        for map in maps.Values do map.Close()
        base.OnDisappearing()
