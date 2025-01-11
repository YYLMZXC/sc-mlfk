using System;
using System.IO;

namespace LibPixz2
{
	public class BitReader
	{
		public const int dataSize = 16;

		public const int readerSize = 8;

		public BinaryReader reader;

		public Markers lastReadMarker;

		public uint readData;

		public int availableBits;

		public bool dataPad;

		public bool lockReading;

		public bool PastEndOfFile
		{
			get
			{
				return dataPad && availableBits <= 0;
			}
		}

		public BinaryReader BaseBinaryReader
		{
			get
			{
				return reader;
			}
		}

		public BitReader(BinaryReader reader)
		{
			Flush();
			dataPad = false;
			this.reader = reader;
		}

		public ushort Peek(uint length)
		{
			if (length > 16)
			{
				throw new Exception("Reading too many bits");
			}
			if (length > availableBits)
			{
				byte b = 0;
				try
				{
					while (availableBits <= length)
					{
						b = ReadByteOrMarker();
						if (lockReading)
						{
							break;
						}
						availableBits += 8;
						readData = (readData << 8) | b;
					}
				}
				catch (EndOfStreamException)
				{
					dataPad = true;
				}
			}
			uint num = readData << 32 - availableBits;
			num >>= (int)(32 - length);
			return (ushort)num;
		}

		public ushort Read(uint length)
		{
			if (length > 16)
			{
				throw new Exception("Reading too many bits");
			}
			ushort result = Peek(length);
			availableBits -= (int)length;
			int num = 32 - availableBits;
			readData <<= num;
			readData >>= num;
			return result;
		}

		public void StopReading()
		{
			if (!dataPad)
			{
				int num = (availableBits + 8 - 1) / 8 + 2;
				reader.BaseStream.Seek(-num, SeekOrigin.Current);
			}
			Flush();
		}

		public void Flush()
		{
			availableBits = 0;
			readData = 0u;
			lastReadMarker = Markers.LiteralFF;
			lockReading = false;
		}

		public byte ReadByteOrMarker()
		{
			if (!lockReading)
			{
				byte b = reader.ReadByte();
				if (b == byte.MaxValue)
				{
					byte b2 = reader.ReadByte();
					if (b2 == 0)
					{
						return b;
					}
					lastReadMarker = (Markers)b2;
					lockReading = true;
					return 0;
				}
				return b;
			}
			return 0;
		}

		public Markers SyncStreamToNextRestartMarker()
		{
			while ((lastReadMarker < Markers.Rs0 || lastReadMarker > Markers.Rs7) && lastReadMarker != Markers.Eoi)
			{
				ReadByteOrMarker();
			}
			Markers result = lastReadMarker;
			Flush();
			return result;
		}

		public bool WasEoiFound()
		{
			if (lastReadMarker == Markers.Eoi && availableBits <= 0)
			{
				Flush();
				return true;
			}
			return false;
		}
	}
}
