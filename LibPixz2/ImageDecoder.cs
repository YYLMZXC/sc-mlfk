using System.IO;
using Engine;
using Engine.Media;

namespace LibPixz2
{
	public class ImageDecoder
	{
		public const int blkSize = 8;

		public static float[,] blockP = new float[8, 8];

		public static short[,] coefDctQnt = new short[8, 8];

		public static float[,] coefDct = new float[8, 8];

		public const int DirRestartMarkerPeriod = 8;

		public static Image DecodeImage(BinaryReader reader, ImgInfo imgInfo)
		{
			BitReader bitReader = new BitReader(reader);
			float[][,] array = new float[imgInfo.numOfComponents][,];
			imgInfo.deltaDc = new short[imgInfo.numOfComponents];
			for (int i = 0; i < imgInfo.numOfComponents; i++)
			{
				array[i] = new float[imgInfo.height, imgInfo.width];
			}
			ComponentInfo[] components = imgInfo.components;
			ComponentInfo componentInfo = components[0];
			for (int j = 1; j < components.Length; j++)
			{
				ComponentInfo componentInfo2 = default(ComponentInfo);
				componentInfo2.samplingFactorX = (byte)MathUtils.Max(componentInfo.samplingFactorX, components[j].samplingFactorX);
				componentInfo2.samplingFactorY = (byte)MathUtils.Max(componentInfo.samplingFactorY, components[j].samplingFactorY);
				componentInfo = componentInfo2;
			}
			int num = 8 * componentInfo.samplingFactorX;
			int num2 = 8 * componentInfo.samplingFactorY;
			int num3 = (imgInfo.width + num - 1) / num;
			int num4 = (imgInfo.height + num2 - 1) / num2;
			for (int num5 = 0; num5 < num3 * num4; num5 = NextMcuPos(imgInfo, bitReader, num5, num3, num4))
			{
				int num6 = num5 % num3;
				int num7 = num5 / num3;
				int num8 = num6 * num;
				int num9 = num7 * num2;
				for (int k = 0; k < imgInfo.numOfComponents; k++)
				{
					int scaleX = componentInfo.samplingFactorX / imgInfo.components[k].samplingFactorX;
					int scaleY = componentInfo.samplingFactorY / imgInfo.components[k].samplingFactorY;
					for (int l = 0; l < imgInfo.components[k].samplingFactorY; l++)
					{
						for (int m = 0; m < imgInfo.components[k].samplingFactorX; m++)
						{
							DecodeBlock(bitReader, imgInfo, array[k], k, num8 + 8 * m, num9 + 8 * l, scaleX, scaleY);
						}
					}
				}
				if (bitReader.PastEndOfFile)
				{
					break;
				}
			}
			Color2[,] array2 = MergeChannels(imgInfo, array);
			Image image = new Image(imgInfo.width, imgInfo.height);
			bitReader.StopReading();
			for (int n = 0; n < imgInfo.height; n++)
			{
				for (int num10 = 0; num10 < imgInfo.width; num10++)
				{
					Color2 color = array2[n, num10];
					Color color2 = new Color(color.r, color.g, color.b, color.a);
					if (MathUtils.Min(color.r, color.g, color.b) < 176)
					{
						image.SetPixel(num10, n, color2);
					}
				}
			}
			return image;
		}

		public static int NextMcuPos(ImgInfo imgInfo, BitReader bReader, int mcu, int numMcusX, int numMcusY)
		{
			if (imgInfo.hasRestartMarkers && mcu % imgInfo.restartInterval == imgInfo.restartInterval - 1 && mcu < numMcusX * numMcusY - 1)
			{
				Markers markers = bReader.SyncStreamToNextRestartMarker();
				if (markers == Markers.Eoi)
				{
					return numMcusX * numMcusY;
				}
				int num = markers - imgInfo.prevRestMarker;
				if (num <= 0)
				{
					num += 8;
				}
				ResetDeltas(imgInfo);
				imgInfo.mcuStrip += num;
				imgInfo.prevRestMarker = markers;
				return imgInfo.mcuStrip * imgInfo.restartInterval;
			}
			if (bReader.WasEoiFound())
			{
				return numMcusX * numMcusY;
			}
			return ++mcu;
		}

