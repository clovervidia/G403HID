namespace G403HID
{
    public class SimpleMapping : ButtonMapping
    {
        public enum MouseButton : uint
        {
            LeftButton = 0x1000180,
            RightButton = 0x2000180,
            MiddleButton = 0x4000180,
            NavigateBackward = 0x8000180,
            NavigateForward = 0x10000180,
            NextTrack = 0xB50C0380,
            PreviousTrack = 0xB60C0380,
            PlayPause = 0xCD0C0380,
            VolumeMute = 0xE20C0380,
            VolumeUp = 0xE90C0380,
            VolumeDown = 0xEA0C0380,
            NoAction = 0xFFFF0090,
            WheelLeft = 0xFFFF0190,
            WheelRight = 0xFFFF0290,
            NextDPI = 0xFFFF0390,
            PreviousDPI = 0xFFFF0490,
            CycleDPI = 0xFFFF0590,
            DefaultDPI = 0xFFFF0690,
            DPIShift = 0xFFFF0790,
            NextProfile = 0xFFFF0890,
            PreviousProfile = 0xFFFF0990,
            CycleProfile = 0xFFFF0A90,
            GShift = 0xFFFF0B90,
            EnableProfile = 0xFFFF0D90,
            PerformanceMode = 0xFFFF0E90,
            HostButton = 0xFFFF0F90,
            ScrollDown = 0xFFFF1090,
            ScrollUp = 0xFFFF1190,
            NoButton = 0xFFFFFFFF,
        }
        public MouseButton Button { get; set; }

        public SimpleMapping(MouseButton button)
        {
            Button = button;
        }

        public SimpleMapping(byte[] bytes)
        {
            Button = (MouseButton)BitConverter.ToUInt32(bytes, 0);
        }

        public override byte[] ToBytes() => BitConverter.GetBytes((uint)Button);

        public override string ToString() => Button.ToString();
    }
}
