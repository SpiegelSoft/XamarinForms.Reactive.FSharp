namespace XamarinForms.Reactive.FSharp

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

open System.Linq.Expressions
open System

module ExpressionConversion =
    let toLinq (expr : Expr<'a -> 'b>) =
        let linq = LeafExpressionConverter.QuotationToExpression expr
        let call = linq :?> MethodCallExpression
        let lambda = call.Arguments.[0] :?> LambdaExpression
        Expression.Lambda<Func<'a, 'b>>(lambda.Body, lambda.Parameters)  
