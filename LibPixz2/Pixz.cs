using System;
using System.IO;
using Engine;
using Engine.Media;

namespace LibPixz2
{
	public class Pixz
	{
		public static DynamicArray<Image> Decode(Stream stream)
		{
			BinaryReader binaryReader = new BinaryReader(stream);
			DynamicArray<Image> dynamicArray = new DynamicArray<Image>();
			stream.Seek(0L, SeekOrigin.Begin);
			ImgInfo imgInfo = new ImgInfo();
			long length = stream.Length;
			while (stream.Position != length)
			{
				if (binaryReader.ReadByte() == byte.MaxValue)
				{
					int num = binaryReader.ReadByte();
					switch ((Markers)num)
					{
					case Markers.Sof2:
					case Markers.Eoi:
						break;
					case Markers.App0:
						ReadForApp0(binaryReader, imgInfo);
						break;
					case Markers.App14:
						ReadForApp14(binaryReader, imgInfo);
						break;
					case Markers.Dqt:
						ReadForDqt(binaryReader, imgInfo);
						break;
					case Markers.Sof0:
						ReadForSof0(binaryReader, imgInfo);
						break;
					case Markers.Dht:
						ReadForDht(binaryReader, imgInfo);
						break;
					case Markers.Dri:
						ReadForDri(binaryReader, imgInfo);
						break;
					case Markers.Sos:
						dynamicArray.Add(ReadForSos(binaryReader, imgInfo));
						break;
					case Markers.Soi:
						imgInfo = new ImgInfo
						{
							startOfImageFound = true
						};
						break;
					default:
						ReadForMarker(binaryReader, imgInfo, (Markers)num);
						break;
					}
				}
			}
			binaryReader.Dispose();
			return dynamicArray;
		}

		public static void ReadForApp0(BinaryReader reader, ImgInfo imgInfo)
		{
			ushort num = reader.ReadUInt16();
			reader.BaseStream.Seek(num - 2, SeekOrigin.Current);
		}

		public static void ReadForApp14(BinaryReader reader, ImgInfo imgInfo)
		{
			reader.ReadUInt16();
			reader.ReadBytes(11);
			imgInfo.app14MarkerFound = true;
			imgInfo.colorMode = (App14ColorMode)reader.ReadByte();
			if (imgInfo.colorMode > App14ColorMode.YCCK)
			{
				throw new Exception("Invalid Adobe colorspace");
			}
		}

		public static void ReadForDqt(BinaryReader reader, ImgInfo imgInfo)
		{
			int num = reader.ReadUInt16() - 2;
			while (num > 0)
			{
				int num2 = ReadDqtTable(reader, imgInfo);
				num -= num2;
			}
		}

		public static void ReadForSof0(BinaryReader reader, ImgInfo imgInfo)
		{
			imgInfo.length = reader.ReadUInt16();
			imgInfo.dataPrecision = reader.ReadByte();
			imgInfo.height = reader.ReadUInt16();
			imgInfo.width = reader.ReadUInt16();
			imgInfo.numOfComponents = reader.ReadByte();
			if (imgInfo.length < 8)
			{
				throw new Exception("Invalid length of Sof0");
			}
			if (imgInfo.height == 0 || imgInfo.width == 0)
			{
				throw new Exception("Invalid image size");
			}
			if (imgInfo.dataPrecision != 8)
			{
				throw new Exception("Unsupported data precision");
			}
			if (imgInfo.numOfComponents != 1 && imgInfo.numOfComponents != 3)
			{
				throw new Exception("Invalid number of components");
			}
			imgInfo.components = new ComponentInfo[imgInfo.numOfComponents];
			for (int i = 0; i < imgInfo.numOfComponents; i++)
			{
				byte b = reader.ReadByte();
				if (b > 3)
				{
					throw new Exception("Invalid component type");
				}
				byte b2 = reader.ReadByte();
				imgInfo.components[i].id = b;
				imgInfo.components[i].samplingFactorX = (byte)(b2 >> 4);
				imgInfo.components[i].samplingFactorY = (byte)(b2 & 0xF);
				imgInfo.components[i].quantTableId = reader.ReadByte();
			}
		}

