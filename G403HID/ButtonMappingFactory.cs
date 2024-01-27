namespace G403HID
{
    internal static class ButtonMappingFactory
    {
        internal static ButtonMapping NewMapping(byte[] bytes)
        {
            switch (bytes[0])
            {
                case 0x90:
                case 0x80 when bytes[1] is 1 or 3:
                case 0xFF:
                    return new SimpleMapping(bytes);
                case 0x80 when bytes[1] == 2:
                    return new ModifierKeypressMapping(bytes);
                default:
                    throw new ArgumentException("Unsupported mapping type.");
            }
        }
    }
}
