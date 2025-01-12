using System;
using System.Collections.Generic;

namespace LibPixz2
{
    public class Huffman
    {
        public struct CodeInfo
        {
            public ushort number;

            public uint code;

            public byte length;
        }

        public static void CreateTable(ref HuffmanTable huffmanTable)
        {
            ConvertToCanonicalCode(ref huffmanTable);
            PreparePreindexedTables(ref huffmanTable);
        }

        public static ushort ReadRunAmplitude(BitReader bReader, HuffmanTable table)
        {
            ushort num = bReader.Peek(table.maxCodeLength);
            CodeInfo codeInfo = table.preIndexTable[num];
            bReader.Read(codeInfo.length);
            return codeInfo.number;
        }

        public static short ReadCoefValue(BitReader bReader, uint size)
        {
            if (size == 0)
            {
                return 0;
            }

            ushort number = bReader.Read(size);
            return SpecialBitsToValue(number, (short)size);
        }

        public static short SpecialBitsToValue(ushort number, short size)
        {
            int num = 1 << size - 1;
            if (number < num)
            {
                return (short)(number - ((num << 1) - 1));
            }

            return (short)number;
        }

        public static void ConvertToCanonicalCode(ref HuffmanTable huffmanTable)
        {
            int num = -1;
            uint num2 = 1u;
            uint num3 = 0u;
            huffmanTable.table = new List<CodeInfo>();
            for (uint num4 = 0u; num4 < huffmanTable.numSymbols.Length; num4++)
            {
                uint num5 = num4 + 1;
                for (uint num6 = 0u; num6 < huffmanTable.numSymbols[num4]; num6++)
                {
                    CodeInfo item = default(CodeInfo);
                    int num7 = (int)(num5 - num2);
                    num = num + 1 << num7;
                    num2 = num5;
                    item.code = (uint)num;
                    item.length = (byte)num5;
                    item.number = huffmanTable.codes[num3++];
                    huffmanTable.table.Add(item);
                }
            }

            huffmanTable.maxCodeLength = (byte)num2;
        }

        public static void PreparePreindexedTables(ref HuffmanTable huffmanTable)
        {
            huffmanTable.preIndexTable = new CodeInfo[1 << (int)huffmanTable.maxCodeLength];
            foreach (CodeInfo item in huffmanTable.table)
            {
                int num = huffmanTable.maxCodeLength - item.length;
                uint num2 = (uint)(1 << num);
                uint num3 = item.code << num;
                for (uint num4 = num3; num4 < num2 + num3; num4++)
                {
                    huffmanTable.preIndexTable[num4] = item;
                }
            }
        }

        public int[] Test()
        {
            int num = 32;
            int maxValue = 40;
            int[] array = new int[num];
            Random random = new Random((int)DateTime.UtcNow.Ticks);
            for (int i = 0; i < num; i++)
            {
                array[i] += random.Next(maxValue);
            }

            return array;
        }
    }
}