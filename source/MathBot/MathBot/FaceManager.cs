using Glovebox.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace MathBot
{
    public class FaceManager
    {
        IMathBotDevice device;
        Dictionary<Faces, FaceImages> faces = new Dictionary<Faces, FaceImages>();
        public FaceManager(IMathBotDevice device)
        {
            this.device = device;
        }

        public async Task LoadImages()
        {
            Dictionary<string, Pixel[]> imageCache = new Dictionary<string, Pixel[]>(StringComparer.OrdinalIgnoreCase);

            faces[Faces.Normal] = await LoadFace("eye/normal", "eye/normal", "mouth/normal", imageCache);
            faces[Faces.Angry] = await LoadFace("eye/angry-right", "eye/angry-left", "mouth/angry", imageCache);
            faces[Faces.Happy] = faces[Faces.Normal];
            faces[Faces.Sad] = faces[Faces.Normal];
            faces[Faces.Surprised] = faces[Faces.Normal];
            faces[Faces.Afraid] = faces[Faces.Normal];
        }

        public void SetFace(Faces face)
        {
            FaceImages faceImages;
            if (faces.TryGetValue(face, out faceImages))
            {
                device.LeftEyeDisplay.FrameSet(faceImages.LeftEye);
                device.RightEyeDisplay.FrameSet(faceImages.RightEye);
                device.MouthDisplay.FrameSet(faceImages.Mouth);

                device.LeftEyeDisplay.FrameDraw();
                device.RightEyeDisplay.FrameDraw();
                device.MouthDisplay.FrameDraw();
            }

        }


        private async Task<FaceImages> LoadFace(string rightEye, string leftEye, string mouth, Dictionary<string, Pixel[]> imageCache)
        {
            return new FaceImages()
            {
                RightEye = await LoadImage(rightEye, imageCache),
                LeftEye = await LoadImage(leftEye, imageCache),
                Mouth = await LoadImage(mouth, imageCache)
            };
        }


        private async Task<Pixel[]> LoadImage(string name, Dictionary<string, Pixel[]> imageCache = null)
        {
            Pixel[] pixels;
            if (imageCache == null || !imageCache.TryGetValue(name, out pixels))
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + name + ".bmp"));
                var stream = await file.OpenStreamForReadAsync();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                var bmp = await decoder.GetSoftwareBitmapAsync();
                var bytes = new byte[decoder.PixelHeight * decoder.PixelWidth * 4];
                bmp.CopyToBuffer(bytes.AsBuffer());

                pixels = new Pixel[decoder.PixelHeight * decoder.PixelWidth];
                for (int i = 0; i < bytes.Length; i += 4)
                {
                    if ((bytes[i] == 0 && bytes[i + 1] == 0 && bytes[i + 2] == 0) || (bytes[i] == 255 && bytes[i + 1] == 255 && bytes[i + 2] == 255))
                        pixels[i / 4] = Led.Off;
                    else
                        pixels[i / 4] = Led.On;
                }
            }
            return pixels;
        }


        private class FaceImages
        {
            public Pixel[] LeftEye, RightEye, Mouth;
        }

    }


    public enum Faces
    {
        Normal,
        Happy,
        Sad,
        Angry,
        Surprised,
        Afraid
    }
    




}
