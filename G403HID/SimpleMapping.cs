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
            NoAction = 0x00000090,
            WheelLeft = 0x00000190,
            WheelRight = 0x00000290,
            NextDPI = 0x00000390,
            PreviousDPI = 0x00000490,
            CycleDPI = 0x00000590,
            DefaultDPI = 0x00000690,
            DPIShift = 0x00000790,
            NextProfile = 0x00000890,
            PreviousProfile = 0x00000990,
            CycleProfile = 0x00000A90,
            GShift = 0x00000B90,
            EnableProfile = 0x00000D90,
            PerformanceMode = 0x00000E90,
            HostButton = 0x00000F90,
            ScrollDown = 0x00001090,
            ScrollUp = 0x00001190,
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
