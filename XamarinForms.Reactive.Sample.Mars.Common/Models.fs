namespace XamarinForms.Reactive.Sample.Mars.Common

open System

open Newtonsoft.Json

open XamarinForms.Reactive.FSharp


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

type Rover =
    {
        [<JsonProperty("id")>] mutable Id: int
        [<JsonProperty("name")>] mutable Name: string
        [<JsonProperty("landing_date")>] mutable LandingDate: DateTime
        [<JsonProperty("launch_date")>] mutable LaunchDate: DateTime
        [<JsonProperty("status")>] mutable Status: string
        [<JsonProperty("max_sol")>] mutable MaxSol: int
        [<JsonProperty("max_sol")>] mutable MaxDate: DateTime
        [<JsonProperty("total_photos")>] mutable TotalPhotos: int
        [<JsonProperty("cameras")>] mutable Cameras: RoverCamera[]
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
    let all = 
        [|
            { RoverCamera.Name = "FHAZ"; FullName = "Front Hazard Avoidance Camera" }
            { Name = "RHAZ"; FullName = "Rear Hazard Avoidance Camera" }
            { Name = "MAST"; FullName = "Mast Camera" }
            { Name = "CHEMCAM"; FullName = "Chemistry and Camera Complex" }
            { Name = "NAVCAM"; FullName = "Navigation Camera" }
        |]
    let getCode = all |> Seq.map (fun c -> (c.FullName, c.Name)) |> dict
    let names = all |> Array.map (fun c -> c.FullName)
    let curiosity =
        {
            Id = 5
            Name = "Curiosity"
            LandingDate = DateTime.Parse("2012-08-06")
            LaunchDate = DateTime.Parse("2011-11-26")
            Status = "Active"
            MaxSol = 1658
            MaxDate = DateTime.Parse("2017-04-05")
            TotalPhotos = 312467
            Cameras = [||]
        }
    let data = 
        dict 
            [
                ("FHAZ", 
                    { Photos = 
                        [| 
                            { 
                                Photo.Id = 102693; 
                                Sol = 1000; 
                                Camera = { Id = 20; RoverId = 5; Name = "FHAZ"; FullName = "Front Hazard Avoidance Camera" };
                                ImgSrc = "https://mars.jpl.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/fcam/FRB_486265257EDR_F0481570FHAZ00323M_.JPG" 
                                EarthDate = DateTime.Parse("2015-05-30")
                                Rover = curiosity
                            } 
                        |] })
                ("RHAZ", { Photos = 
                        [| 
                            { 
                                Photo.Id = 102693; 
                                Sol = 1000; 
                                Camera = { Id = 20; RoverId = 5; Name = "FHAZ"; FullName = "Rear Hazard Avoidance Camera" };
                                ImgSrc = "https://mars.jpl.nasa.gov/msl-raw-images/msss/01000/mcam/1000ML0044631300305227E03_DXXX.jpg" 
                                EarthDate = DateTime.Parse("2015-05-30")
                                Rover = curiosity
                            } 
                        |] })
                ("MAST", { Photos = 
                        [| 
                            { 
                                Photo.Id = 102693; 
                                Sol = 1000; 
                                Camera = { Id = 20; RoverId = 5; Name = "FHAZ"; FullName = "Mast" };
                                ImgSrc = "http://mars.jpl.nasa.gov/msl-raw-images/msss/01000/mcam/1000MR0044631290503689E01_DXXX.jpg" 
                                EarthDate = DateTime.Parse("2015-05-30")
                                Rover = curiosity
                            } 
                        |] })
                ("CHEMCAM", { Photos = 
                        [| 
                            { 
                                Photo.Id = 102693; 
                                Sol = 1000; 
                                Camera = { Id = 20; RoverId = 5; Name = "FHAZ"; FullName = "Mast" };
                                ImgSrc = "http://mars.jpl.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/soas/rdr/ccam/CR0_486263086PRC_F0481570CCAM02000L1.PNG" 
                                EarthDate = DateTime.Parse("2015-05-30")
                                Rover = curiosity
                            } 
                        |] })
                ("NAVCAM",  { Photos = 
                        [| 
                            { 
                                Photo.Id = 102693; 
                                Sol = 1000; 
                                Camera = { Id = 20; RoverId = 5; Name = "FHAZ"; FullName = "Mast" };
                                ImgSrc = "http://mars.jpl.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/ncam/NRB_486272784EDR_F0481570NCAM00415M_.JPG" 
                                EarthDate = DateTime.Parse("2015-05-30")
                                Rover = curiosity
                            } 
                        |] })
            ]

module Mars =
    let genericImage = "https://www.nasa.gov/sites/default/files/styles/full_width_feature/public/thumbnails/image/pia21463.jpg" |> Uri

type IMarsPlatform =
    inherit IPlatform
    abstract member GetCameraDataAsync : RoverCamera -> Async<PhotoSet>
