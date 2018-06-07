using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using Android.Content;
using Android.Opengl;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using Javax.Microedition.Khronos.Opengles;
using TDCARExample.Droid.Renderers;
using TDCARExample.Droid.ViewRenderers;
using TDCARExample.Models;
using TDCARExample.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using ARConfig = Google.AR.Core.Config;
using ARFrame = Google.AR.Core.Frame;

[assembly: ExportRenderer(typeof(ARView), typeof(ARViewRenderer))]
namespace TDCARExample.Droid.ViewRenderers
{
    public class ARViewRenderer : ViewRenderer<ARView, GLSurfaceView>,
        GLSurfaceView.IRenderer, Android.Views.View.IOnTouchListener
    {
        const string TAG = "TDC-AR-EXAMPLE";

        private Session session;
        private BackgroundRenderer backgroundRenderer = new BackgroundRenderer();
        private DisplayRotationHelper displayRotationHelper;

        private ObjectRenderer andyRenderer = new ObjectRenderer();
        private ObjectRenderer vikingRenderer = new ObjectRenderer();
        private ObjectRenderer virtualObjectShadow = new ObjectRenderer();
        private PlaneRenderer planeRenderer = new PlaneRenderer();
        private PointCloudRenderer pointCloud = new PointCloudRenderer();
        ConcurrentQueue<MotionEvent> mQueuedSingleTaps = new ConcurrentQueue<MotionEvent>();

        private static float[] anchorMatrix = new float[16];
        List<Anchor> anchors = new List<Anchor>();
        private GestureDetector gestureDetector;

        private Dictionary<string, ObjectRenderer> modelToRendererMap;
        private Dictionary<Anchor, (Model Model, float Scale)> anchorModels;

        public ARViewRenderer(Context context) : base(context)
        {
            this.modelToRendererMap = new Dictionary<string, ObjectRenderer>();
            this.anchorModels = new Dictionary<Anchor, (Model Model, float Scale)>();
        }

        private ARView FormsView => Element as ARView;

        protected override void OnElementChanged(ElementChangedEventArgs<ARView> e)
        {
            base.OnElementChanged(e);

            this.displayRotationHelper = new DisplayRotationHelper(Context);

            CheckARSupport();

            var config = new ARConfig(this.session);
            if (!this.session.IsSupported(config))
            {
                Toast.MakeText(Context, "This device does not support AR", ToastLength.Long).Show();
            }
            SetNativeControl(CreateGLSurfaceView());

            gestureDetector = new GestureDetector(Context, new SimpleTapGestureDetector
            {
                SingleTapUpHandler = (MotionEvent arg) =>
                {
                    OnSingleTap(arg);
                    return true;
                },
                DownHandler = (MotionEvent arg) => true
            });
            Control.SetOnTouchListener(this);

            Element.VirtualObjects.CollectionChanged += OnVirtualObjectsChanged;

            this.session.Resume();
        }

        private GLSurfaceView CreateGLSurfaceView()
        {
            var surfaceView = new GLSurfaceView(Context);

            surfaceView.PreserveEGLContextOnPause = true;
            surfaceView.SetEGLContextClientVersion(2);
            surfaceView.SetEGLConfigChooser(8, 8, 8, 8, 16, 0); // Alpha used for plane blending.
            surfaceView.SetRenderer(this);
            surfaceView.RenderMode = Rendermode.Continuously;

            return surfaceView;
        }

        private void CheckARSupport()
        {
            Java.Lang.Exception exception = null;
            string message = null;

            try
            {
                this.session = new Session(Context);
            }
            catch (UnavailableArcoreNotInstalledException e)
            {
                message = "Please install ARCore";
                exception = e;
            }
            catch (UnavailableApkTooOldException e)
            {
                message = "Please update ARCore";
                exception = e;
            }
            catch (UnavailableSdkTooOldException e)
            {
                message = "Please update this app";
                exception = e;
            }
            catch (Java.Lang.Exception e)
            {
                exception = e;
                message = "This device does not support AR";
            }

            if (message != null)
            {
                Toast.MakeText(Context, message, ToastLength.Long).Show();
                return;
            }
        }

        private void OnVirtualObjectsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (ARObject newObject in e.NewItems)
            {
                WorldPosition position = newObject.Coordinates;

                var rotation = new float[] { position.RotationX, position.RotationY, position.RotationZ, position.RotationW };
                var translation = new float[] { position.X, position.Y, position.Z };
                var pose = new Pose(translation, rotation);
                anchorModels.Add(this.session.CreateAnchor(pose), (newObject.Model, newObject.Scale));
            }
        }


