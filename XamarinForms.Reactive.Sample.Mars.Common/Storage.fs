namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Threading.Tasks
open System

open Plugin.Connectivity

open SQLite

type ApiSyncFailure =
    | ConnectionUnavailable
    | ExecutionFailed of Exception

type ApiSyncResult =
    | SyncNotRequired
    | SyncSucceeded
    | SyncFailed of ApiSyncFailure

module ApiResults =
    let syncFailed result = match result with | SyncFailed _ -> true | _ -> false

type StorageResult<'a> =
    {
        SyncResult: ApiSyncResult
        Content: 'a
    }

type RoverSolPhotoSetDto(photoSet: RoverSolPhotoSet) =
    [<Indexed; MaxLength(16)>] member val RoverName = photoSet.RoverName with get, set
    [<Indexed>] member val Sol = photoSet.Sol with get, set
    member val TotalPhotos = photoSet.TotalPhotos with get, set
    member val Cameras = String.Join(",", photoSet.Cameras) with get, set
    new() = new RoverSolPhotoSetDto(RoverSolPhotoSet.DefaultValue())
    member this.PhotoSet() =
        {
            RoverName = this.RoverName
            Sol = this.Sol
            TotalPhotos = this.TotalPhotos
            Cameras = this.Cameras.Split(',') |> Array.ofSeq
        }

type PhotoManifestDto(photoManifest: PhotoManifest) =
    [<Indexed>] member val Name = photoManifest.Name with get, set
    member val LandingDate = photoManifest.LandingDate with get, set
    member val LaunchDate = photoManifest.LaunchDate with get, set
    member val Status = photoManifest.Status with get, set
    member val MaxSol = photoManifest.MaxSol with get, set
    member val MaxDate = photoManifest.MaxDate with get, set
    member val TotalPhotos = photoManifest.TotalPhotos with get, set
    new() = new PhotoManifestDto(PhotoManifest.DefaultValue())
    member this.Manifest(photoSet) =
        {
            Name = this.Name
            LandingDate = this.LandingDate
            LaunchDate = this.LaunchDate
            Status = this.Status
            MaxSol = this.MaxSol
            MaxDate = this.MaxDate
            TotalPhotos = this.TotalPhotos
            Photos = photoSet |> Array.ofSeq
        }

type IStorage =
    abstract member GetRoversAsync: unit -> Async<StorageResult<Rover[]>>
    abstract member GetCameraDataAsync : RoverCamera -> Async<StorageResult<PhotoSet>>

type Storage(platform: IMarsPlatform) =
    let [<Literal>] Tag = "Storage"
    let [<Literal>] fileName = "storage.db"
    let [<Literal>] ThresholdDays = 3.0
    let database = fileName |> platform.GetLocalFilePath |> SQLiteAsyncConnection
    let createTables = database.CreateTablesAsync<RoverSolPhotoSetDto, PhotoManifestDto>()
    let maxDate savedRovers = savedRovers |> Seq.map (fun r -> r.MaxDate) |> Seq.max
    let syncRequired (savedRovers: PhotoManifest[]) =
        match savedRovers.Length with
        | 0 -> true
        | _ -> (DateTime.UtcNow - (maxDate savedRovers)).TotalDays > ThresholdDays
    let toRovers manifests = manifests |> Array.map (fun m -> { PhotoManifest = m })
    let updateRoversFromApi roversFromApi (conn: SQLiteConnection) =
        let manifestDtos = roversFromApi |> Array.map (fun r -> r.PhotoManifest) |> Array.map PhotoManifestDto
        let manifestResults = manifestDtos |> Array.map conn.InsertOrReplace
        let photoManifests = roversFromApi |> Array.map (fun r -> r.PhotoManifest)
        let photos = photoManifests |> Array.collect (fun m -> m.Photos)
        let photoSetDtos = photos |> Array.map RoverSolPhotoSetDto 
        let photoResults = photoSetDtos |> Array.map conn.InsertOrReplace
        photoResults |> ignore
    let updateRoversFromApiAsync() =
        async {
            let! roversFromApi = platform.PullRoversAsync()
            do! database.RunInTransactionAsync(updateRoversFromApi roversFromApi) |> Async.AwaitTask
            return roversFromApi
        }
    interface IStorage with
        member __.GetRoversAsync() =
            async {
                let tablesResult = createTables.Result
                let! savedPhotoSets = database.Table<RoverSolPhotoSetDto>().ToListAsync() |> Async.AwaitTask
                let photoSetDictionary = savedPhotoSets |> Seq.map (fun s -> s.PhotoSet()) |> Seq.groupBy (fun photoSet -> photoSet.RoverName) |> dict
                let! savedManifests = database.Table<PhotoManifestDto>().ToListAsync() |> Async.AwaitTask
                let savedManifests = savedManifests |> Seq.map (fun dto -> dto.Manifest(photoSetDictionary.[dto.Name])) |> Array.ofSeq
                let savedRovers = savedManifests |> toRovers
                match syncRequired savedManifests with
                | false -> return { SyncResult = ApiSyncResult.SyncNotRequired; Content = savedRovers }
                | true -> 
                    try
                        match CrossConnectivity.Current.IsConnected with
                        | false -> return { SyncResult = ConnectionUnavailable |> SyncFailed; Content = savedRovers }
                        | true -> 
                            let! roversFromApi = updateRoversFromApiAsync()
                            return { SyncResult = SyncSucceeded; Content = roversFromApi }
                    with | ex -> return { SyncResult = ex |> ExecutionFailed |> SyncFailed; Content = savedRovers }
            }
        member __.GetCameraDataAsync(camera) =
            async {
                return { SyncResult = SyncSucceeded; Content = { Photos = [||] } }
            }