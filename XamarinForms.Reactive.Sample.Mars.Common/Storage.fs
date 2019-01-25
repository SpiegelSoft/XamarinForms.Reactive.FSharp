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

type ApiError = {
    ErrorType: Type
    Message: string
    StackTrace: string
}

type PushedRecords = {
    Added: int
    Updated: int
    Deleted: int
}

type TablesStoreResult =
    | FailedToSave of exn
    | SavedToLocal
    | SavedToLocalAndRemote

type ITablesStore =
    abstract member FlushAllLocalDataAsync: unit -> Async<unit>

type ConflictStrategy =
    | ClientWins
    | ServerWins

type PushResult =
    | NoNewRecordsPushed
    | PushFailed of int
    | PushSucceeded of PushedRecords

type PullResult =
    | NoNewRecordsPulled
    | PullFailed
    | PullSucceeded of int

type SyncResult = { Pushed: PushResult; Pulled: PullResult }

type ErrorResult = | ErrorDescription of string

//module Sync =
//    open System.Collections.Concurrent
//    open System.Reflection
//    open System.Linq
//    open System
//    open Plugin.Connectivity
//    open LinqKit
//    open Splat

//    let memoize (cacheKey: string) f =
//        let cache = ref Map.empty
//        fun x ->
//            match (!cache).TryFind((cacheKey, x)) with
//            | Some res -> res
//            | None ->
//                 let res = f x
//                 cache := (!cache).Add((cacheKey, x), res)
//                 res
    
