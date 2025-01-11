using System.Collections.Generic;

namespace LibPixz2
{
	public struct HuffmanTable
	{
		public bool valid;

		public byte id;

		public byte type;

		public byte[] numSymbols;

		public byte[] codes;

		public byte maxCodeLength;

		public List<Huffman.CodeInfo> table;

		public Huffman.CodeInfo[] preIndexTable;
	}
}
