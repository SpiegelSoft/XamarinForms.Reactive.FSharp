namespace XamarinForms.Reactive.Sample.Mars.Common

open XamarinForms.Reactive.FSharp

open ViewHelpers
open Themes

open Xamarin.Forms

open System
open System.Reactive.Disposables
open Splat

open FFImageLoading.Forms

type PhotoCell(theme: Theme) =
    inherit ViewCell()
    static let nullableTrue = Nullable<bool>(true)
    let pageWidth = Application.Current.MainPage.Width
    let imageWidth = pageWidth - 24.0
    let photoImage = new CachedImage(Aspect = Aspect.AspectFill, InvalidateLayoutAfterLoaded = nullableTrue) 
    let title = theme.GenerateTitle() |> withAlignment LayoutOptions.CenterAndExpand LayoutOptions.FillAndExpand |> withMargin(new Thickness(12.0))
    let separator = theme.GenerateBoxView() |> withHeightRequest 2.0 |> withMargin (new Thickness(0.0, 8.0))
    let imageContainer = 
        theme.VerticalLayout() 
        |> withBlocks([|photoImage|])
        |> withAlignment LayoutOptions.FillAndExpand LayoutOptions.FillAndExpand
        |> withMargin (new Thickness(0.0, 0.0, 0.0, 8.0))
    let content = 
        theme.VerticalLayout() 
        |> withBlocks([| title; imageContainer; separator |])
        |> withMargin (new Thickness(12.0))
        |> withSpacing 0.0
    let disposables = new CompositeDisposable()
    do base.View <- content
    let updateFromBindingContext (photo: Photo) =
        disposables.Clear()
        photoImage.Source <- ImageSource.FromUri(new Uri(photo.ImgSrc))
        title.Text <- sprintf "Photo %i" photo.Id
        photoImage.Success.Subscribe(fun args ->
            let aspectRatio = float args.ImageInformation.CurrentWidth / float args.ImageInformation.CurrentHeight
            photoImage.HeightRequest <- imageWidth / aspectRatio)
            |> disposables.Add
    let clearFields() =
        disposables.Clear()
        photoImage.Source <- Unchecked.defaultof<ImageSource>
        title.Text <- String.Empty
    override this.OnBindingContextChanged() =
        base.OnBindingContextChanged()
        match this.BindingContext with | :? Photo as photo -> updateFromBindingContext photo | _ -> clearFields()

type RoverSolCell(theme: Theme) =
    inherit ViewCell()
    let solSpan = theme.GenerateSpan()
    let earthDateSpan = theme.GenerateSpan()
    let solLabel = 
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Sol: " |> withSpanTextColor Color.White
            solSpan
        ]) |> withAlignment LayoutOptions.FillAndExpand LayoutOptions.FillAndExpand
    let earthDateLabel = 
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Earth Date: " |> withSpanTextColor Color.White
            earthDateSpan
        ]) |> withAlignment LayoutOptions.EndAndExpand LayoutOptions.FillAndExpand
    let buttonStack = theme.VerticalLayout() |> withColumnSpan 2 |> withHorizontalOptions LayoutOptions.Center
    let disposables = new CompositeDisposable()
    let updateFromBindingContext (photoSet: RoverSolPhotoSet) =
        disposables.Clear()
        let createButton camera =
            theme.GenerateButton()
            |> withHorizontalOptions LayoutOptions.Center
            |> withCaption (RoverCameras.all.[camera].FullName)
            |> withButtonCommand photoSet.Command
            |> withButtonCommandParameter camera
        let buttons = photoSet.VisibleCameras |> Array.map createButton
        buttonStack.Children.Clear()
        buttons |> Array.iter buttonStack.Children.Add
        earthDateSpan.Text <- match photoSet.EarthDate > DateTime.MinValue with | true -> photoSet.EarthDate.ToString("d") | false -> "Unavailable"
        solSpan.Text <- photoSet.Sol.ToString("N0")
    let clearFields() =
        solLabel.Text <- String.Empty
        earthDateLabel.Text <- String.Empty
        buttonStack.Children.Clear()
        disposables.Clear()
    let content = 
        theme.GenerateGrid([Auto; Auto], [Star 1; Star 1])
        |> withRow([|solLabel; earthDateLabel|])
        |> thenRow([|buttonStack|])
        |> createFromRows
    do base.View <- content
    override this.OnBindingContextChanged() =
        base.OnBindingContextChanged()
        match this.BindingContext with 
        | :? RoverSolPhotoSet as photoSet -> updateFromBindingContext photoSet 
        | _ -> clearFields()

