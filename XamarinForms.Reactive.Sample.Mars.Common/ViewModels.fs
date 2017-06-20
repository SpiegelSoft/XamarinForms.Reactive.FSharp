namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp.LocatorDefaults
open XamarinForms.Reactive.FSharp

open ReactiveUI

type PhotoSetViewModel(?host: IScreen, ?platform: IMarsPlatform) =
    inherit PageViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let photos = new ReactiveList<Photo>()
    let mutable cameraIndex = 0
    let mutable imageUrl = Mars.genericImage
    let fetchPhotos (vm: PhotoSetViewModel) (_:Reactive.Unit) =
        async {
            return! RoverCameras.all.[vm.CameraIndex] |> platform.GetCameraDataAsync
        } |> Async.StartAsTask
    let updatePhotos (vm:PhotoSetViewModel) photoSet =
        photos.Clear()
        photos.AddRange(photoSet.Photos)
        vm.ImageUrl <- photoSet.Photos.[0].ImgSrc |> Uri
    let handleException (ex: Exception) =
        ex |> ignore
    member this.CameraIndex with get() = cameraIndex and set(value) = this.RaiseAndSetIfChanged(&cameraIndex, value, "Camera") |> ignore
    member this.ImageUrl with get() = imageUrl and set(value) = this.RaiseAndSetIfChanged(&imageUrl, value, "ImageUrl") |> ignore
    member val FetchPhotos = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, PhotoSet>> with get, set
    override this.SetUpCommands() =
        this.FetchPhotos <- ReactiveCommand.CreateFromTask(fetchPhotos this) |> ObservableExtensions.disposeWith this.PageDisposables
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



