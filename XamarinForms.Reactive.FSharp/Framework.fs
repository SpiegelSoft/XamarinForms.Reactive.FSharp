namespace XamarinForms.Reactive.FSharp

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

open GeographicLib

open Xamarin.Forms.Maps
open Xamarin.Forms

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
    static let centerProperty = BindableProperty.Create("Center", typeof<GeodesicLocation>, typeof<GeographicMap>, new GeodesicLocation())
    static let radiusProperty = BindableProperty.Create("Radius", typeof<float>, typeof<GeographicMap>, 1.0)
    member this.Radius
        with get() = 1.0<km> * (this.GetValue(radiusProperty) :?> float)
        and set(value) =
            if not <| value.Equals(this.Radius) then
                this.SetValue(radiusProperty, value)
                this.MoveToRegion(MapSpan.FromCenterAndRadius(this.Center |> XamarinGeographic.position, value |> XamarinGeographic.distance))
    member this.Center 
        with get() = this.GetValue(centerProperty) :?> GeodesicLocation
        and set(value) = 
            if not <| value.Equals(this.Center) then
                this.SetValue(centerProperty, value)
                this.MoveToRegion(MapSpan.FromCenterAndRadius(value |> XamarinGeographic.position, this.Radius |> XamarinGeographic.distance))
    override this.OnPropertyChanged(propertyName) =
        if propertyName = "VisibleRegion" then
            this.SetValue(centerProperty, this.VisibleRegion.Center |> XamarinGeographic.geodesicLocation)
            this.SetValue(radiusProperty, this.VisibleRegion.Radius |> XamarinGeographic.geographicDistance)
