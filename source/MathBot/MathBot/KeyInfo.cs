using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBot
{

    public class KeyInfo
    {
        public KeyPadKey Key { get; private set; }
        public KeyInfo(KeyPadKey key)
        {
            Key = key;
        }


        public int Number {
            get {
                if (IsNumber)
                    return Key - KeyPadKey.Number0;
                return -1;
            }
        }

        public string Text {
            get {
                if (IsNumber)
                    return Number.ToString();
                else if (IsOperation)
                {
                    if (Key == KeyPadKey.Add)
                        return "+";
                    else if (Key == KeyPadKey.Subtract)
                        return "-";
                    else if (Key == KeyPadKey.Divide)
                        return "/";
                    else if (Key == KeyPadKey.Multiply)
                        return "x";
                }
                else if (IsEnter)
                    return "=";
                else if (Key == KeyPadKey.Decimal)
                    return ".";
                return "";
            }
        }

        public string SpeechText
        {
            get
            {
                if (IsNumber)
                    return Number.ToString();
                else if (IsOperation)
                {
                    if (Key == KeyPadKey.Add)
                        return "plus";
                    else if (Key == KeyPadKey.Subtract)
                        return "minus";
                    else if (Key == KeyPadKey.Divide)
                        return "divided by";
                    else if (Key == KeyPadKey.Multiply)
                        return "times";
                }
                else if (IsEnter)
                    return "equals";
                else if (Key == KeyPadKey.Decimal)
                    return "point";
                return "";
            }
        }


        public bool IsNumber
        {
            get
            {
                return Key >= KeyPadKey.Number0 && Key <= KeyPadKey.Number9;
            }
        }

        public bool IsOperation
        {
            get
            {
                return Key == KeyPadKey.Add
                    || Key == KeyPadKey.Subtract
                    || Key == KeyPadKey.Divide
                    || Key == KeyPadKey.Multiply;
            }
        }

        public bool IsEnter
        {
            get
            {
                return Key == KeyPadKey.Enter;
            }
        }

        public bool IsSpecial
        {
            get
            {
                return Key == KeyPadKey.Decimal
                    || Key == KeyPadKey.NumLock
                    || Key == KeyPadKey.Backspace;
            }
        }

    }



}
