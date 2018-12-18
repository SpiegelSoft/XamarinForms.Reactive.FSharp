namespace XamarinForms.Reactive.FSharp

open System.Collections.Specialized
open System.Collections.ObjectModel
open System.Reactive.Disposables
open System.Collections.Generic
open System.Windows.Input
open System.Reactive.Linq
open System.Collections
open System.Reflection
open System.Threading
open System.Linq
open System

open DynamicData

open Microsoft.FSharp.Quotations

open Xamarin.Forms

open ReactiveUI

open ExpressionConversion

open ClrExtensions

type BadgeIcon() =
    inherit AbsoluteLayout()
    static let badgeTextProperty = BindableProperty.Create("BadgeText", typeof<string>, typeof<BadgeIcon>, String.Empty, BindingMode.OneWay)
    static let imageSourceProperty = BindableProperty.Create("ImageSource", typeof<ImageSource>, typeof<BadgeIcon>, Unchecked.defaultof<ImageSource>, BindingMode.OneWay)
    static let zeroThickness = new Thickness(0.0)
    let icon = new Image(HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center)
    let badgeLabel = new Label(
                        TextColor = Color.White, 
                        BackgroundColor = Color.Red, 
                        Margin = zeroThickness, 
                        HorizontalOptions = LayoutOptions.Center, 
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center)
    let badge = new Frame(
                        IsVisible = false,
                        CornerRadius = 12.0f, 
                        BackgroundColor = Color.Red, 
                        OutlineColor = Color.Red,
                        Margin = new Thickness(2.0, 2.0),
                        Padding = new Thickness(4.0, 0.0),
                        Content = badgeLabel)
    member this.Initialise() =
        this.HorizontalOptions <- LayoutOptions.CenterAndExpand
        this.VerticalOptions <- LayoutOptions.CenterAndExpand
        AbsoluteLayout.SetLayoutBounds(icon, new Rectangle(0.0, 0.0, 1.0, 1.0))
        AbsoluteLayout.SetLayoutFlags(icon, AbsoluteLayoutFlags.All)
        AbsoluteLayout.SetLayoutBounds(badge, new Rectangle(0.6, 0.1, 0.3, 0.5))
        AbsoluteLayout.SetLayoutFlags(badge, AbsoluteLayoutFlags.All)
        this.Children.Add(icon)
        this.Children.Add(badge)
    member this.BadgeText 
        with get() = match this.GetValue(badgeTextProperty) with | :? string as s -> s | _ -> String.Empty
        and set(value: string) = badgeLabel.Text <- value; badge.IsVisible <- String.IsNullOrEmpty value |> not; this.SetValue(badgeTextProperty, value)
    member this.ImageSource 
        with get() = match this.GetValue(imageSourceProperty) with | :? ImageSource as is -> is | _ -> Unchecked.defaultof<ImageSource>
        and set(value: ImageSource) = icon.Source <- value; this.SetValue(imageSourceProperty, value)

type ItemsStack() as this =
    inherit StackLayout()
    static let itemsSourceChanged (bindable:BindableObject) (oldValue:obj) (newValue:obj) =
        let itemsLayout = bindable :> obj :?> ItemsStack
        itemsLayout.SetItems()
    static let onSelectedItemChanged (bindable:BindableObject) (oldValue:obj) (newValue:obj) =
        let itemsView = bindable :> obj :?> ItemsStack
        match newValue = oldValue with
        | true -> ()
        | false -> itemsView.SetSelectedItem(newValue)
    static let itemsSourceProperty = BindableProperty.Create("ItemsSource", typeof<IEnumerable>, typeof<ItemsStack>, Unchecked.defaultof<IEnumerable>, BindingMode.TwoWay, propertyChanged = new BindableProperty.BindingPropertyChangedDelegate(itemsSourceChanged))
    static let selectedItemProperty = BindableProperty.Create("SelectedItem", typeof<obj>, typeof<ItemsStack>, Unchecked.defaultof<obj>, BindingMode.TwoWay, propertyChanged = new BindableProperty.BindingPropertyChangedDelegate(onSelectedItemChanged))
    static let itemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof<DataTemplate>, typeof<ItemsStack>)
    let selectedItemChanged = Event<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>()
    let collectionChanged (_: obj) (e: NotifyCollectionChangedEventArgs) =
        let observableSource = this.ObservableSource:ObservableCollection<obj>
        match e.Action with
        | NotifyCollectionChangedAction.Add ->
            let mutable index = e.NewStartingIndex
            for item in e.NewItems do
                this.Children.Insert(index, this.GetItemView(item)); index <- index + 1
        | NotifyCollectionChangedAction.Move ->
            let item = observableSource.[e.OldStartingIndex]
            this.Children.RemoveAt(e.OldStartingIndex)
            this.Children.Insert(e.NewStartingIndex, this.GetItemView(item))
        | NotifyCollectionChangedAction.Remove ->
            this.Children.RemoveAt(e.OldStartingIndex)
        | NotifyCollectionChangedAction.Replace ->
            this.Children.RemoveAt(e.OldStartingIndex)
            this.Children.Insert(e.NewStartingIndex, this.GetItemView(observableSource.[e.NewStartingIndex]))
        | NotifyCollectionChangedAction.Reset ->
            this.Children.Clear()
            let itemsSource = (this.ItemsSource: IEnumerable) :?> IEnumerable<obj>
            for item in itemsSource do this.Children.Add(this.GetItemView(item))
        | _ -> ()
    let collectionChangedHandler = new NotifyCollectionChangedEventHandler(collectionChanged)
    let mutable observableSource = Unchecked.defaultof<ObservableCollection<obj>>
    [<CLIEvent>] member __.SelectedItemChanged = selectedItemChanged.Publish
    member __.ObservableSource
        with get() = observableSource
        and set(value) =
            match box observableSource with
            | null -> () | _ -> observableSource.CollectionChanged.RemoveHandler(collectionChangedHandler)
            observableSource <- value
            match box observableSource with
            | null -> () | _ -> observableSource.CollectionChanged.AddHandler(collectionChangedHandler)
    abstract member SetItems: unit -> unit
    abstract member SetSelectedItem: obj -> unit
    abstract member GetItemView: obj -> View
    member __.ItemsSource 
        with get() = match this.GetValue(itemsSourceProperty) with | :? IEnumerable as s -> s | _ -> Unchecked.defaultof<IEnumerable>
        and set(value: IEnumerable) = this.SetValue(itemsSourceProperty, value)
    member __.SelectedItem 
        with get() = this.GetValue(selectedItemProperty)
        and set(value) = this.SetValue(selectedItemProperty, value)
    member __.ItemTemplate 
        with get() = this.GetValue(itemTemplateProperty) :?> DataTemplate
        and set(value: DataTemplate) = this.SetValue(itemTemplateProperty, value)
    member __.AddGesture(view: View, gesture) =
        view.GestureRecognizers.Add(gesture)
        match view with
        | :? Layout<View> as layout -> for child in layout.Children do this.AddGesture(child, gesture)
        | _ -> ()
    member val ItemSelectedCommand = new Command<obj>(fun item -> this.SelectedItem <- item) :> ICommand
    default __.SetItems() =
        this.Children.Clear()
        match box this.ItemsSource with
        | null -> this.ObservableSource <- Unchecked.defaultof<ObservableCollection<obj>>
        | _ ->
            for item in this.ItemsSource do this.Children.Add(this.GetItemView(item))
            let typeInfo = this.ItemsSource.GetType().GetTypeInfo()
            match typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() = typedefof<ObservableCollection<_>> with
            | true -> this.ObservableSource <- new ObservableCollection<obj>(this.ItemsSource.Cast<obj>())
            | false -> ()
    default __.GetItemView(item) =
        match this.ItemTemplate.CreateContent() with
        | :? View as view ->
            view.BindingContext <- item
            let gesture = new TapGestureRecognizer(Command = this.ItemSelectedCommand, CommandParameter = item)
            this.AddGesture(view, gesture)
            view
        | _ -> Unchecked.defaultof<View>
    default __.SetSelectedItem(selectedItem) =
        selectedItemChanged.Trigger(this, new SelectedItemChangedEventArgs(selectedItem))
        
