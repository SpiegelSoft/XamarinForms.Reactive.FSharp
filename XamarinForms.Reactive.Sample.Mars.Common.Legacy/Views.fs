namespace XamarinForms.Reactive.Sample.Mars.Common

open System

open ReactiveUI.XamForms
open ReactiveUI

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms

type RoverFrontPage(vm, theme) =
    inherit ContentView<PhotoManifestViewModel>(theme)
    do base.ViewModel <- vm
    member val PhotoSets = Unchecked.defaultof<ListView> with get, set    
    member val Title = Unchecked.defaultof<Label> with get, set    
    member val LaunchDate = Unchecked.defaultof<Label> with get, set    
    member val LandingDate = Unchecked.defaultof<Label> with get, set    
    member val MaxSol = Unchecked.defaultof<Label> with get, set    
    member val TotalPhotos = Unchecked.defaultof<Label> with get, set    
    member val Photo = Unchecked.defaultof<Image> with get, set
    override this.CreateContent() =
        let dateString (d:DateTime) = d.ToString("dd-MMM-yyyy")
        theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "Auto"; "Auto"; "*"|], [|"Auto"; "*"|])
            |> withRow(
                [|
                    theme.GenerateTitle(this, <@ fun (v: RoverFrontPage) -> v.Title @>) |> withColumnSpan 2
                        |> withHorizontalOptions LayoutOptions.Center
                        |> withOneWayBinding(this, <@ fun vm -> vm.RoverName @>, <@ fun v -> v.Title.Text @>, id)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withHorizontalOptions LayoutOptions.Start |> withLabelText("Launch Date")
                    theme.GenerateLabel(this, <@ fun (v: RoverFrontPage) -> v.LaunchDate @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.LaunchDate @>, <@ fun v -> v.LaunchDate.Text @>, dateString)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withHorizontalOptions LayoutOptions.Start |> withLabelText("Landing Date")
                    theme.GenerateLabel(this, <@ fun (v: RoverFrontPage) -> v.LandingDate @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.LandingDate @>, <@ fun v -> v.LandingDate.Text @>, dateString)
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withHorizontalOptions LayoutOptions.Start |> withLabelText("Maximum Sol")
                    theme.GenerateLabel(this, <@ fun (v: RoverFrontPage) -> v.MaxSol @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.MaxSol @>, <@ fun v -> v.MaxSol.Text @>, fun s -> s.ToString())
                |])
            |> thenRow(
                [|
                    theme.GenerateLabel() |> withHorizontalOptions LayoutOptions.Start |> withLabelText("Total Photos")
                    theme.GenerateLabel(this, <@ fun (v: RoverFrontPage) -> v.TotalPhotos @>) |> withHorizontalOptions LayoutOptions.End
                        |> withOneWayBinding(this, <@ fun vm -> vm.TotalPhotos @>, <@ fun v -> v.TotalPhotos.Text @>, fun s -> s.ToString())
                |])
            |> thenRow(
                [|
                    theme.GenerateListView(this, <@ fun (v: RoverFrontPage) -> v.PhotoSets @>, ListViewCachingStrategy.RecycleElement) 
                        |> withColumnSpan 2
                        |> withItemsSource(this.ViewModel.PhotoSet)
                        |> withImageCellTemplate(fun () -> 
                            let mutable imageCell = Unchecked.defaultof<ImageCell> 
                            theme.GenerateImageCell(fun i -> imageCell <- i) 
                                |> withOneWayElementBinding(imageCell, <@ fun (vm: RoverSolPhotoSet) -> vm.Sol @>, ImageCell.TextProperty, fun s -> sprintf "Sol %i" s)
                                |> withOneWayElementBinding(imageCell, <@ fun (vm: RoverSolPhotoSet) -> vm.Description @>, ImageCell.DetailProperty, id)
                                |> withOneWayElementBinding(imageCell, <@ fun (vm: RoverSolPhotoSet) -> vm.ImageSource @>, ImageCell.ImageSourceProperty, ImageSource.FromFile))
                |])
            |> createFromRows |> withPadding (new Thickness(18.0, 0.0)) :> View

type PhotoSetView(theme) =
    inherit TabbedPage<PhotoSetViewModel>(theme)
    new() = new PhotoSetView(Themes.XrfMars)
    override this.OnContentCreated() =
        this.ViewModel.RefreshRovers.Execute(this.ViewModel).Subscribe() |> ignore
    override this.CreateContent() =
        dict [
            ("Curiosity", fun () -> RoverFrontPage(this.ViewModel.Curiosity, theme) :> View)
            ("Spirit", fun () -> RoverFrontPage(this.ViewModel.Spirit, theme) :> View)
            ("Opportunity", fun p -> RoverFrontPage(this.ViewModel.Opportunity, theme) :> View)
        ]
