namespace XamarinForms.Reactive.Sample.Mars.Android

open System.Net.Http
open System.IO
open System

open FFImageLoading.Forms.Platform

open Newtonsoft.Json

open Android.Content.PM
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

open Xamarin.Forms.PlatformConfiguration.AndroidSpecific

open XamarinForms.Reactive.Sample.Mars.Common
open XamarinForms.Reactive.Sample.Mars.Data
open XamarinForms.Reactive.FSharp

open ModernHttpClient

open Splat

open ReactiveUI

type MarsResources = XamarinForms.Reactive.Sample.Mars.Android.Resource

type AndroidLogger = Android.Util.Log
type Throwable = Java.Lang.Throwable

type AndroidLogAppender() =
    interface IAppendLog with
        member __.Debug tag message ex = (match ex with | Some x -> AndroidLogger.Debug(tag, message, Throwable.FromException x) | None -> AndroidLogger.Debug(tag, message)) |> ignore
        member __.Information tag message ex = (match ex with | Some x -> AndroidLogger.Info(tag, message, Throwable.FromException x) | None -> AndroidLogger.Info(tag, message)) |> ignore
        member __.Warning tag message ex = (match ex with | Some x -> AndroidLogger.Warn(tag, message, Throwable.FromException x) | None -> AndroidLogger.Warn(tag, message)) |> ignore
        member __.Error tag message ex = (match ex with | Some x -> AndroidLogger.Error(tag, message, Throwable.FromException x) | None -> AndroidLogger.Error(tag, message)) |> ignore
        member __.Critical tag message ex = (match ex with | Some x -> AndroidLogger.Wtf(tag, message, Throwable.FromException x) | None -> AndroidLogger.Wtf(tag, message)) |> ignore

type MarsPlatform(nasaApiKey, mainActivity: Activity) =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    let metadata = mainActivity.PackageManager.GetApplicationInfo(mainActivity.PackageName, PackageInfoFlags.MetaData).MetaData
    interface IMarsPlatform with
        member __.HandleAppLinkRequest _ = ()
        member __.GetMetadataEntry key = metadata.GetString key
        member this.RegisterDependencies dependencyResolver = 
            let modelContextFactory = new ModelContextFactory(this)
            dependencyResolver.RegisterConstant(modelContextFactory, typeof<ICreateModelContext<IMarsContext>>)
            dependencyResolver.RegisterConstant(new Storage(this, modelContextFactory), typeof<IStorage>)
            dependencyResolver.RegisterConstant(new Logger([new AndroidLogAppender()]), typeof<ILog>)
            dependencyResolver.RegisterConstant(this, typeof<IMarsPlatform>)
        member __.GetLocalFilePath fileName = localFilePath fileName
        member __.GetCameraDataAsync roverSolPhotoSet camera =
            async {
                let url = sprintf "https://api.nasa.gov/mars-photos/api/v1/rovers/%s/photos?sol=%i&camera=%s&api_key=%s" roverSolPhotoSet.RoverName roverSolPhotoSet.Sol camera nasaApiKey
                use httpClient = new HttpClient(new NativeMessageHandler())
                use! response = httpClient.GetAsync(url) |> Async.AwaitTask
                let! serialisedData = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let data = JsonConvert.DeserializeObject<PhotoSet>(serialisedData)
                data.Camera <- camera
                data.Sol <- roverSolPhotoSet.Sol
                return data
            }
        member __.GetHeadlineImage name = ImageSource.FromResource name
        member __.PullRoversAsync() =
            let pullRoverAsync roverName =
                async {
                    let url = sprintf "https://api.nasa.gov/mars-photos/api/v1/manifests/%s?api_key=%s" roverName nasaApiKey
                    use httpClient = new HttpClient(new NativeMessageHandler())
                    use! response = httpClient.GetAsync(url) |> Async.AwaitTask
                    let! serialisedData = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    let data = JsonConvert.DeserializeObject<Rover>(serialisedData)
                    for photo in data.PhotoManifest.Photos do photo.RoverName <- data.PhotoManifest.Name
                    return data
                }
            async {
                return! Rovers.names |> Array.map pullRoverAsync |> Async.Parallel
            }

[<Activity(Label = "XRF Mars", Theme = "@style/Theme.AppCompat.NoActionBar", ScreenOrientation = ScreenOrientation.Portrait, MainLauncher = true)>]
type MainActivity() =
    inherit FormsAppCompatActivity()
    static let [<Literal>] NasaApiKey = "nasa-api-key"
    let createRoversViewModel() = new RoversViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        FormsAppCompatActivity.ToolbarResource <- MarsResources.Layout.Toolbar
        FormsAppCompatActivity.TabLayoutResource <- MarsResources.Layout.Tabbar
        base.OnCreate(bundle)
        AppDomain.CurrentDomain.UnhandledException.Subscribe(fun ex ->
            ()
        ) |> ignore
        Forms.Init(this, bundle)
        let metadata = this.PackageManager.GetApplicationInfo(this.PackageName, PackageInfoFlags.MetaData).MetaData
        let app = new App<IMarsPlatform>(new MarsPlatform(metadata.GetString(NasaApiKey), this), new UiContext(this), createRoversViewModel)
        app.Init Themes.XrfMars
        base.LoadApplication app
        CachedImageRenderer.Init(Nullable<bool>(true))
