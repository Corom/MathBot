using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace MathBot
{
    class WindowsKeyPad : IKeyPad
    {
        public WindowsKeyPad()
        {
            Window.Current.CoreWindow.KeyUp += OnKeyUp;
        }

        public event Action<KeyPadKey> KeyPressed;

        private void OnKeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            var key = MapKey(args.VirtualKey);
            // ignore key presses that are not related to the keypad
            if (KeyPressed != null && key != KeyPadKey.Unknown)
            {
                KeyPressed(key);
            }
        }

        public KeyPadKey MapKey(VirtualKey key)
        {
            var isNumLock = Window.Current.CoreWindow.GetKeyState(VirtualKey.NumberKeyLock) == CoreVirtualKeyStates.Locked;
            var isShift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down;

            if (!isNumLock)
            {
                // keypad number keys if num lock is off
                switch (key)
                {
                    case VirtualKey.Home:       return KeyPadKey.Number7;
                    case VirtualKey.Insert:     return KeyPadKey.Number0;
                    case VirtualKey.End:        return KeyPadKey.Number1;
                    case VirtualKey.Down:       return KeyPadKey.Number2;
                    case VirtualKey.PageDown:   return KeyPadKey.Number3;
                    case VirtualKey.Left:       return KeyPadKey.Number4;
                    case VirtualKey.Clear:      return KeyPadKey.Number5;
                    case VirtualKey.Right:      return KeyPadKey.Number6;
                    case VirtualKey.Up:         return KeyPadKey.Number8;
                    case VirtualKey.PageUp:     return KeyPadKey.Number9;
                    case VirtualKey.Delete:     return KeyPadKey.Decimal;
                }
            }

            // keypad number keys
            if (!isShift && key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
                return (KeyPadKey)(key - VirtualKey.NumberPad0);

            // number keys
            if (!isShift && key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
                return (KeyPadKey)(key - VirtualKey.Number0);
            
            // remaining keys from keypad or regular keyboard
            if (key == VirtualKey.Decimal   || (!isShift && (int)key == 190))           return KeyPadKey.Decimal;
            if (key == VirtualKey.Add       || (isShift && (int)key == 187))            return KeyPadKey.Add;
            if (key == VirtualKey.Divide    || (!isShift && (int)key == 220))           return KeyPadKey.Divide;
            if (key == VirtualKey.Multiply  || (isShift && key == VirtualKey.Number8))  return KeyPadKey.Multiply;
            if (key == VirtualKey.Subtract  || (!isShift && (int)key == 189))           return KeyPadKey.Subtract;

            if (key == VirtualKey.NumberKeyLock) return KeyPadKey.NumLock;
            if (key == VirtualKey.Enter) return KeyPadKey.Enter;
            if (key == VirtualKey.Back) return KeyPadKey.Backspace;
            return KeyPadKey.Unknown;
        }
    }
}
