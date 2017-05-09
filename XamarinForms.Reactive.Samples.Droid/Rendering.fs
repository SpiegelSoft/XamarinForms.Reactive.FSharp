namespace XamarinForms.Reactive.Samples.Droid

open Xamarin.Forms.Platform.Android

open Android.Graphics.Drawables
open Android.Graphics
open Android.App

type TabbedPageRenderer() =
    inherit TabbedRenderer()
    let mutable activity = Unchecked.defaultof<Activity>
    let mutable isFirstDesign = true
    override this.OnElementPropertyChanged(sender, e) =
        base.OnElementPropertyChanged(sender, e)
        activity <- this.Context :?> Activity
    override this.OnWindowVisibilityChanged(visibility) =
        base.OnWindowVisibilityChanged visibility
        if isFirstDesign then
            let actionBar = activity.ActionBar
            let colour = new ColorDrawable(Color.MidnightBlue)
            actionBar.SetStackedBackgroundDrawable(colour)
            isFirstDesign <- false
