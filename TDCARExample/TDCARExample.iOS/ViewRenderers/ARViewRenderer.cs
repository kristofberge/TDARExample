using System;
using System.Collections.Specialized;
using System.Linq;
using ARKit;
using CoreGraphics;
using OpenTK;
using SceneKit;
using TDCARExample.iOS.ViewRenderers;
using TDCARExample.Models;
using TDCARExample.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ARView), typeof(ARViewRenderer))]
namespace TDCARExample.iOS.ViewRenderers
{
    public class ARViewRenderer : ViewRenderer<ARView, ARSCNView>, IARSCNViewDelegate
    {
        private readonly Model defaultModel;
        private readonly Model altModel;
        private ARSCNView scene;
        private bool hasAnchor;
        private Model currentModel;

        public ARViewRenderer()
        {
            //this.defaultModel = new Model
            //{
            //    FileName = "art.scnassets/sharkinho",
            //    NodeName = "shark",
            //    Scale = 0.02f,
            //    XOffset = 0.5f,
            //    YOffset = -1f,
            //    ZOffset = -4f
            //};
            //this.altModel = new Model
            //{
            //    FileName = "art.scnassets/minigodzilla",
            //    NodeName = "Armature",
            //    Scale = 0.008f,
            //    XOffset = 0.5f,
            //    YOffset = 0,
            //    ZOffset = -1f
            //};
        }

        protected override void OnElementChanged(ElementChangedEventArgs<ARView> e)
        {
            base.OnElementChanged(e);

            this.scene = new ARSCNView();

            var config = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal,
                LightEstimationEnabled = true
            };

            this.scene.SetDebugOptions(ARSCNDebugOptions.ShowFeaturePoints);
            this.scene.Session.Run(config, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);

            this.scene.AddGestureRecognizer(new UITapGestureRecognizer(HandleTap));
            SetNativeControl(this.scene);

            this.currentModel = altModel;

            Element.VirtualObjects.CollectionChanged += OnVirtualObjectsChanged;
        }

        private void HandleTap(UITapGestureRecognizer recognizer)
        {
            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                CGPoint point = recognizer.LocationInView(this.scene);
                ARHitTestResult[] hits = this.scene.HitTest(point, ARHitTestResultType.EstimatedHorizontalPlane | ARHitTestResultType.ExistingPlane);
                ARHitTestResult firstHit = hits.FirstOrDefault();
                if (firstHit != null)
                {
                    var coords = firstHit.WorldTransform.Column3;
                    ARAnchor anchor = new ARAnchor(firstHit.WorldTransform);
                    Element.OnPlaneTapped(new WorldPosition(coords.X, coords.Y, coords.Z));
                    this.scene.Session.AddAnchor(anchor);
                }
            }
        }

        private void OnVirtualObjectsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (ARObject obj in e.NewItems)
            {
                var anchor = new ARAnchor(new NMatrix4(0, 0, 0, obj.Coordinates.X, 0, 0, 0, obj.Coordinates.Y, 0, 0, 0, obj.Coordinates.Z, 0, 0, 0, 0));
                this.scene.Session.AddAnchor(anchor);
                InitializeModel(anchor, obj);
            }
        }


        //private bool AddAnchorToScene()
        //{
        //    // Get the current frame
        //    var frame = this.scene.Session.CurrentFrame;
        //    if (frame == null) 
        //        return false;

        //    // Create a ray to test from
        //    var point = new CGPoint(0.5, 0.5);

        //    // Preform hit testing on frame
        //    var results = frame.HitTest(point, ARHitTestResultType.ExistingPlane | ARHitTestResultType.EstimatedHorizontalPlane);

        //    // Use the first result
        //    if (results.Length > 0)
        //    {

        //        var result = results[0];
        //        // Create an anchor for it
        //        var anchor = new ARAnchor(result.WorldTransform);
        //        // Add anchor to session
        //        this.scene.Session.AddAnchor(anchor);

        //        InitializeModel(anchor);

        //        return true;
        //    }

        //    return false;
        //}

        private SCNVector3 Translation(NMatrix4 self) => new SCNVector3(self.M14, self.M24, self.M34);

        public void InitializeModel(ARAnchor anchor, ARObject aRObject)
        {
            var modelScene = SCNScene.FromFile($"art.scnassets/{aRObject.Model.Asset}.dae");
            var newNode = modelScene.RootNode.FindChildNode(aRObject.Model.Id, true);

            var scale = aRObject.Scale;
            newNode.Scale = new SCNVector3(scale, scale, scale);
            newNode.Position = new SCNVector3(aRObject.Coordinates.X, aRObject.Coordinates.Y, aRObject.Coordinates.Z);

            var sphere = SCNNode.FromGeometry(SCNSphere.Create(0.02f));
            sphere.Position = new SCNVector3(aRObject.Coordinates.X, aRObject.Coordinates.Y, aRObject.Coordinates.Z);

            this.scene.Scene.RootNode.AddChildNode(newNode);
            Console.WriteLine("Initialized with position X{0} Y{1} Z{2}", newNode.Position.X, newNode.Position.Y, newNode.Position.Z);
        }
    }
}
