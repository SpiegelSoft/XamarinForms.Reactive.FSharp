namespace XamarinForms.Reactive.FSharp

open System.Reflection
open System

open Xamarin.Forms

open Splat

open ReactiveUI

module ViewReflection =
    let private reactiveObjectTypeInfo = typeof<ReactiveObject>.GetTypeInfo()
    let private isReactiveObjectType typeInfo = reactiveObjectTypeInfo.IsAssignableFrom(typeInfo)
    let private viewForInterfaceType viewModelType = typedefof<IViewFor<_>>.MakeGenericType([|viewModelType|]).GetTypeInfo()
    let private typesInAssembly instance = instance.GetType().GetTypeInfo().Assembly.DefinedTypes
    let viewForInterfaceTypes instance = typesInAssembly instance |> Seq.filter isReactiveObjectType |> Seq.map (fun typeInfo -> viewForInterfaceType (typeInfo.AsType()))
    let findViewType (interfaceType: TypeInfo) instance = typesInAssembly instance |> Seq.tryFind (fun typeInfo -> interfaceType.IsAssignableFrom(typeInfo))

type AppBootstrapper<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel) as this =
    inherit ReactiveObject()
    let router = new RoutingState()
    do
        let viewModelInstance = viewModel()
        Locator.CurrentMutable.RegisterConstant(context, typeof<IUiContext>)
        Locator.CurrentMutable.RegisterConstant(platform, typeof<'TPlatform>)
        Locator.CurrentMutable.RegisterConstant(this, typeof<IScreen>)
        platform.RegisterDependencies(Locator.CurrentMutable)
        for interfaceType in ViewReflection.viewForInterfaceTypes viewModelInstance do
            match ViewReflection.findViewType interfaceType viewModelInstance with
            | Some viewType -> Locator.CurrentMutable.Register((fun () -> Activator.CreateInstance(viewType.AsType())), interfaceType.AsType())
            | None -> interfaceType |> ignore
        router.NavigationStack.Add(viewModelInstance)
    interface IScreen with member __.Router = router

type App<'TPlatform when 'TPlatform :> IPlatform>(platform, context, viewModel) as this =
    inherit Application()
    let screen = new AppBootstrapper<'TPlatform>(platform, context, viewModel)
    do this.MainPage <- platform.GetMainPage()
    member val UiContext = context with get
    member val Screen = screen :> IScreen with get