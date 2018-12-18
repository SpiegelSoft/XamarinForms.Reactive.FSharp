namespace XamarinForms.Reactive.FSharp

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

open System.Linq.Expressions
open System

open Splat

module ObservableExtensions =
    open System.Reactive.Disposables
    open System.Reactive.Linq
    let private readObservable (read: unit -> Async<'a>) (obs: IObserver<'a>) =
        async {
            try
                let! result = read()
                obs.OnNext result
            with | ex -> obs.OnError ex
            obs.OnCompleted()
            return Disposable.Empty
        }
    let disposeWith (compositeDisposable: CompositeDisposable) (disposable: #IDisposable) = disposable.DisposeWith compositeDisposable
    let observableExecution (execute: unit -> Async<'a>) = Observable.Create<'a>(fun (obs: IObserver<'a>) -> readObservable execute obs |> Async.StartAsTask)

module ClrExtensions =
    let isNotNull o =
        match box o with
        | null -> false
        | _ -> true
    let isNull o = o |> isNotNull |> not
    let toObj x = x :> obj

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
    let rec propertyName = function
    | Patterns.Lambda(_, expr) -> propertyName expr
    | Patterns.PropertyGet(_, propertyInfo, _) -> propertyInfo.Name
    | _ -> failwith "You have asked for the property name of an expression that does not describe a property."
    let rec setProperty instance value = function
    | Patterns.Lambda(_, expr) -> setProperty instance value expr
    | Patterns.PropertyGet(_, propertyInfo, _) -> propertyInfo.SetValue(instance, value)
    | _ -> failwith "You have tried to set a property value using an expression that does not describe a property."
