namespace XamarinForms.Reactive.Sample.Mars.Common

open System
open ReactiveUI.XamForms
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp
open Xamarin.Forms
open XamarinForms.Reactive.FSharp.ViewHelpers

type PhotoSetView(theme: Theme) =
    inherit TabbedPage<PhotoSetViewModel>(theme)
    new() = new PhotoSetView(DefaultTheme)
    override this.OnContentCreated() =
        this.ViewModel.RefreshRovers.Execute(this.ViewModel).Subscribe() |> ignore
    override this.CreateContent() =
        dict [
            ("Curiosity", fun p -> 
                theme.GenerateGrid([|"Auto"; "*"|], [|"2*"; "*"|]) 
                    |> withRow(
                        [|
                            theme.GeneratePicker(fun p -> this.Cameras <- p) 
                                //|> withPickerItemsSource(RoverCameras)
                                //|> withPickerDisplayBinding(<@ fun (country: LocalisedCountry) -> country.Name @>)
                                //|> withTwoWayBinding(this, 
                                //    <@ fun (vm: NewVenueViewModel) -> vm.AddressCountry @>, 
                                //    <@ fun (v: NewVenueView) -> (v.Country: Picker).SelectedItem @>, 
                                //(fun (vmp: LocalisedCountry) -> vmp :> obj), 
                                //(fun (vp: obj) -> vp :?> LocalisedCountry))
                                |> withTwoWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.CameraIndex @>, <@ fun (v: PhotoSetView) -> (v.Cameras: Picker).SelectedIndex @>, id, id)
                            theme.GenerateButton(fun b -> this.FetchPhotosButton <- b)
                                |> withCaption("View Gallery")
                                |> withCommandBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.FetchPhotos @>, <@ fun (v: PhotoSetView) -> v.FetchPhotosButton @>)
                        |])
                    |> thenRow(
                        [|
                            theme.GenerateImage(fun i -> this.Photo <- i) |> withColumnSpan 2
                                |> withOneWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.ImageUrl @>, <@ fun (v: PhotoSetView) -> (v.Photo: Image).Source @>, ImageSource.FromUri)
                                |> withSource(ImageSource.FromUri(Mars.genericImage)) 
                        |])
                    |> createFromRows :> View)
            ("Spirit", fun p -> 
                theme.GenerateGrid([|"Auto"; "*"|], [|"2*"; "*"|]) 
                    |> withRow(
                        [|
                            theme.GeneratePicker(fun p -> this.Cameras <- p) 
                                //|> withPickerItems RoverCameras.names
                                |> withTwoWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.CameraIndex @>, <@ fun (v: PhotoSetView) -> (v.Cameras: Picker).SelectedIndex @>, id, id)
                            theme.GenerateButton(fun b -> this.FetchPhotosButton <- b)
                                |> withCaption("View Gallery")
                                |> withCommandBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.FetchPhotos @>, <@ fun (v: PhotoSetView) -> v.FetchPhotosButton @>)
                        |])
                    |> thenRow(
                        [|
                            theme.GenerateImage(fun i -> this.Photo <- i) |> withColumnSpan 2
                                |> withOneWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.ImageUrl @>, <@ fun (v: PhotoSetView) -> (v.Photo: Image).Source @>, ImageSource.FromUri)
                                |> withSource(ImageSource.FromUri(Mars.genericImage)) 
                        |])
                    |> createFromRows :> View)
            ("Opportunity", fun p -> 
                theme.GenerateGrid([|"Auto"; "*"|], [|"2*"; "*"|]) 
                    |> withRow(
                        [|
                            theme.GeneratePicker(fun p -> this.Cameras <- p) 
                                //|> withPickerItems RoverCameras.names
                                |> withTwoWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.CameraIndex @>, <@ fun (v: PhotoSetView) -> (v.Cameras: Picker).SelectedIndex @>, id, id)
                            theme.GenerateButton(fun b -> this.FetchPhotosButton <- b)
                                |> withCaption("View Gallery")
                                |> withCommandBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.FetchPhotos @>, <@ fun (v: PhotoSetView) -> v.FetchPhotosButton @>)
                        |])
                    |> thenRow(
                        [|
                            theme.GenerateImage(fun i -> this.Photo <- i) |> withColumnSpan 2
                                |> withOneWayBinding(this, <@ fun (vm: PhotoSetViewModel) -> vm.ImageUrl @>, <@ fun (v: PhotoSetView) -> (v.Photo: Image).Source @>, ImageSource.FromUri)
                                |> withSource(ImageSource.FromUri(Mars.genericImage)) 
                        |])
                    |> createFromRows :> View)
        ]
    member val Cameras = Unchecked.defaultof<Picker> with get, set    
    member val FetchPhotosButton = Unchecked.defaultof<Button> with get, set
    member val Photo = Unchecked.defaultof<Image> with get, set
