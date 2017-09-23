namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp.LocatorDefaults
open XamarinForms.Reactive.FSharp

open ObservableExtensions
open SafeReactiveCommands
open ReactiveUI

type PhotoManifestViewModel(roverName: string, headlineImage: string, launchDate: DateTime, landingDate: DateTime) =
    inherit ReactiveObject()
    let mutable maxDate, maxSol, totalPhotos = landingDate, 0, 0
    let photoSet = ReactiveList<RoverSolPhotoSet>()
    member val LaunchDate = launchDate
    member val LandingDate = landingDate
    member val RoverName = roverName
    member val HeadlineImage = headlineImage
    member __.PhotoSet = photoSet
    member this.MaxSol with get() = maxSol and set(value) = this.RaiseAndSetIfChanged(&maxSol, value, "MaxSol") |> ignore
    member this.MaxDate with get() = maxDate and set(value) = this.RaiseAndSetIfChanged(&maxDate, value, "MaxDate") |> ignore
    member this.TotalPhotos with get() = totalPhotos and set(value) = this.RaiseAndSetIfChanged(&totalPhotos, value, "TotalPhotos") |> ignore
    static member val Curiosity = new PhotoManifestViewModel(Rovers.curiosity, Rovers.imagePaths.Curiosity, DateTime(2011, 11, 26), DateTime(2012, 8, 6))
    static member val Spirit = new PhotoManifestViewModel(Rovers.spirit, Rovers.imagePaths.Spirit, DateTime(2003, 6, 10), DateTime(2004, 1, 4))
    static member val Opportunity = new PhotoManifestViewModel(Rovers.opportunity, Rovers.imagePaths.Opportunity, DateTime(2003, 7, 7), DateTime(2004, 1, 25))

type PhotoSetViewModel(?host: IScreen, ?platform: IMarsPlatform, ?storage: IStorage) =
    inherit PageViewModel()
    let host, platform, storage = LocateIfNone host, LocateIfNone platform, LocateIfNone storage
    let rovers = 
        dict [
            (Rovers.curiosity, PhotoManifestViewModel.Curiosity)
            (Rovers.opportunity, PhotoManifestViewModel.Opportunity); 
            (Rovers.spirit, PhotoManifestViewModel.Spirit)
        ]
    let mutable cameraIndex = 0
    let fetchPhotos (vm: PhotoSetViewModel) =
        let result() = async { return { SyncResult = ApiSyncResult.SyncSucceeded; Content = Unchecked.defaultof<PhotoSet> } }
        result |> ObservableExtensions.observableExecution
    let refreshRovers (vm: PhotoSetViewModel) =
        let result() =
            async {
                return! storage.GetRoversAsync()
            }
        result |> ObservableExtensions.observableExecution
    let cannotRetrieveFirstRovers (result: StorageResult<Rover[]>) = match (result.SyncResult, result.Content.Length) with | (SyncFailed _, 0) -> true | _ -> false
    let hasRetrievedRovers (result: StorageResult<Rover[]>) = result.Content.Length > 0
    let showConnectionError (vm: PhotoSetViewModel) (_:StorageResult<Rover[]>) =
        vm.DisplayAlertMessage({ Title = "Connection Required"; Message = "A workimg connection is required to retrieve the image set for the first time. Please check your connection and try again."; Acknowledge = "OK" }).Subscribe() |> ignore
    let updateManifest (rover: Rover) =
        let photoManifest = rover.PhotoManifest
        let roverViewModel = rovers.[photoManifest.Name]
        roverViewModel.PhotoSet.Clear()
        roverViewModel.MaxSol <- photoManifest.MaxSol
        roverViewModel.TotalPhotos <- photoManifest.TotalPhotos
        for photo in photoManifest.Photos do photo.DefaultImage <- Rovers.imagePath photoManifest.Name
        photoManifest.Photos |> roverViewModel.PhotoSet.AddRange
    let updateManifests (results:StorageResult<Rover[]>) = 
        results.Content |> Seq.iter updateManifest
    member this.CameraIndex with get() = cameraIndex and set(value) = this.RaiseAndSetIfChanged(&cameraIndex, value, "Camera") |> ignore
    member val FetchPhotos = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<PhotoSet>>> with get, set
    member val RefreshRovers = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<Rover[]>>> with get, set
    member val Curiosity = PhotoManifestViewModel.Curiosity
    member val Spirit = PhotoManifestViewModel.Spirit
    member val Opportunity = PhotoManifestViewModel.Opportunity
    override this.SetUpCommands() =
        this.RefreshRovers <- createFromObservable(refreshRovers, None) |> ObservableExtensions.disposeWith this.PageDisposables
        this.FetchPhotos <- createFromObservable(fetchPhotos, None) |> ObservableExtensions.disposeWith this.PageDisposables
        this.RefreshRovers.Where(cannotRetrieveFirstRovers).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showConnectionError this) |> ignore
        this.RefreshRovers.Where(hasRetrievedRovers).ObserveOn(RxApp.MainThreadScheduler).Subscribe(updateManifests) |> ignore
    override this.TearDownCommands() = this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Photos"