type RoverCell(theme: Theme) =
    inherit ViewCell()
    static let nullableTrue = Nullable<bool>(true)
    let pageWidth = Application.Current.MainPage.Width
    let imageWidth = pageWidth - 24.0
    let headlineImage = new CachedImage(Aspect = Aspect.AspectFill, InvalidateLayoutAfterLoaded = nullableTrue) 
    let title = theme.GenerateTitle() |> withAlignment LayoutOptions.CenterAndExpand LayoutOptions.FillAndExpand |> withMargin(new Thickness(12.0))
    let separator1 = theme.GenerateBoxView() |> withHeightRequest 2.0 |> withMargin (new Thickness(0.0, 8.0))
    let separator2 = theme.GenerateBoxView() |> withHeightRequest 2.0 |> withMargin (new Thickness(0.0, 8.0))
    let launchDateSpan = theme.GenerateSpan()
    let landingDateSpan = theme.GenerateSpan()
    let totalPhotosSpan = theme.GenerateSpan()
    let latestPhotoSpan = theme.GenerateSpan()
    let maxSolSpan = theme.GenerateSpan()
    let launchDateLabel = 
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Launched: " |> withSpanTextColor Color.White
            launchDateSpan
        ]) |> withAlignment LayoutOptions.FillAndExpand LayoutOptions.FillAndExpand
    let landingDateLabel = 
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Landed: " |> withSpanTextColor Color.White
            landingDateSpan
        ]) |> withAlignment LayoutOptions.FillAndExpand LayoutOptions.FillAndExpand
    let totalPhotosLabel =
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Total Photos: " |> withSpanTextColor Color.White
            totalPhotosSpan
        ])
    let latestPhotoLabel =
        theme.GenerateLabel() |> withFormattedText([
            theme.GenerateSpan() |> withSpanText "Latest Photo: " |> withSpanTextColor Color.White
            latestPhotoSpan
            theme.GenerateSpan() |> withSpanText " (Sol " |> withSpanTextColor Color.White
            maxSolSpan
            theme.GenerateSpan() |> withSpanText ")" |> withSpanTextColor Color.White
        ])
    let imageContainer = 
        theme.VerticalLayout() 
        |> withBlocks([|headlineImage|])
        |> withAlignment LayoutOptions.FillAndExpand LayoutOptions.FillAndExpand
        |> withMargin (new Thickness(0.0, 0.0, 0.0, 8.0))
    let content = 
        theme.VerticalLayout() 
        |> withBlocks([| title; imageContainer; separator1; launchDateLabel; landingDateLabel; totalPhotosLabel; latestPhotoLabel; separator2 |])
        |> withMargin (new Thickness(12.0))
        |> withSpacing 0.0
    let disposables = new CompositeDisposable()
    let platform = Locator.Current.GetService<IMarsPlatform>()
    do base.View <- content
    let updateFromBindingContext (rover: Rover) =
        disposables.Clear()
        let dateString (date: DateTime) = date.ToString("D")
        headlineImage.Source <- rover.PhotoManifest.Name |> Rovers.imagePath |> platform.GetHeadlineImage
        title.Text <- rover.PhotoManifest.Name
        launchDateSpan.Text <- rover.PhotoManifest.LaunchDate |> dateString
        landingDateSpan.Text <- rover.PhotoManifest.LandingDate |> dateString
        totalPhotosSpan.Text <- rover.PhotoManifest.TotalPhotos.ToString("N0")
        latestPhotoSpan.Text <- rover.PhotoManifest.MaxDate.ToString("D")
        maxSolSpan.Text <- rover.PhotoManifest.MaxSol.ToString("N0")
        headlineImage.Success.Subscribe(fun args ->
            let aspectRatio = float args.ImageInformation.CurrentWidth / float args.ImageInformation.CurrentHeight
            headlineImage.HeightRequest <- imageWidth / aspectRatio)
            |> disposables.Add
    let clearFields() =
        disposables.Clear()
        headlineImage.Source <- Unchecked.defaultof<ImageSource>
        title.Text <- String.Empty
        launchDateSpan.Text <- String.Empty
        landingDateSpan.Text <- String.Empty
        totalPhotosSpan.Text <- String.Empty
    override this.OnBindingContextChanged() =
        base.OnBindingContextChanged()
        match this.BindingContext with | :? Rover as rover -> updateFromBindingContext rover | _ -> clearFields()
