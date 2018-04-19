using System;
using TDCARExample.iOS.ViewRenderers;
using TDCARExample.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using ARKit;
using SceneKit;
using UIKit;
using CoreGraphics;
using OpenTK;
using System.ComponentModel;

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
            this.defaultModel = new Model
            {
                FileName = "art.scnassets/sharkinho",
                NodeName = "shark",
                Scale = 0.02f,
                XOffset = 0.5f,
                YOffset = -1f,
                ZOffset = -4f
            };
            this.altModel = new Model
            {
                FileName = "art.scnassets/minigodzilla",
                NodeName = "Armature",
                Scale = 0.008f,
                XOffset = 0.5f,
                YOffset = 0,
                ZOffset = -1f
            };

            this.currentModel = this.altModel;
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
            this.scene.Delegate = this;
            
            SetNativeControl(this.scene);

            Element.OnTapped += HandleTap;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ARView.UseAlternativeModelProperty.PropertyName)
            {
                this.currentModel = !Element.UseAlternativeModel ? altModel : defaultModel;
                if (this.hasAnchor)
                {
                    
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        private void HandleTap()
        {
            if (!this.hasAnchor)
            {
                this.hasAnchor = AddAnchorToScene();
                Console.WriteLine("Anchor added:" + hasAnchor);
                return;
            }
        }

        private bool AddAnchorToScene()
        {
            // Get the current frame
            var frame = this.scene.Session.CurrentFrame;
            if (frame == null) 
                return false;

            // Create a ray to test from
            var point = new CGPoint(0.5, 0.5);

            // Preform hit testing on frame
            var results = frame.HitTest(point, ARHitTestResultType.ExistingPlane | ARHitTestResultType.EstimatedHorizontalPlane);

            // Use the first result
            if (results.Length > 0)
            {

                var result = results[0];
                // Create an anchor for it
                var anchor = new ARAnchor(result.WorldTransform);
                // Add anchor to session
                this.scene.Session.AddAnchor(anchor);

                InitializeModel(anchor);

                return true;
            }

            return false;
        }

        private SCNVector3 Translation(NMatrix4 self) => new SCNVector3(self.M14, self.M24, self.M34);

        public void InitializeModel(ARAnchor anchor)
        {
            // Create a new scene
            var modelScene = SCNScene.FromFile(this.currentModel.FileName);

            // Set the scene to the view
            this.scene.Scene = modelScene;
            // Find the model and position it just in front of the camera
            SCNNode node = this.scene.Scene.RootNode.FindChildNode(this.currentModel.NodeName, true);
            node.Scale = new SCNVector3(0.5f, 0.5f, 0,5f);
            var monsterPosition = Translation(anchor.Transform);
            //monsterPosition.X += 0.055f;
            //monsterPosition.Y -= 0.004f;
            //monsterPosition.Z -= 0.0169f;

            monsterPosition.X += this.currentModel.XOffset;
            monsterPosition.Y += this.currentModel.YOffset;
            monsterPosition.Z += this.currentModel.ZOffset;
            node.Position = new SCNVector3(1.0f, -0.5f, 5.0);
            Console.WriteLine("Initialized with position X{0} Y{1} Z{2}", node.Position.X, node.Position.Y, node.Position.Z);
        }


    }

    public class Model
    {
        public string FileName { get; set; }
        public string NodeName { get; set; }
        public float Scale { get; set; }
        public float XOffset { get; set; }
        public float YOffset { get; set; }
        public float ZOffset { get; set; }
    }
}
