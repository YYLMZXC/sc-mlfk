using System.Collections.Generic;
using Engine;
using Game;
namespace Mlfk
{
	public class CopyBlockManager
	{
		public class CopyBlockData
		{
			public int Value;

			public int Data;

			public int Id;

			public string ExtraData;

			public object DirectData;
		}

		public string WorldDirectoryName;

		public bool HandleFurniture;

		public bool HandleAir;

		public bool HandleExtraData;

		public SubsystemCommandDef SubsystemCommandDef;

		public WithdrawBlockManager WBManager;

		public CubeArea CubeArea;

		public Point3 CopyOrigin;

		public Dictionary<Point3, CopyBlockData> CopyBlockDatas = new Dictionary<Point3, CopyBlockData>();

		public CopyBlockManager(SubsystemCommandDef subsystemCommandDef, WithdrawBlockManager wbManager, Point3 point1, Point3 point2, bool handleFurniture = false, bool handleAir = false, bool handleExtraData = false)
		{
			SubsystemCommandDef = subsystemCommandDef;
			WBManager = wbManager;
			HandleFurniture = handleFurniture;
			HandleAir = handleAir;
			HandleExtraData = handleExtraData;
			WorldDirectoryName = GameManager.m_worldInfo.DirectoryName;
			CubeArea = new CubeArea(point1, point2);
			CubeArea.Ergodic(delegate
			{
				int cellValue = SubsystemCommandDef.m_subsystemTerrain.Terrain.GetCellValue(CubeArea.Current.X, CubeArea.Current.Y, CubeArea.Current.Z);
				CopyBlockData copyBlockData = new CopyBlockData
				{
					Value = cellValue,
					Data = Terrain.ExtractData(cellValue),
					Id = Terrain.ExtractContents(cellValue)
				};
				if ((!HandleAir && copyBlockData.Id != 0) || HandleAir)
				{
					GetCopyExtraData(copyBlockData, CubeArea.Current);
					CopyBlockDatas[CubeArea.Current] = copyBlockData;
				}
				return false;
			});
		}

		public void ClearBlockArea(bool applyChangeCell = false)
		{
			foreach (Point3 key in CopyBlockDatas.Keys)
			{
				if (!SubsystemCommandDef.m_subsystemTerrain.Terrain.IsCellValid(key.X, key.Y, key.Z))
				{
					break;
				}
				int id = CopyBlockDatas[key].Id;
				if (WithdrawBlockManager.WithdrawMode && WBManager != null)
				{
					WBManager.SetCurrentCell(key.X, key.Y, key.Z, CopyBlockDatas[key].Value, 0);
				}
				if (id == 27 || id == 45 || id == 64 || id == 216)
				{
					ComponentBlockEntity blockEntity = SubsystemCommandDef.m_subsystemBlockEntities.GetBlockEntity(key.X, key.Y, key.Z);
					if (blockEntity != null)
					{
						blockEntity.Entity.FindComponent<ComponentInventoryBase>().m_slots.Clear();
					}
					SubsystemCommandDef.m_subsystemTerrain.ChangeCell(key.X, key.Y, key.Z, 0, true, (ComponentMiner)null);
				}
				else if (applyChangeCell)
				{
					SubsystemCommandDef.m_subsystemTerrain.ChangeCell(key.X, key.Y, key.Z, 0, true, (ComponentMiner)null);
				}
				else
				{
					SubsystemCommandDef.m_subsystemTerrain.Terrain.SetCellValueFast(key.X, key.Y, key.Z, 0);
				}
			}
		}