//    let [<Literal>] batchSize = 2100
//    let [<Literal>] private Tag = "Sync"
//    let private getEndpoint (typeName: string) =
//        let typeName =
//            match typeName with
//            | name when name.EndsWith("Dto") -> name.Remove(name.LastIndexOf("Dto"))
//            | name -> name
//        sprintf "api/%s" typeName
//    let private endpoint = memoize "get-endpoint" getEndpoint
//    let private publicProperties = new ConcurrentDictionary<Type, PropertyInfo[]>()
//    let private getPublicProperties (recordType: Type) =
//        let properties = recordType.GetProperties(BindingFlags.Public ||| BindingFlags.GetProperty ||| BindingFlags.SetProperty ||| BindingFlags.Instance)
//        properties |> Array.filter (fun p -> p.Name <> "ClientUpdated")
//    let synchroniseAsync<'a, 'c when 'a :> EntityDto and 'a : not struct and 'c :> IModelContext> conflictStrategy (modelContext: 'c) (records: DbSet<'a>) =
//        let getUtcNow() = DateTimeOffset.UtcNow
//        let configuration = Locator.Current.GetService<IConfiguration>()
//        let settings = Locator.Current.GetService<ISharedSettings>()
//        let logger = Locator.Current.GetService<ILog>()
//        let recordType = typeof<'a>
//        let readPath = endpoint recordType.Name
//        let properties = publicProperties.GetOrAdd(recordType, getPublicProperties)
//        let updateRecord (src: 'a) (dest: 'a) = properties |> Seq.iter (fun property -> property.SetValue(dest, property.GetValue(src)))
//        let handleConflict =
//            match conflictStrategy with
//            | ClientWins -> 
//                fun timestamp (clientRecord: 'a) (serverRecord: 'a) -> 
//                    clientRecord.ServerUpdated <- serverRecord.ServerUpdated; clientRecord.ClientUpdated <- timestamp
//            | ServerWins ->
//                fun timestamp (clientRecord: 'a) (serverRecord: 'a) ->
//                    updateRecord serverRecord clientRecord; clientRecord.ClientUpdated <- timestamp
//        let updateRecordsAsync (clientRecords: DbSet<'a>) (serverRecords: 'a[]) =
//            async {
//                let timestamp = getUtcNow()
//                let annotatedServerRecords = serverRecords |> Array.map (fun e -> (e, e.Deleted.HasValue))
//                let isDeleted (_, x) = x
//                let isNotDeleted (_, x) = not x
//                let record (x, _) = x
//                let deleteClientRecord (serverRecord: 'a) =
//                    let clientRecord = clientRecords.Find(serverRecord.Id)
//                    clientRecord.Deleted <- serverRecord.Deleted
//                let updateClientRecord (serverRecord: 'a) =
//                    let clientRecord = clientRecords.Find(serverRecord.Id)
//                    match clientRecord.ClientUpdated.UtcDateTime > serverRecord.ServerUpdated.Value.UtcDateTime with
//                    | true -> handleConflict timestamp clientRecord serverRecord
//                    | false -> updateRecord serverRecord clientRecord; clientRecord.ClientUpdated <- timestamp
//                annotatedServerRecords |> Array.filter isDeleted |> Array.map record |> Seq.iter deleteClientRecord
//                annotatedServerRecords |> Array.filter isNotDeleted |> Array.map record |> Seq.iter updateClientRecord
//                return annotatedServerRecords |> Array.map record
//            }
//        let pullAsync (clientRecords: DbSet<'a>) = 
//            async {
//                let! clientRecordIds = clientRecords.Select(fun e -> e.Id).ToArrayAsync() |> Async.AwaitTask
//                let! pullResult = ApiClient.getAsync<'a[]> settings configuration readPath
//                match pullResult with
//                | RestSuccess (pullStatusCode, pulledValues) ->
//                    let text = sprintf "%i %O %s records retrieved from server. Status code = %O" pulledValues.Length typeof<'a> (match pulledValues.Length = 1 with | true -> String.Empty | false -> "s") pullStatusCode
//                    logger.Information Tag text None
//                    let newOrExisting (value: 'a) = match clientRecordIds.Contains (value.Id) with | true -> (value, false, true) | false -> (value, true, false)
//                    let newAndExistingServerRecords = pulledValues |> Array.map newOrExisting
//                    let isNew (_, x, _) = x
//                    let isExisting (_, _, x) = x
//                    let record (x, _, _) = x
//                    let newRecords = newAndExistingServerRecords |> Array.filter isNew |> Array.map record
//                    let existingRecords = newAndExistingServerRecords |> Array.filter isExisting |> Array.map record
//                    do! clientRecords.AddRangeAsync newRecords |> Async.AwaitTask
//                    let! updatedRecords = existingRecords |> updateRecordsAsync clientRecords
//                    let pulledRecordCount = newRecords.Length + updatedRecords.Length
//                    return match pulledRecordCount with | 0 -> NoNewRecordsPulled | _ -> PullSucceeded pulledRecordCount
//                | RestFailure (pushStatusCode, ErrorDescription error) ->
//                    let text = sprintf "Failed to retrieve %O records from server. Status code = %O. Description: %s" recordType pushStatusCode error
//                    logger.Error Tag text None
//                    return PullFailed
//            }
//        let saveAsync (modelContext: IModelContext) =
//            async {
//                let! saveResult = modelContext.SaveAsync()
//                let text = sprintf "Saved %i %O record%s to device." saveResult recordType (match saveResult = 1 with | true -> String.Empty | false -> "s")
//                logger.Information Tag text None
//            }
//        async {
//            match CrossConnectivity.Current.IsConnected with
//            | false -> return { Pushed = NoNewRecordsPushed; Pulled = NoNewRecordsPulled }
//            | true ->
//                let nullDateTimeOffset = Nullable<DateTimeOffset>()
//                let! synchronisedRecords = records.Where(fun r -> r.ServerUpdated <> nullDateTimeOffset).ToArrayAsync() |> Async.AwaitTask
//                let lastSync = 
//                    match synchronisedRecords |> Array.isEmpty with
//                    | true -> DateTimeOffset.MinValue
//                    | false -> synchronisedRecords |> Array.map (fun r -> r.ServerUpdated.Value) |> Array.max
//                let writePath = sprintf "%s/%i" readPath lastSync.UtcTicks
//                let! changedValues = records.Where(fun r -> r.ClientUpdated > lastSync).ToArrayAsync() |> Async.AwaitTask
//                match changedValues |> Array.isEmpty with
//                | true -> 
//                    let! pullResult = pullAsync records
//                    do! modelContext |> saveAsync
//                    return { Pushed = NoNewRecordsPushed; Pulled = pullResult }
//                | false ->
//                    let! pushResult = changedValues |> ApiClient.putAsync<PushedRecords> settings configuration writePath
//                    match pushResult with
//                    | RestSuccess (pushStatusCode, pushedRecords) ->
//                        let text = sprintf "%i %O record%s added, %i updated and %i deleted on server. Status code = %O." pushedRecords.Added recordType (match pushedRecords.Added = 1 with | true -> String.Empty | false -> "s") pushedRecords.Updated pushedRecords.Deleted pushStatusCode
//                        logger.Information Tag text None
//                        let! pullResult = pullAsync records
//                        do! modelContext |> saveAsync
//                        return { Pushed = PushSucceeded pushedRecords; Pulled = pullResult }
//                    | RestFailure (pushStatusCode, ErrorDescription error) ->
//                        let text = sprintf "Failed to update %i %O record%s on server. Status code = %O. Description: %s" changedValues.Length recordType (match changedValues.Length = 1 with | true -> String.Empty | false -> "s") pushStatusCode error
//                        logger.Error Tag text None
//                        return { Pushed = PushFailed changedValues.Length; Pulled = NoNewRecordsPulled }
//        }

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
