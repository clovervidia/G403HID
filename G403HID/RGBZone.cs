namespace G403HID
{
    public class RGBZone
    {
        public enum RGBMode
        {
            Off = 0,
            Fixed = 1,
            Cycling = 3,
            Breathing = 10
        }

        public record Color(byte Red, byte Green, byte Blue);

        public RGBMode Mode;
        public Color RGBColor = new(0, 0, 255);
        private byte[] EffectParameters = new byte[7];
        private byte Animation = 2;
        public byte Intensity = 100;
        public ushort Period = 5000;

        public static RGBZone FromBytes(List<byte> bytes)
        {
            var rgbZone = new RGBZone
            {
                Mode = (RGBMode)bytes[0],
                RGBColor = new Color(bytes[1], bytes[2], bytes[3]),
                EffectParameters = bytes.Skip(4).ToArray()
            };

            switch (rgbZone.Mode)
            {
                case RGBMode.Off:
                    // Everything remains set to 0.
                    break;
                case RGBMode.Fixed:
                    // Only animation is set. Everything else is set to 0.
                    rgbZone.Animation = rgbZone.EffectParameters[0];
                    break;
                case RGBMode.Cycling:
                    // Intensity and period are set. Color is set to 0.
                    rgbZone.Intensity = rgbZone.EffectParameters[4];
                    rgbZone.Period = BitConverter.ToUInt16(rgbZone.EffectParameters.Skip(2).Take(2).Reverse().ToArray(), 0);
                    break;
                case RGBMode.Breathing:
                    // Period and intensity are set.
                    rgbZone.Period = BitConverter.ToUInt16(rgbZone.EffectParameters.Take(2).Reverse().ToArray(), 0);
                    rgbZone.Intensity = rgbZone.EffectParameters[3];
                    break;
            }

            return rgbZone;
        }

        public List<byte> ToBytes()
        {
            var rgbZoneBytes = new List<byte>
            {
                (byte)Mode,
                RGBColor.Red,
                RGBColor.Green,
                RGBColor.Blue
            };

            switch (Mode)
            {
                case RGBMode.Off:
                    // All zeroes
                    rgbZoneBytes.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0 });
                    break;
                case RGBMode.Fixed:
                    // Animation, then 6 zeroes
                    rgbZoneBytes.AddRange(new byte[] { Animation, 0, 0, 0, 0, 0, 0 });
                    break;
                case RGBMode.Cycling:
                    // 0 0
                    rgbZoneBytes.AddRange(new byte[] { 0, 0 });
                    // Period
                    rgbZoneBytes.AddRange(BitConverter.GetBytes(Period).Reverse());
                    // Intensity 0 0
                    rgbZoneBytes.AddRange(new byte[] { Intensity, 0, 0 });
                    break;
                case RGBMode.Breathing:
                    // Period
                    rgbZoneBytes.AddRange(BitConverter.GetBytes(Period).Reverse());
                    // 0 Intensity 0 0 0
                    rgbZoneBytes.AddRange(new byte[] { 0, Intensity, 0, 0, 0 });
                    break;
            }

            return rgbZoneBytes;
        }
    }
}
