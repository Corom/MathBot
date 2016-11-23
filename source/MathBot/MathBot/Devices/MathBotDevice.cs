using Glovebox.Graphics;
using Glovebox.Graphics.Components;
using Glovebox.Graphics.Drivers;
using LcdHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MathBot
{
    class MathBotDevice : IMathBotDevice
    {
        SpeechSynthesizer ss = new SpeechSynthesizer();
        MediaElement me;
        public LED8x8Matrix LeftEyeDisplay { get; private set; }
        public LED8x8Matrix RightEyeDisplay { get; private set; }
        public LED8x8Matrix MouthDisplay { get; private set; }
        public ILcdDisplay LcdDisplay { get; private set; }
        public IKeyPad KeyPad { get; } = new WindowsKeyPad();

        public MathBotDevice(MediaElement me, byte brightness = 2)
        {
            this.me = me;

            // LCD
            LcdDisplay = new LcdDisplay(0x20);
            LcdDisplay.SetBacklight(true);

            // LED matrixes
            MouthDisplay = new LED8x8Matrix(new Ht16K33(new byte[] { 0x73, 0x74 }, new[] { Ht16K33.Rotate.None, Ht16K33.Rotate.D180 }, doubleWide: true));
            LeftEyeDisplay = new LED8x8Matrix(new Ht16K33(new byte[] { 0x70 }, Ht16K33.Rotate.None));
            RightEyeDisplay = new LED8x8Matrix(new Ht16K33(new byte[] { 0x71 }, Ht16K33.Rotate.None));
            MouthDisplay.SetBrightness(brightness);
            LeftEyeDisplay.SetBrightness(brightness);
            RightEyeDisplay.SetBrightness(brightness);
            
            // Speaker
            ss.Voice = SpeechSynthesizer.AllVoices[0];
        }

        public async Task SayIt(string text)
        {
            var s = await ss.SynthesizeTextToStreamAsync(text);
            me.SetSource(s, s.ContentType);

            await me.PlayAsync();
        }

    }


    public static class MediaExtensions
    {
        public static Task PlayAsync(this MediaElement media)
        {
            TaskCompletionSource<object> mediaDone = new TaskCompletionSource<object>();
            RoutedEventHandler done = null;
            done = (sender, e) => {
                media.MediaEnded -= done;
                mediaDone.SetResult(null);
            };
            media.MediaEnded += done;
            return mediaDone.Task;
        }
    }
}
