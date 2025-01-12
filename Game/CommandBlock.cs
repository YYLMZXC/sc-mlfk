using Engine;
using Engine.Graphics;

namespace Game
{
    public class CommandBlock : CubeBlock, IElectricElementBlock, IPaintableBlock
    {
        public const int Index = 333;

        private Texture2D m_texture;

        public override void Initialize()
        {
            m_texture = ContentManager.Get<Texture2D>("Textures/Csharp6");
            EntityInfoManager.SetEntityInfos();
            CommandConfManager.Initialize();
            InstructionManager.Initialize();
            ManualTopicWidget.LoadInformationTopics();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
        {
            generator.GenerateCubeVertices(this, value, x, y, z, Color.White, geometry.GetGeometry(m_texture).OpaqueSubsetsByFace);
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
        {
            BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData, m_texture);
        }

        public override int GetFaceTextureSlot(int face, int value)
        {
            return face switch
            {
                4 => 1,
                5 => 1,
                _ => 0,
            };
        }

        public override int GetTextureSlotCount(int value)
        {
            return 2;
        }

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
        {
            Point3 position = new Point3(x, y, z);
            return new CommandElectricElement(subsystemElectricity, position);
        }

        public int GetConnectionMask(int value)
        {
            return int.MaxValue;
        }

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
        {
            switch (GetWorkingMode(value))
            {
                case WorkingMode.Condition:
                    return ElectricConnectorType.Output;
                case WorkingMode.Variable:
                    if (connectorFace == 4)
                    {
                        return ElectricConnectorType.Output;
                    }

                    break;
            }

            return ElectricConnectorType.Input;
        }

        public static WorkingMode GetWorkingMode(int value)
        {
            int num = Terrain.ExtractData(value) & 0xF;
            if (num >= 8)
            {
                num -= 8;
            }

            return (WorkingMode)num;
        }

        public static int SetWorkingMode(int value, WorkingMode mode)
        {
            int num = Terrain.ExtractData(value) & 0xF;
            num = (num + 8) % 16;
            if (GetWorkingMode(value) != mode)
            {
                num = (int)mode;
            }

            int data = (Terrain.ExtractData(value) & -16) | (num & 0xF);
            return Terrain.ReplaceData(value, data);
        }

        public int? GetPaintColor(int value)
        {
            return GetColor(Terrain.ExtractData(value));
        }

        public int Paint(SubsystemTerrain terrain, int value, int? color)
        {
            int data = Terrain.ExtractData(value);
            return Terrain.ReplaceData(value, SetColor(data, color));
        }

        public static int? GetColor(int data)
        {
            return (data >> 4) & 0xF;
        }

        public static int SetColor(int data, int? color)
        {
            if (color.HasValue)
            {
                return (data & -241) | ((color.Value & 0xF) << 4);
            }

            return data & -241;
        }
    }
}