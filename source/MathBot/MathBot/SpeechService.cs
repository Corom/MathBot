using Adventure_Works.CognitiveServices;
using MathBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Adventure_Works.Speech
{
    public class SpeechService
    {
        private SpeechRecognizer _speechRecognizer;
        private SpeechRecognizer _continousSpeechRecognizer;
        private SpeechSynthesizer _speechSynthesizer;

        private bool _speechInit = false;
        private bool _listening = false;

        private DispatcherTimer _keyTimer;
        private bool _gamepadViewDown = false;

        private static SpeechService _instance;
        public static SpeechService Instance => _instance ?? (_instance = new SpeechService());

        private SpeechService()
        {

        }

        public async Task Initialize()
        {
            if (_speechInit == true || !(await CheckForMicrophonePermission()))
                return;

            _continousSpeechRecognizer = new SpeechRecognizer();
            _continousSpeechRecognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<String>() { "Buddy" }, "start"));
           
            //_continousSpeechRecognizer.Constraints.Add(new SpeechRecognitionGrammarFileConstraint(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/grammer.xml"))));
            
            var result = await _continousSpeechRecognizer.CompileConstraintsAsync();
            
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            _speechRecognizer = new SpeechRecognizer();
            //_speechRecognizer.Constraints.Add(new SpeechRecognitionGrammarFileConstraint(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/grammer-command.xml"))));
            result = await _speechRecognizer.CompileConstraintsAsync();
            _speechRecognizer.HypothesisGenerated += _speechRecognizer_HypothesisGenerated;

            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;

            _continousSpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            await _continousSpeechRecognizer.ContinuousRecognitionSession.StartAsync(SpeechContinuousRecognitionMode.Default);

            _keyTimer = new DispatcherTimer();
            _keyTimer.Interval = TimeSpan.FromSeconds(1);
            _keyTimer.Tick += _keyTimer_Tick;

            _speechInit = true;
        }

        private void _keyTimer_Tick(object sender, object e)
        {
            _keyTimer.Stop();
            if (!_listening)
            {
                WakeUpAndListen();
            }
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (!_listening && !_gamepadViewDown && args.VirtualKey == Windows.System.VirtualKey.GamepadView)
            {
                _gamepadViewDown = true;
                _keyTimer.Start();
            }
        }


        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (!_listening && args.VirtualKey == Windows.System.VirtualKey.Q)
            {
                if (Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    WakeUpAndListen();
                }
            }

            if (args.VirtualKey == Windows.System.VirtualKey.GamepadView)
            {
                _gamepadViewDown = false;
                _keyTimer.Stop();
            }
        }

        
        private void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Confidence == SpeechRecognitionConfidence.High || args.Result.Confidence == SpeechRecognitionConfidence.Medium)
            {
                RunOnCoreDispatcherIfPossible( () => WakeUpAndListen(), false);
            }
        }

        private void _speechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            Debug.WriteLine(args.Hypothesis.Text);
        }


        private async Task WakeUpAndListen()
        {
            _listening = true;
            try
            {
                await _continousSpeechRecognizer.ContinuousRecognitionSession.CancelAsync();
            }
            catch (Exception ex)
            {

            }
            

            await SpeakAsync("hey!");

            int retry = 3;

            while (true)
            {
                Debug.WriteLine("Listening");

                var spokenText = await ListenForText();
                Debug.WriteLine(spokenText);
                if (string.IsNullOrWhiteSpace(spokenText) ||
                    spokenText.ToLower().Contains("cancel") ||
                    spokenText.ToLower().Contains("never mind"))
                {
                    break;
                }
                else
                {
                    Debug.WriteLine("Thinking");
                    Debug.WriteLine(spokenText.ToLower());
                    var state = await LUISAPI.Instance.HandleIntent(spokenText);
                    if (!state.Success)
                    {
                        Debug.WriteLine("don't know that yet");
                        await Task.Delay(1000);

                        if (--retry < 1)
                            break;
                    }
                    else
                    {
                        Debug.WriteLine("Doing something");

                        if (!string.IsNullOrWhiteSpace(state.SpeechRespose))
                        {
                            Debug.WriteLine(state.SpeechRespose);
                            await SpeakAsync(state.SpeechRespose);
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }

                        retry = 3;
                    }
                }
            }

            await _continousSpeechRecognizer.ContinuousRecognitionSession.StartAsync();
            _listening = false;
        }

        private async Task<string> ListenForText()
        {
            string result = null;

            try
            {
                SpeechRecognitionResult speechRecognitionResult = await _speechRecognizer.RecognizeAsync();
                if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    result = speechRecognitionResult.Text;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }

        private async Task SpeakAsync(string toSpeak)
        {
            if (_speechSynthesizer == null)
            {
                _speechSynthesizer = new SpeechSynthesizer();
                var voice = SpeechSynthesizer.AllVoices.Where(v => v.Gender == VoiceGender.Female && v.Language.Contains("en")).FirstOrDefault();
                if (voice != null)
                {
                    _speechSynthesizer.Voice = voice;
                }
            }
            var syntStream = await _speechSynthesizer.SynthesizeTextToStreamAsync(toSpeak);

            await  SoundUtilities.PlaySound(syntStream.AsStream());
        }

        
        private async Task<bool> CheckForMicrophonePermission()
        {
            try
            {
                // Request access to the microphone 
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                MediaCapture capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                // The user has turned off access to the microphone. If this occurs, we should show an error, or disable
                // functionality within the app to ensure that further exceptions aren't generated when 
                // recognition is attempted.
                return false;
            }
            
            return true;
        }

        private static async Task RunOnCoreDispatcherIfPossible(Action action, bool runAnyway = true)
        {
            CoreDispatcher dispatcher = null;

            try
            {
                dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            }
            catch { }

            if (dispatcher != null)
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { action.Invoke(); });
            }
            else if (runAnyway)
            {
                action.Invoke();
            }
        }
    }
}
