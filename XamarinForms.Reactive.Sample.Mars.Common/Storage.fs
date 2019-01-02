namespace XamarinForms.Reactive.Sample.Mars.Common

open System

open Plugin.Connectivity
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Metadata.Builders
open Microsoft.EntityFrameworkCore.Storage
open System.Windows.Input

module DatabaseStorage =
    let [<Literal>] MarsDatabase = "xrf.mars.1.0.1.db"

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

type PhotoDto(photo: Photo) =
    member val Id = photo.Id with get, set
    member val Sol = photo.Sol with get, set
    member val CameraName = photo.Camera.Name with get, set
    member val ImgSrc = photo.ImgSrc with get, set
    member val EarthDate = photo.EarthDate with get, set
    member val RoverName = photo.Rover.PhotoManifest.Name with get, set

type RoverSolPhotoSetDto(photoSet: RoverSolPhotoSet) =
    member val RoverName = photoSet.RoverName with get, set
    member val HeadlineImage = photoSet.HeadlineImage with get, set
    member val Sol = photoSet.Sol with get, set
    member val EarthDate = photoSet.EarthDate with get, set
    member val TotalPhotos = photoSet.TotalPhotos with get, set
    member val Cameras = String.Join(",", photoSet.Cameras) with get, set
    new() = new RoverSolPhotoSetDto(RoverSolPhotoSet.DefaultValue())
    member this.PhotoSet() =
        let cameras = this.Cameras.Split(',') |> Array.ofSeq
        {
            RoverName = this.RoverName
            HeadlineImage = this.HeadlineImage
            DefaultImage = this.HeadlineImage
            Command = Unchecked.defaultof<ICommand>
            VisibleCameras = cameras
            Sol = this.Sol
            EarthDate = this.EarthDate
            TotalPhotos = this.TotalPhotos
            Cameras = cameras
        }

type PhotoManifestDto(photoManifest: PhotoManifest) =
    member val Name = photoManifest.Name with get, set
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


type IModelContext =
    inherit IDisposable
    abstract BeginTransactionAsync: unit -> Async<IDbContextTransaction>
    abstract SaveAsync: unit -> Async<int>
    abstract Initialise: unit -> unit
    abstract PurgeAsync: unit -> Async<unit>

type IMarsContext =
    inherit IModelContext
    abstract PhotoSets: DbSet<RoverSolPhotoSetDto> with get, set
    abstract PhotoManifests: DbSet<PhotoManifestDto> with get, set

type ICreateModelContext<'a when 'a :> IModelContext> =
    abstract CreateModelContextAsync: unit -> Async<'a>

type ModelContext(connectionString: string) =
    inherit DbContext()
    let configureRoverSolPhotoSet (builder: EntityTypeBuilder<RoverSolPhotoSetDto>) =
        builder.HasKey((fun p -> (p.RoverName, p.Sol) :> obj))|> ignore
        builder.Property(fun p -> p.RoverName).ValueGeneratedNever() |> ignore
        builder.Property(fun p -> p.Sol).ValueGeneratedNever() |> ignore
        builder.Property(fun p -> p.RoverName).HasMaxLength(16) |> ignore
        builder.Property(fun p -> p.HeadlineImage).HasMaxLength(256) |> ignore
    let configurePhotoManifest (builder: EntityTypeBuilder<PhotoManifestDto>) =
        builder.HasKey(fun p -> p.Name :> obj) |> ignore
        builder.Property(fun p -> p.Name).ValueGeneratedNever() |> ignore
        builder.Property(fun p -> p.Name).HasMaxLength(256) |> ignore
    override __.OnConfiguring(optionsBuilder) = 
        connectionString |> optionsBuilder.UseSqlite |> ignore
    override __.OnModelCreating(modelBuilder) =
        modelBuilder.Entity<RoverSolPhotoSetDto>() |> configureRoverSolPhotoSet
        modelBuilder.Entity<PhotoManifestDto>() |> configurePhotoManifest
    new(dbFilename: string, platform: IMarsPlatform) = new ModelContext(sprintf "Filename=%s" (platform.GetLocalFilePath dbFilename))
    member this.Initialise() = this.Database.Migrate()
    member this.BeginTransactionAsync() = this.Database.BeginTransactionAsync() |> Async.AwaitTask
    member this.SaveAsync() = async { return! this.SaveChangesAsync() |> Async.AwaitTask }
    member this.PurgeAsync() = async {
        let! _ = this.Database.EnsureDeletedAsync() |> Async.AwaitTask
        do! this.Database.MigrateAsync() |> Async.AwaitTask
    }
    interface IModelContext with
        member this.Initialise() = this.Initialise()
        member this.BeginTransactionAsync() = this.BeginTransactionAsync()
        member this.SaveAsync() = this.SaveAsync()
        member this.PurgeAsync() = this.PurgeAsync()