		public static void ReadForDht(BinaryReader reader, ImgInfo imgInfo)
		{
			int num = reader.ReadUInt16() - 2;
			while (num > 0)
			{
				int num2 = ReadDhtTable(reader, imgInfo);
				num -= num2;
			}
		}

		public static void ReadForDri(BinaryReader reader, ImgInfo imgInfo)
		{
			reader.ReadUInt16();
			ushort num = reader.ReadUInt16();
			if (num == 0)
			{
				throw new Exception("Invalid restart interval (0)");
			}
			imgInfo.restartInterval = num;
			imgInfo.hasRestartMarkers = true;
		}

		public static Image ReadForSos(BinaryReader reader, ImgInfo imgInfo)
		{
			if (imgInfo.numOfComponents != 1 && imgInfo.numOfComponents != 3)
			{
				throw new Exception("Unsupported number of components (" + imgInfo.numOfComponents + ")");
			}
			ushort num = reader.ReadUInt16();
			byte b = reader.ReadByte();
			for (int i = 0; i < b; i++)
			{
				byte b2 = (byte)(reader.ReadByte() - 1);
				byte b3 = reader.ReadByte();
				byte acHuffmanTable = (byte)(b3 & 0xF);
				byte dcHuffmanTable = (byte)(b3 >> 4);
				imgInfo.components[b2].dcHuffmanTable = dcHuffmanTable;
				imgInfo.components[b2].acHuffmanTable = acHuffmanTable;
			}
			reader.ReadBytes(3);
			return ImageDecoder.DecodeImage(reader, imgInfo);
		}

		public static void ReadForMarker(BinaryReader reader, ImgInfo imgInfo, Markers markerId)
		{
			if ((markerId < Markers.Rs0 || markerId > Markers.Rs7) && markerId != 0 && imgInfo.startOfImageFound)
			{
				ushort num = reader.ReadUInt16();
				reader.BaseStream.Seek(num - 2, SeekOrigin.Current);
			}
		}

		public static int ReadDqtTable(BinaryReader reader, ImgInfo imgInfo)
		{
			byte b = reader.ReadByte();
			byte b2 = (byte)(b & 0xF);
			if (b2 > 3)
			{
				throw new Exception("Invalid ID for quantization table");
			}
			QuantTable quantTable = default(QuantTable);
			quantTable.id = b2;
			quantTable.precision = (byte)(b >> 4);
			quantTable.valid = true;
			quantTable.table = new ushort[64];
			QuantTable quantTable2 = quantTable;
			int num = ((quantTable2.precision == 0) ? 1 : 2);
			Point2[] array = FileOps.tablasZigzag[8];
			if (quantTable2.precision == 0)
			{
				for (int i = 0; i < 64; i++)
				{
					quantTable2.table[array[i].Y * 8 + array[i].X] = reader.ReadByte();
				}
			}
			else
			{
				for (int j = 0; j < 64; j++)
				{
					quantTable2.table[array[j].Y * 8 + array[j].X] = reader.ReadUInt16();
				}
			}
			imgInfo.quantTables[b2] = quantTable2;
			return 1 + 64 * num;
		}

		public static int ReadDhtTable(BinaryReader reader, ImgInfo imgInfo)
		{
			byte b = reader.ReadByte();
			byte b2 = (byte)(b & 7);
			int num = 0;
			if (b2 > 3)
			{
				throw new Exception("Invalid ID for huffman table");
			}
			if ((b & 0xE0) != 0)
			{
				throw new Exception("Invalid huffman table");
			}
			HuffmanTable huffmanTable = default(HuffmanTable);
			huffmanTable.id = b2;
			huffmanTable.type = (byte)((b >> 4) & 1);
			huffmanTable.valid = true;
			huffmanTable.numSymbols = new byte[16];
			HuffmanTable huffmanTable2 = huffmanTable;
			for (int i = 0; i < 16; i++)
			{
				huffmanTable2.numSymbols[i] = reader.ReadByte();
				num += huffmanTable2.numSymbols[i];
			}
			huffmanTable2.codes = new byte[num];
			for (int j = 0; j < num; j++)
			{
				huffmanTable2.codes[j] = reader.ReadByte();
			}
			Huffman.CreateTable(ref huffmanTable2);
			imgInfo.huffmanTables[huffmanTable2.type, huffmanTable2.id] = huffmanTable2;
			return 17 + num;
		}
	}
}
