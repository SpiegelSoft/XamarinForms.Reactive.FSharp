namespace XamarinForms.Reactive.FSharp

open System.Windows.Input
open System

open Microsoft.FSharp.Quotations

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
    let withTwoWayBinding<'TElement, 'TProperty, 'TViewModel, 'TView when 'TView :> IViewFor<'TViewModel>>(viewModel: 'TViewModel, view: 'TView, viewModelProperty: Expr<'TViewModel -> 'TProperty>, viewProperty: Expr<'TView -> 'TProperty>) (element: 'TElement) = 
        view.Bind<'TViewModel, 'TView, 'TProperty, 'TProperty>(viewModel, toLinq viewModelProperty, toLinq viewProperty) |> ignore
        element
    let withOneWayBinding<'TElement, 'TProperty, 'TViewModel, 'TView when 'TView :> IViewFor<'TViewModel>>(viewModel: 'TViewModel, view: 'TView, viewModelProperty: Expr<'TViewModel -> 'TProperty>, viewProperty: Expr<'TView -> 'TProperty>) (element: 'TElement) = 
        view.OneWayBind<'TViewModel, 'TView, 'TProperty, 'TProperty>(viewModel, toLinq viewModelProperty, toLinq viewProperty) |> ignore
        element
    let withCommandBinding<'TElement, 'TCommand, 'TViewModel, 'TView when 'TView :> IViewFor<'TViewModel> and 'TCommand :> ICommand and 'TView: not struct>(viewModel: 'TViewModel, view: 'TView, viewModelCommand: Expr<'TViewModel -> 'TCommand>, controlProperty: Expr<'TView -> 'TElement>) (element: 'TElement) = 
        view.BindCommand<'TView, 'TViewModel, 'TCommand, 'TElement>(viewModel, toLinq viewModelCommand, toLinq controlProperty) |> ignore
        element
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
    let withPlaceholder placeholder (element: #Entry) = element.Placeholder <- placeholder; element
    let withSpacing spacing (layout: StackLayout) = layout.Spacing <- spacing; layout
    let withFontAttributes fontAttributes (element: #Label) = element.FontAttributes <- fontAttributes; element
    let withBackgroundColor color (element: #View) = element.BackgroundColor <- color; element
    let withSetUpActions<'TElement> (actions: ('TElement -> unit)[]) (element: 'TElement) = (for action in actions do action(element)); element
    let withSetUpAction<'TElement> (action: 'TElement -> unit) = withSetUpActions([|action|])

module Themes =
    let withBlocks views (stackLayout: StackLayout) = (for view in views do stackLayout.Children.Add(view)); stackLayout
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
        if specifiedRowCount <> actualRowCount then 
            raise <| ArgumentException(sprintf "You have tried to add %i %s to a grid for which %i %s %s specified." actualRowCount (rowNoun actualRowCount) specifiedRowCount (rowNoun specifiedRowCount) (verb specifiedRowCount))
        grid

    let createFromColumns (columnCreation: ColumnCreation) = 
        let grid = columnCreation.Grid
        let specifiedColumnCount, actualColumnCount = grid.ColumnDefinitions.Count, columnCreation.ColumnCount
        if specifiedColumnCount <> actualColumnCount then 
            raise <| ArgumentException(sprintf "You have tried to add %i %s to a grid for which %i %s %s specified." actualColumnCount (columnNoun actualColumnCount) specifiedColumnCount (columnNoun specifiedColumnCount) (verb specifiedColumnCount))
        grid
    
    type Styles =
        {
            BackgroundColor: Color
            SeparatorColor: Color
            LabelStyle: Style
            HyperlinkStyle: Style
            ButtonStyle: Style
            EntryStyle: Style
            ImageStyle: Style
            SwitchStyle: Style
            ListViewStyle: Style
        }

    type Theme =
        {
            Styles: Styles
        }
        member this.GenerateImage() = new Image(Style = this.Styles.ImageStyle)
        member this.GenerateButton() = new Button(Style = this.Styles.ButtonStyle)
        member this.GenerateLabel() = new Label(Style = this.Styles.LabelStyle)
        member this.GenerateSwitch() = new Switch(Style = this.Styles.SwitchStyle)
        member this.GenerateEntry() = new Entry(Style = this.Styles.EntryStyle)
        member this.GenerateHyperlink() = new HyperlinkLabel(Style = this.Styles.HyperlinkStyle)
        member this.GenerateListView() = new ListView(Style = this.Styles.ListViewStyle)
        member __.VerticalLayout() = new StackLayout (Orientation = StackOrientation.Vertical)
        member __.HorizontalLayout() = new StackLayout (Orientation = StackOrientation.Horizontal)
        member __.GenerateGrid(rowDefinitions, columnDefinitions) = setUpGrid (new Grid()) (rowDefinitions, columnDefinitions)
    let private addSetters<'TView when 'TView :> View> (setters: Setter seq) (style: Style) =
        let controlType = typeof<'TView>
        for setter in setters do 
            let setterType = setter.Property.DeclaringType
            if (setterType <> controlType) then
                raise <| ArgumentException(sprintf "A setter for a property of the type %s cannot be used to modify an instance of %s" setterType.Name controlType.Name)
            style.Setters.Add setter
    let withButtonSetters buttonSetters (theme: Theme) = addSetters<Button> buttonSetters theme.Styles.ButtonStyle; theme
    let withLabelSetters labelSetters (theme: Theme) = addSetters<Label> labelSetters theme.Styles.LabelStyle; theme
    let withHyperlinkSetters hyperlinkSetters (theme: Theme) = addSetters<HyperlinkLabel> hyperlinkSetters theme.Styles.HyperlinkStyle; theme
    let withSwitchSetters switchSetters (theme: Theme) = addSetters<Switch> switchSetters theme.Styles.SwitchStyle; theme
    let withEntrySetters entrySetters (theme: Theme) = addSetters<Entry> entrySetters theme.Styles.EntryStyle; theme
    let withImageSetters imageSetters (theme: Theme) = addSetters<Image> imageSetters theme.Styles.ImageStyle; theme
    let withListViewSetters listViewSetters (theme: Theme) = addSetters<ListView> listViewSetters theme.Styles.ListViewStyle; theme
    let withBackgroundColor color (theme: Theme) = { theme with Styles = { theme.Styles with BackgroundColor = color } }
    let withSeparatorColor color (theme: Theme) = { theme with Styles = { theme.Styles with SeparatorColor = color } }

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
                    ImageStyle = new Style(typeof<Image>)
                    SwitchStyle = new Style(typeof<Switch>)
                    ListViewStyle = new Style(typeof<ListView>)
                }
        }

type IUiContext = abstract Context: obj
type UiContext(context) = interface IUiContext with member __.Context = context
