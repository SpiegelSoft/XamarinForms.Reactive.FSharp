namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
open System.Threading
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
    type AlertMessage = { Title: string; Message: string; Accept: string }
    let noMessage = { Title = String.Empty; Message = String.Empty; Accept = String.Empty }

open ExpressionConversion
open Modal

[<AbstractClass>]
type PageViewModel() as this =
    inherit ReactiveObject()
    let mutable message = noMessage
    let uiContext = SynchronizationContext.Current
    member __.SyncContext with get() = uiContext
    member __.Message 
        with get() = message 
        and set(value) =
            this.RaiseAndSetIfChanged(&message, value, "Message") |> ignore
            if message <> noMessage then this.RaiseAndSetIfChanged(&message, noMessage, "Message") |> ignore
    member val MessageSent = this.WhenAnyValue(toLinq <@ fun vm -> vm.Message @>).ObserveOn(RxApp.MainThreadScheduler).Where(fun m -> m <> noMessage) with get
    abstract member SubscribeToCommands: unit -> unit
    abstract member UnsubscribeFromCommands: unit -> unit
