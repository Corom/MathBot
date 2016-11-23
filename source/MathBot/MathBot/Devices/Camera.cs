using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;

namespace MathBot
{
    public class Camera
    {
        MediaCapture mediaCapture;
        CaptureElement captureElement;
        FaceTracker faceTracker;

        public async Task Initialize(string cameraName = "LifeCam")
        {
            // select the camera
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var device = devices.FirstOrDefault(d => d.Name.ToLowerInvariant().Contains(cameraName.ToLower())) ?? devices.FirstOrDefault();
            var settings = new MediaCaptureInitializationSettings() { VideoDeviceId = device.Id };

            // initialize the camera
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings);

            // select a lower framerate and resolution to reduce USB bandwidth
            var props = mediaCapture
                .VideoDeviceController
                .GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview)
                .Cast<VideoEncodingProperties>()
                .First(p => p.FrameRate.Numerator == 10 && p.Height == 720);
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, props);

            // start the preview feed (a CaptureElement is required to sync the feed)
            captureElement = new CaptureElement() { Source = mediaCapture };
            await mediaCapture.StartPreviewAsync();

            // get the video properties
            var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            ImageHeight = (int)previewProperties.Height;
            ImageWidth = (int)previewProperties.Width;

            // intialize face tracking
            faceTracker = await FaceTracker.CreateAsync();
        }


        public int ImageHeight { get; private set; }
        public int ImageWidth { get; private set; }

        public async Task<IEnumerable<DetectedFace>> LookForFaces()
        {
            Stopwatch sw = Stopwatch.StartNew();

            var capturedPhoto = new VideoFrame(BitmapPixelFormat.Nv12, ImageWidth, ImageHeight);
            await mediaCapture.GetPreviewFrameAsync(capturedPhoto);
            var capture = sw.ElapsedMilliseconds;

            sw = Stopwatch.StartNew();
            var faces = await faceTracker.ProcessNextFrameAsync(capturedPhoto);
            Debug.WriteLine($"{capture}-{sw.ElapsedMilliseconds}");

            return faces;
        }
    }
}
