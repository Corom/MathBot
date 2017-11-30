using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace MathBot
{
    public class Camera
    {
        MediaCapture mediaCapture;
        CaptureElement captureElement;
        FaceTracker faceTracker;
        FaceServiceClient faceClient;
        EmotionServiceClient emotClient;
        VisionServiceClient visionClient;

        const string personGroupId = "YOUR_PERSON_GROUP_ID";  // "MathBot" person group in workspace id "Corom" 
        Dictionary<Guid, string> personMap;

        public Camera()
        {
            faceClient = new FaceServiceClient("YOUR_API_KEY");
            emotClient = new EmotionServiceClient("YOUR_API_KEY");
            visionClient = new VisionServiceClient("YOUR_API_KEY");
        }

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

            // Get the known persons
            var persons = await faceClient.GetPersonsAsync(personGroupId);
            personMap = persons.ToDictionary(p => p.PersonId, p => p.Name);
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

        public async Task<ImageInfo> See()
        {
            var capturedPhoto = new VideoFrame(BitmapPixelFormat.Bgra8, ImageWidth, ImageHeight);
            await mediaCapture.GetPreviewFrameAsync(capturedPhoto);
            var img = await GetPixelBytesFromSoftwareBitmapAsync(capturedPhoto.SoftwareBitmap);

            IdentifyResult[] knownPeople = null;
            Face[] faces = null;
            Func<Task> detectFaces = async () =>
            {
                faces = await faceClient.DetectAsync(new MemoryStream(img), true, true, new[] { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Smile, FaceAttributeType.HeadPose });
                knownPeople = faces.Length == 0 ? new IdentifyResult[0] : await faceClient.IdentifyAsync(personGroupId, faces.Select(f => f.FaceId).ToArray());
            };
            var fTask = detectFaces();
            var eTask = emotClient.RecognizeAsync(new MemoryStream(img));
            var eVision = visionClient.AnalyzeImageAsync(new MemoryStream(img), new[] { VisualFeature.Tags }, null);

            await Task.WhenAll(fTask, eTask, eVision);

            var info =  new ImageInfo() {
                Faces = GetFaceInfo(faces, eTask.Result, knownPeople),
                Analysis = eVision.Result
            };
            return info;
        }


        private static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                // Read the pixel bytes from the memory stream
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }


        public FaceInfo[] GetFaceInfo(IEnumerable<Face> DetectedFaces, IEnumerable<Emotion> DetectedEmotion, IEnumerable<IdentifyResult> IdentifiedPersons)
        {

            List<FaceInfo> faceInfoList = new List<FaceInfo>();

            if (DetectedFaces != null)
            {
                foreach (var detectedFace in DetectedFaces)
                {
                    FaceInfo faceInfo = new FaceInfo();

                    // Check if we have age/gender for this face.
                    if (detectedFace?.FaceAttributes != null)
                    {
                        faceInfo.Attributes = detectedFace.FaceAttributes;
                    }

                    // Check if we identified this face. If so send the name along.
                    if (IdentifiedPersons != null)
                    {
                        var matchingPerson = IdentifiedPersons.FirstOrDefault(p => p.FaceId == detectedFace.FaceId);
                        string name;
                        if (matchingPerson != null && matchingPerson.Candidates.Length > 0 && personMap.TryGetValue(matchingPerson.Candidates[0].PersonId, out name))
                        {
                            faceInfo.Name = name;
                        }
                    }

                    // Check if we have emotion for this face. If so send it along.
                    if (DetectedEmotion != null)
                    {
                        Emotion matchingEmotion = CoreUtil.FindFaceClosestToRegion(DetectedEmotion, detectedFace.FaceRectangle);
                        if (matchingEmotion != null)
                        {
                            faceInfo.Emotion = matchingEmotion.Scores;
                        }
                    }

                    //// Check if we have an unique Id for this face. If so send it along.
                    //if (SimilarFaceMatches != null)
                    //{
                    //    var matchingPerson = SimilarFaceMatches.FirstOrDefault(p => p.Face.FaceId == detectedFace.FaceId);
                    //    if (matchingPerson != null)
                    //    {
                    //        faceInfo.UniqueId = matchingPerson.SimilarPersistedFace.PersistedFaceId.ToString("N").Substring(0, 4);
                    //    }
                    //}

                    faceInfoList.Add(faceInfo);
                }
            }
            else if (DetectedEmotion != null)
            {
                // If we are here we only have emotion. No age/gender or id.
                faceInfoList.AddRange(DetectedEmotion.Select(emotion => new FaceInfo { Emotion = emotion.Scores }));
            }

            return faceInfoList.ToArray();
        }


        private class CoreUtil
        {
            public static uint MinDetectableFaceCoveragePercentage = 0;

            public static bool IsFaceBigEnoughForDetection(int faceHeight, int imageHeight)
            {
                if (imageHeight == 0)
                {
                    // sometimes we don't know the size of the image, so we assume the face is big enough
                    return true;
                }

                double faceHeightPercentage = 100 * ((double)faceHeight / imageHeight);

                return faceHeightPercentage >= MinDetectableFaceCoveragePercentage;
            }

            public static Emotion FindFaceClosestToRegion(IEnumerable<Emotion> emotion, FaceRectangle region)
            {
                return emotion?.Where(e => CoreUtil.AreFacesPotentiallyTheSame(e.FaceRectangle, region))
                                      .OrderBy(e => Math.Abs(region.Left - e.FaceRectangle.Left) + Math.Abs(region.Top - e.FaceRectangle.Top)).FirstOrDefault();
            }

            public static bool AreFacesPotentiallyTheSame(Rectangle face1, FaceRectangle face2)
            {
                return AreFacesPotentiallyTheSame((int)face1.Left, (int)face1.Top, (int)face1.Width, (int)face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
            }

            public static bool AreFacesPotentiallyTheSame(int face1X, int face1Y, int face1Width, int face1Height,
                                                           int face2X, int face2Y, int face2Width, int face2Height)
            {
                double distanceThresholdFactor = 1;
                double sizeThresholdFactor = 0.5;

                // See if faces are close enough from each other to be considered the "same"
                if (Math.Abs(face1X - face2X) <= face1Width * distanceThresholdFactor &&
                    Math.Abs(face1Y - face2Y) <= face1Height * distanceThresholdFactor)
                {
                    // See if faces are shaped similarly enough to be considered the "same"
                    if (Math.Abs(face1Width - face2Width) <= face1Width * sizeThresholdFactor &&
                        Math.Abs(face1Height - face2Height) <= face1Height * sizeThresholdFactor)
                    {
                        return true;
                    }
                }

                return false;
            }

        }

    }

    public class ImageInfo
    {
        public FaceInfo[] Faces { get; set; }
        public Microsoft.ProjectOxford.Vision.Contract.AnalysisResult Analysis { get; set; }
    }

    public class FaceInfo
    {
        public FaceAttributes Attributes { get; set; }
        public Scores Emotion { get; set; }
        public string Name { get; set; }
        public string UniqueId { get; set; }
    }
}
