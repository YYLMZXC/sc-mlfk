using System;
using System.Collections.Generic;
using Engine;

namespace LibPixz2
{
	public class FileOps
	{
		public static Dictionary<int, Point2[]> tablasZigzag = new Dictionary<int, Point2[]>
		{
			{
				8,
				GetZigzagTable(8, 8)
			},
			{
				16,
				GetZigzagTable(16, 16)
			},
			{
				32,
				GetZigzagTable(32, 32)
			},
			{
				64,
				GetZigzagTable(64, 64)
			}
		};

		public static Point2[] GetZigzagTable(int width, int height)
		{
			if (width <= 0 || height <= 0)
			{
				throw new Exception("Block dimensions can't be less than zero");
			}
			Point2[] array = new Point2[height * width];
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			array[num3++] = new Point2(num, num2);
			while (num3 < height * width)
			{
				if (num == width - 1)
				{
					array[num3++] = new Point2(num, ++num2);
				}
				else
				{
					array[num3++] = new Point2(++num, num2);
				}
				if (num3 == height * width)
				{
					break;
				}
				while (num > 0 && num2 < height - 1)
				{
					array[num3++] = new Point2(--num, ++num2);
				}
				if (num2 == height - 1)
				{
					array[num3++] = new Point2(++num, num2);
				}
				else
				{
					array[num3++] = new Point2(num, ++num2);
				}
				if (num3 == height * width)
				{
					break;
				}
				while (num2 > 0 && num < width - 1)
				{
					array[num3++] = new Point2(++num, --num2);
				}
			}
			return array;
		}

		public static void ZigZagToArray(short[] coefZig, short[,] coefDct, Point2[] order, int size)
		{
			int num = size * size;
			for (int i = 0; i < num; i++)
			{
				coefDct[order[i].Y, order[i].X] = coefZig[i];
			}
		}
	}
}