		public void CopyFromCache(Point3 placePoint)
		{
			if (HandleFurniture && GameManager.m_worldInfo.DirectoryName != WorldDirectoryName)
			{
				SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>();
				int num = 1;
				bool flag;
				string text;
				do
				{
					flag = false;
					text = "CommandSet" + num;
					foreach (FurnitureSet furnitureSet2 in subsystemFurnitureBlockBehavior.FurnitureSets)
					{
						if (furnitureSet2.Name == text)
						{
							flag = true;
							break;
						}
					}
					num++;
				}
				while (flag);
				int num2 = -1;
				for (int i = 0; i < subsystemFurnitureBlockBehavior.m_furnitureDesigns.Length; i++)
				{
					if (subsystemFurnitureBlockBehavior.m_furnitureDesigns[i] != null)
					{
						num2 = i;
					}
				}
				List<FurnitureDesign> list = SortFurniture();
				if (list.Count > 0)
				{
					FurnitureSet furnitureSet = subsystemFurnitureBlockBehavior.NewFurnitureSet(text, string.Empty);
					foreach (FurnitureDesign item in list)
					{
						if (item != null)
						{
							FurnitureDesign furnitureDesign = item.Clone();
							furnitureDesign.Index = item.Index + num2;
							subsystemFurnitureBlockBehavior.m_furnitureDesigns[furnitureDesign.Index] = furnitureDesign;
							subsystemFurnitureBlockBehavior.AddToFurnitureSet(furnitureDesign, furnitureSet);
						}
					}
				}
				foreach (Point3 key in CopyBlockDatas.Keys)
				{
					if (CopyBlockDatas[key].Id == 227 && CopyBlockDatas[key].DirectData != null)
					{
						FurnitureDesign furnitureDesign2 = (FurnitureDesign)CopyBlockDatas[key].DirectData;
						furnitureDesign2.Index += num2;
						CopyBlockDatas[key].Value = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(CopyBlockDatas[key].Data, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter));
					}
				}
			}
			foreach (Point3 key2 in CopyBlockDatas.Keys)
			{
				Point3 point = placePoint - CopyOrigin + key2;
				if (CopyBlockDatas[key2].Id != 0)
				{
					ChangeBlockValue(point, CopyBlockDatas[key2]);
				}
			}
		}

		public void DirectCopy(Point3 placePoint, bool applyAir)
		{
			foreach (Point3 key in CopyBlockDatas.Keys)
			{
				Point3 point = placePoint - CopyOrigin + key;
				if (!applyAir)
				{
					if (CopyBlockDatas[key].Id != 0)
					{
						ChangeBlockValue(point, CopyBlockDatas[key]);
					}
				}
				else
				{
					ChangeBlockValue(point, CopyBlockDatas[key]);
				}
			}
		}

		public void MirrorCopy(Point3 planePoint, string plane, bool laminate)
		{
			foreach (Point3 key in CopyBlockDatas.Keys)
			{
				if (CopyBlockDatas[key].Id != 0)
				{
					SetMirrorValue(CopyBlockDatas[key], plane);
					Point3 mirrorPoint = GetMirrorPoint(key, planePoint, plane, laminate);
					ChangeBlockValue(mirrorPoint, CopyBlockDatas[key]);
				}
			}
		}

		public void RotateCopy(Point3 axisPoint, string axis, string angle, bool applyChangeCell = false)
		{
			foreach (Point3 key in CopyBlockDatas.Keys)
			{
				if (CopyBlockDatas[key].Id != 0)
				{
					SetRotateValue(CopyBlockDatas[key], axis, angle);
					Point3 rotatePoint = GetRotatePoint(key, axisPoint, axis, angle);
					ChangeBlockValue(rotatePoint, CopyBlockDatas[key], applyChangeCell);
				}
			}
		}

