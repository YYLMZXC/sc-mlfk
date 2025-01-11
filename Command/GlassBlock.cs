using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game;
using Mlfk;
namespace Command
{
    public class GlassBlock : Game.GlassBlock
    {
        public new const int Index = 15;

        public static string CommandCategory = "命令方块彩色玻璃";

        public Texture2D Texture;

        public override void Initialize()
        {
            Texture = ContentManager.Get<Texture2D>("Textures/Glass");
        }

        public override int GetFaceTextureSlot(int face, int value)
        {
            int data = Terrain.ExtractData(value);
            Color commandColor = GetCommandColor(data);
            if (IsDefaultColor(commandColor))
            {
                return base.GetFaceTextureSlot(face, value);
            }

            return GetCommandColorAlpha(data);
        }

        public override int GetTextureSlotCount(int value)
        {
            Color commandColor = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(commandColor))
            {
                return base.GetTextureSlotCount(value);
            }

            return 4;
        }

        public override IEnumerable<int> GetCreativeValues()
        {
            List<int> list = new List<int>();
            if (SubsystemCommandDef.DisplayColorBlock)
            {
                for (int i = 0; i < 4096; i++)
                {
                    list.Add(15 + i * 40 * 16384);
                }
            }
            else
            {
                list.Add(15);
            }

            return list;
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
        {
            Color commandColor = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(commandColor))
            {
                return base.CreateDebrisParticleSystem(subsystemTerrain, position, value, strength);
            }

            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, commandColor, GetFaceTextureSlot(0, value));
        }

        public override string GetCategory(int value)
        {
            if (SubsystemCommandDef.DisplayColorBlock && value != 15)
            {
                return CommandCategory;
            }

            return base.GetCategory(value);
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
        {
            Color commandColor = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(commandColor))
            {
                generator.GenerateCubeVertices(this, value, x, y, z, Color.White, geometry.AlphaTestSubsetsByFace);
            }
            else
            {
                generator.GenerateCubeVertices(this, value, x, y, z, commandColor, geometry.GetGeometry(Texture).TransparentSubsetsByFace);
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
        {
            Color commandColor = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(commandColor))
            {
                BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData);
            }
            else
            {
                BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, commandColor, commandColor, environmentData, Texture);
            }
        }

        public static Color GetCommandColor(int data)
        {
            int num = (data >> 5) & 0xFFF;
            int num2 = (num >> 8) & 0xF;
            int num3 = (num >> 4) & 0xF;
            int num4 = num & 0xF;
            return new Color(num2 * 16, num3 * 16, num4 * 16) + new Color(15, 15, 15);
        }

        public static int SetCommandColor(int value, Color color)
        {
            int num = Terrain.ExtractData(value);
            color -= new Color(15, 15, 15);
            int num2 = (int)((float)(int)color.R / 16f) << 8;
            int num3 = (int)((float)(int)color.G / 16f) << 4;
            int num4 = (int)((float)(int)color.B / 16f);
            int num5 = ((num2 | num3 | num4) & 0xFFF) << 5;
            num = (num & -131041) | num5;
            return Terrain.ReplaceData(value, num);
        }

        public static int GetCommandColorAlpha(int data)
        {
            return (data >> 1) & 0xF;
        }

        public static int SetCommandColorAlpha(int value, int alpha)
        {
            int num = Terrain.ExtractData(value);
            alpha = (alpha & 0xF) << 1;
            num = (num & -31) | alpha;
            return Terrain.ReplaceData(value, num);
        }

        public static bool IsDefaultColor(Color color)
        {
            return color.R == 15 && color.G == 15 && color.B == 15;
        }
    }
}