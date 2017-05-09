namespace XamarinForms.Reactive.FSharp

open System.Reactive.Linq
open System.Reflection
open System

open Xamarin.Forms

open Splat

open ReactiveUI
open ReactiveUI.XamForms

module ViewReflection =
    let private reactiveObjectTypeInfo = typeof<ReactiveObject>.GetTypeInfo()
    let private isReactiveObjectType typeInfo = reactiveObjectTypeInfo.IsAssignableFrom(typeInfo)
    let private viewForInterfaceType viewModelType = typedefof<IViewFor<_>>.MakeGenericType([|viewModelType|]).GetTypeInfo()
    let private typesInAssembly instance = instance.GetType().GetTypeInfo().Assembly.DefinedTypes
    let viewForInterfaceTypes instance = typesInAssembly instance |> Seq.filter isReactiveObjectType |> Seq.map (fun typeInfo -> viewForInterfaceType (typeInfo.AsType()))
    let findViewType (interfaceType: TypeInfo) instance = typesInAssembly instance |> Seq.tryFind (fun typeInfo -> interfaceType.IsAssignableFrom(typeInfo))

type AppBootstrapper<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel: unit -> IRoutableViewModel) =
    inherit ReactiveObject()
    member internal __.Bootstrap(screen: IScreen) =
        let dependencyResolver = Locator.CurrentMutable
        dependencyResolver.RegisterConstant(context, typeof<IUiContext>)
        dependencyResolver.RegisterConstant(platform, typeof<'TPlatform>)
        dependencyResolver.RegisterConstant(screen, typeof<IScreen>)
        platform.RegisterDependencies dependencyResolver
        let viewModelInstance = viewModel()
        for interfaceType in ViewReflection.viewForInterfaceTypes viewModelInstance do
            match ViewReflection.findViewType interfaceType viewModelInstance with
            | Some viewType -> dependencyResolver.Register((fun () -> Activator.CreateInstance(viewType.AsType())), interfaceType.AsType())
            | None -> interfaceType |> ignore
        viewModelInstance

type FrontPageViewModel() = inherit ReactiveObject()
type FrontPage(router: RoutingState, viewModelInstance) =
    inherit ReactiveContentPage<FrontPageViewModel>()
    override __.OnAppearing() = router.NavigateAndReset.Execute(viewModelInstance).Subscribe() |> ignore

type App<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel) =
    inherit Application()
    let router = new RoutingState()
    let bootstrapper = new AppBootstrapper<'TPlatform>(platform, context, viewModel)
    member val UiContext = context with get
    member this.Screen = this :> IScreen
    member this.Init() =
        let viewModelInstance = bootstrapper.Bootstrap(this)
        let navigationPage = new RoutedViewHost() :> NavigationPage
        navigationPage.PushAsync(new FrontPage(router, viewModelInstance, ViewModel = new FrontPageViewModel())).Wait()
        this.MainPage <- navigationPage
    interface IScreen with member __.Router = router
