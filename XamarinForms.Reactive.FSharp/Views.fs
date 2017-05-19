namespace XamarinForms.Reactive.FSharp

open System.Reactive.Threading.Tasks
open System.Reactive.Disposables
open System.Collections.Generic
open System.Reactive.Linq
open System

open Xamarin.Forms

open ReactiveUI.XamForms
open ReactiveUI

open Modal

open Themes

open ExpressionConversion

open ClrExtensions

type IContentView = 
    abstract member InitialiseContent: unit -> unit
    abstract member OnContentCreated: unit -> unit

module internal MessageHandling =
    let alertMessageReceived (page: Page) (alertMessage: AlertMessage) = page.DisplayAlert(alertMessage.Title, alertMessage.Message, alertMessage.Acknowledge).ToObservable()
    let confirmationReceived (page: Page) (confirmation: Confirmation) = page.DisplayAlert(confirmation.Title, confirmation.Message, confirmation.Accept, confirmation.Decline).ToObservable()
    let addMessageSubscription (page: 'TPage when 'TPage :> Page and 'TPage :> IContentView and 'TPage :> IViewFor<'TViewModel> and 'TViewModel :> PageViewModel) =
        let commands = new CompositeDisposable()
        let subscribeToMessages (viewModel: 'TViewModel) =
            commands.Clear()
            match box viewModel with
            | null -> viewModel |> ignore
            | _ -> 
                let displayAlertCommand = ReactiveCommand.CreateFromObservable(alertMessageReceived page)
                let confirmCommand = ReactiveCommand.CreateFromObservable(confirmationReceived page)
                viewModel.DisplayAlertCommand <- displayAlertCommand |> Some
                viewModel.ConfirmCommand <- confirmCommand |> Some
                commands.Add(displayAlertCommand); commands.Add(confirmCommand)
        let viewModelSubscription = page.WhenAnyValue(toLinq <@ fun v -> v.ViewModel @>).Subscribe(subscribeToMessages)
        (viewModelSubscription, commands)

module internal ViewHierarchy =
    let createDescendantEvents (maps: IDictionary<Guid, GeographicMap>) =
        let descendantAdded = 
            let processElement _ (eventArgs:ElementEventArgs) =
                match box eventArgs.Element with
                | :? GeographicMap as map -> maps.Add (map.Id, map)
                | _ -> eventArgs |> ignore
            new EventHandler<ElementEventArgs> (processElement)
        let descendantRemoved = 
            let processElement _ (eventArgs:ElementEventArgs) =
                match eventArgs.Element with
                | :? GeographicMap as map -> maps.Remove map.Id |> ignore
                | _ -> eventArgs |> ignore
            new EventHandler<ElementEventArgs> (processElement)
        (descendantAdded, descendantRemoved)

module internal PageSetup =
    let lifetimeHandlers (disposables: CompositeDisposable) page =
        let maps = new Dictionary<Guid, GeographicMap>()
        let descendantAdded, descendantRemoved = maps |> ViewHierarchy.createDescendantEvents
        let viewModelSubscription, messageSubscriptions = MessageHandling.addMessageSubscription page
        let appearingHandler() =
            page.DescendantAdded.AddHandler descendantAdded
            page.DescendantRemoved.AddHandler descendantRemoved
            page.ViewModel.PageAppearing()
            page.InitialiseContent()
            page.OnContentCreated()
        let disappearingHandler() =
            viewModelSubscription.Dispose()
            messageSubscriptions.Clear()
            match box page.ViewModel with
            | null -> page |> ignore
            | _ -> page.ViewModel.PageDisappearing()
            page.DescendantRemoved.RemoveHandler descendantRemoved
            page.DescendantAdded.RemoveHandler descendantAdded
        let viewModelAdded _ = appearingHandler()    
        let viewModelRemoved _ = disappearingHandler(); disposables.Clear()
        (viewModelAdded, viewModelRemoved)

type PropertyChange<'a> = { Previous: 'a; Current: 'a }

[<AbstractClass>]
type ContentView<'TViewModel when 'TViewModel :> ReactiveObject and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveContentView<'TViewModel>()
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    abstract member OnContentCreated: unit -> unit
    default this.OnContentCreated() = this |> ignore
    interface IContentView with 
        member this.InitialiseContent() = this.Content <- this.CreateContent()
        member this.OnContentCreated() = this.OnContentCreated()

[<AbstractClass>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveContentPage<'TViewModel>()
    let disposables = new CompositeDisposable()
    let viewModelAdded, viewModelRemoved = PageSetup.lifetimeHandlers disposables this
    let viewModelChangeStream = this.WhenAnyValue(fun v -> v.ViewModel).Buffer(2, 1).Select(fun a -> { Previous = a.[0]; Current = a.[1] })
    do 
        viewModelChangeStream.Where(fun p -> isNull(p.Previous) && isNotNull(p.Current)).Subscribe(fun p -> viewModelAdded p.Current) |> disposables.Add
        viewModelChangeStream.Where(fun p -> isNotNull(p.Previous) && isNull(p.Current)).Subscribe(fun p -> viewModelRemoved p.Previous) |> disposables.Add
        base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    abstract member OnContentCreated: unit -> unit
    default __.OnContentCreated() = this |> ignore
    interface IContentView with
        member __.InitialiseContent() = this.Content <- this.CreateContent()
        member __.OnContentCreated() = this.OnContentCreated()

type CarouselContent<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(page, theme, title, createContent) =
    inherit ReactiveContentPage<'TViewModel>()
    do base.Title <- title
    override this.OnAppearing() = this.Content <- createContent(page)

and [<AbstractClass>] CarouselPage<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) =
    inherit ReactiveCarouselPage<'TViewModel>()
    override this.OnParentSet() =
        base.OnParentSet()
        this.CreateContent() |> Seq.iter (fun kvp -> new CarouselContent<'TViewModel>(this, theme, kvp.Key, kvp.Value) |> this.Children.Add)
    abstract member CreateContent: unit -> IDictionary<string, CarouselPage<'TViewModel> -> View>

type TabContent(title, createContent: unit -> View) =
    inherit ContentPage()
    do base.Title <- title
    override this.OnAppearing() = this.Content <- createContent()

and [<AbstractClass>] TabbedPage<'TViewModel when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveTabbedPage<'TViewModel>()
    let disposables = new CompositeDisposable()
    let viewModelAdded, viewModelRemoved = PageSetup.lifetimeHandlers disposables this
    let viewModelChangeStream = this.WhenAnyValue(fun v -> v.ViewModel).Buffer(2, 1).Select(fun a -> { Previous = a.[0]; Current = a.[1] })
    do 
        viewModelChangeStream.Where(fun p -> isNull(p.Previous) && isNotNull(p.Current)).Subscribe(fun p -> viewModelAdded p.Current) |> disposables.Add
        viewModelChangeStream.Where(fun p -> isNotNull(p.Previous) && isNull(p.Current)).Subscribe(fun p -> viewModelRemoved p.Previous) |> disposables.Add
    abstract member CreateContent: unit -> IDictionary<string, unit -> View>
    abstract member OnContentCreated: unit -> unit
    default this.OnContentCreated() = this |> ignore
    interface IContentView with
        member __.InitialiseContent() = this.CreateContent() |> Seq.iter (fun kvp -> new TabContent(kvp.Key, kvp.Value) |> this.Children.Add)
        member __.OnContentCreated() = this.OnContentCreated()
