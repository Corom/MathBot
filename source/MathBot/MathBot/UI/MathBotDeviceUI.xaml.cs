using Glovebox.Graphics.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LcdHelper;
using System.Diagnostics;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MathBot
{
    public sealed partial class MathBotDeviceUI : UserControl, IMathBotDevice
    {
        public MathBotDeviceUI()
        {
            this.InitializeComponent();

            // Led Matrix Displays
            LeftEyeDisplay = new LED8x8Matrix(this.eyeLeftLed);
            RightEyeDisplay = new LED8x8Matrix(this.eyeRightLed);
            MouthDisplay = new LED8x8Matrix(this.mouthLed);

            // LCD display
            LcdDisplay = this.display;

            // Speaker
            ss.Voice = SpeechSynthesizer.AllVoices[0];
        }

        SpeechSynthesizer ss = new SpeechSynthesizer();
        public LED8x8Matrix LeftEyeDisplay { get; private set; }
        public LED8x8Matrix RightEyeDisplay { get; private set; }
        public LED8x8Matrix MouthDisplay { get; private set; }
        public ILcdDisplay LcdDisplay { get; private set; }
        public IKeyPad KeyPad { get; } = new WindowsKeyPad();
        
        public async Task SayIt(string text)
        {
            //var ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en\"><voice xml:lang=\"en\"><prosody rate=\"1\">" + text + "</prosody></voice></speak>";
            var ssml = @"<speak version='1.0' " +
                "xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
                text + "<mark name=\"utteranceComplete\"/>!" +
                "</speak>";

            var s = await ss.SynthesizeSsmlToStreamAsync(ssml);
            var a = s.Markers[0];
            Stopwatch sw = Stopwatch.StartNew();

            var t = SoundUtilities.PlaySound(s.AsStream());
            await Task.Delay(a.Time);

           // await t;
            //sw.Stop();

           // me.SetSource(s, s.ContentType);

            //await me.PlayAsync();
        }


    }
}
