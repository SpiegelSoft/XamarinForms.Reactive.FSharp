namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
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

type AppBootstrapper<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel: unit -> IRoutableViewModel) as this =
    inherit ReactiveObject()
    let router = new RoutingState()
    let dependencyResolver = Locator.CurrentMutable
    let viewModelInstance = viewModel()
    do
        dependencyResolver.RegisterConstant(context, typeof<IUiContext>)
        dependencyResolver.RegisterConstant(platform, typeof<'TPlatform>)
        dependencyResolver.RegisterConstant(this, typeof<IScreen>)
        platform.RegisterDependencies dependencyResolver
        for interfaceType in ViewReflection.viewForInterfaceTypes viewModelInstance do
            match ViewReflection.findViewType interfaceType viewModelInstance with
            | Some viewType -> dependencyResolver.Register((fun () -> Activator.CreateInstance(viewType.AsType())), interfaceType.AsType())
            | None -> interfaceType |> ignore
    member internal __.Init(page:NavigationPage) =
        let view = ViewLocator.Current.ResolveView(viewModelInstance)
        view.ViewModel <- viewModelInstance
        page.PushAsync(view :?> Page).Wait()
    interface IScreen with member __.Router = router

type App<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel) =
    inherit Application()
    let screen = new AppBootstrapper<'TPlatform>(platform, context, viewModel)
    member val UiContext = context with get
    member val Screen = screen :> IScreen with get
    member this.Init() =
        let mainPage = platform.GetMainPage()
        this.MainPage <- mainPage
        screen.Init(mainPage)
