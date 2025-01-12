namespace LibPixz2
{
    public class Rgb : IColorspaceConverter
    {
        public Color2 ConvertToRgb(Info info)
        {
            int num = (byte)Common.Clamp(info.a + 128f, 0f, 255f);
            int num2 = (byte)Common.Clamp(info.b + 128f, 0f, 255f);
            int num3 = (byte)Common.Clamp(info.c + 128f, 0f, 255f);
            Color2 result = default(Color2);
            result.a = byte.MaxValue;
            result.r = (byte)num;
            result.g = (byte)num2;
            result.b = (byte)num3;
            return result;
        }

        public Info ConvertFromRgb(Color2 color)
        {
            Info result = default(Info);
            result.a = color.r - 128;
            result.b = color.g - 128;
            result.c = color.b - 128;
            return result;
        }
    }
}