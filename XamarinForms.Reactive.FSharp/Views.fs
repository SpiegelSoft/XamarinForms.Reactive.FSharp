namespace XamarinForms.Reactive.FSharp

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open Xamarin.Forms

open ReactiveUI.XamForms
open ReactiveUI

open Modal

open Themes

open ClrExtensions
open ObservableExtensions

type IContentView = 
    abstract member InitialiseContent: unit -> unit
    abstract member OnContentCreated: unit -> unit

[<AbstractClass>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveContentPage<'TViewModel>()
    let pageDisposables = new CompositeDisposable()
    let alertMessageReceived (alertMessage: AlertMessage) = this.DisplayAlert(alertMessage.Title, alertMessage.Message, alertMessage.Acknowledge)
    let confirmationReceived (confirmation: Confirmation) = this.DisplayAlert(confirmation.Title, confirmation.Message, confirmation.Accept, confirmation.Decline)
    let subscribeToMessages (viewModel: 'TViewModel) disposables =
        let displayAlertCommand = ReactiveCommand.CreateFromTask(alertMessageReceived) |> disposeWith disposables
        let confirmCommand = ReactiveCommand.CreateFromTask(confirmationReceived) |> disposeWith disposables
        viewModel.DisplayAlertCommand <- displayAlertCommand |> Some
        viewModel.ConfirmCommand <- confirmCommand |> Some
    let setUpLifetime disposables =
        let viewModelAdded (viewModel: 'TViewModel) =
            subscribeToMessages viewModel disposables
            viewModel.SetUpCommands()
            this.Content <- this.CreateContent()
            this.OnContentCreated()
        let viewModelRemoved (viewModel: 'TViewModel) =
            viewModel.TearDownCommands()
            disposables.Clear()
        let viewModelObservable = this.WhenAnyValue(fun v -> v.ViewModel)
        viewModelObservable.Buffer(2, 1).Select(fun b -> (b.[0], b.[1]))
            .Where(fun (previous, current) -> previous |> isNotNull && current |> isNull)
            .Select(fun (p, _) -> p).Subscribe(viewModelRemoved) |> disposables.Add
        disposables.Add pageDisposables
        viewModelAdded this.ViewModel
    do this.WhenActivated(setUpLifetime) |> pageDisposables.Add
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: unit -> View
    abstract member OnContentCreated: unit -> unit
    default __.OnContentCreated() = ()