		public static Color2[,] MergeChannels(ImgInfo imgInfo, float[][,] imgS)
		{
			Color2[,] array = new Color2[imgInfo.height, imgInfo.width];
			IColorspaceConverter colorspaceConverter;
			if (imgInfo.app14MarkerFound)
			{
				switch (imgInfo.colorMode)
				{
				case App14ColorMode.Unknown:
				{
					IColorspaceConverter colorspaceConverter3;
					if (imgInfo.numOfComponents != 3)
					{
						IColorspaceConverter colorspaceConverter2 = new YCbCr();
						colorspaceConverter3 = colorspaceConverter2;
					}
					else
					{
						IColorspaceConverter colorspaceConverter2 = new Rgb();
						colorspaceConverter3 = colorspaceConverter2;
					}
					colorspaceConverter = colorspaceConverter3;
					break;
				}
				case App14ColorMode.YCbCr:
					colorspaceConverter = new YCbCr();
					break;
				case App14ColorMode.YCCK:
					colorspaceConverter = new YCbCr();
					break;
				default:
					colorspaceConverter = new Rgb();
					break;
				}
			}
			else
			{
				colorspaceConverter = new YCbCr();
			}
			Info info = default(Info);
			for (int i = 0; i < imgInfo.height; i++)
			{
				for (int j = 0; j < imgInfo.width; j++)
				{
					if (imgInfo.numOfComponents == 1)
					{
						info.a = imgS[0][i, j];
						info.b = 0f;
						info.c = 0f;
					}
					else
					{
						info.a = imgS[0][i, j];
						info.b = imgS[1][i, j];
						info.c = imgS[2][i, j];
					}
					array[i, j] = colorspaceConverter.ConvertToRgb(info);
				}
			}
			return array;
		}

		public static void DecodeBlock(BitReader bReader, ImgInfo imgInfo, float[,] img, int compIndex, int ofsX, int ofsY, int scaleX, int scaleY)
		{
			int quantTableId = imgInfo.components[compIndex].quantTableId;
			short[] coefficients = GetCoefficients(bReader, imgInfo, compIndex, 64);
			FileOps.ZigZagToArray(coefficients, coefDctQnt, FileOps.tablasZigzag[8], 8);
			ImgOps.Dequant(coefDctQnt, coefDct, imgInfo.quantTables[quantTableId].table, 8);
			ImgOps.Fidct(coefDct, blockP, 8, 8);
			ImgOps.ResizeAndInsertBlock(imgInfo, blockP, img, 8, 8, ofsX, ofsY, scaleX, scaleY);
		}

		public static short[] GetCoefficients(BitReader bReader, ImgInfo imgInfo, int compIndex, int numCoefs)
		{
			short[] array = new short[numCoefs];
			int acHuffmanTable = imgInfo.components[compIndex].acHuffmanTable;
			int dcHuffmanTable = imgInfo.components[compIndex].dcHuffmanTable;
			uint num = Huffman.ReadRunAmplitude(bReader, imgInfo.huffmanTables[0, dcHuffmanTable]);
			uint size = num & 0xF;
			array[0] = (short)(Huffman.ReadCoefValue(bReader, size) + imgInfo.deltaDc[compIndex]);
			imgInfo.deltaDc[compIndex] = array[0];
			uint num2 = 0u;
			while (num2 < 63)
			{
				num = Huffman.ReadRunAmplitude(bReader, imgInfo.huffmanTables[1, acHuffmanTable]);
				if (num == 0)
				{
					break;
				}
				uint num3 = num >> 4;
				size = num & 0xF;
				num2 += num3 + 1;
				if (num2 >= 64)
				{
					break;
				}
				array[num2] = Huffman.ReadCoefValue(bReader, size);
			}
			return array;
		}

		public static void ResetDeltas(ImgInfo imgInfo)
		{
			for (int i = 0; i < imgInfo.numOfComponents; i++)
			{
				imgInfo.deltaDc[i] = 0;
			}
		}
	}
}
