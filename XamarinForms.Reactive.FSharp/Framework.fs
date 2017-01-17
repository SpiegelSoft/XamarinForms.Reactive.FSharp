namespace XamarinForms.Reactive.FSharp

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

open GeographicLib

open Xamarin.Forms.Maps
open Xamarin.Forms

open System.Collections.Generic
open System.Linq.Expressions
open System

module ExpressionConversion =
    let toLinq (expr : Expr<'a -> 'b>) =
        let linq = LeafExpressionConverter.QuotationToExpression expr
        let call = linq :?> MethodCallExpression
        let lambda = call.Arguments.[0] :?> LambdaExpression
        Expression.Lambda<Func<'a, 'b>>(lambda.Body, lambda.Parameters)

module XamarinGeographic =
    let geodesicLocation (position: Position) = new GeodesicLocation(position.Latitude * 1.0<deg>, position.Longitude * 1.0<deg>)
    let position (location: GeodesicLocation) = new Position(location.Latitude / 1.0<deg>, location.Longitude / 1.0<deg>)
    let distance (geographicDistance: float<km>) = new Distance(1000.0 * geographicDistance / 1.0<km>)
    let geographicDistance (distance: Distance) = 1.0<km> * distance.Kilometers

type GeographicMap() =
    inherit Map()
    static let centerProperty = BindableProperty.Create("Center", typeof<GeodesicLocation>, typeof<GeographicMap>, new GeodesicLocation(), BindingMode.TwoWay)
    static let radiusProperty = BindableProperty.Create("Radius", typeof<float>, typeof<GeographicMap>, 1.0, BindingMode.TwoWay)
    static let searchResultsProperty = BindableProperty.Create("SearchResults", typeof<IEnumerable<Pin>>, typeof<GeographicMap>, null, BindingMode.TwoWay)
    member this.Radius
        with get() = 1.0<km> * (this.GetValue(radiusProperty) :?> float)
        and set(value: float<km>) = if not <| value.Equals(this.Radius) then this.SetValue(radiusProperty, value / 1.0<km>)
    member this.Center 
        with get() = this.GetValue(centerProperty) :?> GeodesicLocation
        and set(value: GeodesicLocation) = if not <| value.Equals(this.Center) then this.SetValue(centerProperty, value)
    member this.SearchResults
        with get() = this.GetValue(searchResultsProperty) :?> IEnumerable<Pin>
        and set(value: IEnumerable<Pin>) = if not <| value.Equals(this.SearchResults) then this.SetValue(searchResultsProperty, value)
    override this.OnPropertyChanged(propertyName) =
        base.OnPropertyChanged(propertyName)
        match propertyName with
        | "VisibleRegion" ->
            this.Center <- this.VisibleRegion.Center |> XamarinGeographic.geodesicLocation
            this.Radius <- this.VisibleRegion.Radius |> XamarinGeographic.geographicDistance
        | "Radius" | "Center" -> 
            match box this.VisibleRegion with
            | null -> this.MoveToRegion(MapSpan.FromCenterAndRadius(this.Center |> XamarinGeographic.position, this.Radius |> XamarinGeographic.distance))
            | _ ->
                let existingCenter, existingRadius = this.VisibleRegion.Center |> XamarinGeographic.geodesicLocation, this.VisibleRegion.Radius |> XamarinGeographic.geographicDistance
                let deltaCenter, deltaRadius = Geodesic.WGS84.Distance existingCenter (this.Center), existingRadius - this.Radius
                let threshold =  0.4 * this.Radius
                if Math.Abs(deltaRadius / 1.0<km>) > threshold / 1.0<km> || Math.Abs((deltaCenter |> UnitConversion.kilometres) / 1.0<km>) > threshold / 1.0<km> then
                    this.MoveToRegion(MapSpan.FromCenterAndRadius(this.Center |> XamarinGeographic.position, this.Radius |> XamarinGeographic.distance))
        | _ -> propertyName |> ignore
    interface IItemsView<Pin> with
        member __.CreateDefault _ = new Pin()
        member __.SetupContent(pin, _) = pin |> ignore
        member __.UnhookContent pin = pin |> ignore
