using LcdHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MathBot
{
    public sealed partial class LcdDisplayControl : UserControl, ILcdDisplay
    {
        const int Rows = 2;
        const int Columns = 16;
        char[][] displayData;
        StringBuilder line2 = new StringBuilder();
        bool enabled = true;
        bool backlight = true;

        const char NullChar = (char)0x0;

        int cursorX, cursorY = 0;

        public LcdDisplayControl()
        {
            this.InitializeComponent();

            displayData = new char[Rows][];
            for (int r = 0; r < Rows; r++)
                displayData[r] = new char[Columns];

            UpdateDisplay();
        }

        public Brush BackLightOnColor { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0x12, 0x67, 0xFF));
        public Brush BackLightOffColor { get; set; } = new SolidColorBrush(Colors.Gray);


        public void SetBacklight(bool state)
        {
            this.backlight = state;
            UpdateDisplay();
        }

        public void Enable(bool state)
        {
            this.enabled = state;
            UpdateDisplay();
        }

        public void SetCursor(int x, int y)
        {
            cursorX = x;
            cursorY = y;

            UpdateDisplay();
        }

        public void Clear()
        {
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                displayData[r][c] = NullChar;

            cursorX = 0;
            cursorY = 0;
            UpdateDisplay();
        }

        public void Print(string text)
        {
            if (cursorX >= 0 && cursorX < Columns 
                && cursorY >= 0 && cursorY < Rows)
            {
                // copy the charactors to the display data
                var length = Math.Min(text.Length, Columns - cursorX);
                Array.Copy(text.ToCharArray(),
                    0,
                    displayData[cursorY],
                    cursorX,
                    length);

                // move the cursor
                cursorX += length;

                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            var backColor = backlight && enabled ? this.BackLightOnColor : this.BackLightOffColor;
            var newText = enabled ? string.Join("\n", displayData.Select(c => new string(c))) : string.Empty;
            DispatchedHandler callback = () => {
                this.displayText.Text = newText;
                displayBorder.Background = backColor;
            };

            if (this.Dispatcher.HasThreadAccess)
                callback();
            else
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, callback);
            }
        }
    }
}
