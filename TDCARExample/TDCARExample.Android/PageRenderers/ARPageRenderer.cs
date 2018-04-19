using Android.App;
using Android.Content;
using Android.Opengl;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using Javax.Microedition.Khronos.Opengles;
using System;
using TDCARExample;
using TDCARExample.Droid.PageRenderers;
using TDCARExample.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using ARFrame = Google.AR.Core.Frame;
using ARConfig = Google.AR.Core.Config;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Android.Views;

[assembly: ExportRenderer(typeof(ARPage), typeof(ARPageRenderer))]
namespace TDCARExample.Droid.PageRenderers
{
    public class ARPageRenderer : PageRenderer, GLSurfaceView.IRenderer
    {
		const string TAG = "TDC-AR-EXAMPLE";


        private GLSurfaceView surfaceView;
        private Session session;
        private BackgroundRenderer backgroundRenderer = new BackgroundRenderer();
        private DisplayRotationHelper displayRotationHelper;

        private ObjectRenderer virtualObject = new ObjectRenderer();
        private ObjectRenderer virtualObjectShadow = new ObjectRenderer();
        private PlaneRenderer planeRenderer = new PlaneRenderer();
        private PointCloudRenderer pointCloud = new PointCloudRenderer();

        private ContentPage FormsPage => Element as ContentPage;

        private Activity Activity => Context as Activity;

        // Temporary matrix allocated here to reduce number of allocations for each frame.
        static float[] anchorMatrix = new float[16];

        ConcurrentQueue<MotionEvent> mQueuedSingleTaps = new ConcurrentQueue<MotionEvent>();

        // Tap handling and UI.
        List<Anchor> anchors = new List<Anchor>();

        public ARPageRenderer(Context context) : base (context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            FormsPage.Appearing += OnPageAppearing;

            Activity.SetContentView(Resource.Layout.ARView);

            this.surfaceView = Activity.FindViewById<GLSurfaceView>(Resource.Id.surfaceview);
            this.displayRotationHelper = new DisplayRotationHelper(Context);

            CheckARSupport();
            
            var config = new ARConfig(this.session);
            if (!this.session.IsSupported(config))
            {
                Toast.MakeText(Context, "This device does not support AR", ToastLength.Long).Show();
                Activity.Finish();
                return;
            }

            this.surfaceView.PreserveEGLContextOnPause = true;
            this.surfaceView.SetEGLContextClientVersion(2);
            this.surfaceView.SetEGLConfigChooser(8, 8, 8, 8, 16, 0); // Alpha used for plane blending.
            this.surfaceView.SetRenderer(this);
            this.surfaceView.RenderMode = Rendermode.Continuously;
        }

        private void OnPageAppearing(object sender, EventArgs e)
        {

            if (ContextCompat.CheckSelfPermission(Context, Android.Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
            {
                if (this.session != null)
                {
                    this.session.Resume();
                }

                this.surfaceView.OnResume();
            }
            else
            {
                ActivityCompat.RequestPermissions(Activity, new string[] { Android.Manifest.Permission.Camera }, 0);
            }

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
                MotionEvent tap = null;
                mQueuedSingleTaps.TryDequeue(out tap);

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
                            if (anchors.Count >= 16)
                            {
                                anchors[0].Detach();
                                anchors.RemoveAt(0);
                            }
                            // Adding an Anchor tells ARCore that it should track this position in
                            // space.  This anchor is created on the Plane to place the 3d model
                            // in the correct position relative to both the world and to the plane
                            anchors.Add(hit.CreateAnchor());

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

                // Visualize planes.
                this.planeRenderer.DrawPlanes(planes, camera.DisplayOrientedPose, projmtx);

                // Visualize anchors created by touch.
                float scaleFactor = 1.0f;
                foreach (var anchor in anchors)
                {
                    if (anchor.TrackingState != TrackingState.Tracking)
                        continue;

                    // Get the current combined pose of an Anchor and Plane in world space. The Anchor
                    // and Plane poses are updated during calls to session.update() as ARCore refines
                    // its estimate of the world.
                    anchor.Pose.ToMatrix(anchorMatrix, 0);

                    // Update and draw the model and its shadow.
                    this.virtualObject.updateModelMatrix(anchorMatrix, scaleFactor);
                    this.virtualObjectShadow.updateModelMatrix(anchorMatrix, scaleFactor);
                    this.virtualObject.Draw(viewmtx, projmtx, lightIntensity);
                    this.virtualObjectShadow.Draw(viewmtx, projmtx, lightIntensity);
                }

            }
            catch (System.Exception ex)
            {
                // Avoid crashing the application due to unhandled exceptions.
                Log.Error(TAG, "Exception on the OpenGL thread", ex);
            }

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
                virtualObject.CreateOnGlThread(Context, "andy.obj", "andy.png");
                virtualObject.setMaterialProperties(0.0f, 3.5f, 1.0f, 6.0f);

                virtualObjectShadow.CreateOnGlThread(Context,
                    "andy_shadow.obj", "andy_shadow.png");
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
    }
}