using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game;

namespace Mlfk
{
    public class SpruceLeavesBlock : Game.SpruceLeavesBlock
    {
        public new const int Index = 14;

        public static string CommandCategory = "命令方块彩色云杉叶";

        public SpruceLeavesBlock()
        {
            ((LeavesBlock)(object)this).m_blockColorsMap = BlockColorsMap.SpruceLeavesColorsMap;
        }

        public override IEnumerable<int> GetCreativeValues()
        {
            List<int> list = new List<int>();
            if (SubsystemCommandDef.DisplayColorBlock)
            {
                for (int i = 0; i < 4096; i++)
                {
                    list.Add(14 + i * 32 * 16384);
                }
            }
            else
            {
                list.Add(14);
            }

            return list;
        }

        public override string GetCategory(int value)
        {
            if (SubsystemCommandDef.DisplayColorBlock && value != 14)
            {
                return CommandCategory;
            }

            return base.GetCategory(value);
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
        {
            if (((LeavesBlock)(object)this).m_random.Bool(0.15f))
            {
                dropValues.Add(new BlockDropValue
                {
                    Value = 23,
                    Count = 1
                });
                showDebris = true;
                return;
            }

            Color commandColor = GetCommandColor(Terrain.ExtractData(oldValue));
            if (IsDefaultColor(commandColor))
            {
                ((LeavesBlock)(object)this).GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
                return;
            }

            dropValues.Add(new BlockDropValue
            {
                Value = oldValue,
                Count = 1
            });
            showDebris = true;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
        {
            Color color = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(color))
            {
                color = ((LeavesBlock)(object)this).m_blockColorsMap.Lookup(generator.Terrain, x, y, z);
            }

            Color color2 = color;
            generator.GenerateCubeVertices(this, value, x, y, z, color2, geometry.AlphaTestSubsetsByFace);
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
        {
            Color commandColor = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(commandColor))
            {
                color *= ((LeavesBlock)(object)this).m_blockColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
            }
            else
            {
                color = commandColor;
            }

            BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData);
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
        {
            Color color = GetCommandColor(Terrain.ExtractData(value));
            if (IsDefaultColor(color))
            {
                color = ((LeavesBlock)(object)this).m_blockColorsMap.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
            }

            Color color2 = color;
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color2, GetFaceTextureSlot(4, value));
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
            color -= new Color(15, 15, 15);
            int num = (int)((float)(int)color.R / 16f) << 8;
            int num2 = (int)((float)(int)color.G / 16f) << 4;
            int num3 = (int)((float)(int)color.B / 16f);
            int data = ((num | num2 | num3) & 0xFFF) << 5;
            return Terrain.ReplaceData(value, data);
        }

        public static bool IsDefaultColor(Color color)
        {
            return color.R == 15 && color.G == 15 && color.B == 15;
        }
    }
}