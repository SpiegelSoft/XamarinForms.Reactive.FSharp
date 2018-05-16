namespace XamarinForms.Reactive.Sample.Mars.Common

open System

open Newtonsoft.Json

open XamarinForms.Reactive.FSharp

module Themes =
    open XamarinForms.Reactive.FSharp.Themes
    open Xamarin.Forms
    let XrfMars = 
        DefaultTheme 
            |> applyLabelSetters 
                [
                    new Setter(Property = Label.TextColorProperty, Value = Color.Yellow)
                    new Setter(Property = Label.FontAttributesProperty, Value = FontAttributes.Bold)
                ]
            |> applyTextCellTextColor Color.Yellow

type PhotoCamera = 
    {
        [<JsonProperty("id")>] mutable Id: int
        [<JsonProperty("name")>] mutable Name: string
        [<JsonProperty("rover_id")>] mutable RoverId: int
        [<JsonProperty("full_name")>] mutable FullName: string
    }

type RoverCamera =
    {
        [<JsonProperty("name")>] mutable Name: string
        [<JsonProperty("full_name")>] mutable FullName: string
    }

type RoverSolPhotoSet =
    {
        mutable RoverName: string
        mutable HeadlineImage: string
        mutable DefaultImage: string
        [<JsonProperty("sol")>] mutable Sol: int
        [<JsonProperty("total_photos")>] mutable TotalPhotos: int
        [<JsonProperty("cameras")>] mutable Cameras: string[]
    }
    member this.Description = sprintf "%i Cameras, %i Photos" this.Cameras.Length this.TotalPhotos
    member this.ImageSource =
        match String.IsNullOrEmpty this.HeadlineImage with
        | true -> this.DefaultImage
        | false -> this.HeadlineImage
    static member DefaultValue() =
        {
            RoverName = String.Empty
            HeadlineImage = String.Empty
            DefaultImage = String.Empty
            Sol = 0
            TotalPhotos = 0
            Cameras = [||]
        }

type PhotoManifest =
    {
        [<JsonProperty("name")>] mutable Name: string
        [<JsonProperty("landing_date")>] mutable LandingDate: DateTime
        [<JsonProperty("launch_date")>] mutable LaunchDate: DateTime
        [<JsonProperty("status")>] mutable Status: string
        [<JsonProperty("max_sol")>] mutable MaxSol: int
        [<JsonProperty("max_date")>] mutable MaxDate: DateTime
        [<JsonProperty("total_photos")>] mutable TotalPhotos: int
        [<JsonProperty("cameras")>] mutable Photos: RoverSolPhotoSet[]
    }
    static member DefaultValue() =
        {
            Name = String.Empty
            LandingDate = DateTime.MinValue
            LaunchDate = DateTime.MinValue
            Status = String.Empty
            MaxSol = 0
            MaxDate = DateTime.MinValue
            TotalPhotos = 0
            Photos = [||]
        }

type Rover =
    {
        [<JsonProperty("photo_manifest")>] PhotoManifest: PhotoManifest
    }

type Photo =
    {
        [<JsonProperty("id")>] mutable Id: int
        [<JsonProperty("sol")>] mutable Sol: int
        [<JsonProperty("camera")>] mutable Camera: PhotoCamera
        [<JsonProperty("img_src")>] mutable ImgSrc: string
        [<JsonProperty("earth_date")>] mutable EarthDate: DateTime
        [<JsonProperty("rover")>] mutable Rover: Rover
    }

type PhotoSet =
    {
        [<JsonProperty("photos")>] mutable Photos: Photo[]
    }

module RoverCameras =
    let private fhaz = { Name = "FHAZ"; FullName = "Front Hazard Avoidance Camera" }
    let private rhaz = { Name = "RHAZ"; FullName = "Rear Hazard Avoidance Camera" }
    let private mast = { Name = "MAST"; FullName = "Mast Camera" }
    let private chemCam = { Name = "CHEMCAM"; FullName = "Chemistry and Camera Complex" }
    let private mahli = { Name = "MAHLI"; FullName = "Mars Hand Lens Imager" }
    let private mardi = { Name = "MARDI"; FullName = "Mars Descent Imager" }
    let private navCam = { Name = "NAVCAM"; FullName = "Navigation Camera" }
    let private panCam = { Name = "PANCAM"; FullName = "Panoramic Camera" }
    let private minites = { Name = "MINITES"; FullName = "Miniature Thermal Emission Spectrometer (Mini-TES)" }
    let all = dict [(fhaz.Name, fhaz); (rhaz.Name, rhaz); (mast.Name, mast); (chemCam.Name, chemCam); (mahli.Name, mahli); (mardi.Name, mardi); (navCam.Name, navCam); (panCam.Name, panCam); (minites.Name, minites)]

module Rovers =
    type HeadlineImagePath = { Curiosity: string; Spirit: string; Opportunity: string }
    let curiosity, spirit, opportunity = "Curiosity", "Spirit", "Opportunity"
    let names = [|curiosity; spirit; opportunity|]
    let imagePaths = { Curiosity = "curiosity.jpg"; Spirit = "spirit.jpg"; Opportunity = "opportunity.jpg" }
    let imagePath name = 
        match name with 
        | n when n = curiosity -> imagePaths.Curiosity
        | n when n = spirit -> imagePaths.Spirit
        | n when n = opportunity -> imagePaths.Opportunity
        | _ -> String.Empty

type IMarsPlatform =
    inherit IPlatform
    abstract member GetCameraDataAsync : photoSet:RoverSolPhotoSet -> camera:string -> Async<PhotoSet>
    abstract member PullRoversAsync: unit -> Async<Rover[]>
