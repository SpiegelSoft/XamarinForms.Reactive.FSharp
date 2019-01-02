namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp.LocatorDefaults
open XamarinForms.Reactive.FSharp

open ObservableExtensions
open SafeReactiveCommands
open ExpressionConversion
open ReactiveUI
open DynamicData
open ClrExtensions
open DynamicData.Binding

type PhotoSetViewModel(rover: Rover, photoSet: PhotoSet, ?host: IScreen) =
    inherit PageViewModel()
    let host = LocateIfNone host
    let photos = new SourceList<Photo>()
    member val Photos = Observables.createObservableCollection<Photo>()
    member val Title = sprintf "Camera: %s" RoverCameras.all.[photoSet.Camera].FullName
    override this.Initialise() =
        photos.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(this.Photos).Subscribe() |> disposeWith this.Disposables |> ignore
        photos.AddRange photoSet.Photos
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = sprintf "%s: Sol %s" rover.PhotoManifest.Name (photoSet.Sol.ToString("N0"))

type PhotoManifestViewModelState(minEarthDate: DateTime, maxEarthDate: DateTime) =
    inherit ReactiveObject()
    let mutable solFilter = Unchecked.defaultof<string>
    let mutable cameraIndex = 0
    let mutable startEarthDate = minEarthDate
    let mutable endEarthDate = maxEarthDate
    member this.StartEarthDate with get() = startEarthDate and set(value) = this.RaiseAndSetIfChanged(&startEarthDate, value, "StartEarthDate") |> ignore
    member this.EndEarthDate with get() = endEarthDate and set(value) = this.RaiseAndSetIfChanged(&endEarthDate, value, "EndEarthDate") |> ignore
    member this.SolFilter with get() = solFilter and set(value) = this.RaiseAndSetIfChanged(&solFilter, value, "SolFilter") |> ignore
    member this.CameraIndex with get() = cameraIndex and set(value) = this.RaiseAndSetIfChanged(&cameraIndex, value, "CameraIndex") |> ignore

type PhotoManifestViewModel(rover: Rover, ?host: IScreen, ?platform: IMarsPlatform) =
    inherit PageViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let earthDates = rover.PhotoManifest.Photos |> Array.map (fun p -> p.EarthDate)
    let state = new PhotoManifestViewModelState(earthDates |> Array.min, earthDates |> Array.max)
    let photoSets = new SourceList<RoverSolPhotoSet>()
    let cameraCodes = rover.PhotoManifest.Photos |> Seq.collect (fun p -> p.Cameras) |> Seq.distinct |> Array.ofSeq
    member val State = state
    member val Cameras = [[|{ Name = "All"; FullName = "All Cameras" }|]; cameraCodes |> Array.map (fun c -> RoverCameras.all.[c])] |> Array.concat
    member val MinEarthDate = state.StartEarthDate
    member val MaxEarthDate = state.EndEarthDate
    member val PhotoSets = Observables.createObservableCollection<RoverSolPhotoSet>()
    member val RoverName = rover.PhotoManifest.Name
    member val LaunchDate = rover.PhotoManifest.LaunchDate
    member val LandingDate = rover.PhotoManifest.LandingDate
    member val MaxSol = rover.PhotoManifest.MaxSol
    member val TotalPhotos = rover.PhotoManifest.TotalPhotos
    override this.Initialise() =
        let matchesFilter solFilter cameraIndex startEarthDate endEarthDate =
            let betweenDates (photoSet: RoverSolPhotoSet) = photoSet.EarthDate >= startEarthDate && photoSet.EarthDate <= endEarthDate
            match String.IsNullOrWhiteSpace solFilter, cameraIndex with
            | true, 0 -> new Func<RoverSolPhotoSet, bool>(betweenDates)
            | false, 0 -> new Func<RoverSolPhotoSet, bool>(fun photoSet -> 
                photoSet.Sol.ToString("G").Contains solFilter && betweenDates photoSet)
            | true, n ->
                let camera = this.Cameras.[n].Name
                new Func<RoverSolPhotoSet, bool>(fun photoSet ->
                    betweenDates photoSet && photoSet.Cameras |> Array.contains camera)
            | false, n ->
                let camera = this.Cameras.[n].Name
                new Func<RoverSolPhotoSet, bool>(fun photoSet ->
                    photoSet.Sol.ToString("G").Contains solFilter && betweenDates photoSet && photoSet.Cameras |> Array.contains camera)
        let stateObservable = 
            state.WhenAnyValue(
                toLinq <@ fun (vm: PhotoManifestViewModelState) -> vm.SolFilter @>, 
                toLinq <@ fun (vm: PhotoManifestViewModelState) -> vm.CameraIndex @>,
                toLinq <@ fun (vm: PhotoManifestViewModelState) -> vm.StartEarthDate @>,
                toLinq <@ fun (vm: PhotoManifestViewModelState) -> vm.EndEarthDate @>)
        let predicateObservable = stateObservable.Select(fun (solFilter, cameraIndex, startEarthDate, endEarthDate) -> matchesFilter solFilter cameraIndex startEarthDate endEarthDate)
        let filterCameras (roverSolPhotoSet: RoverSolPhotoSet) =
            roverSolPhotoSet.VisibleCameras <-
                match state.CameraIndex with
                | 0 -> roverSolPhotoSet.Cameras
                | n -> [|this.Cameras.[n].Name|]
            let command = createFromAsync(platform.GetCameraDataAsync roverSolPhotoSet, None)
            let goToPhotos photos =
                let destination = new PhotoSetViewModel(rover, photos)
                host.Router.Navigate.Execute(destination).Subscribe() |> disposeWith this.Disposables |> ignore
            command.Subscribe(goToPhotos) |> disposeWith this.Disposables |> ignore
            roverSolPhotoSet.Command <- command
            roverSolPhotoSet
        photoSets.Connect()
            .Filter(predicateObservable, ListFilterPolicy.ClearAndReplace)
            .Sort(SortExpressionComparer<RoverSolPhotoSet>.Descending(fun photoSet -> photoSet.Sol :> IComparable))
            .Transform(filterCameras)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(this.PhotoSets).Subscribe() |> disposeWith this.Disposables |> ignore
        let photoManifest = rover.PhotoManifest
        for photo in photoManifest.Photos do photo.DefaultImage <- Rovers.imagePath photoManifest.Name
        photoManifest.Photos |> photoSets.AddRange
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = rover.PhotoManifest.Name

