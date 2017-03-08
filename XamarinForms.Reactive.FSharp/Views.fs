namespace XamarinForms.Reactive.FSharp

open System.Reactive.Disposables
open System.Collections.Generic
open System

open Xamarin.Forms

open ReactiveUI.XamForms
open ReactiveUI

open Modal

open Themes

open ExpressionConversion

type IContentView = 
    abstract member CreateContent: unit -> View
    abstract member Content: View with get, set

[<AbstractClass>]
type ContentView<'TViewModel when 'TViewModel :> ReactiveObject and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveContentView<'TViewModel>()
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    interface IContentView with 
        member this.CreateContent() = this.CreateContent()
        member this.Content with get() = base.Content and set(content) = base.Content <- content

[<AbstractClass>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveContentPage<'TViewModel>()
    let messageReceived (message: AlertMessage) = this.DisplayAlert(message.Title, message.Message, message.Accept) |> ignore
    let messageSubscriptions = new CompositeDisposable()
    let maps = new Dictionary<Guid, GeographicMap>()
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    let descendantAdded = 
        let processElement _ (eventArgs:ElementEventArgs) =
            match box eventArgs.Element with
            | :? GeographicMap as map -> maps.Add (map.Id, map)
            | :? IContentView as contentView -> contentView.Content <- contentView.CreateContent()
            | _ -> eventArgs |> ignore
        new EventHandler<ElementEventArgs> (processElement)
    let descendantRemoved = 
        let processElement _ (eventArgs:ElementEventArgs) =
            match eventArgs.Element with
            | :? GeographicMap as map -> map.Close(); maps.Remove map.Id |> ignore
            | _ -> eventArgs |> ignore
        new EventHandler<ElementEventArgs> (processElement)
    let subscribeToMessages (viewModel: 'TViewModel) =
        messageSubscriptions.Clear(); 
        match box viewModel with
        | null -> viewModel |> ignore
        | _ -> viewModel.MessageSent.Subscribe(messageReceived) |> messageSubscriptions.Add
    let viewModelSubscription = this.WhenAnyValue(toLinq <@ fun v -> v.ViewModel @>).Subscribe(subscribeToMessages)
    abstract member CreateContent: unit -> View
    override __.OnAppearing() = 
        base.OnAppearing()
        this.DescendantAdded.AddHandler descendantAdded
        this.DescendantRemoved.AddHandler descendantRemoved
        this.ViewModel.SubscribeToCommands()
        match box this.Content with | null -> this.Content <- this.CreateContent() | _ -> this |> ignore
    override __.OnDisappearing() = 
        viewModelSubscription.Dispose()
        messageSubscriptions.Clear()
        this.ViewModel.UnsubscribeFromCommands()
        this.DescendantRemoved.RemoveHandler descendantRemoved
        this.DescendantAdded.RemoveHandler descendantAdded
        for map in maps.Values do map.Close()
        base.OnDisappearing()
