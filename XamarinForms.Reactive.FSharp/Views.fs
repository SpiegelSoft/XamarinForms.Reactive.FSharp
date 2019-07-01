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
open Splat

type IContentView = 
    abstract member InitialiseContent: unit -> unit
    abstract member OnContentCreated: unit -> unit

open System.Reactive
open DynamicData
open System.Threading

[<AbstractClass>]
[<DisableAnimation>]
type ContentPage<'TViewModel, 'TView when 'TViewModel :> PageViewModel and 'TViewModel :> IRoutableViewModel and 'TViewModel : not struct>(theme: Theme) as this =
    inherit ReactiveContentPage<'TViewModel>()
    let pageDisposables = new CompositeDisposable()
    let alertMessageReceived (alertMessage: AlertMessage) = this.DisplayAlert(alertMessage.Title, alertMessage.Message, alertMessage.Acknowledge)
    let confirmationReceived (confirmation: Confirmation) = this.DisplayAlert(confirmation.Title, confirmation.Message, confirmation.Accept, confirmation.Decline)
    let viewModelObservable = base.WhenAnyValue(fun v -> v.ViewModel)
    let subscribeToMessages (viewModel: 'TViewModel) disposables =
        let displayAlertCommand = ReactiveCommand.CreateFromTask(alertMessageReceived)
        let confirmCommand = ReactiveCommand.CreateFromTask(confirmationReceived)
        viewModel.DisplayAlertCommand <- displayAlertCommand |> Some
        viewModel.ConfirmCommand <- confirmCommand |> Some
    let viewModelActivated (disposables: CompositeDisposable) =
        pageDisposables.Add disposables
        match box this.Content with
        | null ->
            this.Content <- this.CreateContent(viewModelObservable.Where(isNotNull))
            let viewModel = this.ViewModel
            viewModel.WhenNavigatingFromObservable().Subscribe((fun (_:Unit) -> ()), fun () -> 
                viewModel.TearDown()
                pageDisposables.Clear()) |> pageDisposables.Add
            subscribeToMessages viewModel pageDisposables
            async {
                do! viewModel.InitialiseAsync()
            } |> Async.Start
        | _ -> ()
    do 
        this.WhenActivated(viewModelActivated) |> pageDisposables.Add
        base.BackgroundColor <- theme.Styles.BackgroundColor
    abstract member CreateContent: IObservable<'TViewModel> -> View
    interface IDisposableView with member __.Disposables = pageDisposables
