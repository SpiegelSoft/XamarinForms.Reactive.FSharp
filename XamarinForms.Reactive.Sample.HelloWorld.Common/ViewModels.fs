namespace XamarinForms.Reactive.Sample.HelloWorld.Common

open System.Threading.Tasks
open System.Reactive.Linq
open System

open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open ReactiveUI

open LocatorDefaults
open ExpressionConversion

type DashboardViewModel(?host: IScreen) = 
    inherit PageViewModel()
    let host = LocateIfNone host
    let submitDetails (vm: DashboardViewModel) (_: Reactive.Unit) = Observable.Return true
    let goToGitHubUrl (vm: DashboardViewModel) (_: Reactive.Unit) = Observable.Return true
    let mutable name = String.Empty
    let mutable dateOfBirth = DateTime.Parse("1990-01-01")
    member this.Name with get() = name and set(value) = this.RaiseAndSetIfChanged(&name, value, "Name") |> ignore
    member this.DateOfBirth with get() = dateOfBirth and set(value) = this.RaiseAndSetIfChanged(&dateOfBirth, value, "DateOfBirth") |> ignore
    member val PageTitle = "XRF |> I <3"
    member val SubmitDetails = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    member val GoToGitHubUrl = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    override this.SetUpCommands() =
        let canSubmitDetails = this.WhenAnyValue(toLinq <@ fun vm -> vm.Name @>).Select(not << String.IsNullOrWhiteSpace)
        // The command itself is disposable, and so needs to be cleaned up at the end of its lifecycle. The easiest way to do this is to add it to the current PageDisposables collection.
        this.SubmitDetails <- ReactiveCommand.CreateFromObservable(submitDetails this, canSubmitDetails) |> ObservableExtensions.disposeWith this.PageDisposables
        this.GoToGitHubUrl <- ReactiveCommand.CreateFromObservable(goToGitHubUrl this) |> ObservableExtensions.disposeWith this.PageDisposables
        // A ReactiveCommand is an IObservable, so based on the result of the submission we can perform further actions, such as navigation.
        this.SubmitDetails.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(fun _ -> this.DisplayAlertMessage({ Title = "Details Submitted"; Message = sprintf "Your name is %s and your date of birth is %s" this.Name ((this.DateOfBirth: DateTime).ToString("dd/MM/yyyy")); Acknowledge = "OK" }) |> ignore)
            |> ObservableExtensions.disposeWith(this.PageDisposables) |> ignore
        this.GoToGitHubUrl.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(fun _ -> "https://github.com/SpiegelSoft/XamarinForms.Reactive.FSharp" |> Uri |> Device.OpenUri |> ignore)
            |> ObservableExtensions.disposeWith(this.PageDisposables) |> ignore
    override this.TearDownCommands() =
        // We set the observables and subscriptions up, so it is our responsibility to dispose of them. The Clear() method on the PageDisposable collection achieves this because of the use of disposeWith in the SetUpCommands method.
        this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
