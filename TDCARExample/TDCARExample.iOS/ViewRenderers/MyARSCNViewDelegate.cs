using System;
using ARKit;
using OpenTK;
using SceneKit;

namespace TDCARExample.iOS.ViewRenderers
{
    public class MyARSCNViewDelegate : ARSCNViewDelegate
    {
        private readonly ARSCNView arView;

        public MyARSCNViewDelegate(ARSCNView arView)
        {
            this.arView = arView;
        }
        public Model CurrentModel { get; set; }

        public override void DidAddNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            

            if (anchor is ARPlaneAnchor planeAnchor)
            {
                var plane = new SCNPlane
                {
                    Width = planeAnchor.Extent.X,
                    Height = planeAnchor.Extent.Z
                };
                var planeNode = new SCNNode { Geometry = plane };
                planeNode.Position = new SCNVector3(planeAnchor.Center.X, 0, planeAnchor.Center.Z);

                planeNode.EulerAngles = new SCNVector3((float)(-Math.PI / 2), planeNode.EulerAngles.Y, planeNode.EulerAngles.Z);
                planeNode.Opacity = (nfloat)0.25;
                node.AddChildNode(planeNode);
            }

            //base.DidAddNode(renderer, node, anchor);
        }

        private SCNVector3 Translation(NMatrix4 self) => new SCNVector3(self.M14, self.M24, self.M34);

        public void InitializeModel(ARAnchor anchor)
        {
            // Create a new scene
            var modelScene = SCNScene.FromFile(CurrentModel.FileName);

            // Set the scene to the view
            this.arView.Scene = modelScene;
            // Find the model and position it just in front of the camera
            SCNNode node = this.arView.Scene.RootNode.FindChildNode(CurrentModel.NodeName, true);
            node.Scale = new SCNVector3(CurrentModel.Scale, CurrentModel.Scale, CurrentModel.Scale);
            var monsterPosition = Translation(anchor.Transform);
            //monsterPosition.X += 0.055f;
            //monsterPosition.Y -= 0.004f;
            //monsterPosition.Z -= 0.0169f;

            monsterPosition.X += CurrentModel.XOffset;
            monsterPosition.Y += CurrentModel.YOffset;
            monsterPosition.Z += CurrentModel.ZOffset;
            node.Position = monsterPosition;
            Console.WriteLine("Initialized with position X{0} Y{1} Z{2}", node.Position.X, node.Position.Y, node.Position.Z);
        }
    }
}
