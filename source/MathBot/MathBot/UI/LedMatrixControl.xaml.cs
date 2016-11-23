using Glovebox.Graphics.Drivers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Glovebox.Graphics;
using Windows.UI.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MathBot
{
    public sealed partial class LedMatrixControl : UserControl, ILedDriver
    {
        private Pixel[] frame;
        private bool isOn = true;

        public LedMatrixControl()
        {
            this.InitializeComponent();
            this.Loading += (s, e) =>
            {
                for (int c = 0; c < Columns; c++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                for (int r = 0; r < Rows; r++)
                    grid.RowDefinitions.Add(new RowDefinition());

                // create each led in a paneled order of square matrixes by row size
                for (int p = 0; p < PanelsPerFrame; p++)
                {
                    for (int r = 0; r < Rows; r++)
                    {
                        for (int c = 0; c < Rows; c++)
                        {
                            var border = new Border()
                            {
                                BorderThickness = new Thickness(1, 1, 1, 1),
                                BorderBrush = GridColor,
                                Background = OffColor
                            };
                            Grid.SetColumn(border, Rows * p + c);
                            Grid.SetRow(border, r);
                            grid.Children.Add(border);
                        }
                    }
                }
            };
        }


        public Brush GridColor { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 5, 5, 5));
        public Brush OnColor { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0x03, 0x86, 0xFC));
        public Brush OffColor { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xB2, 0xB1, 0xAC));
        public int Rows { get; set; } = 8;
        public int Columns { get; set; } = 8;

        #region ILedDriver implementation

        public int PanelsPerFrame { get { return Columns / Rows; } }

        void ILedDriver.SetBlinkRate(LedDriver.BlinkRate blinkrate)
        {
        }

        void ILedDriver.SetBrightness(byte level)
        {
        }

        void ILedDriver.SetFrameState(LedDriver.Display state)
        {
            isOn = state == LedDriver.Display.On;
            UpdateDisplay(this.frame, isOn);
        }

        void ILedDriver.Write(Pixel[] frame)
        {
            // save for when the display if off
            this.frame = frame;
            UpdateDisplay(frame, isOn);
        }

        void ILedDriver.Write(ulong[] frame)
        {
            throw new NotImplementedException();
        }

        void UpdateDisplay(Pixel[] frame, bool isOn)
        {
            // save for when the display if off
            this.frame = frame;

            if (this.Dispatcher.HasThreadAccess)
                UpdateDisplayImpl(frame, isOn);
            else
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { UpdateDisplayImpl(frame, isOn); });
            }
        }

        private void UpdateDisplayImpl(Pixel[] frame, bool isOn)
        {
            for (int i = 0; i < grid.Children.Count; i++)
            {
                ((Border)grid.Children[i]).Background =
                    (isOn && frame != null && frame.Length >= i + 1 && frame[i].ColourValue == Led.On.ColourValue) ?
                    OnColor : OffColor;
            }
        }

        #endregion
    }
}
