using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game;

namespace Command
{
	public class ClayBlock : Game.ClayBlock, IPaintableBlock
	{
		public new const int Index = 72;

		public static string CommandCategory = "命令方块彩色黏土";

		public override int GetFaceTextureSlot(int face, int value)
		{
			Color commandColor = GetCommandColor(Terrain.ExtractData(value));
			if (!PaintedCubeBlock.IsColored(Terrain.ExtractData(value)) && IsDefaultColor(commandColor))
			{
				return DefaultTextureSlot;
			}
			return 15;
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			if (SubsystemCommandDef.DisplayColorBlock)
			{
				List<int> list = new List<int>();
				for (int i = 0; i < 4096; i++)
				{
					list.Add(72 + i * 32 * 16384);
				}
				return list;
			}
			return base.GetCreativeValues();
		}

		public override string GetCategory(int value)
		{
			int data = Terrain.ExtractData(value);
			if (SubsystemCommandDef.DisplayColorBlock && !IsDefaultColor(GetCommandColor(data)))
			{
				return CommandCategory;
			}
			return base.GetCategory(value);
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			int data = Terrain.ExtractData(value);
			Color color = GetCommandColor(data);
			if (IsDefaultColor(color))
			{
				color = SubsystemPalette.GetColor(generator, PaintedCubeBlock.GetColor(data));
			}
			Color color2 = color;
			generator.GenerateCubeVertices(this, value, x, y, z, color2, geometry.OpaqueSubsetsByFace);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int data = Terrain.ExtractData(value);
			Color color2 = GetCommandColor(data);
			if (IsDefaultColor(color2))
			{
				color2 = SubsystemPalette.GetColor(environmentData, PaintedCubeBlock.GetColor(data));
			}
			color *= color2;
			BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData);
		}

		public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			int data = Terrain.ExtractData(value);
			Color color = GetCommandColor(data);
			if (IsDefaultColor(color))
			{
				color = SubsystemPalette.GetColor(subsystemTerrain, PaintedCubeBlock.GetColor(data));
			}
			Color color2 = color;
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color2, GetFaceTextureSlot(0, value));
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			int data = Terrain.ExtractData(oldValue);
			Color commandColor = GetCommandColor(data);
			if (PaintedCubeBlock.GetColor(data).HasValue || !IsDefaultColor(commandColor))
			{
				showDebris = true;
				if (toolLevel >= RequiredToolLevel)
				{
					dropValues.Add(new BlockDropValue
					{
						Value = Terrain.MakeBlockValue(DefaultDropContent, 0, data),
						Count = (int)DefaultDropCount
					});
				}
			}
			else
			{
				base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
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

		public new int Paint(SubsystemTerrain terrain, int value, int? color)
		{
			value = SetCommandColor(value, new Color(15, 15, 15));
			int data = Terrain.ExtractData(value);
			return Terrain.ReplaceData(value, PaintedCubeBlock.SetColor(data, color));
		}
	}
}
