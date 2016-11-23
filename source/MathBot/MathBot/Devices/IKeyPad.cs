using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBot
{
    public interface IKeyPad
    {
        event Action<KeyPadKey> KeyPressed;
    }

    public enum KeyPadKey
    {
        Unknown = -1,
        Number0 = 0,
        Number1 = 1,
        Number2 = 2,
        Number3 = 3,
        Number4 = 4,
        Number5 = 5,
        Number6 = 6,
        Number7 = 7,
        Number8 = 8,
        Number9 = 9,
        Decimal = 10,
        Multiply = 11,
        Add = 12,
        Subtract = 13,
        Divide = 14,
        Enter = 15,
        Backspace = 16,
        NumLock = 17,
    }

}
