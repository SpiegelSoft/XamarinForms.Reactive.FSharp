﻿namespace XamarinForms.Reactive.FSharp

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

module internal MessageHandling =
    let messageReceived (page: Page) (message: AlertMessage) = page.DisplayAlert(message.Title, message.Message, message.Accept) |> ignore
    let addMessageSubscription (page: 'TPage when 'TPage :> Page and 'TPage :> IContentView and 'TPage :> IViewFor<'TViewModel> and 'TViewModel :> PageViewModel) =
        let messageSubscriptions = new CompositeDisposable()
        let subscribeToMessages (viewModel: 'TViewModel) =
            messageSubscriptions.Clear(); 
            match box viewModel with
            | null -> viewModel |> ignore
            | _ -> viewModel.MessageSent.Subscribe(messageReceived page) |> messageSubscriptions.Add
        let viewModelSubscription = page.WhenAnyValue(toLinq <@ fun v -> v.ViewModel @>).Subscribe(subscribeToMessages)
        (viewModelSubscription, messageSubscriptions)

module internal ViewHierarchy =
    let createDescendantEvents (maps: IDictionary<Guid, GeographicMap>) =
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
        (descendantAdded, descendantRemoved)

module internal PageSetup =
    let lifetimeHandlers page =
        let maps = new Dictionary<Guid, GeographicMap>()
        let descendantAdded, descendantRemoved = maps |> ViewHierarchy.createDescendantEvents
        let viewModelSubscription, messageSubscriptions = MessageHandling.addMessageSubscription page
        let appearingHandler() =
            page.DescendantAdded.AddHandler descendantAdded
            page.DescendantRemoved.AddHandler descendantRemoved
            page.ViewModel.SubscribeToCommands()
            match box page.Content with | null -> page.Content <- page.CreateContent() | _ -> page |> ignore
        let disappearingHandler() =
            viewModelSubscription.Dispose()
            messageSubscriptions.Clear()
            page.ViewModel.UnsubscribeFromCommands()
            page.DescendantRemoved.RemoveHandler descendantRemoved
            page.DescendantAdded.RemoveHandler descendantAdded
            for map in maps.Values do map.Close()
        (appearingHandler, disappearingHandler)

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
    let appearingHandler, disappearingHandler = PageSetup.lifetimeHandlers this
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    interface IContentView with
        member this.CreateContent() = this.CreateContent()
        member this.Content with get() = base.Content and set(content) = base.Content <- content
    override __.OnAppearing() = base.OnAppearing(); appearingHandler()
    override __.OnDisappearing() = disappearingHandler(); base.OnDisappearing()

[<AbstractClass>]
type NavigationPage<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveNavigationPage<'TViewModel>()

[<AbstractClass>]
type CarouselPage<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveCarouselPage<'TViewModel>()

[<AbstractClass>]
type TabbedPage<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveTabbedPage<'TViewModel>()

module Carousel =
    let withContentPage contentPage (carouselPage: CarouselPage<'TViewModel>) =
        carouselPage.Children.Add contentPage
        carouselPage

module Tabs =
    let withContentPage contentPage (tabbedPage: TabbedPage<'TViewModel>) =
        tabbedPage.Children.Add contentPage
        tabbedPage