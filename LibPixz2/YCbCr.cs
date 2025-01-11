namespace LibPixz2
{
	public class YCbCr : IColorspaceConverter
	{
		public static float[,] mRgbYcbcr = new float[3, 3]
		{
			{ 0.299f, 0.587f, 0.114f },
			{ -0.1687f, -0.3313f, 0.5f },
			{ 0.5f, -0.4187f, -0.0813f }
		};

		public static float[,] mYcbcrRgb = new float[3, 3]
		{
			{ 1f, 0f, 1.402f },
			{ 1f, -0.34414f, -0.71414f },
			{ 1f, 1.772f, 0f }
		};

		public Color2 ConvertToRgb(Info info)
		{
			float num = info.a + 128f;
			float b = info.b;
			float c = info.c;
			byte r = (byte)Common.Clamp(num + c * mYcbcrRgb[0, 2], 0f, 255f);
			byte g = (byte)Common.Clamp(num + b * mYcbcrRgb[1, 1] + c * mYcbcrRgb[1, 2], 0f, 255f);
			byte b2 = (byte)Common.Clamp(num + b * mYcbcrRgb[2, 1], 0f, 255f);
			Color2 result = default(Color2);
			result.a = byte.MaxValue;
			result.r = r;
			result.g = g;
			result.b = b2;
			return result;
		}

		public Info ConvertFromRgb(Color2 rgb)
		{
			Info result = default(Info);
			result.a = (float)(int)rgb.r * mRgbYcbcr[0, 0] + (float)(int)rgb.g * mRgbYcbcr[0, 1] + (float)(int)rgb.b * mRgbYcbcr[0, 2] - 128f;
			result.b = (float)(int)rgb.r * mRgbYcbcr[1, 0] + (float)(int)rgb.g * mRgbYcbcr[1, 1] + (float)(int)rgb.b * mRgbYcbcr[1, 2];
			result.c = (float)(int)rgb.r * mRgbYcbcr[2, 0] + (float)(int)rgb.g * mRgbYcbcr[2, 1] + (float)(int)rgb.b * mRgbYcbcr[2, 2];
			return result;
		}
	}
}
