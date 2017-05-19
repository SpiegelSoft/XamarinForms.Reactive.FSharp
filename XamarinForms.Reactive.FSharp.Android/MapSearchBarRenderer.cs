using System;
using Android.Views;

using Xamarin.Forms.Platform.Android;
using XamarinForms.Reactive.FSharp;
using Xamarin.Forms;

using AndroidContext = Android.Content.Context;

[assembly: ExportRenderer(typeof(MapSearchBar), typeof(XamarinForms.Reactive.FSharp.Android.MapSearchBarRenderer))]
namespace XamarinForms.Reactive.FSharp.Android
{
    public class MapSearchBarRenderer : SearchBarRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<SearchBar> e)
        {
            var inflatorService = (LayoutInflater)Context.GetSystemService(AndroidContext.LayoutInflaterService);
            try
            {
                var containerView = inflatorService.Inflate(Resource.Layout.Places, null, false);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            base.OnElementChanged(e);
        }
    }
}