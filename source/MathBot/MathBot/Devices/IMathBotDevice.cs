using Glovebox.Graphics.Components;
using LcdHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBot
{
    public interface IMathBotDevice
    {
        IKeyPad KeyPad { get; }
        LED8x8Matrix LeftEyeDisplay { get; }
        LED8x8Matrix RightEyeDisplay { get; }
        LED8x8Matrix MouthDisplay { get; }
        ILcdDisplay LcdDisplay { get; }
        Task SayIt(string text);
    }
}
