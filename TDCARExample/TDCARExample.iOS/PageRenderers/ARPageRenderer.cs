using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using TDCARExample;
using TDCARExample.iOS.PageRenderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using ARKit;
using SceneKit;

[assembly: ExportRenderer(typeof(ARPage), typeof(ARPageRenderer))]
namespace TDCARExample.iOS.PageRenderers
{
    public class ARPageRenderer : PageRenderer, IARSCNViewDelegate
    {
        private ARSCNView scene = new ARSCNView();

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            try
            {
                this.scene.Delegate = this;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ViewDidLoad: {ex.Message}");
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            try
            {
                this.scene.Bounds = new CoreGraphics.CGRect(0, 0, View.Bounds.Width * 2, View.Bounds.Height * 2);
                var config = new ARWorldTrackingConfiguration
                {
                    PlaneDetection = ARPlaneDetection.Horizontal,
                    LightEstimationEnabled = true
                };

                this.scene.SetDebugOptions(ARSCNDebugOptions.ShowFeaturePoints);
                this.scene.Session.Run(config, ARSessionRunOptions.ResetTracking);

                Add(this.scene);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ViewWillAppear: {ex.Message}");
            }
        }
    }
}