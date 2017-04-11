namespace XamarinForms.Reactive.FSharp

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

open GeographicLib

open Xamarin.Forms.Maps

open System.Linq.Expressions
open System

open Splat

module ObservableExtensions =
    open System.Reactive.Disposables
    let disposeWith (compositeDisposable: CompositeDisposable) (disposable: #IDisposable) = disposable.DisposeWith compositeDisposable

module LocatorDefaults =
    let LocateIfNone(arg : 'a option) =
        match arg with
        | None -> Locator.Current.GetService<'a>()
        | Some a -> a

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
