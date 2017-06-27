namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp.LocatorDefaults
open XamarinForms.Reactive.FSharp

open ObservableExtensions
open ReactiveUI

type PhotoSetViewModel(?host: IScreen, ?platform: IMarsPlatform, ?storage: IStorage) =
    inherit PageViewModel()
    let host, platform, storage = LocateIfNone host, LocateIfNone platform, LocateIfNone storage
    let photos = new ReactiveList<Photo>()
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
    let updatePhotos (vm:PhotoSetViewModel) (photoSet: StorageResult<PhotoSet>) =
        photos.Clear()
    let handleException (ex: Exception) =
        ex |> ignore
    member this.CameraIndex with get() = cameraIndex and set(value) = this.RaiseAndSetIfChanged(&cameraIndex, value, "Camera") |> ignore
    member this.ImageUrl with get() = imageUrl and set(value) = this.RaiseAndSetIfChanged(&imageUrl, value, "ImageUrl") |> ignore
    member val FetchPhotos = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<PhotoSet>>> with get, set
    member val RefreshRovers = Unchecked.defaultof<ReactiveCommand<PhotoSetViewModel, StorageResult<Rover[]>>> with get, set
    override this.SetUpCommands() =
        this.RefreshRovers <- ReactiveCommand.CreateFromObservable(refreshRovers) |> ObservableExtensions.disposeWith this.PageDisposables
        this.FetchPhotos <- ReactiveCommand.CreateFromObservable(fetchPhotos) |> ObservableExtensions.disposeWith this.PageDisposables
        this.FetchPhotos.Where(fun p -> ApiResults.syncFailed p).ObserveOn(RxApp.MainThreadScheduler).Subscribe()
        this.FetchPhotos.ObserveOn(RxApp.MainThreadScheduler).Subscribe(updatePhotos this)
            |> ObservableExtensions.disposeWith this.PageDisposables
            |> ignore
        this.FetchPhotos.ThrownExceptions.Subscribe(handleException)
            |> ObservableExtensions.disposeWith this.PageDisposables
            |> ignore
    override this.TearDownCommands() = this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Photos"



