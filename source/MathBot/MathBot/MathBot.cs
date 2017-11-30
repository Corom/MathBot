using Adventure_Works.Speech;
using Glovebox.Graphics;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MathBot
{
    public class MathBot
    {
        IMathBotDevice device;
        Camera camera = new Camera();

        // keypad state machine
        string num1 = string.Empty;
        string num2 = string.Empty;
        KeyInfo operation = null;
        bool answerDisplayed = false;
        //DispatcherTimer timer;
        Timer timer;
        Dictionary<string, bool> greeted = new Dictionary<string, bool>();


        public FaceManager FaceManager { get; private set; }

        public MathBot(IMathBotDevice device)
        {
            SoundUtilities.Volume = .1f;

            this.device = device;
            FaceManager = new FaceManager(device);
            FaceManager.LoadImages().ContinueWith(t => FaceManager.SetFace(Faces.Sad));
            device.KeyPad.KeyPressed += Device_KeyPressed;

            var ignore = camera.Initialize();

            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(1);
            //timer.Tick += (a, b) => { SeeWithCamera(); };
            //timer = new Timer(a => SeeWithCamera(), null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

            
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(4));
                while (true)
                {

                    await SeeWithCamera();
                    //await Task.Delay(TimeSpan.FromSeconds(1));

                }
            });
            
        }

        enum WatchState { Waiting,Faces,Recognized}

        private WatchState watching = WatchState.Waiting;

        HashSet<string> peopleSeen = new HashSet<string>();

        private async Task SeeWithCamera()
        {
            // avoid overlapping timmer
            //timer.Stop();



            bool isAngry = false;
            var sw = Stopwatch.StartNew();
            var scene = await camera.See();
            sw.Stop();
            if (scene.Faces.Length > 0)
            {
                foreach (var face in scene.Faces.Where(f => f.Name != null))
                {
                    if (!peopleSeen.Contains(face.Name))
                    {
                        peopleSeen.Add(face.Name);
                        if (face.Name == "Malik")
                            FaceManager.SetFace(Faces.Angry);
                        else if (face.Attributes.Gender == "Female")
                            FaceManager.SetFace(Faces.Happy);
                        else
                            FaceManager.SetFace(Faces.Normal);

                        await device.SayIt($"Hello {face.Name}.");
                    }
                }

                if (peopleSeen.Count == 0 && FaceManager.CurrentFace != Faces.Surprised)
                    FaceManager.SetFace(Faces.Surprised);

                if (scene.Faces.Max(f => f.Emotion.Anger) > .3 && FaceManager.CurrentFace != Faces.Angry)
                    FaceManager.SetFace(Faces.Angry);
            }
            else
            {
                peopleSeen.Clear();
                if (FaceManager.CurrentFace != Faces.Sad)
                    FaceManager.SetFace(Faces.Sad);
                Debug.WriteLine($"I am lonely. ({sw.ElapsedMilliseconds}ms)");
            }

            /*
            var face = (await camera.LookForFaces()).FirstOrDefault();
            if (face != null)
            {
                var x = face.FaceBox.Width / ((double)face.FaceBox.X + (double)face.FaceBox.Width / 2);
                var y = face.FaceBox.Height / ((double)face.FaceBox.Y + (double)face.FaceBox.Height / 2);

                device.LeftEyeDisplay.FrameClear();
                device.LeftEyeDisplay.FrameSet(Led.On, device.LeftEyeDisplay.PointPostion((int)((1 - y) * 8), (int)(x * 8)));
                device.LeftEyeDisplay.FrameDraw();
            }
            else
            {
                device.LeftEyeDisplay.FrameClear();
                device.LeftEyeDisplay.FrameDraw();
            }
            */

            //timer.Start();
        }

        private async void Device_KeyPressed(KeyPadKey key1)
        {
            var key = new KeyInfo(key1);

            //if (args.VirtualKey == VirtualKey.Back)
            //    await SayIt("Matties butt plus lizzy sprinkles is the tastiest flavor of icecream in the universe!");
            //if (args.VirtualKey == VirtualKey.NumberKeyLock || args.VirtualKey == VirtualKey.Decimal)
            //{
            //    var txt = @"<speak version='1.0' " +
            //    "xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
            //    "<prosody speed='x-fast' pitch='+80%'>chocolate flavored diarea is maliks favorite treat!</prosody> " +
            //    "</speak>";

            //    var s = await ss.SynthesizeSsmlToStreamAsync(txt);
            //    me.SetSource(s, s.ContentType);
            //    await me.PlayAsync();
            //}
            if (key.Key == KeyPadKey.Divide || key.Key == KeyPadKey.Multiply)
            {
                if (key.Key == KeyPadKey.Divide & SoundUtilities.Volume > 0)
                    SoundUtilities.Volume = Math.Max(SoundUtilities.Volume - .1f, 0);
                if (key.Key == KeyPadKey.Multiply & SoundUtilities.Volume < 1)
                    SoundUtilities.Volume = Math.Min(SoundUtilities.Volume + .1f, 1);

                device.LcdDisplay.Clear();
                device.LcdDisplay.SetCursor(0, 0);
                device.LcdDisplay.Print("Volume: " + (int)(SoundUtilities.Volume * 100));
                answerDisplayed = true;
                return;
            }

            if (key.Key == KeyPadKey.Decimal)
            {
                //greeted.Clear();
                /* if (timer.IsEnabled)
                     timer.Stop();
                 else
                     timer.Start();*/

                //FaceManager.SetFace(Faces.Angry);
                //ListenAsync();

                //SpeechService.Instance.Initialize();

                //device.SayIt("If you only knew the power of the darkside");
                return;
            }

            if (key.IsNumber)
            {
                if (answerDisplayed)
                {
                    answerDisplayed = false;
                    device.LcdDisplay.Clear();
                    device.LcdDisplay.SetCursor(0, 0);
                }

                if (operation == null)
                    num1 += key.Text;
                else
                    num2 += key.Text;
                device.LcdDisplay.Print(key.Text);
                await device.SayIt(key.SpeechText);
            }
            else if (num1.Length > 0 && key.IsOperation)
            {
                operation = key;
                device.LcdDisplay.Print(key.Text);

                await device.SayIt(key.SpeechText);
            }
            else if (key.IsEnter && num2.Length > 0)
            {
                double number1 = double.Parse(num1);
                double number2 = double.Parse(num2);
                double answer = 0;
                if (operation.Key == KeyPadKey.Add)
                    answer = number1 + number2;
                else if (operation.Key == KeyPadKey.Subtract)
                    answer = number1 - number2;
                else if (operation.Key == KeyPadKey.Divide)
                    answer = number1 / number2;
                else if (operation.Key == KeyPadKey.Multiply)
                    answer = number1 * number2;

                FaceManager.SetFace(Faces.Happy);

                device.LcdDisplay.Print("=" + answer + Environment.NewLine);
                answerDisplayed = true;

                await this.device.MouthDisplay.DrawString($"{number1}{operation.Text}{number2}={answer}");

                //await SayIt(num2);
                //await SayIt("equals");
                //await SayIt(answer.ToString());

                await device.SayIt($"{number1.ToString()} {operation.SpeechText} {number2.ToString()} equals {answer}");

                num1 = string.Empty;
                num2 = string.Empty;
                operation = null;
            }
        }


        SpeechRecognizer recognizer;
        private async Task ListenAsync()
        {
            recognizer = new SpeechRecognizer();
            //recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new[] { "Mathbot" }));
            //recognizer.Constraints.Add(new SpeechRecognitionGrammarFileConstraint(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/grammer.xml"))));
            //await recognizer.CompileConstraintsAsync();

            //recognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            //recognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(20);
            //recognizer.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(2);



            recognizer.HypothesisGenerated += Recognizer_HypothesisGenerated;
            recognizer.RecognitionQualityDegrading += Recognizer_RecognitionQualityDegrading;
            recognizer.StateChanged += Recognizer_StateChanged;

            this.recognizer.ContinuousRecognitionSession.ResultGenerated += (s, e) =>
            {
                Debug.WriteLine($"Result: {e.Result.Text} {e.Result.RawConfidence}");
                //var rotationList = e.Result?.SemanticInterpretation?.Properties?["rotation"];
                //var rotation = rotationList.FirstOrDefault();

                //if (!string.IsNullOrEmpty(rotation))
                //{
                //    var angle = 0;

                //    switch (rotation)
                //    {
                //        case "left":
                //            angle = -90;
                //            break;
                //        case "right":
                //            angle = 90;
                //            break;
                //        default:
                //            break;
                //    }

                    //this.recognizer.ContinuousRecognitionSession.Resume();
                //}
            };

            await this.recognizer.ContinuousRecognitionSession.StartAsync(SpeechContinuousRecognitionMode.PauseOnRecognition);
        }

        private void Recognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine($"state: {args.State.ToString()}");

        }

        private void Recognizer_RecognitionQualityDegrading(SpeechRecognizer sender, SpeechRecognitionQualityDegradingEventArgs args)
        {
            Debug.WriteLine("degrade");
        }

        private void Recognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            Debug.WriteLine(args.Hypothesis.Text);
        }


    }
}
