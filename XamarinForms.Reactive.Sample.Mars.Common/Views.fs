namespace XamarinForms.Reactive.Sample.Mars.Common

open System.Reactive.Linq
open System
open ReactiveUI.XamForms
open ReactiveUI
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp
open Xamarin.Forms
open XamarinForms.Reactive.FSharp.ViewHelpers

type RoverFrontPage(vm, theme) =
    inherit ContentView<PhotoManifestViewModel>(theme)
    do base.ViewModel <- vm
    member val PhotoSets = Unchecked.defaultof<ListView> with get, set    
    member val Title = Unchecked.defaultof<Label> with get, set    
    member val LaunchDate = Unchecked.defaultof<Label> with get, set    
    member val LandingDate = Unchecked.defaultof<Label> with get, set    
    member val Photo = Unchecked.defaultof<Image> with get, set
    override this.CreateContent() =
        let dateString (d:DateTime) = d.ToString("dd-MMM-yyyy")
        theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "*"|], [|"Auto"; "*"|])
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
                    theme.GenerateListView(this, <@ fun (v: RoverFrontPage) -> v.PhotoSets @>) |> withColumnSpan 2
                        |> withItemsSource(this.ViewModel.PhotoSet)
                        |> withItemTemplate(fun () -> 
                            let mutable solNumberLabel = Unchecked.defaultof<Label> 
                            let mutable totalPhotosLabel = Unchecked.defaultof<Label> 
                            let mutable camerasLabel = Unchecked.defaultof<Label> 
                            let sol s = 
                                sprintf "Sol %i" s
                            theme.GenerateGrid([|"Auto"; "Auto"|], [|"Auto"; "*"|]) |> withColumn(
                                [|
                                    theme.GenerateLabel(fun l -> solNumberLabel <- l)
                                        |> withOneWayElementBinding(solNumberLabel, <@ fun (vm: RoverSolPhotoSet) -> vm.Sol @>, Label.TextProperty, sol)
                                        |> withRowSpan 2
                                |]) |> thenColumn(
                                [|
                                    theme.GenerateLabel(fun l -> totalPhotosLabel <- l)
                                        |> withOneWayElementBinding(totalPhotosLabel, <@ fun (vm: RoverSolPhotoSet) -> vm.TotalPhotos @>, Label.TextProperty, fun s -> sprintf "%i Photo%s" s (if s > 1 then "s" else ""))
                                    theme.GenerateLabel(fun l -> camerasLabel <- l)
                                        |> withOneWayElementBinding(camerasLabel, <@ fun (vm: RoverSolPhotoSet) -> vm.Cameras @>, Label.TextProperty, fun c -> String.Join(", ", c))
                                |])|> createFromColumns :> View)
                |])
            |> createFromRows |> withPadding (new Thickness(18.0, 0.0)) :> View

type PhotoSetView(theme) =
    inherit TabbedPage<PhotoSetViewModel>(theme)
    new() = new PhotoSetView(DefaultTheme)
    override this.OnContentCreated() =
        this.ViewModel.RefreshRovers.Execute(this.ViewModel).Subscribe() |> ignore
    override this.CreateContent() =
        dict [
            ("Curiosity", fun () -> RoverFrontPage(this.ViewModel.Curiosity, theme) :> View)
            ("Spirit", fun () -> RoverFrontPage(this.ViewModel.Spirit, theme) :> View)
            ("Opportunity", fun p -> RoverFrontPage(this.ViewModel.Opportunity, theme) :> View)
        ]
