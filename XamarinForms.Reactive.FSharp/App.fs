namespace XamarinForms.Reactive.FSharp

open System.Reactive.Disposables
open System.Collections.Generic
open System.Reactive.Linq
open System.Reflection
open System.Threading
open System

open Xamarin.Forms.PlatformConfiguration.AndroidSpecific.AppCompat
open Xamarin.Forms.PlatformConfiguration
open Xamarin.Forms

open Splat

open ReactiveUI.XamForms
open ReactiveUI

open Themes

module ViewReflection =
    let private reactiveObjectTypeInfo = typeof<ReactiveObject>.GetTypeInfo()
    let private isReactiveObjectType typeInfo = reactiveObjectTypeInfo.IsAssignableFrom(typeInfo)
    let private viewForInterfaceType viewModelType = typedefof<IViewFor<_>>.MakeGenericType([|viewModelType|]).GetTypeInfo()
    let private typesInAssembly instance = instance.GetType().GetTypeInfo().Assembly.DefinedTypes
    let viewForInterfaceTypes instance = typesInAssembly instance |> Seq.filter isReactiveObjectType |> Seq.map (fun typeInfo -> viewForInterfaceType (typeInfo.AsType()))
    let findViewType (interfaceType: TypeInfo) instance = typesInAssembly instance |> Seq.tryFind (fun typeInfo -> interfaceType.IsAssignableFrom(typeInfo))

type AppBootstrapper<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel: unit -> IRoutableViewModel) =
    inherit ReactiveObject()
    member __.Bootstrap(screen: IScreen) =
        let dependencyResolver = Locator.CurrentMutable
        dependencyResolver.RegisterConstant(context, typeof<IUiContext>)
        dependencyResolver.RegisterConstant(platform, typeof<'TPlatform>)
        dependencyResolver.RegisterConstant(screen, typeof<IScreen>)
        platform.RegisterDependencies dependencyResolver
        let viewModelInstance = viewModel()
        for interfaceType in ViewReflection.viewForInterfaceTypes viewModelInstance do
            match ViewReflection.findViewType interfaceType viewModelInstance with
            | Some viewType -> dependencyResolver.Register((fun () -> Activator.CreateInstance(viewType.AsType())), interfaceType.AsType())
            | None -> ()
        viewModelInstance

type App<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel) =
    inherit Application()
    let mutable observerIndex = 0
    let navigationErrorObservers = new Dictionary<int, IObserver<exn>>()
    let router = new RoutingState()
    let handleException ex = 
        navigationErrorObservers.Values |> Seq.iter (fun obs -> obs.OnNext(ex))
    let bootstrapper = new AppBootstrapper<'TPlatform>(platform, context, viewModel)
    do 
        router.Navigate.ThrownExceptions.Subscribe(handleException) |> ignore
        router.NavigateAndReset.ThrownExceptions.Subscribe(handleException) |> ignore
        router.NavigateBack.ThrownExceptions.Subscribe(handleException) |> ignore
    member val UiContext = context with get
    member this.Screen = this :> IScreen
    member __.NavigationError = Observable.Create<exn>(fun (obs: IObserver<exn>) ->
        async {
            let index = Interlocked.Increment(ref observerIndex)
            navigationErrorObservers.[index] <- obs
            return Disposable.Create(fun () -> navigationErrorObservers.Remove(index) |> ignore)
        } |> Async.StartAsTask)
    member this.Init(theme: Theme) =
        let viewModelInstance = bootstrapper.Bootstrap(this)
        router.NavigateAndReset.Execute(viewModelInstance).Subscribe() |> ignore
        let navigationPage = new RoutedViewHost(Style = theme.Styles.NavigationPageStyle)
        navigationPage.On<Android>().SetBarHeight(200) |> ignore
        this.MainPage <- navigationPage
    override __.OnAppLinkRequestReceived uri = base.OnAppLinkRequestReceived uri; platform.HandleAppLinkRequest uri
    interface IScreen with member __.Router = router
