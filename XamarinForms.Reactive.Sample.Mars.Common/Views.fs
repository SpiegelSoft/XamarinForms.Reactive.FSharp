namespace XamarinForms.Reactive.Sample.Mars.Common

open System

open ReactiveUI.XamForms
open ReactiveUI

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms
open System.Collections
open System.Diagnostics

type PhotoSetView(theme) =
    inherit ContentPage<PhotoSetViewModel, PhotoSetView>(theme)
    member val Title = Unchecked.defaultof<Label> with get, set    
    member val Photos = Unchecked.defaultof<ListView> with get, set    
    new() = new PhotoSetView(Themes.XrfMars)
    override this.CreateContent(viewModelAdded) =
        let title = 
            theme.GenerateTitle<PhotoSetView>(this, <@ fun v -> v.Title @>)
            |> withOneWayBinding(this, <@ fun vm -> vm.Title @>, <@ fun v -> v.Title.Text @>, id)
            |> withMargin(new Thickness(12.0))
            |> withHorizontalOptions LayoutOptions.Center
        theme.GenerateListView<PhotoSetView>(this, <@ fun v -> v.Photos @>, ListViewCachingStrategy.RecycleElement)
        |> withUnevenRows
        |> withListViewHeader title
        |> withOneWayBinding (this, <@ fun vm -> vm.Photos @>, <@ fun v -> v.Photos.ItemsSource @>, fun c -> c :> IEnumerable)
        |> withCellTemplate(fun () -> new PhotoCell(theme))
        :> View

type PhotoManifestView(theme) =
    inherit ContentPage<PhotoManifestViewModel, PhotoManifestView>(theme)
    member val PhotoSets = Unchecked.defaultof<ListView> with get, set    
    member val SolFilter = Unchecked.defaultof<Entry> with get, set    
    member val FromEarthDate = Unchecked.defaultof<DatePicker> with get, set    
    member val ToEarthDate = Unchecked.defaultof<DatePicker> with get, set    
    member val Cameras = Unchecked.defaultof<Picker> with get, set
    new() = new PhotoManifestView(Themes.XrfMars)
    override this.CreateContent(viewModelAdded) =
        theme.GenerateGrid([|Auto; Auto; Auto; Auto; Auto; Star 1|], [|Auto; Star 1|])
            |> withRow(
                [|
                    theme.GenerateLabel() |> withAlignment LayoutOptions.Start LayoutOptions.Center |> withLabelText("Sol (Martian Day)")
                    theme.GenerateEntry<PhotoManifestView>(this, <@ fun v -> v.SolFilter @>) 
                        |> withHorizontalOptions LayoutOptions.End
                        |> withEntryPlaceholder "Sol number...."
                        |> withTwoWayBinding(this, <@ fun vm -> vm.State.SolFilter @>, <@ fun v -> v.SolFilter.Text @>, id, id)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withAlignment LayoutOptions.Start LayoutOptions.Center |> withLabelText("From Earth Date")
                    theme.GenerateDatePicker<PhotoManifestView>(this, <@ fun v -> v.FromEarthDate @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.MinEarthDate @>, <@ fun v -> v.FromEarthDate.MinimumDate @>, id)
                        |> withOneWayBinding(this, <@ fun vm -> vm.State.EndEarthDate @>, <@ fun v -> v.FromEarthDate.MaximumDate @>, id)
                        |> withTwoWayBinding(this, <@ fun vm -> vm.State.StartEarthDate @>, <@ fun v -> v.FromEarthDate.Date @>, id, id)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withAlignment LayoutOptions.Start LayoutOptions.Center |> withLabelText("To Earth Date")
                    theme.GenerateDatePicker<PhotoManifestView>(this, <@ fun v -> v.ToEarthDate @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.State.StartEarthDate @>, <@ fun v -> v.ToEarthDate.MinimumDate @>, id)
                        |> withOneWayBinding(this, <@ fun vm -> vm.MaxEarthDate @>, <@ fun v -> v.ToEarthDate.MaximumDate @>, id)
                        |> withTwoWayBinding(this, <@ fun vm -> vm.State.EndEarthDate @>, <@ fun v -> v.ToEarthDate.Date @>, id, id)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withAlignment LayoutOptions.Start LayoutOptions.Center |> withLabelText("Cameras")
                    theme.GeneratePicker<PhotoManifestView>(this, <@ fun v -> v.Cameras @>) |> withHorizontalOptions LayoutOptions.EndAndExpand
                        |> withOneWayBinding(this, <@ fun vm -> vm.Cameras @>, <@ fun v -> v.Cameras.ItemsSource @>, fun c -> c :> IList)
                        |> withPickerDisplayBinding <@ fun (camera: RoverCamera) -> camera.FullName @>
                        |> withTwoWayBinding(this, <@ fun vm -> vm.State.CameraIndex @>, <@ fun v -> v.Cameras.SelectedIndex @>, id, id)
                        |> withWidthRequest 300.0
                |])
            |> thenRow(
                [|
                    theme.GenerateBoxView() |> withHeightRequest 2.0 |> withColumnSpan 2
                |])
            |> thenRow(
                [|
                    theme.GenerateListView<PhotoManifestView>(this, <@ fun v -> v.PhotoSets @>, ListViewCachingStrategy.RecycleElement) 
                        |> withColumnSpan 2
                        |> withUnevenRows
                        |> withItemsSource(this.ViewModel.PhotoSets)
                        |> withOneWayBinding (this, <@ fun vm -> vm.PhotoSets @>, <@ fun v -> v.PhotoSets.ItemsSource @>, fun c -> c :> IEnumerable)
                        |> withCellTemplate(fun () -> new RoverSolCell(theme))
                |])
            |> createFromRows |> withPadding (new Thickness(18.0, 0.0)) :> View

