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

module internal PageSetup =
    let lifetimeHandlers (disposables: CompositeDisposable) page =
        let descendantEvents = new CompositeDisposable()
        let viewModelSubscription, messageSubscriptions = MessageHandling.addMessageSubscription page
        let appearingHandler (viewModel: PageViewModel) =
            match box viewModel with
            | null -> page |> ignore
            | _ -> viewModel.SetUpCommands()
            page.InitialiseContent()
            page.OnContentCreated()
        let disappearingHandler (viewModel: PageViewModel) =
            viewModelSubscription.Dispose()
            messageSubscriptions.Clear()
            match box viewModel with
            | null -> page |> ignore
            | _ -> viewModel.TearDownCommands()
            descendantEvents.Clear()
        let viewModelAdded viewModel = appearingHandler viewModel    
        let viewModelRemoved viewModel = disposables.Clear(); disappearingHandler viewModel
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
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    abstract member OnContentCreated: unit -> unit
    override __.OnParentSet() =
        base.OnParentSet()
        match box this.Parent with
        | null -> viewModelRemoved this.ViewModel
        | _ -> viewModelAdded this.ViewModel
    default __.OnContentCreated() = ()
    interface IContentView with
        member __.InitialiseContent() = this.Content <- this.CreateContent()
        member __.OnContentCreated() = this.OnContentCreated()
