namespace LibPixz2
{
	public static class Common
	{
		public static float Clamp(float num, float min, float max)
		{
			if (num < min)
			{
				return min;
			}
			if (num > max)
			{
				return max;
			}
			return num;
		}

		public static void Transpose(float[,] bloque, float[,] blTrns, int tamX, int tamY)
		{
			for (int i = 0; i < tamY; i++)
			{
				for (int j = 0; j < tamX; j++)
				{
					blTrns[j, i] = bloque[i, j];
				}
			}
		}
	}
}