        public void OnDrawFrame(IGL10 gl)
        {
            GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit);

            if (this.session == null)
                return;

            // Notify ARCore session that the view size changed so that the perspective matrix and the video background
            // can be properly adjusted
            this.displayRotationHelper.UpdateSessionIfNeeded(this.session);

            try
            {
                // Obtain the current frame from ARSession. When the configuration is set to
                // UpdateMode.BLOCKING (it is by default), this will throttle the rendering to the
                // camera framerate.
                ARFrame frame = this.session.Update();
                Camera camera = frame.Camera;

                // Handle taps. Handling only one tap per frame, as taps are usually low frequency
                // compared to frame rate.
                mQueuedSingleTaps.TryDequeue(out MotionEvent tap);

                if (tap != null && camera.TrackingState == TrackingState.Tracking)
                {
                    foreach (var hit in frame.HitTest(tap))
                    {
                        var trackable = hit.Trackable;

                        // Check if any plane was hit, and if it was hit inside the plane polygon.
                        if (trackable is Plane && ((Plane)trackable).IsPoseInPolygon(hit.HitPose))
                        {
                            // Cap the number of objects created. This avoids overloading both the
                            // rendering system and ARCore.
                            //if (anchors.Count >= 16)
                            //{
                            //    anchors[0].Detach();
                            //    anchors.RemoveAt(0);
                            //}
                            Pose pose = hit.HitPose;
                            var position = new WorldPosition(pose.Tx(), pose.Ty(), pose.Tz(), pose.Qx(), pose.Qy(), pose.Qz(), pose.Qw());
                            Element.OnPlaneTapped(position);
                            // Adding an Anchor tells ARCore that it should track this position in
                            // space.  This anchor is created on the Plane to place the 3d model
                            // in the correct position relative to both the world and to the plane
                            //anchors.Add(hit.CreateAnchor());
                            // Hits are sorted by depth. Consider only closest hit on a plane.
                            break;
                        }
                    }
                }

                // Draw background.
                this.backgroundRenderer.Draw(frame);

                // If not tracking, don't draw 3d objects.
                if (camera.TrackingState == TrackingState.Paused)
                    return;

                // Get projection matrix.
                float[] projmtx = new float[16];
                camera.GetProjectionMatrix(projmtx, 0, 0.1f, 100.0f);

                // Get camera matrix and draw.
                float[] viewmtx = new float[16];
                camera.GetViewMatrix(viewmtx, 0);

                // Compute lighting from average intensity of the image.
                var lightIntensity = frame.LightEstimate.PixelIntensity;

                // Visualize tracked points.
                var pointCloud = frame.AcquirePointCloud();
                this.pointCloud.Update(pointCloud);
                this.pointCloud.Draw(camera.DisplayOrientedPose, viewmtx, projmtx);

                // App is repsonsible for releasing point cloud resources after using it
                pointCloud.Release();

                var planes = new List<Plane>();
                foreach (var p in this.session.GetAllTrackables(Java.Lang.Class.FromType(typeof(Plane))))
                {
                    var plane = (Plane)p;
                    planes.Add(plane);
                }

                // Check if we detected at least one plane. If so, hide the loading message.
                //if (mLoadingMessageSnackbar != null)
                //{
                //    foreach (var plane in planes)
                //    {
                //        if (plane.GetType() == Plane.Type.HorizontalUpwardFacing
                //                && plane.TrackingState == TrackingState.Tracking)
                //        {
                //            hideLoadingMessage();
                //            break;
                //        }
                //    }
                //}

                // Visualize planes.
                this.planeRenderer.DrawPlanes(planes, camera.DisplayOrientedPose, projmtx);

                // Visualize anchors created by touch.
                //float scaleFactor = 1.0f;
                //foreach (var anchor in anchors)
                //{
                //    if (anchor.TrackingState != TrackingState.Tracking)
                //        continue;

                //    // Get the current combined pose of an Anchor and Plane in world space. The Anchor
                //    // and Plane poses are updated during calls to session.update() as ARCore refines
                //    // its estimate of the world.
                //    anchor.Pose.ToMatrix(anchorMatrix, 0);

                //    // Update and draw the model and its shadow.
                //    if (FormsView.UseAlternativeModel)
                //    {
                //        this.vikingRenderer.updateModelMatrix(anchorMatrix, scaleFactor);
                //        this.vikingRenderer.Draw(viewmtx, projmtx, lightIntensity);
                //    }
                //    else
                //    {
                //        this.andyRenderer.updateModelMatrix(anchorMatrix, scaleFactor);
                //        this.andyRenderer.Draw(viewmtx, projmtx, lightIntensity);
                //    }
                //    this.virtualObjectShadow.updateModelMatrix(anchorMatrix, scaleFactor);
                //    this.virtualObjectShadow.Draw(viewmtx, projmtx, lightIntensity);
                //}


                foreach (Anchor anchor in anchorModels.Keys)
                {
                    if (anchor.TrackingState != TrackingState.Tracking)
                    {
                        continue;
                    }

                    anchor.Pose.ToMatrix(anchorMatrix, 0);

                    Model model = anchorModels[anchor].Model;
                    if (!this.modelToRendererMap.ContainsKey(model.Id))
                    {
                        CreateNewRenderer(model);
                    }

                    ObjectRenderer renderer = this.modelToRendererMap[model.Id];
                    renderer.updateModelMatrix(anchorMatrix, anchorModels[anchor].Scale);
                    renderer.Draw(viewmtx, projmtx, lightIntensity);
                }
            }
            catch (Exception ex)
            {
                // Avoid crashing the application due to unhandled exceptions.
                Log.Error(TAG, "Exception on the OpenGL thread", ex);
            }

        }

        private void CreateNewRenderer(Model model)
        {
            var renderer = new ObjectRenderer();
            renderer.CreateOnGlThread(Context, $"{model.Asset}.obj", $"{model.Texture}.png");
            renderer.setMaterialProperties(0.0f, 3.5f, 1.0f, 6.0f);
            this.modelToRendererMap.Add(model.Id, renderer);
        }

        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            displayRotationHelper.OnSurfaceChanged(width, height);
            GLES20.GlViewport(0, 0, width, height);
        }

        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            GLES20.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f);

            // Create the texture and pass it to ARCore session to be filled during update().
            this.backgroundRenderer.CreateOnGlThread(Context);
            if (this.session != null)
            {
                this.session.SetCameraTextureName(backgroundRenderer.TextureId);
            }

            // Prepare the other rendering objects.
            try
            {
                andyRenderer.CreateOnGlThread(Context, "andy.obj", "andy.png");
                vikingRenderer.CreateOnGlThread(Context, "viking.obj", "viking.png");
                andyRenderer.setMaterialProperties(0.0f, 3.5f, 1.0f, 6.0f);
                vikingRenderer.setMaterialProperties(0.0f, 3.5f, 1.0f, 6.0f);


                virtualObjectShadow.CreateOnGlThread(Context, "andy_shadow.obj", "andy_shadow.png");
                virtualObjectShadow.SetBlendMode(ObjectRenderer.BlendMode.Shadow);
                virtualObjectShadow.setMaterialProperties(1.0f, 0.0f, 0.0f, 1.0f);
            }
            catch (Java.IO.IOException e)
            {
                Log.Error(TAG, "Failed to read obj file");
            }

            try
            {
                planeRenderer.CreateOnGlThread(Context, "trigrid.png");
            }
            catch (Java.IO.IOException e)
            {
                Log.Error(TAG, "Failed to read plane texture");
            }
            pointCloud.CreateOnGlThread(Context);
        }

        private void OnSingleTap(MotionEvent e)
        {
            // Queue tap if there is space. Tap is lost if queue is full.
            if (mQueuedSingleTaps.Count < 16)
                mQueuedSingleTaps.Enqueue(e);
        }

        public bool OnTouch(Android.Views.View v, MotionEvent e)
        {
            return gestureDetector.OnTouchEvent(e);
        }
    }

    internal class SimpleTapGestureDetector : GestureDetector.SimpleOnGestureListener
    {
        public Func<MotionEvent, bool> SingleTapUpHandler { get; set; }

        public override bool OnSingleTapUp(MotionEvent e)
        {
            return SingleTapUpHandler?.Invoke(e) ?? false;
        }

        public Func<MotionEvent, bool> DownHandler { get; set; }

        public override bool OnDown(MotionEvent e)
        {
            return DownHandler?.Invoke(e) ?? false;
        }
    }
}
