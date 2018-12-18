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
open System

type IContentView = 
    abstract member InitialiseContent: unit -> unit
    abstract member OnContentCreated: unit -> unit

open System.Reactive

[<AbstractClass>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> PageViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveContentPage<'TViewModel>()
    let pageDisposables = new CompositeDisposable()
    let alertMessageReceived (alertMessage: AlertMessage) = this.DisplayAlert(alertMessage.Title, alertMessage.Message, alertMessage.Acknowledge)
    let confirmationReceived (confirmation: Confirmation) = this.DisplayAlert(confirmation.Title, confirmation.Message, confirmation.Accept, confirmation.Decline)
    let viewModelObservable = base.WhenAnyValue(fun v -> v.ViewModel)
    let subscribeToMessages (viewModel: 'TViewModel) disposables =
        let displayAlertCommand = ReactiveCommand.CreateFromTask(alertMessageReceived) |> disposeWith disposables
        let confirmCommand = ReactiveCommand.CreateFromTask(confirmationReceived) |> disposeWith disposables
        viewModel.DisplayAlertCommand <- displayAlertCommand |> Some
        viewModel.ConfirmCommand <- confirmCommand |> Some
    let viewModelActivated (disposables: CompositeDisposable) =
        pageDisposables.Add disposables
        match box this.Content with
        | null ->
            this.Content <- this.CreateContent(viewModelObservable.Where(isNotNull))
            let viewModelRemoved (viewModel: 'TViewModel) =
                viewModel.TearDown()
                pageDisposables.Clear()
            viewModelObservable.Buffer(2, 1).Select(fun b -> (b.[0], b.[1]))
                .Where(fun (previous, current) -> previous |> isNotNull && current |> isNull)
                .Select(fun (p, _) -> p).Subscribe(viewModelRemoved) |> disposables.Add
            subscribeToMessages this.ViewModel disposables
            this.ViewModel.Initialise()
        | _ -> ()
    do 
        this.WhenActivated(viewModelActivated) |> pageDisposables.Add
    do base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: IObservable<'TViewModel> -> View
    interface IDisposableView with member __.Disposables = pageDisposables