type NestedScrollView() = inherit ScrollView()

type AutoCompleteView() =
    inherit ContentView()
    static let onSuggestionItemTemplateChanged (bindable:obj) (oldValue:obj) (newValue:obj) =
        let autoCompleteView = bindable :?> AutoCompleteView
        match box autoCompleteView with
        | null -> ()
        | _ -> (autoCompleteView.SuggestionsListView: ItemsStack).ItemTemplate <- autoCompleteView.SuggestionItemTemplate
    static let onPlaceholderChanged (bindable:obj) (oldValue:obj) (newValue:obj) =
        let autoCompleteView = bindable :?> AutoCompleteView
        (autoCompleteView.SearchEntry: Entry).Placeholder <- 
            match box newValue with
            | null -> String.Empty
            | _ -> newValue.ToString()
    static let onSuggestionsChanged (bindable:obj) (oldValue:obj) (newValue:obj) =
        let autoCompleteView = bindable :?> AutoCompleteView
        let suggestions = newValue :?> IEnumerable
        autoCompleteView.OriginSuggestions <- suggestions;
        let suggestions = autoCompleteView.FilterSuggestions(suggestions, autoCompleteView.SearchText)
        (autoCompleteView.SuggestionsListView: ItemsStack).ItemsSource <- suggestions
    static let onSearchTextChanged (bindable:obj) (oldValue:obj) (newValue:obj) =
        let autoCompleteView = bindable :?> AutoCompleteView
        let mutable suggestions = autoCompleteView.OriginSuggestions;
        match box newValue with
        | null -> ()
        | _ ->
            let searchEntry:Entry = autoCompleteView.SearchEntry
            suggestions <- autoCompleteView.FilterSuggestions(suggestions, autoCompleteView.SearchText)
            match searchEntry.Text <> autoCompleteView.SearchText with
            | true -> searchEntry.Text <- autoCompleteView.SearchText
            | false -> ()
        autoCompleteView.SuggestionsListView.ItemsSource <- suggestions
        autoCompleteView.RefreshLayout()
    static let suggestionsProperty = BindableProperty.Create("Suggestions", typeof<IEnumerable>, typeof<AutoCompleteView>, null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(onSuggestionsChanged))
    static let searchTextProperty = BindableProperty.Create("SearchText", typeof<string>, typeof<AutoCompleteView>, null, BindingMode.TwoWay, null, new BindableProperty.BindingPropertyChangedDelegate(onSearchTextChanged))
    static let placeholderProperty = BindableProperty.Create("Placeholder", typeof<string>, typeof<AutoCompleteView>, null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(onPlaceholderChanged))
    static let maximumVisibleSuggestionItemsProperty = BindableProperty.Create("MaximumVisibleSuggestionItems", typeof<int>, typeof<AutoCompleteView>, 4)
    static let suggestionItemTemplateProperty = BindableProperty.Create("SuggestionItemTemplate", typeof<DataTemplate>, typeof<AutoCompleteView>, null, BindingMode.OneWay, null, new BindableProperty.BindingPropertyChangedDelegate(onSuggestionItemTemplateChanged))
    static let displayPropertyNameProperty = BindableProperty.Create("DisplayPropertyName", typeof<string>, typeof<AutoCompleteView>);
    let suggestionsListView = new ItemsStack(BackgroundColor = Color.White, VerticalOptions = LayoutOptions.End)
    let searchEntry = new Entry(HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill)
    let container = new Grid(RowSpacing = 0.0, HeightRequest = 50.0)
    let suggestionWrapper = new NestedScrollView(VerticalOptions = LayoutOptions.Fill, Orientation = ScrollOrientation.Vertical, BackgroundColor = Color.White, Content = suggestionsListView)
    do 
        container.ColumnDefinitions.Add(new ColumnDefinition(Width = GridLength.Star))
        container.RowDefinitions.Add(new RowDefinition(Height = GridLength.Star))
        container.RowDefinitions.Add(new RowDefinition(Height = new GridLength(50.0)))
        container.Children.Add(suggestionWrapper)
        container.Children.Add(searchEntry, 0, 1)
    let mutable isSelected = false
    let mutable totalNumberOfTypings = 0
    let mutable originSuggestions = [] :> IEnumerable
    member val ViewDisposables = new CompositeDisposable()
    member val SuggestionsListView = suggestionsListView
    member val SearchEntry = searchEntry
    member __.IsSelected with get() = isSelected
    member __.TotalNumberOfTypings with get() = totalNumberOfTypings
    member this.Suggestions 
        with get() = this.GetValue(suggestionsProperty) :?> IEnumerable
        and set(value:IEnumerable) = this.SetValue(suggestionsProperty, value)
    member this.Placeholder 
        with get() = this.GetValue(placeholderProperty) :?> string
        and set(value:string) = this.SetValue(placeholderProperty, value)
    member this.SearchText 
        with get() = this.GetValue(searchTextProperty) :?> string
        and set(value:string) = this.SetValue(searchTextProperty, value)
    member this.DisplayPropertyName 
        with get() = this.GetValue(displayPropertyNameProperty) :?> string
        and set(value:string) = this.SetValue(displayPropertyNameProperty, value)
    member this.MaximumVisibleSuggestionItems 
        with get() = this.GetValue(maximumVisibleSuggestionItemsProperty) :?> int
        and set(value:int) = this.SetValue(maximumVisibleSuggestionItemsProperty, value)
    member this.SuggestionItemTemplate 
        with get() = this.GetValue(suggestionItemTemplateProperty) :?> DataTemplate
        and set(value:DataTemplate) = this.SetValue(suggestionItemTemplateProperty, value)
    member private this.GetExpectedHeight() =
        let items = suggestionsListView.ItemsSource :?> IList
        let wrapperHeightRequest = 
            match box items with
            | null -> 0
            | _ -> 40 * match items.Count >= this.MaximumVisibleSuggestionItems with | true -> this.MaximumVisibleSuggestionItems | false -> items.Count 
        match Device.RuntimePlatform with 
        | Device.Android -> wrapperHeightRequest + 50
        | _ -> this.MaximumVisibleSuggestionItems * 40 + 50;
    member this.UpdateLayout() =
        let expectedHeight = this.GetExpectedHeight()
        container.HeightRequest <- float expectedHeight
        container.ForceLayout()
    member this.RefreshLayout() =
        match Device.RuntimePlatform with 
        | Device.Android ->
            match isSelected with
            | true -> isSelected <- false
            | false -> this.UpdateChildrenLayout()
        | _ -> ()
    member private this.SearchTextChanged(textChangedEventArgs: TextChangedEventArgs) =
        Interlocked.Increment(&totalNumberOfTypings) |> ignore
        let updateSearchTextAfterTyping() =
            match Interlocked.Decrement(&totalNumberOfTypings) with
            | 0 -> this.SearchText <- textChangedEventArgs.NewTextValue
            | _ -> ()
            false
        Device.StartTimer(TimeSpan.FromMilliseconds(1000.0), new Func<bool>(updateSearchTextAfterTyping))
    member private this.SearchEntryUnfocused(_: FocusEventArgs) = container.HeightRequest <- 50.0; container.ForceLayout()
    member private this.SearchEntryFocused(_: FocusEventArgs) = this.UpdateLayout(); isSelected <- false
    member private this.SuggestionSelected(itemSelectedEventArgs: SelectedItemChangedEventArgs) =
        isSelected <- true
        searchEntry.Text <-
            let item = itemSelectedEventArgs.SelectedItem
            match String.IsNullOrEmpty(this.DisplayPropertyName) with
            | true -> 
                match box item with
                | null -> String.Empty
                | _ -> 
                    let prop = item.GetType().GetTypeInfo().GetDeclaredProperty(this.DisplayPropertyName)
                    match box prop with
                    | null -> String.Empty
                    | _ ->
                        let value = prop.GetValue(item)
                        match box value with | null -> String.Empty | _ -> value.ToString()
            | false -> match box item with | null -> String.Empty | _ -> item.ToString()
        container.HeightRequest <- 50.0
        container.ForceLayout()

    override this.OnParentSet() = 
        base.OnParentSet()
        match box this.Parent with
        | null -> this.ViewDisposables.Clear()
        | _ -> ()
    member this.Init() = 
        this.Content <- container
        searchEntry.TextChanged.Subscribe(this.SearchTextChanged) |> this.ViewDisposables.Add
        searchEntry.Focused.Subscribe(this.SearchEntryFocused) |> this.ViewDisposables.Add
        searchEntry.Unfocused.Subscribe(this.SearchEntryUnfocused) |> this.ViewDisposables.Add
        suggestionsListView.SelectedItemChanged.Subscribe(this.SuggestionSelected) |> this.ViewDisposables.Add
    member __.OriginSuggestions with get() = originSuggestions and private set(value) = originSuggestions <- value
    member this.FilterSuggestions(suggestions, keyword: string) =
        match String.IsNullOrEmpty keyword, box suggestions with
        | true, _ | _, null -> suggestions
        | _, _ ->
            let searchWords = keyword.ToLower().Split([|" "|], StringSplitOptions.RemoveEmptyEntries)
            let mutable result = suggestions.Cast<obj>()
            let displayPropertyName = this.DisplayPropertyName
            for item in searchWords do
                match String.IsNullOrEmpty(this.DisplayPropertyName) with
                | false ->
                    result <- result.Where(fun x -> x.GetType().GetTypeInfo().GetDeclaredProperty(displayPropertyName).GetValue(x).ToString().ToLower().Contains(item)).ToArray()
                | true ->
                    result <- result.Where(fun x -> x.ToString().ToLower().Contains(item)).ToList();
            result :> IEnumerable
    
