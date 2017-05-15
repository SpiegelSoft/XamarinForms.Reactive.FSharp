namespace XamarinForms.Reactive.Sample.Places.Common

open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive
open System

open XamarinForms.Reactive.FSharp

open GeographicLib

open ReactiveUI

open Plugin.Connectivity
open Plugin.Geolocator

open LocatorDefaults

open ObservableExtensions


type MarkerViewModel(location: GeodesicLocation, ?host: IScreen) =
    inherit ReactiveObject()
    let host = LocateIfNone host
    member val Location = location
    member val Screen = host

type PlacesViewModel(?host: IScreen, ?platform: ICustomPlatform) =
    inherit PageViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let markers = new ReactiveList<MarkerViewModel>()
    let locator = CrossGeolocator.Current
    let connectivity = CrossConnectivity.Current
    let isConnected = connectivity.IsConnected
    let mutable searchTerm = String.Empty
    let mutable location = new GeodesicLocation(51.49996<deg>, -0.13663<deg>)
    let mutable radius = 2.0<km>
    let scaleMap (vm: PlacesViewModel) (scaledRadius, scaledLocation) =
        vm.Radius <- scaledRadius
        vm.Location <- scaledLocation
    let initialisePage (_:Unit) = Observable.Create<float<km> * GeodesicLocation>(fun (obs: IObserver<float<km> * GeodesicLocation>) -> 
        async {
            match locator.IsGeolocationAvailable, locator.IsGeolocationEnabled with
            | (true, true) ->
                let! position = locator.GetPositionAsync() |> Async.AwaitTask
                match box position with
                | null -> 0 |> ignore
                | _ -> obs.OnNext(radius, new GeodesicLocation(position.Latitude * 1.0<deg>, position.Longitude * 1.0<deg>))
            | (_, _) -> 0 |> ignore
            obs.OnCompleted()
            return Disposable.Empty
        } |> Async.StartAsTask
    )
    let notifyUserOfConnectivityUnavailable (vm: PlacesViewModel) _ =
        vm.DisplayAlertMessage({ Title = "Connection Required"; Message = "You must have a working connection to search for places"; Acknowledge = "Ok" }).Subscribe() |> ignore
    let notifyUserOfConnectivityRestored (vm: PlacesViewModel) _ =
        platform.ShowToastNotification("Your connection has been restored.")
    member val Markers = markers
    member val InitialisePageCommand = Unchecked.defaultof<ReactiveCommand<Unit, float<km> * GeodesicLocation>> with get, set
    member this.SearchTerm with get() = searchTerm and set(value) = this.RaiseAndSetIfChanged(&searchTerm, value, "SearchTerm") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    member this.Radius with get() = radius and set(value) = this.RaiseAndSetIfChanged(&radius, value, "Radius") |> ignore
    override this.SetUpCommands() =
        let connected = connectivity.ConnectivityChanged.Select(fun ea -> ea.IsConnected)
        connected.Merge(Observable.Return(isConnected)).Where(not).ObserveOn(RxApp.MainThreadScheduler).Subscribe(notifyUserOfConnectivityUnavailable this) |> disposeWith this.PageDisposables |> ignore
        connected.Where(id).ObserveOn(RxApp.MainThreadScheduler).Subscribe(notifyUserOfConnectivityRestored this) |> disposeWith this.PageDisposables |> ignore
        this.InitialisePageCommand <- ReactiveCommand.CreateFromObservable(initialisePage) |> disposeWith this.PageDisposables
        this.InitialisePageCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe(scaleMap this) |> ignore
    override this.TearDownCommands() = this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Places Demo"
