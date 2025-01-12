namespace LibPixz2
{
    public class ImgInfo
    {
        public ushort length;

        public byte dataPrecision;

        public ushort height;

        public ushort width;

        public bool hasRestartMarkers;

        public ushort restartInterval;

        public byte numOfComponents;

        public ComponentInfo[] components;

        public HuffmanTable[,] huffmanTables = new HuffmanTable[2, 4];

        public QuantTable[] quantTables = new QuantTable[4];

        public bool startOfImageFound;

        public bool app14MarkerFound;

        public App14ColorMode colorMode;

        public short[] deltaDc;

        public int mcuStrip = 0;

        public Markers prevRestMarker = Markers.Rs7;

        public const int blockSize = 8;
    }
}