type FrameOverlay() =
    inherit Frame()
    static let boundaryProperty =
        BindableProperty.Create("Boundary", typeof<Rectangle>, typeof<Frame>, Rectangle.Zero, BindingMode.OneWay,
            propertyChanged = new BindableProperty.BindingPropertyChangedDelegate(fun bindableObject _ newValue -> AbsoluteLayout.SetLayoutBounds(bindableObject, newValue :?> Rectangle)))
    member this.Boundary
        with get() = this.GetValue(boundaryProperty) :?> Rectangle
        and set(value: Rectangle) = this.SetValue(boundaryProperty, value)

type HyperlinkLabel() =
    inherit Label()
    static let commandProperty = BindableProperty.Create("Command", typeof<ICommand>, typeof<HyperlinkLabel>)
    static let commandParameterProperty = BindableProperty.Create("CommandParameter", typeof<obj>, typeof<HyperlinkLabel>)
    let updateGestureRecognisers (label: HyperlinkLabel) =
        label.GestureRecognizers.Clear()
        match label.Command with
        | null -> ()
        | _ -> label.GestureRecognizers.Add(new TapGestureRecognizer(Command = label.Command, CommandParameter = label.CommandParameter))
    member this.Command 
        with get() = this.GetValue(commandProperty) :?> ICommand
        and set(value: ICommand) = this.SetValue(commandProperty, value); updateGestureRecognisers this
    member this.CommandParameter 
        with get() = this.GetValue(commandParameterProperty)
        and set(value: obj) = this.SetValue(commandParameterProperty, value); updateGestureRecognisers this

type IDisposableView = abstract Disposables: CompositeDisposable 

