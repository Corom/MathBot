using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SpeechSynthesizer ss = new SpeechSynthesizer();
        

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            display.Text = "";


            ss.Voice = SpeechSynthesizer.AllVoices[0];
        }

        string num1 = string.Empty;
        string num2 = string.Empty;
        VirtualKey operation = VirtualKey.None;


        private async void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == VirtualKey.Back)
                await SayIt("Matties butt plus lizzy sprinkles is the tastiest flavor of icecream in the universe!");
            if (args.VirtualKey == VirtualKey.NumberKeyLock || args.VirtualKey == VirtualKey.Decimal)
            {
                var txt = @"<speak version='1.0' " +
                "xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
                "<prosody speed='x-fast' pitch='+80%'>chocolate flavored diarea is maliks favorite treat!</prosody> " +
                "</speak>";

                var s = await ss.SynthesizeSsmlToStreamAsync(txt);
                me.SetSource(s, s.ContentType);
                await me.PlayAsync();
            }


            if (args.VirtualKey >= VirtualKey.NumberPad0 && args.VirtualKey <= VirtualKey.NumberPad9)
            {
                int num = args.VirtualKey - VirtualKey.NumberPad0;
                if (operation == VirtualKey.None)
                    num1 += num.ToString();
                else
                    num2 += num.ToString();
                display.Text += num.ToString();
                await SayIt(num.ToString());
            }
            else if (num1.Length > 0 && 
                (args.VirtualKey == VirtualKey.Add
                || args.VirtualKey == VirtualKey.Subtract
                || args.VirtualKey == VirtualKey.Divide
                || args.VirtualKey == VirtualKey.Multiply))
            {
                operation = args.VirtualKey;
                display.Text += GetOperationText(operation);

                //await SayIt(num1);
                await SayOperation(args.VirtualKey);
            }
            else if (args.VirtualKey == VirtualKey.Enter && num2.Length > 0)
            {
                double number1 = double.Parse(num1);
                double number2 = double.Parse(num2);
                double answer = 0;
                if (operation == VirtualKey.Add)
                    answer = number1 + number2;
                else if (operation == VirtualKey.Subtract)
                    answer = number1 - number2;
                else if (operation == VirtualKey.Divide)
                    answer = number1 / number2;
                else if (operation == VirtualKey.Multiply)
                    answer = number1 * number2;

                display.Text += "=" + answer + Environment.NewLine;

                //await SayIt(num2);
                //await SayIt("equals");
                //await SayIt(answer.ToString());

                await SayIt($"{number1.ToString()} {GetOperationText(operation)} {number2.ToString()} equals {answer}");

                num1 = string.Empty;
                num2 = string.Empty;
                operation = VirtualKey.None;
            }
        }


        private async Task SayIt(string text)
        {
            var s = await ss.SynthesizeTextToStreamAsync(text);
            me.SetSource(s, s.ContentType);
            
            await me.PlayAsync();
        }

        private async Task SayOperation(VirtualKey operation)
        {
            await SayIt(GetOperationSpeechText(operation));
        }

        private string GetOperationSpeechText(VirtualKey operation)
        {
            if (operation == VirtualKey.Add)
                return "plus";
            else if (operation == VirtualKey.Subtract)
                return "minus";
            else if (operation == VirtualKey.Divide)
                return "divided by";
            else if (operation == VirtualKey.Multiply)
                return "muliplied by";
            return "unknown";
        }

        private string GetOperationText(VirtualKey operation)
        {
            if (operation == VirtualKey.Add)
                return "+";
            else if (operation == VirtualKey.Subtract)
                return "-";
            else if (operation == VirtualKey.Divide)
                return "/";
            else if (operation == VirtualKey.Multiply)
                return "*";
            return "";
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
