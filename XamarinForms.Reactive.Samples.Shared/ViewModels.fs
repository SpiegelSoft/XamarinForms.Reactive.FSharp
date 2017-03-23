namespace XamarinForms.Reactive.Samples.Shared

open System.Threading.Tasks
open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp

open ReactiveUI

open LocatorDefaults

type DashboardViewModel(?host: IScreen) = 
    inherit PageViewModel()
    let host = LocateIfNone host
    let submitDetails (vm: DashboardViewModel) (_: Reactive.Unit) =
        async {
            // Save details to database; perform asynchronous online or offline actions
            return true
        } |> Async.StartAsTask
    member val Name = String.Empty with get, set
    member val DateOfBirth = DateTime.Parse("1990-01-01") with get, set
    member val PageTitle = "XamarinForms.Reactive.FSharp |> I <3"
    member val SubmitDetails = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    override this.SetUpCommands() =
        // The command itself is disposable, and so needs to be cleaned up at the end of its lifecycle. The easiest way to do this is to add it to the current PageDisposables collection.
        this.SubmitDetails <- submitDetails this |> ReactiveCommand.CreateFromTask |> ObservableExtensions.disposeWith this.PageDisposables
        // A ReactiveCommand is an IObservable, so based on the result of the submission we can perform further actions, such as navigation.
        this.SubmitDetails.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(fun _ -> this.DisplayAlertMessage({ Title = "Details Submitted"; Message = sprintf "Your name is %s and your date of birth is %s" this.Name ((this.DateOfBirth: DateTime).ToString("dd/MM/yyyy")); Accept = "OK" }) |> ignore)
            |> ObservableExtensions.disposeWith(this.PageDisposables) 
            |> ignore
    override this.TearDownCommands() =
        // We set the observables and subscriptions up, so it is our responsibility to dispose of them. The Clear() method on the PageDisposable collection achieves this because of the use of disposeWith in the SetUpCommands method.
        this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