		public void ChangeBlockValue(Point3 point, CopyBlockData copyData, bool applyChangeCell = false)
		{
			if (!SubsystemCommandDef.m_subsystemTerrain.Terrain.IsCellValid(point.X, point.Y, point.Z))
			{
				return;
			}
			int cellValueFast = SubsystemCommandDef.m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y, point.Z);
			int num = Terrain.ExtractContents(cellValueFast);
			if (WithdrawBlockManager.WithdrawMode && WBManager != null)
			{
				WBManager.SetCurrentCell(point.X, point.Y, point.Z, cellValueFast, copyData.Value);
			}
			if (num == 27 || num == 45 || num == 64 || num == 216)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, 0, true, (ComponentMiner)null);
			}
			if (copyData.Id == 27 || copyData.Id == 45 || copyData.Id == 64 || copyData.Id == 216)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
				ComponentBlockEntity blockEntity = SubsystemCommandDef.m_subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
				if (blockEntity == null)
				{
					return;
				}
				ComponentInventoryBase componentInventoryBase = blockEntity.Entity.FindComponent<ComponentInventoryBase>();
				if (copyData.DirectData == null)
				{
					return;
				}
				List<ComponentInventoryBase.Slot> list = (List<ComponentInventoryBase.Slot>)copyData.DirectData;
				if (copyData.Id == 27)
				{
					for (int i = 0; i < list.Count - 2; i++)
					{
						componentInventoryBase.AddSlotItems(i, list[i].Value, list[i].Count);
					}
				}
				else
				{
					for (int j = 0; j < list.Count; j++)
					{
						componentInventoryBase.AddSlotItems(j, list[j].Value, list[j].Count);
					}
				}
			}
			else if (copyData.Id == 97 || copyData.Id == 210 || copyData.Id == 98 || copyData.Id == 211)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
				SubsystemSignBlockBehavior subsystemSignBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemSignBlockBehavior>();
				if (copyData.DirectData != null)
				{
					subsystemSignBlockBehavior.SetSignData(point, ((SignData)copyData.DirectData).Lines, ((SignData)copyData.DirectData).Colors, ((SignData)copyData.DirectData).Url);
				}
			}
			else if (copyData.Id == 333)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
				SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
				if (copyData.DirectData != null)
				{
					subsystemCommandBlockBehavior.SetCommandData(point, ((CommandData)copyData.DirectData).Line);
				}
			}
			else if (copyData.Id == 186)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
				SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
				if (copyData.DirectData != null)
				{
					subsystemMemoryBankBlockBehavior.SetBlockData(point, (MemoryBankData)copyData.DirectData);
				}
			}
			else if (copyData.Id == 227 || copyData.Id == 94 || copyData.Id == 163 || copyData.Id == 164 || copyData.Id == 193 || copyData.Id == 202 || copyData.Id == 31)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
			}
			else if (applyChangeCell)
			{
				SubsystemCommandDef.m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, copyData.Value, true, (ComponentMiner)null);
			}
			else
			{
				SubsystemCommandDef.m_subsystemTerrain.Terrain.SetCellValueFast(point.X, point.Y, point.Z, copyData.Value);
			}
		}

		public void SetMirrorValue(CopyBlockData blockData, string plane)
		{
			Block block = BlocksManager.Blocks[blockData.Id];
			if (blockData.Id == 227 && blockData.DirectData != null)
			{
				FurnitureDesign furnitureDesign = ((FurnitureDesign)blockData.DirectData).Clone();
				int rotation = FurnitureBlock.GetRotation(blockData.Data);
				rotation = 4 - rotation;
				switch (plane)
				{
				case "xoy":
					furnitureDesign.Rotate(1, rotation);
					furnitureDesign.Mirror(0);
					break;
				case "xoz":
					furnitureDesign.Rotate(1, rotation);
					furnitureDesign.Mirror(1);
					furnitureDesign.Rotate(2, 2);
					break;
				case "zoy":
					furnitureDesign.Rotate(1, rotation);
					furnitureDesign.Mirror(1);
					break;
				}
				FurnitureDesign furnitureDesign2 = SubsystemCommandDef.m_subsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign);
				blockData.Value = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter));
			}
			else if (block is SlabBlock)
			{
				if (plane == "xoz")
				{
					bool isTop = SlabBlock.GetIsTop(blockData.Data);
					blockData.Value = Terrain.ReplaceData(blockData.Value, SlabBlock.SetIsTop(blockData.Data, !isTop));
				}
			}
			else if (block is StairsBlock)
			{
				StairsBlock.CornerType cornerType = StairsBlock.GetCornerType(blockData.Data);
				bool isUpsideDown = StairsBlock.GetIsUpsideDown(blockData.Data);
				int num = StairsBlock.GetRotation(blockData.Data);
				switch (plane)
				{
				case "xoz":
					blockData.Value = Terrain.ReplaceData(blockData.Value, StairsBlock.SetIsUpsideDown(blockData.Data, !isUpsideDown));
					break;
				case "xoy":
					if (cornerType == StairsBlock.CornerType.None)
					{
						switch (num)
						{
						case 0:
							num = 2;
							break;
						case 2:
							num = 0;
							break;
						}
					}
					else
					{
						switch (num)
						{
						case 0:
							num = 3;
							break;
						case 3:
							num = 0;
							break;
						case 1:
							num = 2;
							break;
						case 2:
							num = 1;
							break;
						}
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, StairsBlock.SetRotation(blockData.Data, num));
					break;
				case "zoy":
					if (cornerType == StairsBlock.CornerType.None)
					{
						switch (num)
						{
						case 1:
							num = 3;
							break;
						case 3:
							num = 1;
							break;
						}
					}
					else
					{
						switch (num)
						{
						case 0:
							num = 1;
							break;
						case 1:
							num = 0;
							break;
						case 2:
							num = 3;
							break;
						case 3:
							num = 2;
							break;
						}
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, StairsBlock.SetRotation(blockData.Data, num));
					break;
				}
			}
			else if (block is DoorBlock)
			{
				int num2 = DoorBlock.GetRotation(blockData.Data);
				if (plane == "xoy")
				{
					switch (num2)
					{
					case 0:
						num2 = 2;
						break;
					case 2:
						num2 = 0;
						break;
					}
				}
				else if (plane == "zoy")
				{
					switch (num2)
					{
					case 1:
						num2 = 3;
						break;
					case 3:
						num2 = 1;
						break;
					}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, DoorBlock.SetRotation(blockData.Data, num2));
			}
			else if (block is LadderBlock)
			{
				int num3 = LadderBlock.GetFace(blockData.Data);
				if (plane == "xoy")
				{
					switch (num3)
					{
					case 0:
						num3 = 2;
						break;
					case 2:
						num3 = 0;
						break;
					}
				}
				else if (plane == "zoy")
				{
					switch (num3)
					{
					case 1:
						num3 = 3;
						break;
					case 3:
						num3 = 1;
						break;
					}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, LadderBlock.SetFace(blockData.Data, num3));
			}
			else if (block is TrapdoorBlock)
			{
				switch (plane)
				{
				case "xoy":
				{
					int num5 = TrapdoorBlock.GetRotation(blockData.Data);
					switch (num5)
					{
					case 0:
						num5 = 2;
						break;
					case 2:
						num5 = 0;
						break;
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, TrapdoorBlock.SetRotation(blockData.Data, num5));
					break;
				}
				case "zoy":
				{
					int num4 = TrapdoorBlock.GetRotation(blockData.Data);
					switch (num4)
					{
					case 1:
						num4 = 3;
						break;
					case 3:
						num4 = 1;
						break;
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, TrapdoorBlock.SetRotation(blockData.Data, num4));
					break;
				}
				case "xoz":
				{
					bool upsideDown = TrapdoorBlock.GetUpsideDown(blockData.Data);
					blockData.Value = Terrain.ReplaceData(blockData.Value, TrapdoorBlock.SetUpsideDown(blockData.Data, !upsideDown));
					break;
				}
				}
			}
			else if (block is AttachedSignBlock)
			{
				int num6 = AttachedSignBlock.GetFace(blockData.Data);
				if (plane == "xoy")
				{
					switch (num6)
					{
					case 0:
						num6 = 2;
						break;
					case 2:
						num6 = 0;
						break;
					}
				}
				else if (plane == "zoy")
				{
					switch (num6)
					{
					case 1:
						num6 = 3;
						break;
					case 3:
						num6 = 1;
						break;
					}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, AttachedSignBlock.SetFace(blockData.Data, num6));
			}
			else if (block is PostedSignBlock)
			{
				int num7 = PostedSignBlock.GetDirection(blockData.Data);
				switch (plane)
				{
				case "xoz":
				{
					bool hanging = PostedSignBlock.GetHanging(blockData.Data);
					blockData.Value = Terrain.ReplaceData(blockData.Value, PostedSignBlock.SetHanging(blockData.Data, !hanging));
					break;
				}
				case "xoy":
					switch (num7)
					{
					case 0:
						num7 = 4;
						break;
					case 4:
						num7 = 0;
						break;
					case 1:
						num7 = 3;
						break;
					case 3:
						num7 = 1;
						break;
					case 5:
						num7 = 7;
						break;
					case 7:
						num7 = 5;
						break;
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, PostedSignBlock.SetDirection(blockData.Data, num7));
					break;
				case "zoy":
					switch (num7)
					{
					case 1:
						num7 = 7;
						break;
					case 7:
						num7 = 1;
						break;
					case 2:
						num7 = 6;
						break;
					case 6:
						num7 = 2;
						break;
					case 3:
						num7 = 5;
						break;
					case 5:
						num7 = 3;
						break;
					}
					blockData.Value = Terrain.ReplaceData(blockData.Value, PostedSignBlock.SetDirection(blockData.Data, num7));
					break;
				}
			}
			else if (block is FenceGateBlock)
			{
				int num8 = FenceGateBlock.GetRotation(blockData.Data);
				if (plane == "xoy")
				{
					switch (num8)
					{
					case 0:
						num8 = 2;
						break;
					case 2:
						num8 = 0;
						break;
					}
				}
				else if (plane == "zoy")
				{
					switch (num8)
					{
					case 1:
						num8 = 3;
						break;
					case 3:
						num8 = 1;
						break;
					}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, FenceGateBlock.SetRotation(blockData.Data, num8));
			}
			else if (block is DispenserBlock)
			{
				int num9 = DispenserBlock.GetDirection(blockData.Data);
				switch (plane)
				{
				case "xoy":
					switch (num9)
					{
					case 0:
						num9 = 2;
						break;
					case 2:
						num9 = 0;
						break;
					}
					break;
				case "zoy":
					switch (num9)
					{
					case 1:
						num9 = 3;
						break;
					case 3:
						num9 = 1;
						break;
					}
					break;
				case "xoz":
					switch (num9)
					{
					case 4:
						num9 = 5;
						break;
					case 5:
						num9 = 4;
						break;
					}
					break;
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, DispenserBlock.SetDirection(blockData.Data, num9));
			}
			else
			{
				if (!(block is TorchBlock))
				{
					return;
				}
				int num10 = blockData.Data;
				if (plane == "xoy")
				{
					switch (num10)
					{
					case 0:
						num10 = 2;
						break;
					case 2:
						num10 = 0;
						break;
					}
				}
				else if (plane == "zoy")
				{
					switch (num10)
					{
					case 1:
						num10 = 3;
						break;
					case 3:
						num10 = 1;
						break;
					}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, num10);
			}
		}

		public void SetRotateValue(CopyBlockData blockData, string axis, string angle)
		{
			Block block = BlocksManager.Blocks[blockData.Id];
			int num = 0;
			switch (angle)
			{
			case "+90":
				num = 1;
				break;
			case "+180":
				num = 2;
				break;
			case "+270":
				num = 3;
				break;
			}
			if (blockData.Id == 227 && blockData.DirectData != null)
			{
				FurnitureDesign furnitureDesign = ((FurnitureDesign)blockData.DirectData).Clone();
				int rotation = FurnitureBlock.GetRotation(blockData.Data);
				switch (axis)
				{
				case "+x":
				{
					switch (angle)
					{
					case "+90":
						furnitureDesign.Rotate(0, 3);
						furnitureDesign.Rotate(2, rotation);
						break;
					case "+180":
						furnitureDesign.Rotate(0, 2);
						furnitureDesign.Rotate(1, rotation);
						break;
					case "+270":
						furnitureDesign.Rotate(0, 1);
						furnitureDesign.Rotate(2, 4 - rotation);
						break;
					}
					FurnitureDesign furnitureDesign3 = SubsystemCommandDef.m_subsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign);
					blockData.Value = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign3.Index, furnitureDesign3.ShadowStrengthFactor, furnitureDesign3.IsLightEmitter));
					break;
				}
				case "+y":
					rotation = (rotation + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, FurnitureBlock.SetRotation(blockData.Data, rotation));
					break;
				case "+z":
				{
					switch (angle)
					{
					case "+90":
						furnitureDesign.Rotate(2, 1);
						furnitureDesign.Rotate(0, rotation);
						break;
					case "+180":
						furnitureDesign.Rotate(2, 2);
						furnitureDesign.Rotate(1, rotation);
						break;
					case "+270":
						furnitureDesign.Rotate(2, 3);
						furnitureDesign.Rotate(0, 4 - rotation);
						break;
					}
					FurnitureDesign furnitureDesign2 = SubsystemCommandDef.m_subsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign);
					blockData.Value = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter));
					break;
				}
				}
			}
			else if (block is StairsBlock)
			{
				StairsBlock.CornerType cornerType = StairsBlock.GetCornerType(blockData.Data);
				int rotation2 = StairsBlock.GetRotation(blockData.Data);
				switch (axis)
				{
				case "+y":
					rotation2 = (rotation2 + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, StairsBlock.SetRotation(blockData.Data, rotation2));
					break;
				case "+x":
					if (cornerType == StairsBlock.CornerType.None)
					{
						int irregularData2 = GetIrregularData(new int[4] { 0, 4, 6, 2 }, blockData.Data, num);
						if (irregularData2 != -1)
						{
							blockData.Value = Terrain.ReplaceData(blockData.Value, irregularData2);
						}
					}
					break;
				case "+z":
					if (cornerType == StairsBlock.CornerType.None)
					{
						int irregularData = GetIrregularData(new int[4] { 1, 3, 7, 5 }, blockData.Data, num);
						if (irregularData != -1)
						{
							blockData.Value = Terrain.ReplaceData(blockData.Value, irregularData);
						}
					}
					break;
				}
			}
			else if (block is WoodBlock)
			{
				switch (axis)
				{
				case "+y":
					if (num % 2 != 0 && blockData.Data != 0)
					{
						int num3 = blockData.Data;
						switch (num3)
						{
						case 1:
							num3 = 2;
							break;
						case 2:
							num3 = 1;
							break;
						}
						blockData.Value = Terrain.ReplaceData(blockData.Value, num3);
					}
					break;
				case "+x":
					if (num % 2 != 0 && blockData.Data != 2)
					{
						int num4 = blockData.Data;
						switch (num4)
						{
						case 0:
							num4 = 1;
							break;
						case 1:
							num4 = 0;
							break;
						}
						blockData.Value = Terrain.ReplaceData(blockData.Value, num4);
					}
					break;
				case "+z":
					if (num % 2 != 0 && blockData.Data != 1)
					{
						int num2 = blockData.Data;
						switch (num2)
						{
						case 0:
							num2 = 2;
							break;
						case 2:
							num2 = 0;
							break;
						}
						blockData.Value = Terrain.ReplaceData(blockData.Value, num2);
					}
					break;
				}
			}
			else if (block is DoorBlock)
			{
				if (axis == "+y")
				{
					int rotation3 = DoorBlock.GetRotation(blockData.Data);
					rotation3 = (rotation3 + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, DoorBlock.SetRotation(blockData.Data, rotation3));
				}
			}
			else if (block is LadderBlock)
			{
				if (axis == "+y")
				{
					int face = LadderBlock.GetFace(blockData.Data);
					face = (face + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, LadderBlock.SetFace(blockData.Data, face));
				}
			}
			else if (block is TrapdoorBlock)
			{
				if (axis == "+y")
				{
					int rotation4 = TrapdoorBlock.GetRotation(blockData.Data);
					rotation4 = (rotation4 + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, TrapdoorBlock.SetRotation(blockData.Data, rotation4));
				}
			}
			else if (block is AttachedSignBlock)
			{
				if (axis == "+y")
				{
					int face2 = AttachedSignBlock.GetFace(blockData.Data);
					face2 = (face2 + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, AttachedSignBlock.SetFace(blockData.Data, face2));
				}
			}
			else if (block is PostedSignBlock)
			{
				if (axis == "+y")
				{
					int direction = PostedSignBlock.GetDirection(blockData.Data);
					direction = (direction + 8 - 2 * num) % 8;
					blockData.Value = Terrain.ReplaceData(blockData.Value, PostedSignBlock.SetDirection(blockData.Data, direction));
				}
			}
			else if (block is FenceGateBlock)
			{
				if (axis == "+y")
				{
					int rotation5 = FenceGateBlock.GetRotation(blockData.Data);
					rotation5 = (rotation5 + 4 - num) % 4;
					blockData.Value = Terrain.ReplaceData(blockData.Value, FenceGateBlock.SetRotation(blockData.Data, rotation5));
				}
			}
			else if (block is DispenserBlock)
			{
				int num5 = DispenserBlock.GetDirection(blockData.Data);
				switch (axis)
				{
				case "+y":
					if (num5 >= 0 && num5 < 4)
					{
						num5 = (num5 + 4 - num) % 4;
					}
					break;
				case "+x":
				{
					int irregularData4 = GetIrregularData(new int[4] { 0, 5, 2, 4 }, num5, num);
					if (irregularData4 != -1)
					{
						num5 = irregularData4;
					}
					break;
				}
				case "+z":
				{
					int irregularData3 = GetIrregularData(new int[4] { 1, 4, 3, 5 }, num5, num);
					if (irregularData3 != -1)
					{
						num5 = irregularData3;
					}
					break;
				}
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, DispenserBlock.SetDirection(blockData.Data, num5));
			}
			else if (block is TorchBlock && axis == "+y")
			{
				int num6 = blockData.Data;
				if (num6 >= 0 && num6 < 4)
				{
					num6 = (num6 + 4 - num) % 4;
				}
				blockData.Value = Terrain.ReplaceData(blockData.Value, num6);
			}
		}

		public int GetIrregularData(int[] adata, int data, int angleIndex)
		{
			for (int i = 0; i < adata.Length; i++)
			{
				if (data == adata[i])
				{
					return adata[(i + angleIndex) % adata.Length];
				}
			}
			return -1;
		}

		public Point3 GetMirrorPoint(Point3 p, Point3 planePoint, string plane, bool laminate)
		{
			Point3 result = Point3.Zero;
			switch (plane)
			{
			case "xoy":
				result = new Point3(p.X, p.Y, 2 * planePoint.Z - p.Z);
				break;
			case "xoz":
				result = new Point3(p.X, 2 * planePoint.Y - p.Y, p.Z);
				break;
			case "zoy":
				result = new Point3(2 * planePoint.X - p.X, p.Y, p.Z);
				break;
			}
			if (laminate)
			{
				switch (plane)
				{
				case "xoy":
					result.Z = ((result.Z - planePoint.Z == 0) ? result.Z : ((result.Z - planePoint.Z > 0) ? (result.Z - 1) : (result.Z + 1)));
					break;
				case "xoz":
					result.Y = ((result.Y - planePoint.Y == 0) ? result.Y : ((result.Y - planePoint.Y > 0) ? (result.Y - 1) : (result.Y + 1)));
					break;
				case "zoy":
					result.X = ((result.X - planePoint.X == 0) ? result.X : ((result.X - planePoint.X > 0) ? (result.X - 1) : (result.X + 1)));
					break;
				}
			}
			return result;
		}

		public Point3 GetRotatePoint(Point3 p, Point3 axisPoint, string axis, string angle)
		{
			Point3 result = Point3.Zero;
			switch (axis)
			{
			case "+x":
				switch (angle)
				{
				case "+90":
					result = new Point3(p.X, axisPoint.Y - p.Z + axisPoint.Z, axisPoint.Z + p.Y - axisPoint.Y);
					break;
				case "+180":
					result = new Point3(p.X, 2 * axisPoint.Y - p.Y, 2 * axisPoint.Z - p.Z);
					break;
				case "+270":
					result = new Point3(p.X, axisPoint.Y + p.Z - axisPoint.Z, axisPoint.Z - p.Y + axisPoint.Y);
					break;
				}
				break;
			case "+y":
				switch (angle)
				{
				case "+90":
					result = new Point3(axisPoint.X - p.Z + axisPoint.Z, p.Y, axisPoint.Z + p.X - axisPoint.X);
					break;
				case "+180":
					result = new Point3(2 * axisPoint.X - p.X, p.Y, 2 * axisPoint.Z - p.Z);
					break;
				case "+270":
					result = new Point3(axisPoint.X + p.Z - axisPoint.Z, p.Y, axisPoint.Z - p.X + axisPoint.X);
					break;
				}
				break;
			case "+z":
				switch (angle)
				{
				case "+90":
					result = new Point3(axisPoint.X - p.Y + axisPoint.Y, axisPoint.Y + p.X - axisPoint.X, p.Z);
					break;
				case "+180":
					result = new Point3(2 * axisPoint.X - p.X, 2 * axisPoint.Y - p.Y, p.Z);
					break;
				case "+270":
					result = new Point3(axisPoint.X + p.Y - axisPoint.Y, axisPoint.Y - p.X + axisPoint.X, p.Z);
					break;
				}
				break;
			}
			return result;
		}

		public void GetCopyExtraData(CopyBlockData copyData, Point3 point)
		{
			if (copyData.Id == 27 || copyData.Id == 45 || copyData.Id == 64 || copyData.Id == 216)
			{
				ComponentBlockEntity blockEntity = SubsystemCommandDef.m_subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
				if (blockEntity == null)
				{
					return;
				}
				List<ComponentInventoryBase.Slot> list = new List<ComponentInventoryBase.Slot>();
				switch (copyData.Id)
				{
				case 27:
				{
					ComponentCraftingTable componentCraftingTable = blockEntity.Entity.FindComponent<ComponentCraftingTable>();
					if (componentCraftingTable == null)
					{
						break;
					}
					foreach (ComponentInventoryBase.Slot slot5 in componentCraftingTable.m_slots)
					{
						ComponentInventoryBase.Slot slot4 = new ComponentInventoryBase.Slot();
						slot4.Count = slot5.Count;
						slot4.Value = slot5.Value;
						list.Add(slot4);
					}
					break;
				}
				case 45:
				{
					ComponentChest componentChest = blockEntity.Entity.FindComponent<ComponentChest>();
					if (componentChest == null)
					{
						break;
					}
					foreach (ComponentInventoryBase.Slot slot6 in componentChest.m_slots)
					{
						ComponentInventoryBase.Slot slot2 = new ComponentInventoryBase.Slot();
						slot2.Count = slot6.Count;
						slot2.Value = slot6.Value;
						list.Add(slot2);
					}
					break;
				}
				case 64:
				{
					ComponentFurnace componentFurnace = blockEntity.Entity.FindComponent<ComponentFurnace>();
					if (componentFurnace == null)
					{
						break;
					}
					foreach (ComponentInventoryBase.Slot slot7 in componentFurnace.m_slots)
					{
						ComponentInventoryBase.Slot slot3 = new ComponentInventoryBase.Slot();
						slot3.Count = slot7.Count;
						slot3.Value = slot7.Value;
						list.Add(slot3);
					}
					break;
				}
				case 216:
				{
					ComponentDispenser componentDispenser = blockEntity.Entity.FindComponent<ComponentDispenser>();
					if (componentDispenser == null)
					{
						break;
					}
					foreach (ComponentInventoryBase.Slot slot8 in componentDispenser.m_slots)
					{
						ComponentInventoryBase.Slot slot = new ComponentInventoryBase.Slot();
						slot.Count = slot8.Count;
						slot.Value = slot8.Value;
						list.Add(slot);
					}
					break;
				}
				}
				copyData.DirectData = list;
				if (!HandleExtraData)
				{
					return;
				}
				copyData.ExtraData = string.Empty;
				{
					foreach (ComponentInventoryBase.Slot item in list)
					{
						copyData.ExtraData = copyData.ExtraData + item.Value + ":" + item.Count + ";";
					}
					return;
				}
			}
			if (copyData.Id == 97 || copyData.Id == 210 || copyData.Id == 98 || copyData.Id == 211)
			{
				SubsystemSignBlockBehavior subsystemSignBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemSignBlockBehavior>();
				SignData signData = subsystemSignBlockBehavior.GetSignData(point);
				if (signData != null)
				{
					SignData signData2 = new SignData();
					signData2.Colors = new Color[signData.Colors.Length];
					signData2.Lines = new string[signData.Colors.Length];
					signData2.Url = signData.Url;
					copyData.DirectData = signData2;
					copyData.ExtraData = string.Empty;
					copyData.ExtraData += "Colors=";
					for (int i = 0; i < signData.Colors.Length; i++)
					{
						signData2.Colors[i] = signData.Colors[i];
						copyData.ExtraData = copyData.ExtraData + signData.Colors[i].ToString() + ";";
					}
					copyData.ExtraData += "&Lines=";
					for (int j = 0; j < signData.Lines.Length; j++)
					{
						signData2.Lines[j] = signData.Lines[j];
						copyData.ExtraData = copyData.ExtraData + signData.Lines[j] + ";";
					}
					copyData.ExtraData = copyData.ExtraData + "&Url=" + signData.Url;
				}
			}
			else if (copyData.Id == 333)
			{
				SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
				CommandData commandData = subsystemCommandBlockBehavior.GetCommandData(point);
				if (commandData != null)
				{
					CommandData commandData2 = new CommandData(point, commandData.Line);
					commandData2.TrySetValue();
					copyData.DirectData = commandData2;
					copyData.ExtraData = commandData.Line;
				}
			}
			else if (copyData.Id == 186)
			{
				SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = SubsystemCommandDef.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
				MemoryBankData blockData = subsystemMemoryBankBlockBehavior.GetBlockData(point);
				if (blockData != null)
				{
					MemoryBankData memoryBankData = (MemoryBankData)(copyData.DirectData = (MemoryBankData)blockData.Copy());
					if (HandleExtraData)
					{
						copyData.ExtraData = memoryBankData.SaveString();
					}
				}
			}
			else if (copyData.Id == 227)
			{
				int designIndex = FurnitureBlock.GetDesignIndex(copyData.Data);
				FurnitureDesign design = SubsystemCommandDef.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>().GetDesign(designIndex);
				copyData.DirectData = design.Clone();
				if (!HandleExtraData)
				{
				}
			}
		}

		public List<FurnitureDesign> SortFurniture()
		{
			List<FurnitureDesign> list = new List<FurnitureDesign>();
			List<FurnitureDesign> list2 = new List<FurnitureDesign>();
			foreach (Point3 key in CopyBlockDatas.Keys)
			{
				if (CopyBlockDatas[key].Id == 227 && CopyBlockDatas[key].DirectData != null)
				{
					list.Add((FurnitureDesign)CopyBlockDatas[key].DirectData);
				}
			}
			int num = 1;
			foreach (FurnitureDesign item in list)
			{
				bool flag = false;
				foreach (FurnitureDesign item2 in list2)
				{
					if (item.Compare(item2))
					{
						item.Index = item2.Index;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					item.Index = num;
					list2.Add(item);
					num++;
				}
			}
			return list2;
		}
	}
}
