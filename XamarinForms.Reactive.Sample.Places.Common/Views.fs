namespace XamarinForms.Reactive.Sample.Places.Common

open System

open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp
open Xamarin.Forms

open ViewHelpers
open Themes

type MarkedLocation(viewModel: MarkerViewModel) =
    inherit GeographicPin(viewModel.Location)
    member val ViewModel = viewModel

module PinConversion = let toPin (marker: MarkerViewModel) = new MarkedLocation(marker)

type PlacesView(theme: Theme) =
    inherit ContentPage<PlacesViewModel, PlacesView>(theme)
    override this.OnContentCreated() =
        base.OnContentCreated()
        this.ViewModel.InitialisePageCommand.Execute().Subscribe() |> ignore
    override this.CreateContent() = 
        theme.GenerateScrollView()
            |> withContent(
                theme.GenerateGrid([|"Auto"; "*"; "Auto"|], [|"*"|]) |> withRow(
                    [|
                        theme.VerticalLayout()
                            |> withAlignment LayoutOptions.Center LayoutOptions.Center
                            |> withBlocks(
                                [|
                                    theme.GenerateTitle(fun l -> this.Title <- l) |> withHorizontalOptions LayoutOptions.Center |> withLabelText "Google Places API"
                                    theme.GenerateSubtitle() |> withHorizontalOptions LayoutOptions.Center |> withLabelText("Search for places known by Google")
                                |])
                    |]) |> thenRow(
                    [|
                        theme.GenerateMap<MarkedLocation>(fun m -> this.Map <- m)
                            |> withTwoWayBinding(this, <@ fun (vm: PlacesViewModel) -> vm.Location @>, <@ fun (v:PlacesView) -> (v.Map: GeographicMap<MarkedLocation>).Center @>, id, id)
                            |> withTwoWayBinding(this, <@ fun (vm: PlacesViewModel) -> vm.Radius @>, <@ fun (v:PlacesView) -> (v.Map: GeographicMap<MarkedLocation>).Radius @>, id, id)
                            |> withPinBinding(this.ViewModel.Markers, PinConversion.toPin)
                    |]) |> thenRow(
                    [|
                        theme.GenerateMapSearchBar(fun sb -> this.SearchBar <- sb)
                        |> withSearchBarPlaceholder "Type in a place name...."
                    |]) |> createFromRows :> View
            ) :> View
    new() = new PlacesView(CustomTheme)
    member val Title = Unchecked.defaultof<Label> with get, set
    member val SearchBar = Unchecked.defaultof<MapSearchBar> with get, set
    member val Map = Unchecked.defaultof<GeographicMap<MarkedLocation>> with get, set