type IStorage =
    abstract member GetRoversAsync: unit -> Async<StorageResult<Rover[]>>
    abstract member GetCameraDataAsync : camera:RoverCamera -> sol:int -> Async<StorageResult<PhotoSet>>

type Storage(platform: IMarsPlatform, modelContextFactory: ICreateModelContext<IMarsContext>) =
    let [<Literal>] ThresholdDays = 3.0
    let maxDate savedRovers = savedRovers |> Seq.map (fun r -> r.MaxDate) |> Seq.max
    let syncRequired (savedRovers: PhotoManifest[]) =
        match savedRovers.Length with
        | 0 -> true
        | _ -> (DateTime.UtcNow - (maxDate savedRovers)).TotalDays > ThresholdDays
    let toRovers manifests = manifests |> Array.map (fun m -> { PhotoManifest = m })
    let updateRoversFromApiAsync roversFromApi = async {
        let manifestDtos = roversFromApi |> Array.map (fun r -> r.PhotoManifest) |> Array.map PhotoManifestDto
        use! modelContext = modelContextFactory.CreateModelContextAsync()
        use! transaction = modelContext.BeginTransactionAsync()
        let! existingManifestDtos = modelContext.PhotoManifests.ToDictionaryAsync(fun p -> p.Name) |> Async.AwaitTask
        let manifestsToAdd, manifestsToModify = 
            manifestDtos |> Array.filter (fun p -> existingManifestDtos.ContainsKey p.Name |> not),
            manifestDtos |> Array.filter (fun p -> existingManifestDtos.ContainsKey p.Name)
        do! modelContext.PhotoManifests.AddRangeAsync(manifestsToAdd) |> Async.AwaitTask
        modelContext.PhotoManifests.UpdateRange manifestsToModify
        let photoManifests = roversFromApi |> Array.map (fun r -> r.PhotoManifest)
        let photos = photoManifests |> Array.collect (fun m -> m.Photos)
        let photoSetDtos = photos |> Array.map RoverSolPhotoSetDto 
        let! existingPhotoSetDtos = modelContext.PhotoSets.ToDictionaryAsync(fun p -> (p.RoverName, p.Sol)) |> Async.AwaitTask
        let photoSetsToAdd, photoSetsToModify = 
            photoSetDtos |> Array.filter (fun p -> existingPhotoSetDtos.ContainsKey (p.RoverName, p.Sol) |> not),
            photoSetDtos |> Array.filter (fun p -> existingPhotoSetDtos.ContainsKey (p.RoverName, p.Sol))
        do! modelContext.PhotoSets.AddRangeAsync(photoSetsToAdd) |> Async.AwaitTask
        modelContext.PhotoSets.UpdateRange photoSetsToModify
        transaction.Commit()
    }
    let updateRoversFromApiAsync() =
        async {
            let! roversFromApi = platform.PullRoversAsync()
            do! updateRoversFromApiAsync roversFromApi
            return roversFromApi
        }
    interface IStorage with
        member __.GetRoversAsync() =
            async {
                use! modelContext = modelContextFactory.CreateModelContextAsync()
                let! savedPhotoSets = modelContext.PhotoSets.ToArrayAsync() |> Async.AwaitTask
                let photoSetDictionary = savedPhotoSets |> Seq.map (fun s -> s.PhotoSet()) |> Seq.groupBy (fun photoSet -> photoSet.RoverName) |> dict
                let! savedManifests = modelContext.PhotoManifests.ToArrayAsync() |> Async.AwaitTask
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
        member __.GetCameraDataAsync camera sol =
            async {
                return { SyncResult = SyncSucceeded; Content = { Camera = camera.Name; Photos = [||]; Sol = sol } }
            }