type RoversViewModelState() =
    inherit ReactiveObject()
    let mutable selectedRover = Unchecked.defaultof<Rover>
    member this.SelectedRover with get() = selectedRover and set(value) = this.RaiseAndSetIfChanged(&selectedRover, value, "SelectedRover") |> ignore

type RoversViewModelCommands(state: RoversViewModelState, storage: IStorage) as this =
    inherit ReactiveObject()
    let fetchPhotos() = async { 
        return { SyncResult = ApiSyncResult.SyncSucceeded; Content = Unchecked.defaultof<PhotoSet> } 
    }
    let refreshRovers() = async {
        return! storage.GetRoversAsync()
    }
    let refreshRoversCommand = createFromAsync(refreshRovers, None)
    let fetchPhotosCommand = createFromAsync(fetchPhotos, None)
    let refreshingRovers = refreshRoversCommand.IsExecuting.ToProperty(this, fun vm -> vm.RefreshingRovers)
    member val RefreshRovers = refreshRoversCommand
    member val FetchPhotos = fetchPhotosCommand
    member __.RefreshingRovers = refreshingRovers.Value

type RoversViewModel(?host: IScreen, ?platform: IMarsPlatform, ?storage: IStorage) =
    inherit PageViewModel()
    let host, platform, storage = LocateIfNone host, LocateIfNone platform, LocateIfNone storage
    let rovers = new SourceList<Rover>()
    let state = new RoversViewModelState()
    let commands = new RoversViewModelCommands(state, storage)
    let cannotRetrieveFirstRovers (result: StorageResult<Rover[]>) = match (result.SyncResult, result.Content.Length) with | (SyncFailed _, 0) -> true | _ -> false
    let showConnectionError (vm: RoversViewModel) (_:StorageResult<Rover[]>) =
        vm.DisplayAlertMessage({ Title = "Connection Required"; Message = "A workimg connection is required to retrieve the image set for the first time. Please check your connection and try again."; Acknowledge = "OK" }).Subscribe() |> ignore
    member val State = state
    member val Commands = commands
    member val Rovers = Observables.createObservableCollection<Rover>()
    override this.Initialise() =
        let disposables = this.Disposables
        rovers.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(this.Rovers).Subscribe() |> disposeWith disposables |> ignore
        commands.RefreshRovers.Where(cannotRetrieveFirstRovers).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showConnectionError this) |> disposeWith disposables |> ignore
        this.DisplayAlertMessage({ Title = "API Key"; Message = platform.GetMetadataEntry "NASA_API_KEY"; Acknowledge = "OK" }).Subscribe(fun _ ->
            commands.RefreshRovers.Execute().Subscribe(fun r -> rovers.AddRange(r.Content)) |> disposeWith disposables |> ignore
        ) |> disposeWith disposables |> ignore
        state.WhenAnyValue(fun vm -> vm.SelectedRover).Where(isNotNull).Subscribe(fun r ->
            let viewModel = new PhotoManifestViewModel(r)
            host.Router.Navigate.Execute(viewModel).Subscribe(fun _ -> state.SelectedRover <- Unchecked.defaultof<Rover>) |> disposeWith disposables |> ignore) |> disposeWith disposables |> ignore
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Rovers"
