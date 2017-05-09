namespace XamarinForms.Reactive.Samples.Common

open XamarinForms.Reactive.FSharp

open Xamarin.Forms

open ViewHelpers
open Themes

type DashboardView(theme: Theme) = 
    inherit TabbedPage<DashboardViewModel>(theme)
    new() = new DashboardView(CustomTheme)
    override this.CreateContent() =
       dict [
                ("Hello World",  fun p -> 
                    theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "Auto"|], [|"Auto"; "*"|]) |> withRow(
                        [|
                            theme.GenerateTitle(fun l -> this.PageTitle <- l) 
                                |> withColumnSpan 2 
                                |> withAlignment LayoutOptions.Center LayoutOptions.Center
                                |> withMargin (new Thickness(0.0, 12.0))
                                |> withOneWayBinding(this, <@ fun (vm: DashboardViewModel) -> vm.PageTitle @>, <@ fun (v: DashboardView) -> (v.PageTitle: Label).Text @>, id)
                        |]) |> thenRow(
                        [|
                            theme.GenerateLabel() |> withLabelText("Your name")
                            theme.GenerateEntry(fun e -> this.UserName <- e) 
                                |> withEntryPlaceholder "Enter your name here"
                                |> withTwoWayBinding(this, <@ fun (vm: DashboardViewModel) -> vm.Name @>, <@ fun (v: DashboardView) -> (v.UserName: Entry).Text @>, id, id)
                        |]) |> thenRow(
                        [|
                            theme.GenerateLabel() |> withLabelText("Date of birth")
                            theme.GenerateDatePicker(fun e -> this.UserDateOfBirth <- e) 
                                |> withTwoWayBinding(this, <@ fun (vm: DashboardViewModel) -> vm.DateOfBirth @>, <@ fun (v: DashboardView) -> (v.UserDateOfBirth: DatePicker).Date @>, id, id)
                        |]) |> thenRow(
                        [|
                            theme.GenerateButton(fun b -> this.SubmitButton <- b)
                                |> withColumnSpan 2
                                |> withCaption("Submit")
                                |> withHorizontalOptions LayoutOptions.End
                                |> withCommandBinding (this, <@ fun (vm: DashboardViewModel) -> vm.SubmitDetails @>, <@ fun (v: DashboardView) -> v.SubmitButton @>)
                        |])
                        |> createFromRows |> withMargin (new Thickness(6.0, 0.0)) 
                        :> View)
                ("About XRF", fun p ->
                    theme.GenerateGrid([|"Auto"; "Auto"; "*"|], [|"*"|]) |> withColumn(
                        [|
                            theme.GenerateTitle() |> withLabelText("XamarinForms.Reactive.FSharp") |> withHorizontalOptions LayoutOptions.Center
                            theme.GenerateLabel() |> withLabelText("By SpiegelSoft Ltd") |> withHorizontalOptions LayoutOptions.Center
                            theme.GenerateHyperlink() 
                                |> withLabelText("GitHub Page") |> withHorizontalOptions LayoutOptions.Center |> withVerticalOptions LayoutOptions.End
                                |> withHyperlinkCommand(this.ViewModel.GoToGitHubUrl)
                        |])
                        |> createFromColumns
                        |> withVerticalOptions LayoutOptions.Fill :> View)]
    override this.OnAppearing() =
        this.ToolbarItems.Add(new ToolbarItem("Hello", Unchecked.defaultof<string>, (fun () -> 0 |> ignore), ToolbarItemOrder.Secondary)) |> ignore
    member val SubmitButton = Unchecked.defaultof<Button> with get, set
    member val PageTitle = Unchecked.defaultof<Label> with get, set
    member val UserName = Unchecked.defaultof<Entry> with get, set
    member val UserDateOfBirth = Unchecked.defaultof<DatePicker> with get, set
