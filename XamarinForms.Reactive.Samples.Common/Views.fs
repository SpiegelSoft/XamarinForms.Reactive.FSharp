namespace XamarinForms.Reactive.Samples.Common

open XamarinForms.Reactive.FSharp

open Xamarin.Forms

open ViewHelpers
open Themes

type DashboardView(theme: Theme) = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(CustomTheme)
    override this.CreateContent() =
        theme.GenerateGrid([|"Auto"; "Auto"; "Auto"; "Auto"|], [|"Auto"; "*"|]) |> withRow(
            [|
                theme.GenerateTitle(fun l -> this.PageTitle <- l) 
                    |> withColumnSpan 2 
                    |> withAlignment LayoutOptions.Center LayoutOptions.Center
                    |> withMargin (new Thickness(0.0, 12.0))
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.PageTitle @>, <@ fun (v: DashboardView) -> (v.PageTitle: Label).Text @>, id)
            |]) |> thenRow(
            [|
                theme.GenerateLabel() |> withLabelText("Your name")
                theme.GenerateEntry(fun e -> this.UserName <- e) 
                    |> withEntryPlaceholder "Enter your name here"
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Name @>, <@ fun (v: DashboardView) -> (v.UserName: Entry).Text @>, id, id)
            |]) |> thenRow(
            [|
                theme.GenerateLabel() |> withLabelText("Date of birth")
                theme.GenerateDatePicker(fun e -> this.UserDateOfBirth <- e) 
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.DateOfBirth @>, <@ fun (v: DashboardView) -> (v.UserDateOfBirth: DatePicker).Date @>, id, id)
            |]) |> thenRow(
            [|
                theme.GenerateButton(fun b -> this.SubmitButton <- b)
                    |> withColumnSpan 2
                    |> withCaption("Submit")
                    |> withHorizontalOptions LayoutOptions.End
                    |> withCommandBinding (this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SubmitDetails @>, <@ fun (v: DashboardView) -> v.SubmitButton @>)
            |])
            |> createFromRows |> withMargin (new Thickness(6.0, 0.0)) :> View
    member val SubmitButton = Unchecked.defaultof<Button> with get, set
    member val PageTitle = Unchecked.defaultof<Label> with get, set
    member val UserName = Unchecked.defaultof<Entry> with get, set
    member val UserDateOfBirth = Unchecked.defaultof<DatePicker> with get, set