module ViewHelpers =
    open ObservableExtensions
    open ImageCircle.Forms.Plugin.Abstractions

    let private oneWayConverter (selector: 'a -> 'b) =
        { new IValueConverter with 
            member __.Convert(value, _, _, _) = selector(value :?> 'a) :> obj
            member __.ConvertBack(_, _, _, _) = failwith "This is a one-way converter. You should never hit this error." }
    let unitRectangle = Rectangle.FromLTRB(0.0, 0.0, 1.0, 1.0)
    let withTwoWayBinding(view: 'v when 'v :> IViewFor<'vm> and 'v :> IDisposableView, viewModelProperty: Expr<'vm -> 'vmp>, viewProperty, vmToViewConverter, viewToVmConverter) element = 
        view.WhenAnyValue(fun v -> v.ViewModel).Where(isNotNull).Subscribe(fun viewModel ->
            view.Bind(viewModel, toLinq viewModelProperty, toLinq viewProperty, null, fun x -> vmToViewConverter(x), fun x -> viewToVmConverter(x)) |> disposeWith view.Disposables |> ignore
        ) |> disposeWith view.Disposables |> ignore
        element
    let withOneWayBinding(view: 'v when 'v :> IViewFor<'vm> and 'v :> IDisposableView, viewModelProperty, viewProperty, selector) element = 
        view.WhenAnyValue(fun v -> v.ViewModel).Where(isNotNull).Subscribe(fun viewModel ->
            view.OneWayBind(viewModel, toLinq viewModelProperty, toLinq viewProperty, fun x -> selector(x)) |> disposeWith view.Disposables |> ignore
        ) |> disposeWith view.Disposables |> ignore
        element
    let withOneWayElementBinding(view: 'v :> Element, viewModelProperty: Expr<'vm -> 'a>, viewProperty: BindableProperty, selector: 'a -> 'b) element = 
        view.SetBinding(viewProperty, propertyName viewModelProperty, BindingMode.OneWay, oneWayConverter selector)
        element
    let withTextCellCommandParameterBinding(view: 'v :> #TextCell, viewModelProperty: Expr<'vm -> 'a>, selector: 'a -> 'b) element = 
        view.SetBinding(TextCell.CommandParameterProperty, propertyName viewModelProperty, BindingMode.OneWay, oneWayConverter selector)
        element
    let withContextMenuItem(disposables: CompositeDisposable, view: #Cell, caption: string, viewModelProperty: Expr<'vm -> 'a>, command: ReactiveCommand<'b, 'c>, selector: 'a -> 'b) element =
        let menuItem = new MenuItem(Text = caption)
        menuItem.SetBinding(MenuItem.CommandParameterProperty, propertyName viewModelProperty, BindingMode.OneWay, oneWayConverter selector)
        menuItem.Clicked.Subscribe(fun _ ->
            menuItem.BindingContext <- view.BindingContext
            command.Execute(menuItem.CommandParameter :?> 'b).Subscribe() |> ignore) |> disposables.Add
        view.ContextActions.Add menuItem
        element
    let withCommandBinding(view: 'v when 'v :> IViewFor<'vm> and 'v :> IDisposableView, viewModelCommand, controlProperty) element = 
        view.WhenAnyValue(fun v -> v.ViewModel).Where(isNotNull).Subscribe(fun viewModel ->
            view.BindCommand(viewModel, toLinq viewModelCommand, toLinq controlProperty) |> disposeWith view.Disposables |> ignore
        ) |> disposeWith view.Disposables |> ignore
        element
    let withEventCommandBinding(view: 'v when 'v :> IViewFor<'vm>, viewModelCommand, controlProperty, event) element = 
        view.BindCommand(view.ViewModel, toLinq viewModelCommand, toLinq controlProperty, event) |> ignore
        element
    let withTapCommand(view: 'v :> View, command, commandParameter: Expr<'vm -> 'b> option) element =
        view.BindingContextChanged.Subscribe(fun _ -> 
            let viewModel = view.BindingContext :?> 'vm
            view.GestureRecognizers.Add(new TapGestureRecognizer(Command = command, CommandParameter = match commandParameter with | Some p -> p |> toLinq |> (fun e -> e.Compile()) |> (fun e -> e.Invoke(viewModel)) :> obj | None -> Unchecked.defaultof<obj>))) |> ignore
        element
    let withHyperlinkCommand command (element: #HyperlinkLabel) = element.Command <- command; element
    let withHorizontalTextAlignment alignment (element: #Label) = element.HorizontalTextAlignment <- alignment; element
    let withVerticalTextAlignment alignment (element: #Label) = element.VerticalTextAlignment <- alignment; element
    let withHorizontalOptions options (element: #View) = element.HorizontalOptions <- options; element
    let withVerticalOptions options (element: #View) = element.VerticalOptions <- options; element
    let withHeightRequest request (element: #View) = element.HeightRequest <- request; element
    let withWidthRequest request (element: #View) = element.WidthRequest <- request; element
    let withHeightAndWidthRequest height width (element: #View) = element |> withHeightRequest height |> withWidthRequest width
    let withAutomationId id (element: #View) = element.AutomationId <- id; element
    let withAlignment horizontalOptions verticalOptions element = element |> withHorizontalOptions horizontalOptions |> withVerticalOptions verticalOptions
    let withRowSpan rowSpan (element: #View) = Grid.SetRowSpan(element, rowSpan); element
    let withColumnSpan columnSpan (element: #View) = Grid.SetColumnSpan(element, columnSpan); element
    let withRowSpacing spacing (element: #Grid) = element.RowSpacing <- spacing; element
    let withColumnSpacing spacing (element: #Grid) = element.ColumnSpacing <- spacing; element
    let withRaisedChild child (element: #Grid) = element.RaiseChild child; element
    let withMargin margin (element: #View) = element.Margin <- margin; element
    let withSource source (element: #Image) = element.Source <- source; element
    let withBadgeSource source (element: #BadgeIcon) = element.ImageSource <- source; element
    let withButtonImage image (element: #Button) = element.Image <- image; element
    let withButtonTextColor color (element: #Button) = element.TextColor <- color; element
    let withButtonCommand command (element: #Button) = element.Command <- command; element
    let withButtonCommandParameter parameter (element: #Button) = element.CommandParameter <- parameter; element
    let withContentLayout contentLayout (element: #Button) = element.ContentLayout <- contentLayout; element
    let withAspect aspect (element: #Image) = element.Aspect <- aspect; element
    let withPadding padding (element: #Layout) = element.Padding <- padding; element
    let withCaption text (element: #Button) = element.Text <- text; element
    let withEntryText text (element: #Entry) = element.Text <- text; element
    let withScrollOrientation orientation (element: #ScrollView) = element.Orientation <- orientation; element
    let withPickerItemsSource itemsSource (element: Picker) = element.ItemsSource <- itemsSource; element
    let withPickerDisplayBinding (binding: Expr<'a -> string>) (element: Picker) = element.ItemDisplayBinding <- new Binding(propertyName binding) ; element
    let withListViewHeader header (element: #ListView) = element.Header <- header; element
    let withListViewFooter footer (element: #ListView) = element.Footer <- footer; element
    let withUnevenRows (element: #ListView) = element.HasUnevenRows <- true; element
    let withItemsSource source (element: #ItemsView<'v>) = element.ItemsSource <- source; element
    let withItemTapped (itemTapped: ItemTappedEventArgs -> unit, disposables) (element: #ListView) = element.ItemTapped.Subscribe(itemTapped) |> disposeWith disposables |> ignore; element
    let withItemSelected (itemSelected: SelectedItemChangedEventArgs -> unit, disposables) (element: #ListView) = element.ItemSelected.Subscribe(itemSelected) |> disposeWith disposables |> ignore; element
    let withSelectionMode selectionMode (element: #ListView) = element.SelectionMode <- selectionMode; element
    let viewCell view = new ViewCell(View = view)
    let withLabelFontSize size (element: #Label) = element.FontSize <- size; element
    let withButtonFontSize size (element: #Button) = element.FontSize <- size; element
    let withFontFamily family (element: #Label) = element.FontFamily <- family; element
    let withCellTemplate (createTemplate: unit -> #Cell) (element: #ItemsView<'v>) = element.ItemTemplate <- new DataTemplate(fun() -> createTemplate() :> obj); element
    let withEditorText text (element: #Editor) = element.Text <- text; element
    let withContent content (element: #ScrollView) = element.Content <- content; element
    let withViewContent content (element: #ContentView) = element.Content <- content; element
    let withBoxColor color (element: #BoxView) = element.Color <- color; element
    let withFrameBorderColor color (element: #Frame) = element.BorderColor <- color; element
    let withLabelTextColor color (element: #Label) = element.TextColor <- color; element
    let withEditorTextColor color (element: #Editor) = element.TextColor <- color; element
    let withEntryTextColor color (element: #Entry) = element.TextColor <- color; element
    let withEditorFontSize fontSize (element: #Editor) = element.FontSize <- fontSize; element
    let withEditorFontAttributes fontAttributes (element: #Editor) = element.FontAttributes <- fontAttributes; element
    let withEditorFontFamily fontFamily (element: #Editor) = element.FontFamily <- fontFamily; element
    let withLabelText text (element: #Label) = element.Text <- text; element
    let withBindingContext bindingContext (element: #View) = element.BindingContext <- bindingContext; element
    let withFormattedText spans (element: #Label) = 
        element.FormattedText <- new FormattedString(); spans |> Seq.iter element.FormattedText.Spans.Add; element
    let withStyle style (element: #View) = element.Style <- style; element
    let withSpanBackgroundColor color (element: Span) = element.BackgroundColor <- color; element
    let withSpanTextColor color (element: Span) = element.ForegroundColor <- color; element
    let withSpanText text (element: Span) = element.Text <- text; element
    let withSpanFontSize fontSize (element: Span) = element.FontSize <- fontSize; element
    let withSpanFontFamily fontFamily (element: Span) = element.FontFamily <- fontFamily; element
    let withBorderThickness thickness (element: CircleImage) = element.BorderThickness <- thickness; element
    let withBorderColor color (element: CircleImage) = element.BorderColor <- color; element
    let withKeyboard keyboard (element: #InputView) = element.Keyboard <- keyboard; element
    let withSearchCommand (disposer: CompositeDisposable) command (element: #SearchBar) = 
        element.TextChanged.Subscribe(fun args -> element.SearchCommandParameter <- args.NewTextValue) |> disposeWith disposer |> ignore
        element.SearchCommand <- command; element
    let withEntryPlaceholder placeholder (element: #Entry) = element.Placeholder <- placeholder; element
    let withSearchBarPlaceholder placeholder (element: #SearchBar) = element.Placeholder <- placeholder; element
    let withSpacing spacing (layout: StackLayout) = layout.Spacing <- spacing; layout
    let withScale scale (element: #VisualElement) = element.Scale <- scale; element
    let withFontAttributes fontAttributes (element: #Label) = element.FontAttributes <- fontAttributes; element
    let withBackgroundColor color (element: #View) = element.BackgroundColor <- color; element
    let withEffect effectId (element: #Element) = element.Effects.Add(Effect.Resolve(effectId)); element     
    let withRoutingEffect (effect: #RoutingEffect) (element: #Element) = element.Effects.Add(effect); element 
    let withDataTemplate (template: unit -> Cell) (element: #ListView) = element.ItemTemplate <- new DataTemplate(fun () -> template() :> obj); element
    let withTextCellCommand command (element: #TextCell) = 
        element.Command <- command
        element.SetBinding(TextCell.CommandParameterProperty, ".", BindingMode.OneWay)
        element
    let withTextCellCommandParameter commandParameter (element: #TextCell) = element.CommandParameter <- commandParameter; element
    let withTextCellText text (element: #TextCell) = element.Text <- text; element
    let withTextCellTextColor textColor (element: #TextCell) = element.TextColor <- textColor; element
    let withTextCellDetail detail (element: #TextCell) = element.Detail <- detail; element
    let withTextCellDetailColor detailColor (element: #TextCell) = element.DetailColor <- detailColor; element
    let withImageCellSource source (element: #ImageCell) = element.ImageSource <- source; element
    let withAbsoluteLayoutBounds bounds (element: #View) = AbsoluteLayout.SetLayoutBounds(element, bounds); element
    let withAbsoluteLayoutFlags flags (element: #View) = AbsoluteLayout.SetLayoutFlags(element, flags); element
    let withOpacity opacity (element: #VisualElement) = element.Opacity <- opacity; element
    let withDebouncedSearchPreview (disposer: CompositeDisposable) (time: TimeSpan) (command: ICommand) (element: #SearchBar) =
        element.TextChanged.Throttle(time).Subscribe(fun args -> command.Execute(args.NewTextValue)) |> disposeWith disposer |> ignore; element

open ViewHelpers

module Themes =
    open Microsoft.FSharp.Quotations
    open Xamarin.Forms
    open ImageCircle.Forms.Plugin.Abstractions

    type GridSize =
        | Star of int
        | Absolute of float
        | Auto
    let withBlocks (views:View[]) (stackLayout: #StackLayout) = views |> Seq.iter stackLayout.Children.Add; stackLayout
    let withAbsoluteOverlayViews (views:View[]) (absoluteLayout: #AbsoluteLayout) = views |> Seq.iter absoluteLayout.Children.Add; absoluteLayout
    let withRelativeOverlayViews (views:View[]) (relativeLayout: #RelativeLayout) = views |> Seq.iter relativeLayout.Children.Add; relativeLayout
    let private gridLengthTypeConverter = new GridLengthTypeConverter()
    let private textToGridLength text = gridLengthTypeConverter.ConvertFromInvariantString(text) :?> GridLength
    let private sizeToGridLength size = 
        match size with
        | Star n -> new GridLength(float n, GridUnitType.Star)
        | Absolute size -> new GridLength(size, GridUnitType.Absolute)
        | Auto -> GridLength.Auto
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

    let SuccessDark = Color.FromHex("#4F8A10") 
    let SuccessLight = Color.FromHex("#DFF2BF") 
    let InfoDark = Color.FromHex("#00529B") 
    let InfoLight = Color.FromHex("#BDE5F8") 
    let WarningDark = Color.FromHex("#9F6000") 
    let WarningLight = Color.FromHex("#FEEFB3") 
    let FacebookBlue = Color.FromHex("#4167B2")
    let ErrorDark = Color.FromHex("#D8000C") 
    let ErrorLight = Color.FromHex("#FFBABA") 
    let private withForegroundColor color (style: Style) = style.Setters.Add(new Setter(Property = Label.TextColorProperty, Value = color)); style
    let private withBackgroundColor color (style: Style) = style.Setters.Add(new Setter(Property = VisualElement.BackgroundColorProperty, Value = color)); style
    let private elementNoun i = if i = 1 then "element" else "elements"
    let private columnNoun i = if i = 1 then "column" else "columns"
    let private rowNoun i = if i = 1 then "row" else "rows"
    let private verb i = if i = 1 then "was" else "were"
    let rec private addRow rowCount (grid: Grid) (row: View[]) =
        if grid.ColumnDefinitions.Count <> (row |> Array.map(fun c -> Grid.GetColumnSpan(c)) |> Array.sum) then 
            let specifiedColumnCount = grid.ColumnDefinitions.Count
            raise <| ArgumentException(sprintf "You have tried to add a row with %i %s to a grid with %i %s." row.Length (elementNoun row.Length) specifiedColumnCount (columnNoun specifiedColumnCount), "row")
        for index = 0 to row.Length - 1 do
            Grid.SetRow(row.[index], rowCount)
            Grid.SetColumn(row.[index], index)
            grid.Children.Add row.[index]
        let newRowCount = rowCount + 1
        { RowCreation.RowCount = newRowCount; Grid = grid }
    let rec private addColumn columnCount (grid: Grid) (column: View[]) =
        if grid.RowDefinitions.Count <> (column |> Array.map(fun c -> Grid.GetRowSpan(c)) |> Array.sum) then 
            let specifiedRowCount = grid.RowDefinitions.Count
            raise <| ArgumentException(sprintf "You have tried to add a column with %i %s to a grid with %i %s." column.Length (elementNoun column.Length) specifiedRowCount (rowNoun specifiedRowCount), "column")
        for index = 0 to column.Length - 1 do
            Grid.SetRow(column.[index], index)
            Grid.SetColumn(column.[index], columnCount)
            grid.Children.Add column.[index]
        let newColumnCount = columnCount + 1
        { ColumnCreation.ColumnCount = newColumnCount; Grid = grid }
    let private setUpGridFromText (grid: Grid) (rowDefinitions, columnDefinitions) =
        for rowDefinition in rowDefinitions do grid.RowDefinitions.Add(new RowDefinition(Height = textToGridLength rowDefinition))
        for columnDefinition in columnDefinitions do grid.ColumnDefinitions.Add(new ColumnDefinition(Width = textToGridLength columnDefinition))
        { Grid = grid; RowCount = grid.RowDefinitions.Count; ColumnCount = grid.ColumnDefinitions.Count }
    let applyRowSizes rowSizes (rowDefinitions: RowDefinitionCollection) =
        for rowDefinition in rowSizes do rowDefinitions.Add(new RowDefinition(Height = sizeToGridLength rowDefinition))
    let applyColumnSizes columnSizes (columnDefinitions: ColumnDefinitionCollection) =
        for columnDefinition in columnSizes do columnDefinitions.Add(new ColumnDefinition(Width = sizeToGridLength columnDefinition))
    let private setUpGridFromSizes (grid: Grid) (rowSizes, columnSizes) =
        grid.RowDefinitions |> applyRowSizes rowSizes
        grid.ColumnDefinitions |> applyColumnSizes columnSizes
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
            TitleStyle: Style
            SubtitleStyle: Style
            HyperlinkStyle: Style
            ButtonStyle: Style
            EntryStyle: Style
            EditorStyle: Style
            SearchBarStyle: Style
            ImageStyle: Style
            FrameStyle: Style
            ContentViewStyle: Style
            SwitchStyle: Style
            ListViewStyle: Style
            BoxViewStyle: Style
            MenuItemStyle: Style
            ScrollViewStyle: Style
            DatePickerStyle: Style
            TimePickerStyle: Style
            PickerStyle: Style
            TabbedPageStyle: Style
            ActivityIndicatorStyle: Style
            TextCellTextColor: Color
            TextCellDetailColor: Color
            NavigationPageStyle: Style
        }

    let apply setUp view = setUp |> Seq.iter (fun s -> s view); view
    let initialise (property: Expr<'a -> 'b>) (view: 'a) (value: 'b) = ExpressionConversion.setProperty view value property; value
    let private initialiseBadgeIcon (badgeIcon: BadgeIcon) = badgeIcon.Initialise(); badgeIcon
    let private applyCellColors (styles:Styles) (cell:#TextCell) = cell.TextColor <- styles.TextCellTextColor; cell.DetailColor <- styles.TextCellDetailColor; cell
    let getOrAddStyle (element: VisualElement) = 
        // match box element.Style with | null -> new Style(element.GetType()) | _ -> element.Style
         new Style(element.GetType())
    type Theme =
        {
            Styles: Styles
        }
        member this.GenerateSpan([<ParamArray>] setUp: (Span -> unit)[]) = new Span() |> apply setUp
        member this.GenerateSpan(view, property, [<ParamArray>] setUp: (Span -> unit)[]) = new Span() |> initialise property view |> apply setUp
        member this.GenerateSearchBar([<ParamArray>] setUp: (SearchBar -> unit)[]) = new SearchBar(Style = this.Styles.SearchBarStyle) |> apply setUp
        member this.GenerateSearchBar(view, property, [<ParamArray>] setUp: (SearchBar -> unit)[]) = new SearchBar(Style = this.Styles.SearchBarStyle) |> initialise property view |> apply setUp
        member this.GenerateImage([<ParamArray>] setUp: (Image -> unit)[]) = new Image(Style = this.Styles.ImageStyle) |> apply setUp
        member this.GenerateImage(view, property, [<ParamArray>] setUp: (Image -> unit)[]) = new Image(Style = this.Styles.ImageStyle) |> initialise property view |> apply setUp
        member this.GenerateCircularImage([<ParamArray>] setUp: (CircleImage -> unit)[]) = new CircleImage(Style = this.Styles.ImageStyle) |> apply setUp
        member this.GenerateCircularImage(view, property, [<ParamArray>] setUp: (CircleImage -> unit)[]) = new CircleImage(Style = this.Styles.ImageStyle) |> initialise property view |> apply setUp
        member this.GenerateFrame([<ParamArray>] setUp: (Frame -> unit)[]) = new Frame(Style = this.Styles.FrameStyle) |> apply setUp
        member this.GenerateFrame(view, property, [<ParamArray>] setUp: (Frame -> unit)[]) = new Frame(Style = this.Styles.FrameStyle) |> initialise property view |> apply setUp
        member this.GenerateFrameOverlay([<ParamArray>] setUp: (FrameOverlay -> unit)[]) = new FrameOverlay(Style = this.Styles.FrameStyle) |> apply setUp
        member this.GenerateFrameOverlay(view, property, [<ParamArray>] setUp: (FrameOverlay -> unit)[]) = new FrameOverlay(Style = this.Styles.FrameStyle) |> initialise property view |> apply setUp
        member this.GenerateContentView([<ParamArray>] setUp: (ContentView -> unit)[]) = new ContentView(Style = this.Styles.ContentViewStyle) |> apply setUp
        member this.GenerateButton([<ParamArray>] setUp: (Button -> unit)[]) = new Button(Style = this.Styles.ButtonStyle) |> apply setUp
        member this.GenerateButton(view, property, [<ParamArray>] setUp: (Button -> unit)[]) = new Button(Style = this.Styles.ButtonStyle) |> initialise property view |> apply setUp
        member this.GenerateLabel([<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.LabelStyle) |> apply setUp
        member this.GenerateLabel(view, property, [<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.LabelStyle) |> initialise property view |> apply setUp
        member this.GenerateTitle([<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.TitleStyle) |> apply setUp
        member this.GenerateTitle(view, property, [<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.TitleStyle) |> initialise property view |> apply setUp
        member this.GenerateSubtitle([<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.SubtitleStyle) |> apply setUp
        member this.GenerateSubtitle(view, property, [<ParamArray>] setUp: (Label -> unit)[]) = new Label(Style = this.Styles.SubtitleStyle) |> initialise property view |> apply setUp
        member this.GenerateSwitch([<ParamArray>] setUp: (Switch -> unit)[]) = new Switch(Style = this.Styles.SwitchStyle) |> apply setUp
        member this.GenerateSwitch(view, property, [<ParamArray>] setUp: (Switch -> unit)[]) = new Switch(Style = this.Styles.SwitchStyle) |> initialise property view |> apply setUp
        member this.GenerateEntry([<ParamArray>] setUp: (Entry -> unit)[]) = new Entry(Style = this.Styles.EntryStyle) |> apply setUp
        member this.GenerateEntry(view, property, [<ParamArray>] setUp: (Entry -> unit)[]) = new Entry(Style = this.Styles.EntryStyle) |> initialise property view |> apply setUp
        member this.GeneratePassword([<ParamArray>] setUp: (Entry -> unit)[]) = new Entry(Style = this.Styles.EntryStyle, IsPassword = true) |> apply setUp
        member this.GeneratePassword(view, property, [<ParamArray>] setUp: (Entry -> unit)[]) = new Entry(Style = this.Styles.EntryStyle, IsPassword = true) |> initialise property view |> apply setUp
        member this.GenerateEditor([<ParamArray>] setUp: (Editor -> unit)[]) = new Editor(Style = this.Styles.EditorStyle) |> apply setUp
        member this.GenerateEditor(view, property, [<ParamArray>] setUp: (Editor -> unit)[]) = new Editor(Style = this.Styles.EditorStyle) |> initialise property view |> apply setUp
        member this.GenerateHyperlink([<ParamArray>] setUp: (HyperlinkLabel -> unit)[]) = new HyperlinkLabel(Style = this.Styles.HyperlinkStyle) |> apply setUp
        member this.GenerateHyperlink(view, property, [<ParamArray>] setUp: (HyperlinkLabel -> unit)[]) = new HyperlinkLabel(Style = this.Styles.HyperlinkStyle) |> initialise property view |> apply setUp
        member this.GenerateListView(cachingStrategy: ListViewCachingStrategy, [<ParamArray>] setUp: (ListView -> unit)[]) = new ListView(cachingStrategy, Style = this.Styles.ListViewStyle) |> apply setUp
        member this.GenerateListView(view, property, cachingStrategy: ListViewCachingStrategy, [<ParamArray>] setUp: (ListView -> unit)[]) = new ListView(cachingStrategy, Style = this.Styles.ListViewStyle) |> initialise property view |> apply setUp
        member this.GenerateBoxView([<ParamArray>] setUp: (BoxView -> unit)[]) = new BoxView(Style = this.Styles.BoxViewStyle) |> apply setUp
        member this.GenerateBoxView(view, property, [<ParamArray>] setUp: (BoxView -> unit)[]) = new BoxView(Style = this.Styles.BoxViewStyle) |> initialise property view |> apply setUp
        member this.GenerateAutoCompleteView([<ParamArray>] setUp: (AutoCompleteView -> unit)[]) = new AutoCompleteView(Style = this.Styles.BoxViewStyle) |> apply (Array.append setUp [|fun (v:AutoCompleteView) -> v.Init()|])
        member this.GenerateAutoCompleteView(view, property, [<ParamArray>] setUp: (AutoCompleteView -> unit)[]) = new AutoCompleteView(Style = this.Styles.BoxViewStyle) |> initialise property view |> apply (Array.append setUp [|fun (v:AutoCompleteView) -> v.Init()|])
        member this.GenerateScrollView([<ParamArray>] setUp: (ScrollView -> unit)[]) = new ScrollView(Style = this.Styles.ScrollViewStyle) |> apply setUp
        member this.GenerateScrollView(view, property, [<ParamArray>] setUp: (ScrollView -> unit)[]) = new ScrollView(Style = this.Styles.ScrollViewStyle) |> initialise property view |> apply setUp
        member this.GenerateDatePicker([<ParamArray>] setUp: (DatePicker -> unit)[]) = new DatePicker(Style = this.Styles.DatePickerStyle) |> apply setUp
        member this.GenerateDatePicker(view, property, [<ParamArray>] setUp: (DatePicker -> unit)[]) = new DatePicker(Style = this.Styles.DatePickerStyle) |> initialise property view |> apply setUp
        member this.GenerateTimePicker([<ParamArray>] setUp: (TimePicker -> unit)[]) = new TimePicker(Style = this.Styles.TimePickerStyle) |> apply setUp
        member this.GenerateTimePicker(view, property, [<ParamArray>] setUp: (TimePicker -> unit)[]) = new TimePicker(Style = this.Styles.TimePickerStyle) |> initialise property view  |> apply setUp
        member this.GeneratePicker([<ParamArray>] setUp: (Picker -> unit)[]) = new Picker(Style = this.Styles.PickerStyle) |> apply setUp
        member this.GeneratePicker(view, property, [<ParamArray>] setUp: (Picker -> unit)[]) = new Picker(Style = this.Styles.PickerStyle) |> initialise property view |> apply setUp
        member this.GenerateActivityIndicator([<ParamArray>] setUp: (ActivityIndicator -> unit)[]) = new ActivityIndicator(Style = this.Styles.ActivityIndicatorStyle) |> apply setUp
        member this.GenerateActivityIndicator(view, property, [<ParamArray>] setUp: (ActivityIndicator -> unit)[]) = new ActivityIndicator(Style = this.Styles.ActivityIndicatorStyle) |> initialise property view |> apply setUp
        member this.GenerateTextCell([<ParamArray>] setUp: (TextCell -> unit)[]) = new TextCell() |> apply setUp |> applyCellColors this.Styles
        member this.GenerateImageCell([<ParamArray>] setUp: (ImageCell -> unit)[]) = new ImageCell() |> apply setUp |> applyCellColors this.Styles
        member __.GenerateBadgeIcon([<ParamArray>] setUp: (BadgeIcon -> unit)[]) = new BadgeIcon() |> apply setUp |> initialiseBadgeIcon
        member __.GenerateBadgeIcon(view, property, [<ParamArray>] setUp: (BadgeIcon -> unit)[]) = new BadgeIcon() |> initialise property view |> apply setUp |> initialiseBadgeIcon
        member __.GenerateViewCell([<ParamArray>] setUp: (ViewCell -> unit)[]) = new ViewCell() |> apply setUp
        member __.GenerateCustomCell<'TCell when 'TCell :> ViewCell and 'TCell : (new : unit -> 'TCell)> ([<ParamArray>] setUp: ('TCell -> unit)[]) = new 'TCell() |> apply setUp
        member __.GenerateToolbarItem(name, icon, activated, toolbarItemOrder, priority) = new ToolbarItem(name, icon, activated, toolbarItemOrder, priority)
        member __.VerticalLayout([<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Vertical) |> apply setUp
        member __.HorizontalLayout([<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Horizontal) |> apply setUp
        member __.VerticalLayout(view, property, [<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Vertical) |> initialise property view |> apply setUp
        member __.HorizontalLayout(view, property, [<ParamArray>] setUp: (StackLayout -> unit)[]) = new StackLayout (Orientation = StackOrientation.Horizontal) |> initialise property view |> apply setUp
        member __.AbsoluteLayout([<ParamArray>] setUp: (AbsoluteLayout -> unit)[]) = new AbsoluteLayout () |> apply setUp
        member __.AbsoluteLayout(view, property, [<ParamArray>] setUp: (AbsoluteLayout -> unit)[]) = new AbsoluteLayout () |> initialise property view |> apply setUp
        member __.RelativeLayout([<ParamArray>] setUp: (RelativeLayout -> unit)[]) = new RelativeLayout () |> apply setUp
        member __.GenerateGrid(rowDefinitions, columnDefinitions, [<ParamArray>] setUp: (Grid -> unit)[]) = setUpGridFromText (new Grid() |> apply setUp) (rowDefinitions, columnDefinitions)
        member __.GenerateGrid(rowDefinitions, columnDefinitions, [<ParamArray>] setUp: (Grid -> unit)[]) = setUpGridFromSizes (new Grid() |> apply setUp) (rowDefinitions, columnDefinitions)
    let addSetters<'TView when 'TView :> Element> (setters: Setter seq) (style: Style) = setters |> Seq.iter style.Setters.Add
    let applyButtonSetters buttonSetters (theme: Theme) = addSetters<Button> buttonSetters theme.Styles.ButtonStyle; theme
    let applyLabelSetters labelSetters (theme: Theme) = addSetters<Label> labelSetters theme.Styles.LabelStyle; theme
    let applyTitleSetters titleSetters (theme: Theme) = addSetters<Label> titleSetters theme.Styles.TitleStyle; theme
    let applySubtitleSetters subtitleSetters (theme: Theme) = addSetters<Label> subtitleSetters theme.Styles.SubtitleStyle; theme
    let applyHyperlinkSetters hyperlinkSetters (theme: Theme) = addSetters<HyperlinkLabel> hyperlinkSetters theme.Styles.HyperlinkStyle; theme
    let applySwitchSetters switchSetters (theme: Theme) = addSetters<Switch> switchSetters theme.Styles.SwitchStyle; theme
    let applyEntrySetters entrySetters (theme: Theme) = addSetters<Entry> entrySetters theme.Styles.EntryStyle; theme
    let applyImageSetters imageSetters (theme: Theme) = addSetters<Image> imageSetters theme.Styles.ImageStyle; theme
    let applyPickerSetters pickerSetters (theme: Theme) = addSetters<Picker> pickerSetters theme.Styles.PickerStyle; theme
    let applyNavigationPageSetters navigationPageSetters (theme: Theme) = addSetters<NavigationPage> navigationPageSetters theme.Styles.NavigationPageStyle; theme
    let applyListViewSetters listViewSetters (theme: Theme) = addSetters<ListView> listViewSetters theme.Styles.ListViewStyle; theme
    let applyMenuItemSetters menuItemSetters (theme: Theme) = addSetters<MenuItem> menuItemSetters theme.Styles.MenuItemStyle; theme
    let applyTabbedPageSetters tabbedPageSetters (theme: Theme) = addSetters<TabbedPage> tabbedPageSetters theme.Styles.TabbedPageStyle; theme
    let applyBackgroundColor color (theme: Theme) = { theme with Styles = { theme.Styles with BackgroundColor = color } }
    let applySeparatorColor color (theme: Theme) = { theme with Styles = { theme.Styles with SeparatorColor = color } }
    let applyTextCellTextColor color (theme: Theme) = { theme with Styles = { theme.Styles with TextCellTextColor = color } }
    let applyTextCellDetailColor color (theme: Theme) = { theme with Styles = { theme.Styles with TextCellDetailColor = color } }
    let withFacebookBackground (element: #VisualElement) = element.BackgroundColor <- FacebookBlue; element
    let withSuccessStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor SuccessDark |> withBackgroundColor SuccessLight; element
    let withInfoStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor InfoDark |> withBackgroundColor InfoLight; element
    let withWarningStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor WarningDark |> withBackgroundColor WarningLight; element
    let withErrorStyle (element: #VisualElement) = element.Style <- element |> getOrAddStyle |> withBackgroundColor ErrorLight; element
    let withInverseSuccessStyle (element: #VisualElement) = element.Style <- element |> getOrAddStyle |> withBackgroundColor SuccessDark; element
    let withInverseInfoStyle (element: #VisualElement) = element.Style <- element |> getOrAddStyle |> withBackgroundColor InfoDark; element
    let withInverseWarningStyle (element: #VisualElement) = element.Style <- element |> getOrAddStyle |> withBackgroundColor WarningDark; element
    let withInverseErrorStyle (element: #VisualElement) = element.Style <- element |> getOrAddStyle |> withForegroundColor ErrorLight |> withBackgroundColor ErrorDark; element
    let withSuccessLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor SuccessDark |> withBackgroundColor SuccessLight; element
    let withInfoLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor InfoDark |> withBackgroundColor InfoLight; element
    let withWarningLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor WarningDark |> withBackgroundColor WarningLight; element
    let withErrorLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor ErrorDark |> withBackgroundColor ErrorLight; element
    let withInverseSuccessLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor SuccessLight |> withBackgroundColor SuccessDark; element
    let withInverseInfoLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor InfoLight |> withBackgroundColor InfoDark; element
    let withInverseWarningLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor WarningLight |> withBackgroundColor WarningDark; element
    let withInverseErrorLabelStyle (element: #Label) = element.Style <- element |> getOrAddStyle |> withForegroundColor ErrorLight |> withBackgroundColor ErrorDark; element

    let DefaultTheme =
        let titleStyle = new Style(typeof<Label>)
        titleStyle.Setters.Add(new Setter(Property = Label.FontSizeProperty, Value = Device.GetNamedSize (NamedSize.Large, typeof<Label>)))
        titleStyle.Setters.Add(new Setter(Property = Label.FontAttributesProperty, Value = FontAttributes.Bold))
        let subtitleStyle = new Style(typeof<Label>)
        subtitleStyle.Setters.Add(new Setter(Property = Label.FontSizeProperty, Value = Device.GetNamedSize (NamedSize.Small, typeof<Label>)))
        let boxViewStyle = new Style(typeof<BoxView>)
        boxViewStyle.Setters.Add(new Setter(Property = BoxView.ColorProperty, Value = Color.Yellow))
        {
            Styles =
                {
                    BackgroundColor = Color.Black
                    SeparatorColor = Color.White
                    LabelStyle = new Style(typeof<Label>)
                    TitleStyle = titleStyle
                    SubtitleStyle = subtitleStyle
                    HyperlinkStyle = new Style(typeof<HyperlinkLabel>)
                    ButtonStyle = new Style(typeof<Button>)
                    EntryStyle = new Style(typeof<Entry>)
                    EditorStyle = new Style(typeof<Editor>)
                    SearchBarStyle = new Style(typeof<SearchBar>)
                    ImageStyle = new Style(typeof<Image>)
                    FrameStyle = new Style(typeof<Frame>)
                    ContentViewStyle = new Style(typeof<ContentView>)
                    SwitchStyle = new Style(typeof<Switch>)
                    ListViewStyle = new Style(typeof<ListView>)
                    BoxViewStyle = boxViewStyle
                    MenuItemStyle = new Style(typeof<MenuItem>)
                    ScrollViewStyle = new Style(typeof<ScrollView>)
                    DatePickerStyle = new Style(typeof<DatePicker>)
                    TimePickerStyle = new Style(typeof<TimePicker>)
                    PickerStyle = new Style(typeof<Picker>)
                    TabbedPageStyle = new Style(typeof<TabbedPage>)
                    ActivityIndicatorStyle = new Style(typeof<ActivityIndicator>)
                    TextCellTextColor = TextCell.TextColorProperty.DefaultValue :?> Color
                    TextCellDetailColor = TextCell.DetailColorProperty.DefaultValue :?> Color
                    NavigationPageStyle = new Style(typeof<NavigationPage>)
                }
        }

type IUiContext = abstract Context: obj
type UiContext(context) = interface IUiContext with member __.Context = context
