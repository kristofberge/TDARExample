using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TDCARExample.Droid.PageRenderers;
using TDCARExample;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Opengl;

[assembly: ExportRenderer(typeof(ARPage), typeof(ARPageRenderer))]
namespace TDCARExample.Droid.PageRenderers
{
    public class ARPageRenderer : PageRenderer
    {
        private GLSurfaceView surfaceView;

        private Activity Activity => Context as Activity;

        public ARPageRenderer(Context context) : base (context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
            
            Activity.SetContentView(Resource.Layout.ARView);

            this.surfaceView = Activity.FindViewById<GLSurfaceView>(Resource.Id.surfaceview);
        }
    }
}