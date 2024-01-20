namespace G403HID
{
    public class ModifierKeypressMapping : ButtonMapping
    {
        public enum ModifierSide
        {
            None,
            Left,
            Right,
        }

        [Flags]
        public enum Modifier
        {
            None = 0,
            LeftCtrl = 1,
            LeftShift = 2,
            LeftAlt = 4,
            LeftGui = 8,
            RightCtrl = 16,
            RightShift = 32,
            RightAlt = 64,
            RightGui = 128,
        }

        readonly bool leftCtrl = false;
        readonly bool leftShift = false;
        readonly bool leftAlt = false;
        readonly bool leftGui = false;
        readonly bool rightCtrl = false;
        readonly bool rightShift = false;
        readonly bool rightAlt = false;
        readonly bool rightGui = false;

        public enum KeyCode
        {
            A = 4,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
            Q,
            R,
            S,
            T,
            U,
            V,
            W,
            X,
            Y,
            Z,
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine,
            Zero,
            Enter,
            Escape,
            Backspace,
            Tab,
            Space,
            Minus,
            Equal,
            LeftBracket,
            RightBracket,
            Backslash,
            NonUSPound,
            Semicolon,
            Quote,
            GraveAccent,
            Comma,
            Period,
            Slash,
            CapsLock,
            F1,
            F2,
            F3,
            F4,
            F5,
            F6,
            F7,
            F8,
            F9,
            F10,
            F11,
            F12,
            PrintScreen,
            ScrollLock,
            Pause,
            Insert,
            Home,
            PageUp,
            Delete,
            End,
            PageDown,
            RightArrow,
            LeftArrow,
            DownArrow,
            UpArrow,
            NumLock,
            KeypadSlash,
            KeypadAsterisk,
            KeypadMinus,
            KeypadPlus,
            KeypadEnter,
            Keypad1,
            Keypad2,
            Keypad3,
            Keypad4,
            Keypad5,
            Keypad6,
            Keypad7,
            Keypad8,
            Keypad9,
            Keypad0,
            KeypadPeriod,
            NonUSBackslash,
            Menu,
            Power,
            KeypadEquals,
            F13,
            F14,
            F15,
            F16,
            F17,
            F18,
            F19,
            F20,
            F21,
            F22,
            F23,
            F24,
            KeypadComma = 133,
            International1 = 135,
            International2,
            International3,
            International4,
            International5,
            International6,
            Language1 = 144,
            Language2,
            Language3,
            Language4,
            Language5,
            LCtrl = 224,
            LShift,
            LAlt,
            LGui,
            RCtrl,
            RShift,
            RAlt,
            RGui,
        }

        readonly KeyCode keyCode;

        public ModifierKeypressMapping(KeyCode keyCode, bool leftCtrl = false, bool leftShift = false, bool leftAlt = false, bool leftGui = false, bool rightCtrl = false, bool rightShift = false, bool rightAlt = false, bool rightGui = false)
        {
            this.leftCtrl = leftCtrl;
            this.leftShift = leftShift;
            this.leftAlt = leftAlt;
            this.leftGui = leftGui;
            this.rightCtrl = rightCtrl;
            this.rightShift = rightShift;
            this.rightAlt = rightAlt;
            this.rightGui = rightGui;

            this.keyCode = keyCode;
        }

        public ModifierKeypressMapping(ModifierSide ctrl, ModifierSide shift, ModifierSide alt, ModifierSide gui, KeyCode keyCode)
        {
            switch (ctrl)
            {
                case ModifierSide.None:
                    break;
                case ModifierSide.Left:
                    leftCtrl = true;
                    break;
                case ModifierSide.Right:
                    rightCtrl = true;
                    break;
            }

            switch (shift)
            {
                case ModifierSide.None:
                    break;
                case ModifierSide.Left:
                    leftShift = true;
                    break;
                case ModifierSide.Right:
                    rightShift = true;
                    break;
            }

            switch (alt)
            {
                case ModifierSide.None:
                    break;
                case ModifierSide.Left:
                    leftAlt = true;
                    break;
                case ModifierSide.Right:
                    rightAlt = true;
                    break;
            }

            switch (gui)
            {
                case ModifierSide.None:
                    break;
                case ModifierSide.Left:
                    leftGui = true;
                    break;
                case ModifierSide.Right:
                    rightGui = true;
                    break;
            }

            this.keyCode = keyCode;
        }

        public ModifierKeypressMapping(Modifier modifiers, KeyCode keyCode)
        {
            leftCtrl = modifiers.HasFlag(Modifier.LeftCtrl);
            leftShift = modifiers.HasFlag(Modifier.LeftShift);
            leftAlt = modifiers.HasFlag(Modifier.LeftAlt);
            leftGui = modifiers.HasFlag(Modifier.LeftGui);
            rightCtrl = modifiers.HasFlag(Modifier.RightCtrl);
            rightShift = modifiers.HasFlag(Modifier.RightShift);
            rightAlt = modifiers.HasFlag(Modifier.RightAlt);
            rightGui = modifiers.HasFlag(Modifier.RightGui);

            this.keyCode = keyCode;
        }

        public ModifierKeypressMapping(byte[] bytes)
        {
            var modifiers = (Modifier)bytes[2];
            keyCode = (KeyCode)bytes[3];

            leftCtrl = modifiers.HasFlag(Modifier.LeftCtrl);
            leftShift = modifiers.HasFlag(Modifier.LeftShift);
            leftAlt = modifiers.HasFlag(Modifier.LeftAlt);
            leftGui = modifiers.HasFlag(Modifier.LeftGui);
            rightCtrl = modifiers.HasFlag(Modifier.RightCtrl);
            rightShift = modifiers.HasFlag(Modifier.RightShift);
            rightAlt = modifiers.HasFlag(Modifier.RightAlt);
            rightGui = modifiers.HasFlag(Modifier.RightGui);
        }

        public override byte[] ToBytes()
        {
            byte modifiers = 0;

            if (leftCtrl)
            {
                modifiers |= 1;
            }
            if (leftShift)
            {
                modifiers |= 2;
            }
            if (leftAlt)
            {
                modifiers |= 4;
            }
            if (leftGui)
            {
                modifiers |= 8;
            }
            if (rightCtrl)
            {
                modifiers |= 16;
            }
            if (rightShift)
            {
                modifiers |= 32;
            }
            if (rightAlt)
            {
                modifiers |= 64;
            }
            if (rightGui)
            {
                modifiers |= 128;
            }

            return new byte[4] { 0x80, 0x02, modifiers, (byte)keyCode };
        }

        public override string ToString()
        {
            var components = new List<string>();

            if (leftCtrl)
            {
                components.Add("LCtrl");
            }
            if (leftShift)
            {
                components.Add("LShift");
            }
            if (leftAlt)
            {
                components.Add("LAlt");
            }
            if (leftGui)
            {
                components.Add("LGui");
            }
            if (rightCtrl)
            {
                components.Add("RCtrl");
            }
            if (rightShift)
            {
                components.Add("RShift");
            }
            if (rightAlt)
            {
                components.Add("RAlt");
            }
            if (rightGui)
            {
                components.Add("RGui");
            }

            components.Add(keyCode.ToString());

            return string.Join(" + ", components);
        }
    }
}
