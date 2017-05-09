namespace XamarinForms.Reactive.Samples.Common

module Themes =
    open XamarinForms.Reactive.FSharp.Themes
    open Xamarin.Forms

    let CustomTheme = 
        DefaultTheme 
            |> applyLabelSetters 
                [
                    new Setter(Property = Label.TextColorProperty, Value = Color.Yellow)
                    new Setter(Property = Label.FontAttributesProperty, Value = FontAttributes.Bold)
                ]
            |> applyTitleSetters
                [
                    new Setter(Property = Label.TextColorProperty, Value = Color.Silver)
                ]
            |> applyTabbedPageSetters
                [
                    new Setter(Property = TabbedPage.BarBackgroundColorProperty, Value = Color.MidnightBlue)
                ]
