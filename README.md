# XamarinForms.Reactive.FSharp [![NuGet Status](https://img.shields.io/nuget/vpre/XamarinForms.Reactive.FSharp.svg)](https://www.nuget.org/packages/XamarinForms.Reactive.FSharp)
**A fluent interface for building MVVM-based Xamarin Forms apps using ReactiveUI and F#**

Using this package, you can create views directly in F# using a fluent interface. This is an alternative to XAML, avoiding the verboseness of XML, and bringing in the expressive elegance and efficiency of functional reactive programming.

The package is built on the excellent MVVM framework [ReactiveUI](http://reactiveui.net/).

### Getting Started

You will need to start by creating a shared configuration type, implementing `IConfiguration`:

```fs
module SharedConfiguration =
    let [<Literal>] AppName = "My App Name";
    type Configuration() =
        interface IConfiguration with
            member __.MobileServiceUri = None
            member __.AppName = AppName
```

Then you will have to implement `IPlatform` in your platform-specific projects.

#### Android

```fs
type DroidPlatform() =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface IPlatform with
        member __.GetMainPage() = new RoutedViewHost() :> Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName
```
#### iOS

```fs
type IosPlatform() =
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    interface IPlatform with
        member __.GetMainPage() = new ReactiveUI.XamForms.RoutedViewHost() :> Xamarin.Forms.Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.GetLocalFilePath fileName = localFilePath fileName
```

Optionally, to add platform-specific interface features, you can extend the interface `IPlatform`:

```fs
type ICustomPlatform = 
    inherit IPlatform
    abstract member TakePicture: unit -> unit
    abstract member DialNumber: string -> unit
```

and then implement `ICustomPlatform`, rather than `IPlatform`, in your `DroidPlatform` and/or `IosPlatform` classes.

You can register additional dependencies in the override of ```__.RegisterDependencies()``` (but don't forget to call down to the base implementation -- i.e. call `base.RegisterDependencies()` to implement the boilerplate registrations). The dependency resolver is provided by [Splat](https://github.com/paulcbetts/splat), which is used internally by [ReactiveUI](http://reactiveui.net/). You may be tempted to use your own favourite IoC provider. Don't. That will create unnecessary pain and confusion, for benefits that can best be described as questionable.

You can now set up your application in the normal way:

#### Android

```fs
type XamarinForms = Xamarin.Forms.Forms

[<Activity (Label = SharedConfiguration.AppName, MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsApplicationActivity()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        XamarinForms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let application = new App<ICustomPlatform>(new DroidPlatform() :> ICustomPlatform, new UiContext(this), new SharedConfiguration.Configuration(), createDashboardViewModel)
        this.LoadApplication application
```

#### iOS

```fs
type XamarinForms = Xamarin.Forms.Forms

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit FormsApplicationDelegate ()
    let createDashboardViewModel() = new DashboardViewModel() :> IRoutableViewModel
    override this.FinishedLaunching (app, options) =
        XamarinForms.Init()
        this.LoadApplication(new App<IPlatform>(new IosPlatform() :> IPlatform, new UiContext(this), new SharedConfiguration.Configuration(), createDashboardViewModel))
        base.FinishedLaunching(app, options)
```

Now you can define your ViewModels and Views.

### ViewModels

Each page should be coupled to its corresponding page ViewModel. To create a ViewModel, you need to derive from `PageViewModel` and implement `IRoutableViewModel`:

```fs
open System

open XamarinForms.Reactive.FSharp

open ReactiveUI

open LocatorDefaults

type DashboardViewModel(?host: IScreen) = 
    inherit PageViewModel()
    let host = LocateIfNone host
    member val Name = String.Empty with get, set
    member val DateOfBirth = DateTime.Parse("1990-01-01") with get, set
    member val PageTitle = "XamarinForms.Reactive.FSharp |> I <3"
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
```

### Views

And now you can create your view. Views use *themes* to create UI components. The Hello World view looks like this:

```fs
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms

open ViewHelpers

type DashboardView(theme: Theme) = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.DefaultTheme)
    override this.CreateContent() = theme.GenerateLabel() |> withLabelText "Hello World" :> View
```

Once you have set up the views and viewmodels, you don't have to worry about registering them with the dependency provider: this is done automatically in the default implementation of the platform's `RegisterDependencies()` method.

### Binding Views to ViewModels

To build more elaborate views, you will need to bind the view data to the corresponding viewmodel properties. This is achieved using the `withOneWayBinding` and `withTwoWayBinding` functions:

```fs
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms

open ViewHelpers

type DashboardView(theme: Theme) = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(DefaultTheme)
    override this.CreateContent() =
        theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "Auto"|], [|"Auto"; "*"|]) |> withRow(
            [|
                theme.GenerateTitle(fun l -> this.PageTitle <- l) 
                    |> withColumnSpan 2 
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.PageTitle @>, <@ fun (v: DashboardView) -> (v.PageTitle: Label).Text @>, id)
            |]) |> thenRow(
            [|
                theme.GenerateLabel() |> withLabelText("Your name")
                theme.GenerateEntry(fun e -> this.UserName <- e) 
                    |> withEntryPlaceholder "Enter your name here"
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Name @>, <@ fun (v: DashboardView) -> (v.UserName: Entry).Text @>, id, id)
            |]) |> thenRow(
            [|
                theme.GenerateLabel() |> withLabelText("Date of birth")
                theme.GenerateDatePicker(fun e -> this.UserDateOfBirth <- e) 
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.DateOfBirth @>, <@ fun (v: DashboardView) -> (v.UserDateOfBirth: DatePicker).Date @>, id, id)
            |]) |> thenRow(
            [|
                theme.GenerateButton(fun b -> this.SubmitButton <- b)
                    |> withColumnSpan 2
                    |> withCaption("Submit")
                    |> withHorizontalOptions LayoutOptions.End
            |])
            |> createFromRows :> View
    member val SubmitButton = Unchecked.defaultof<Button> with get, set
    member val PageTitle = Unchecked.defaultof<Label> with get, set
    member val UserName = Unchecked.defaultof<Entry> with get, set
    member val UserDateOfBirth = Unchecked.defaultof<DatePicker> with get, set
```

Note the `[<ParamArray>]` argument to the control generators: e.g. `theme.GenerateEntry(fun e -> this.UserName <- e)`. This allows you to assign the controls generated in the `CreateContent()` override to properties in your `View`, whose own properties can then be bound to corresponding properties in the ViewModel.

### Command Binding

Commands should be handled in the ViewModel. The correct way to set up and tear down commands in your ViewModel is using the `setUpCommands` and `TearDownCommands` overrides:

```fs
open System.Threading.Tasks
open System.Reactive.Linq
open System

open XamarinForms.Reactive.FSharp

open ReactiveUI

open LocatorDefaults

type DashboardViewModel(?host: IScreen) = 
    inherit PageViewModel()
    let host = LocateIfNone host
    let submitDetails (vm: DashboardViewModel) (_: Reactive.Unit) =
        async {
            // Save details to database; perform asynchronous online or offline actions
            return true
        } |> Async.StartAsTask
    member val Name = String.Empty with get, set
    member val DateOfBirth = DateTime.Parse("1990-01-01") with get, set
    member val PageTitle = "XamarinForms.Reactive.FSharp |> I <3"
    member val SubmitDetails = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    override this.SetUpCommands() =
        // The command itself is disposable, and so needs to be cleaned up at the end of its lifecycle. The easiest way to do this is to add it to the current PageDisposables collection.
        this.SubmitDetails <- submitDetails this |> ReactiveCommand.CreateFromTask |> ObservableExtensions.disposeWith this.PageDisposables
        // A ReactiveCommand is an IObservable, so based on the result of the submission we can perform further actions, such as navigation.
        this.SubmitDetails.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(fun _ -> this.DisplayAlertMessage({ Title = "Details Submitted"; Message = sprintf "Your name is %s and your date of birth is %s" this.Name ((this.DateOfBirth: DateTime).ToString("dd/MM/yyyy")); Accept = "OK" }) |> ignore)
            |> ObservableExtensions.disposeWith(this.PageDisposables) 
            |> ignore
    override this.TearDownCommands() =
        // We set the observables and subscriptions up, so it is our responsibility to dispose of them. The Clear() method on the PageDisposable collection achieves this because of the use of disposeWith in the SetUpCommands method.
        this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
```

Here, we are making extensive use of the `PageDisposables` member of the base class. `SetUpCommands` is triggered by the `OnAppearing` callback, and `TearDownCommands` is triggered by the `OnDisappearing` callback. In the world of [ReactiveUI](http://reactiveui.net/), commands are observables, which sets us up very cleanly for responsive, asynchronous architecture.

Once you have set the commands up in the ViewModels, you can hook them up to controls in your View using the `withCommandBinding` function:

```fs
type DashboardView(theme: Theme) = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.DefaultTheme)
    override this.CreateContent() =
        theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "Auto"|], [|"Auto"; "*"|]) |> withRow(
            ...
            [|
                theme.GenerateButton(fun b -> this.SubmitButton <- b)
                    |> withColumnSpan 2
                    |> withCaption("Submit")
                    |> withHorizontalOptions LayoutOptions.End
                    |> withCommandBinding (this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SubmitDetails @>, <@ fun (v: DashboardView) -> v.SubmitButton @>)
            |])
            |> createFromRows :> View
    member val SubmitButton = Unchecked.defaultof<Button> with get, set
    member val PageTitle = Unchecked.defaultof<Label> with get, set
    member val UserName = Unchecked.defaultof<Entry> with get, set
    member val UserDateOfBirth = Unchecked.defaultof<DatePicker> with get, set
```

### Why F# is Suited to MVVM

One of the advantages of F# over C# is conciseness. In XamarinForms.Reactive.FSharp, we have a simple class and interface for holding platform-specific context information. In C#, their representation is

```cs
public class UiContext : IUiContext
{
    public UiContext(object context)
    {
        Context = context;
    }

    public object Context { get; }
}

public interface IUiContext
{
    object Context { get; }
}
```

In F#, this becomes

```fs
type IUiContext = abstract Context: obj
type UiContext(context) = interface IUiContext with member __.Context = context
```

There are certain features contained in F# that look unlikely to be replicated in C#, such as type providers and units of measure; however, its main advantage lies in something it does not do: the F# compiler does not allow circular dependencies. This is discussed at length by Mark Seeman in http://blog.ploeh.dk/2015/04/15/c-will-eventually-get-all-f-features-right/. Suffice to say that the lack of circular dependencies serves to reduce cyclotomic complexity, thereby increasing code quality.

In the case of MVVM, however, the advantages go deeper. Out of the restriction on circular dependencies comes an implicit enforcement of the Model-View-ViewModel architecture.

![MVVM Architecture](https://i.stack.imgur.com/yDjEr.png "MVVM Architecture")

As can be seen from the diagram above, the ViewModel should be unaware of the View. Each ViewModel exists in its own world, exposing `Command` properties to the outside world, which can be triggered from within Views, but ViewModels cannot directly read or update the associated views. There are various benefits to this loosely coupled approach. It promotes reuse, and makes the ViewModels testable: their logic can be tested and verified independently from the way the views are set up.

The implementation of MVVM can often break down in production systems. Faced by a tight deadline, a programmer may well try to modify the View directly from the ViewModel. I've seen it done. It may solve the immediate problem, and allow the release to happen on time, but it breaks testability and introduces a cyclic dependency that may have grave unforeseen ramifications, resulting in infinite event loops and system crashes.

In the sample projects, this can't be done. All ViewModels are defined in the file `ViewModels.fs`, and all views are defined in `Views.fs`. The former comes before the latter in the sample projects. Because F# does not allow circular dependencies, the compiler will break if any of the `ViewModel`s try to reference their `View`, or any other `View` for that matter. *If you break the MVVM architecture, the code will not compile.*

Well that sounds good, but what if someone ignores the convention, and adds a `ViewModel` somewhere after its `View` in the codebase? Well then the compiler will break because  the `View` no longer recognises its `ViewModel`, and by convention, all of our views know about their viewmodels explicitly, because under the covers they implement the `IViewFor<MyViewModel>` interface. Of course, there may be a clever way around all of this, using F# augmentations or extensions, but the point is, this will be hard to do. It will be much easier, and crucially far less time-consuming, to stick to the MVVM architecture. Define your `ViewModel` in `ViewModels.fs`, and your view in `Views.fs`. Bind your view to the relevant `ViewModel` properties, and use TDD to test that your `ViewModel` sets its properties in a sensible, rational way.

