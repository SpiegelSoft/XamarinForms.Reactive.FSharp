namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp.LocatorDefaults
open XamarinForms.Reactive.FSharp

open ObservableExtensions
open ReactiveUI

type PhotoManifestViewModel(roverName: string, launchDate: DateTime, landingDate: DateTime) =
    inherit ReactiveObject()
    let mutable maxDate, maxSol, totalPhotos = landingDate, 0, 0
    member val LaunchDate = launchDate
    member val LandingDate = landingDate
    member val PhotoSet = ReactiveList<RoverSolPhotoSet>()
    member this.MaxSol with get() = maxSol and set(value) = this.RaiseAndSetIfChanged(&maxSol, value, "MaxSol") |> ignore
    member this.MaxDate with get() = maxDate and set(value) = this.RaiseAndSetIfChanged(&maxDate, value, "MaxDate") |> ignore
    member this.TotalPhotos with get() = totalPhotos and set(value) = this.RaiseAndSetIfChanged(&totalPhotos, value, "TotalPhotos") |> ignore

type PhotoSetViewModel(?host: IScreen, ?platform: IMarsPlatform, ?storage: IStorage) =
    inherit PageViewModel()
    let host, platform, storage = LocateIfNone host, LocateIfNone platform, LocateIfNone storage
    let rovers = dict [(RoverNames.curiosity, PhotoManifestViewModel); (RoverNames.opportunity, PhotoManifestViewModel); (RoverNames.spirit, PhotoManifestViewModel)]
    let mutable cameraIndex = 0
    let mutable imageUrl = Mars.genericImage
    let fetchPhotos (vm: PhotoSetViewModel) =
        let result() =
            async {
                return { SyncResult = ApiSyncResult.SyncSucceeded; Content = Unchecked.defaultof<PhotoSet> }
            }
        result |> ObservableExtensions.observableExecution
    let refreshRovers (vm: PhotoSetViewModel) =
        let result() =
            async {
                return! storage.GetRoversAsync()
            }
        result |> ObservableExtensions.observableExecution
    let handleException (ex: Exception) =
        ex |> ignore
    let cannotRetrieveFirstRovers (result: StorageResult<Rover[]>) = match (result.SyncResult, result.Content.Length) with | (SyncFailed _, 0) -> true | _ -> false
    let hasRetrievedRovers (result: StorageResult<Rover[]>) = result.Content.Length > 0
    let showConnectionError (vm: PhotoSetViewModel) (_:StorageResult<Rover[]>) =
        vm.DisplayAlertMessage({ Title = "Connection Required"; Message = "A workimg connection is required to retrieve the image set for the first time. Please check your connection and try again."; Acknowledge = "OK" }).Subscribe() |> ignore
    let updateManifests (vm: PhotoSetViewModel) (results:StorageResult<Rover[]>) =
        for rover in results.Content do
            rover.PhotoManifest |> ignore
    member this.CameraIndex with get() = cameraIndex and set(value) = this.RaiseAndSetIfChanged(&cameraIndex, value, "Camera") |> ignore
    member this.ImageUrl with get() = imageUrl and set(value) = this.RaiseAndSetIfChanged(&imageUrl, value, "ImageUrl") |> ignore
    member val FetchPhotos = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<PhotoSet>>> with get, set
    member val RefreshRovers = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<Rover[]>>> with get, set
    override this.SetUpCommands() =
        this.RefreshRovers <- ReactiveCommand.CreateFromObservable(refreshRovers) |> ObservableExtensions.disposeWith this.PageDisposables
        this.FetchPhotos <- ReactiveCommand.CreateFromObservable(fetchPhotos) |> ObservableExtensions.disposeWith this.PageDisposables
        this.RefreshRovers.Where(cannotRetrieveFirstRovers).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showConnectionError this) |> ignore
        this.RefreshRovers.Where(hasRetrievedRovers).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showConnectionError this) |> ignore
        this.FetchPhotos.ThrownExceptions.Subscribe(handleException)
            |> ObservableExtensions.disposeWith this.PageDisposables
            |> ignore
    override this.TearDownCommands() = this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Photos"
