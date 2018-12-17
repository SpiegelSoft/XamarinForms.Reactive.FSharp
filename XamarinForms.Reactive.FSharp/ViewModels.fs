namespace XamarinForms.Reactive.FSharp

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open GeographicLib

open ReactiveUI

module NullableArguments =
    let toNullable<'a> arg =
        match arg with
        | Some value -> value
        | None -> Unchecked.defaultof<'a>

module GeographicMapScaling =
    let private correctForInternationalDateLine (west: GeodesicLocation) (east: GeodesicLocation) =
        match east.Longitude - west.Longitude > 180.0<deg> with
        | true -> east, west
        | false -> west, east
    let scaleToMarkers (locations: GeodesicLocation[]) =
        let southMostResult = locations |> Seq.minBy (fun l -> l.Latitude)
        let northMostResult = locations |> Seq.maxBy (fun l -> l.Latitude)
        let westMostResult = locations |> Seq.minBy(fun l -> l.Longitude)
        let eastMostResult = locations |> Seq.maxBy(fun l -> l.Longitude)
        let westMostResult, eastMostResult = correctForInternationalDateLine westMostResult eastMostResult
        let centralLatitude = [| southMostResult.Latitude; northMostResult.Latitude |] |> Seq.average
        let centralLongitude = [| westMostResult.Longitude; eastMostResult.Longitude |] |> Seq.average
        let northWest, northEast, southWest, southEast =
            new GeodesicLocation(northMostResult.Latitude, westMostResult.Longitude),
            new GeodesicLocation(northMostResult.Latitude, eastMostResult.Longitude),
            new GeodesicLocation(southMostResult.Latitude, westMostResult.Longitude),
            new GeodesicLocation(southMostResult.Latitude, eastMostResult.Longitude)
        let maxDimension =
            [| 
                Geodesic.WGS84.Distance northWest northEast
                Geodesic.WGS84.Distance southWest southEast
                Geodesic.WGS84.Distance northWest southWest
                1000.0<m>
            |] |> Seq.max
        (0.7 * maxDimension |> UnitConversion.kilometres, new GeodesicLocation(centralLatitude, centralLongitude))

module Modal =
    type AlertMessage = { Title: string; Message: string; Acknowledge: string }
    type Confirmation = { Title: string; Message: string; Accept: string; Decline: string }

open Modal

[<AbstractClass>]
type PageViewModel() =
    inherit ReactiveObject()
    let mutable displayAlertCommand: ReactiveCommand<AlertMessage, Reactive.Unit> option = None
    let mutable confirmCommand: ReactiveCommand<Confirmation, bool> option = None
    let disposables = new CompositeDisposable()
    member val Disposables = disposables
    member __.DisplayAlertMessage(alertMessage) = match displayAlertCommand with | Some command -> command.Execute(alertMessage) | None -> Observable.Never<Reactive.Unit>()
    member __.DisplayConfirmation(confirmation) = match confirmCommand with | Some command -> command.Execute(confirmation) | None -> Observable.Never<bool>()
    member internal __.DisplayAlertCommand with get() = displayAlertCommand and set(value) = displayAlertCommand <- value
    member internal __.ConfirmCommand with get() = confirmCommand and set(value) = confirmCommand <- value
    abstract member Initialise: unit -> unit
    default __.Initialise() = ()
    interface IDisposable with member __.Dispose() = disposables.Clear()
