namespace XamarinForms.Reactive.FSharp

open System

open Xamarin.Forms.Maps
open Xamarin.Forms

open ReactiveUI

open Splat

open ExpressionConversion

module LocatorDefaults =
    let LocateIfNone(arg : 'a option) =
        match arg with
        | None -> Locator.Current.GetService<'a>()
        | Some a -> a

type HyperlinkLabel() =
    inherit Label()
    member this.AddCommand command = this.GestureRecognizers.Add(new TapGestureRecognizer(Command = command))

module ViewHelpers =
    open Microsoft.FSharp.Quotations
    let withTwoWayBinding(viewModel: 'vm, view, viewModelProperty: Expr<'vm -> 'vmp>, viewProperty, vmToViewConverter, viewToVmConverter) element = 
        view.Bind(viewModel, toLinq viewModelProperty, toLinq viewProperty, null, fun x -> vmToViewConverter(x), fun x -> viewToVmConverter(x)) |> ignore
        element
    let withOneWayBinding(viewModel, view, viewModelProperty, viewProperty, selector) element = 
        view.OneWayBind(viewModel, toLinq viewModelProperty, toLinq viewProperty, fun x -> selector(x)) |> ignore
        element
    let withCommandBinding(viewModel, view, viewModelCommand, controlProperty) element = 
        view.BindCommand(viewModel, toLinq viewModelCommand, toLinq controlProperty) |> ignore
        element
    let withPinBinding(markers, markerToPin) (element: GeographicMap) = element.BindPinsToCollection(markers, markerToPin); element
    let withHyperlinkCommand command (element: #HyperlinkLabel) = element.AddCommand command; element
    let withHorizontalOptions options (element: #View) = element.HorizontalOptions <- options; element
    let withVerticalOptions options (element: #View) = element.VerticalOptions <- options; element
    let withHeightRequest request (element: #View) = element.HeightRequest <- request; element
    let withWidthRequest request (element: #View) = element.WidthRequest <- request; element
    let withHeightAndWidthRequest (height, width) (element: #View) = element |> withHeightRequest height |> withWidthRequest width
    let withAutomationId id (element: #View) = element.AutomationId <- id; element
    let withAlignment horizontalOptions verticalOptions element = element |> withHorizontalOptions horizontalOptions |> withVerticalOptions verticalOptions
    let withMargin margin (element: #View) = element.Margin <- margin; element
    let withSource source (element: Image) = element.Source <- source; element
    let withPadding padding (element: #Layout) = element.Padding <- padding; element
    let withCaption text (element: #Button) = element.Text <- text; element
    let withText text (element: #Entry) = element.Text <- text; element
    let withContent text (element: #Label) = element.Text <- text; element
    let withStyle style (element: #View) = element.Style <- style; element
    let withKeyboard keyboard (element: #InputView) = element.Keyboard <- keyboard; element
    let withSearchCommand command (element: SearchBar) = element.SearchCommand <- command; element
    let withEntryPlaceholder placeholder (element: #Entry) = element.Placeholder <- placeholder; element
    let withSearchBarPlaceholder placeholder (element: #SearchBar) = element.Placeholder <- placeholder; element
    let withSpacing spacing (layout: StackLayout) = layout.Spacing <- spacing; layout
    let withFontAttributes fontAttributes (element: #Label) = element.FontAttributes <- fontAttributes; element
    let withBackgroundColor color (element: #View) = element.BackgroundColor <- color; element
    let withEffect effectId (element: #Element) = element.Effects.Add(Effect.Resolve(effectId)); element     
    let withRoutingEffect (effect: #RoutingEffect) (element: #Element) = element.Effects.Add(effect); element     

module Themes =
    let withBlocks (views:View seq) (stackLayout: StackLayout) = (for view in views do stackLayout.Children.Add(view)); stackLayout
    let private gridLengthTypeConverter = new GridLengthTypeConverter()
    let private toGridLength text = gridLengthTypeConverter.ConvertFromInvariantString(text) :?> GridLength
    type RowCreation =
        {
            RowCount: int
            Grid: Grid
        }
    type ColumnCreation =
        {
            ColumnCount: int
            Grid: Grid
        }
    type GridCreation =
        {
            Grid: Grid
            RowCount: int
            ColumnCount: int
        }

    let private elementNoun i = if i = 1 then "element" else "elements"
    let private columnNoun i = if i = 1 then "column" else "columns"
    let private rowNoun i = if i = 1 then "row" else "rows"
    let private verb i = if i = 1 then "was" else "were"
    let rec private addRow rowCount (grid: Grid) (row: View[]) =
        if grid.ColumnDefinitions.Count <> row.Length then 
            let specifiedColumnCount = grid.ColumnDefinitions.Count
            raise <| ArgumentException(sprintf "You have tried to add a row with %i %s to a grid with %i %s." row.Length (elementNoun row.Length) specifiedColumnCount (columnNoun specifiedColumnCount), "row")
        for index = 0 to row.Length - 1 do
            Grid.SetRow(row.[index], rowCount)
            Grid.SetColumn(row.[index], index)
            grid.Children.Add row.[index]
        let newRowCount = rowCount + 1
        { RowCreation.RowCount = newRowCount; Grid = grid }
    let rec private addColumn columnCount (grid: Grid) (column: View[]) =
        if grid.RowDefinitions.Count <> column.Length then 
            let specifiedRowCount = grid.RowDefinitions.Count
            raise <| ArgumentException(sprintf "You have tried to add a column with %i %s to a grid with %i %s." column.Length (elementNoun column.Length) specifiedRowCount (rowNoun specifiedRowCount), "column")
        for index = 0 to column.Length - 1 do
            Grid.SetRow(column.[index], index)
            Grid.SetColumn(column.[index], columnCount)
            grid.Children.Add column.[index]
        let newColumnCount = columnCount + 1
        { ColumnCreation.ColumnCount = newColumnCount; Grid = grid }
    let private setUpGrid (grid: Grid) (rowDefinitions, columnDefinitions) =
        for rowDefinition in rowDefinitions do grid.RowDefinitions.Add(new RowDefinition(Height = toGridLength rowDefinition))
        for columnDefinition in columnDefinitions do grid.ColumnDefinitions.Add(new ColumnDefinition(Width = toGridLength columnDefinition))
        { Grid = grid; RowCount = grid.RowDefinitions.Count; ColumnCount = grid.ColumnDefinitions.Count }
    let withRow (views: View[]) (gridCreation: GridCreation) = addRow 0 gridCreation.Grid views
    let withColumn (views: View[]) (gridCreation: GridCreation) = addColumn 0 gridCreation.Grid views
    let thenRow (views: View[]) (rowCreation: RowCreation) = addRow rowCreation.RowCount rowCreation.Grid views
    let thenColumn (views: View[]) (columnCreation: ColumnCreation) = addColumn columnCreation.ColumnCount columnCreation.Grid views
    let createFromRows (rowCreation: RowCreation) = 
        let grid = rowCreation.Grid
        let specifiedRowCount, actualRowCount = grid.RowDefinitions.Count, rowCreation.RowCount
        if specifiedRowCount <> actualRowCount then raise <| ArgumentException(sprintf "You have tried to add %i %s to a grid for which %i %s %s specified." actualRowCount (rowNoun actualRowCount) specifiedRowCount (rowNoun specifiedRowCount) (verb specifiedRowCount))
        grid

    let createFromColumns (columnCreation: ColumnCreation) = 
        let grid = columnCreation.Grid
        let specifiedColumnCount, actualColumnCount = grid.ColumnDefinitions.Count, columnCreation.ColumnCount
        if specifiedColumnCount <> actualColumnCount then raise <| ArgumentException(sprintf "You have tried to add %i %s to a grid for which %i %s %s specified." actualColumnCount (columnNoun actualColumnCount) specifiedColumnCount (columnNoun specifiedColumnCount) (verb specifiedColumnCount))
        grid
    
    type Styles =
        {
            BackgroundColor: Color
            SeparatorColor: Color
            LabelStyle: Style
            HyperlinkStyle: Style
            ButtonStyle: Style
            EntryStyle: Style
            SearchBarStyle: Style
            ImageStyle: Style
            SwitchStyle: Style
            ListViewStyle: Style
            MapStyle: Style
        }

    let private apply setUp view = setUp |> Seq.iter (fun s -> s view); view
    type Theme =
        {
            Styles: Styles
        }
        member this.GenerateSearchBar([<ParamArray>] setUp: (SearchBar -> unit)[]) = new SearchBar(Style = this.Styles.SearchBarStyle) |> apply setUp
        member this.GenerateImage([<ParamArray>] setUp: (Image -> unit)[]) = new Image(Style = this.Styles.ImageStyle) |> apply setUp
        member this.GenerateButton([<ParamArray>] setUp: (Button -> unit)[]) = new Button(Style = this.Styles.ButtonStyle) |> apply setUp
        member this.GenerateLabel([<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.LabelStyle) |> apply setUp
        member this.GenerateSwitch([<ParamArray>] setUp: (Switch -> unit)[]) = new Switch(Style = this.Styles.SwitchStyle) |> apply setUp
        member this.GenerateEntry([<ParamArray>] setUp: (Entry -> unit)[]) = new Entry(Style = this.Styles.EntryStyle) |> apply setUp
        member this.GenerateHyperlink([<ParamArray>] setUp: (HyperlinkLabel -> unit)[]) = new HyperlinkLabel(Style = this.Styles.HyperlinkStyle) |> apply setUp
        member this.GenerateListView([<ParamArray>] setUp: (ListView -> unit)[]) = new ListView(Style = this.Styles.ListViewStyle) |> apply setUp
        member this.GenerateMap([<ParamArray>] setUp: (GeographicMap -> unit)[]) = new GeographicMap(Style = this.Styles.MapStyle) |> apply setUp
        member __.VerticalLayout([<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Vertical) |> apply setUp
        member __.HorizontalLayout([<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Horizontal) |> apply setUp
        member __.GenerateGrid(rowDefinitions, columnDefinitions, [<ParamArray>] setUp: (Grid -> unit)[]) = setUpGrid (new Grid() |> apply setUp) (rowDefinitions, columnDefinitions)
    let private addSetters<'TView when 'TView :> View> (setters: Setter seq) (style: Style) =
        let controlType = typeof<'TView>
        for setter in setters do 
            let setterType = setter.Property.DeclaringType
            if (setterType <> controlType) then raise <| ArgumentException(sprintf "A setter for a property of the type %s cannot be used to modify an instance of %s" setterType.Name controlType.Name)
            style.Setters.Add setter
    let applyButtonSetters buttonSetters (theme: Theme) = addSetters<Button> buttonSetters theme.Styles.ButtonStyle; theme
    let applyLabelSetters labelSetters (theme: Theme) = addSetters<Label> labelSetters theme.Styles.LabelStyle; theme
    let applyHyperlinkSetters hyperlinkSetters (theme: Theme) = addSetters<HyperlinkLabel> hyperlinkSetters theme.Styles.HyperlinkStyle; theme
    let applySwitchSetters switchSetters (theme: Theme) = addSetters<Switch> switchSetters theme.Styles.SwitchStyle; theme
    let applyEntrySetters entrySetters (theme: Theme) = addSetters<Entry> entrySetters theme.Styles.EntryStyle; theme
    let applyImageSetters imageSetters (theme: Theme) = addSetters<Image> imageSetters theme.Styles.ImageStyle; theme
    let applyListViewSetters listViewSetters (theme: Theme) = addSetters<ListView> listViewSetters theme.Styles.ListViewStyle; theme
    let applyBackgroundColor color (theme: Theme) = { theme with Styles = { theme.Styles with BackgroundColor = color } }
    let applySeparatorColor color (theme: Theme) = { theme with Styles = { theme.Styles with SeparatorColor = color } }

    let DefaultTheme =
        {
            Styles =
                {
                    BackgroundColor = Color.Black
                    SeparatorColor = Color.White
                    LabelStyle = new Style(typeof<Label>)
                    HyperlinkStyle = new Style(typeof<HyperlinkLabel>)
                    ButtonStyle = new Style(typeof<Button>)
                    EntryStyle = new Style(typeof<Entry>)
                    SearchBarStyle = new Style(typeof<SearchBar>)
                    ImageStyle = new Style(typeof<Image>)
                    SwitchStyle = new Style(typeof<Switch>)
                    ListViewStyle = new Style(typeof<ListView>)
                    MapStyle = new Style(typeof<Map>)
                }
        }

type IUiContext = abstract Context: obj
type UiContext(context) = interface IUiContext with member __.Context = context
