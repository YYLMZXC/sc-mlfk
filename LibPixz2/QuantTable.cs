using Game;

namespace Mlfk
{
    public struct QuantTable
    {
        public bool valid;

        public byte id;

        public ushort length;

        public byte precision;

        public ushort[] table;
    }
}