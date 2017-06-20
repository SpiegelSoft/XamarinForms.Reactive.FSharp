namespace XamarinForms.Reactive.Sample.Mars.Droid

open System.Net.Http
open System.IO
open System

open Newtonsoft.Json

open Android.Content.PM
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

open XamarinForms.Reactive.Sample.Mars.Common
open XamarinForms.Reactive.FSharp

open ModernHttpClient

open ReactiveUI

type Resources = XamarinForms.Reactive.Sample.Mars.Droid.Resource

type DroidPlatform(nasaApiKey) =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface IMarsPlatform with
        member __.RegisterDependencies _ = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName
        member __.GetCameraDataAsync(camera) =
            async {
                let url = sprintf "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&camera=%s&api_key=%s" camera.Name nasaApiKey
                use httpClient = new HttpClient(new NativeMessageHandler())
                use! response = httpClient.GetAsync(url) |> Async.AwaitTask
                let! serialisedData = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let data = JsonConvert.DeserializeObject<PhotoSet>(serialisedData)
                return RoverCameras.data.[camera.Name]
            }

[<Activity (Label = "XRF Mars", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity() =
    inherit FormsApplicationActivity()
    static let [<Literal>] NasaApiKey = "nasa-api-key"
    let createPhotoSetViewModel() = new PhotoSetViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        Forms.Init(this, bundle)
        let metaData = this.PackageManager.GetApplicationInfo(this.PackageName, PackageInfoFlags.MetaData).MetaData
        let app = new App<IMarsPlatform>(new DroidPlatform(metaData.GetString(NasaApiKey)), new UiContext(this), createPhotoSetViewModel)
        app.Init()
        base.LoadApplication app
