﻿namespace XamarinForms.Reactive.FSharp

open System.Reflection
open System.Threading
open System

open ReactiveUI

module ViewReflection =
    let private reactiveObjectType = typeof<ReactiveObject>
    let private isReactiveObjectType = reactiveObjectType.IsAssignableFrom
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

type HostingPage() =
    inherit RoutedViewHost()
    member val PageDisposables = new CompositeDisposable()
    override this.OnDisappearing() =
        base.OnDisappearing()
        this.PageDisposables.Clear()

type App<'TPlatform when 'TPlatform :> IPlatform>(platform: 'TPlatform, context, viewModel) =
    inherit Application()
    let mutable observerIndex = 0
    let navigationErrorObservers = new Dictionary<int, IObserver<exn>>()
    let router = new RoutingState()
    let handleException ex = navigationErrorObservers.Values |> Seq.iter (fun obs -> obs.OnNext(ex))
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
    member this.Init() =
        let viewModelInstance = bootstrapper.Bootstrap(this)
        let navigationPage = new HostingPage()
        navigationPage.PushAsync(new FrontPage(router, viewModelInstance, ViewModel = new FrontPageViewModel())).Wait()
        this.MainPage <- navigationPage
        navigationPage.Popped.Subscribe(fun eventArgs ->
            match eventArgs.Page :> obj with
            | :? IContentView as view -> view.PagePopped()
            | _ -> 0 |> ignore) |> navigationPage.PageDisposables.Add
    interface IScreen with member __.Router = router
