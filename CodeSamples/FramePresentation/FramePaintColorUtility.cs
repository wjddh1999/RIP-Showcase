using UnityEngine;

namespace RIP.Player.Modular
{
    public static class FramePaintColorUtility
    {
        public const int LegacyDefaultPacked = 0;

        public static readonly Color32 DefaultColor =
            new(31, 122, 255, byte.MaxValue);

        public static int Pack(Color32 color)
        {
            uint packed = color.r |
                          ((uint)color.g << 8) |
                          ((uint)color.b << 16) |
                          ((uint)color.a << 24);
            return unchecked((int)packed);
        }

        public static Color32 Unpack(int packed)
        {
            if (packed == LegacyDefaultPacked)
            {
                return DefaultColor;
            }

            uint value = unchecked((uint)packed);
            return new Color32(
                (byte)value,
                (byte)(value >> 8),
                (byte)(value >> 16),
                (byte)(value >> 24));
        }
    }
}