type RoversView(theme) =
    inherit ContentPage<RoversViewModel, RoversView>(theme)
    member val Rovers = Unchecked.defaultof<ListView> with get, set    
    member val RefreshingRovers = Unchecked.defaultof<Grid> with get, set    
    member val RefreshingIndicator = Unchecked.defaultof<ActivityIndicator> with get, set    
    new() = new RoversView(Themes.XrfMars)
    override this.CreateContent(viewModelAdded) =
        theme.VerticalLayout() |> withBlocks([|
            theme.GenerateGrid<RoversView>(this, <@ fun (v: RoversView) -> v.RefreshingRovers @>, [Auto; Auto], [Auto]) |> withColumn([|
                theme.GenerateActivityIndicator<RoversView>(this, <@ fun v -> v.RefreshingIndicator @>) 
                |> withHorizontalOptions LayoutOptions.CenterAndExpand
                |> withOneWayBinding(this, <@ fun vm -> vm.Commands.RefreshingRovers @>, <@ fun v -> v.RefreshingIndicator.IsRunning @>, id)
                theme.GenerateLabel() |> withLabelText "Refreshing Rovers...." |> withHorizontalOptions LayoutOptions.CenterAndExpand
            |]) 
            |> createFromColumns
            |> withOneWayBinding (this, <@ fun vm -> vm.Commands.RefreshingRovers @>, <@ fun v -> v.RefreshingRovers.IsVisible @>, id)
            |> withAlignment LayoutOptions.Center LayoutOptions.CenterAndExpand :> View
            theme.GenerateListView<RoversView>(this, <@ fun v -> v.Rovers @>, ListViewCachingStrategy.RetainElement)
            |> withUnevenRows
            |> withOneWayBinding (this, <@ fun vm -> vm.Commands.RefreshingRovers @>, <@ fun v -> v.Rovers.IsVisible @>, not)
            |> withOneWayBinding (this, <@ fun vm -> vm.Rovers @>, <@ fun v -> v.Rovers.ItemsSource @>, fun c -> c :> IEnumerable)
            |> withTwoWayBinding (this, <@ fun vm -> vm.State.SelectedRover @>, <@ fun v -> v.Rovers.SelectedItem @>, (fun r -> r :> obj), (fun r -> r :> obj :?> Rover))
            |> withCellTemplate(fun () -> new RoverCell(theme))
        |]) :> View
