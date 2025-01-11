using System.Collections.Generic;
using Engine;
using Game;
namespace Mlfk
{
    public class WithdrawBlockManager
    {
        public class Cell
        {
            public Point3 Point;

            public int Value;
        }

        public class WithdrawEntity
        {
            public List<Cell> WithdrawCellList = new List<Cell>();

            public Point3 MinPoint;

            public Point3 MaxPoint;
        }

        public static bool WithdrawMode = true;

        public static int Index = -1;

        public static int MaxSteps = 10;

        public static string WorldDirectoryName;

        public static Dictionary<int, WithdrawEntity> WithdrawEntitys = new Dictionary<int, WithdrawEntity>();

        public static Dictionary<int, CommandData> RecoveryEntitys = new Dictionary<int, CommandData>();

        public DynamicArray<Cell> CurrentCells = new DynamicArray<Cell>();

        public void SetCurrentCell(int x, int y, int z, int oldValue, int value)
        {
            if (oldValue != value)
            {
                CurrentCells.Add(new Cell
                {
                    Point = new Point3(x, y, z),
                    Value = oldValue
                });
            }
        }

        public void SetWithdrawEntity(Point3 minPoint, Point3 maxPoint)
        {
            WithdrawEntitys[Index] = new WithdrawEntity();
            WithdrawEntitys[Index].MinPoint = minPoint;
            WithdrawEntitys[Index].MaxPoint = maxPoint;
            WithdrawEntitys[Index].WithdrawCellList = CurrentCells.ToList();
        }

        public void SetRecoveryEntity(CommandData commandData)
        {
            RecoveryEntitys[Index] = new CommandData(commandData.Position, commandData.Line);
            RecoveryEntitys[Index].TrySetValue();
        }

        public void UpdateWithdrawCell(CommandData commandData, Point3 minPoint, Point3 maxPoint)
        {
            if (!WithdrawMode || CurrentCells.Count == 0)
            {
                return;
            }

            if (WithdrawEntitys.Count < MaxSteps)
            {
                Index = WithdrawEntitys.Count;
                SetWithdrawEntity(minPoint, maxPoint);
                SetRecoveryEntity(commandData);
            }
            else
            {
                for (int i = 0; i < MaxSteps - 1; i++)
                {
                    WithdrawEntitys[i] = WithdrawEntitys[i + 1];
                    RecoveryEntitys[i] = RecoveryEntitys[i + 1];
                }

                Index = MaxSteps - 1;
                SetWithdrawEntity(minPoint, maxPoint);
                SetRecoveryEntity(commandData);
            }

            CurrentCells.Clear();
        }

        public static void Recovery(SubsystemCommandDef subsystemCommandDef)
        {
            if (RecoveryEntitys.TryGetValue(Index + 1, out var value) && value != null)
            {
                subsystemCommandDef.Project.FindSubsystem<SubsystemCommand>().Submit(value.Name, value, Judge: false);
                subsystemCommandDef.ShowSubmitTips("重做完成:" + value.Name + "$" + value.Type);
            }
            else
            {
                subsystemCommandDef.ShowSubmitTips("没有可重做步骤");
            }
        }

        public static void CarryOut(SubsystemCommandDef subsystemCommandDef)
        {
            if (WithdrawEntitys.TryGetValue(Index, out var value))
            {
                subsystemCommandDef.ShowSubmitTips("正在撤回上一步，请稍后");
                foreach (Cell withdrawCell in value.WithdrawCellList)
                {
                    int cellValueFast = subsystemCommandDef.m_subsystemTerrain.Terrain.GetCellValueFast(withdrawCell.Point.X, withdrawCell.Point.Y, withdrawCell.Point.Z);
                    int num = Terrain.ExtractContents(cellValueFast);
                    subsystemCommandDef.m_subsystemTerrain.Terrain.SetCellValueFast(withdrawCell.Point.X, withdrawCell.Point.Y, withdrawCell.Point.Z, withdrawCell.Value);
                    if (num == 27 || num == 45 || num == 64 || num == 216)
                    {
                        SubsystemBlockBehavior[] blockBehaviors = subsystemCommandDef.m_subsystemTerrain.m_subsystemBlockBehaviors.GetBlockBehaviors(num);
                        for (int i = 0; i < blockBehaviors.Length; i++)
                        {
                            blockBehaviors[i].OnBlockRemoved(cellValueFast, withdrawCell.Value, withdrawCell.Point.X, withdrawCell.Point.Y, withdrawCell.Point.Z);
                        }
                    }
                }

                subsystemCommandDef.UpdateChunks(value.MinPoint, value.MaxPoint);
                WithdrawEntitys.Remove(Index);
                Index--;
                subsystemCommandDef.ShowSubmitTips("撤回完成，剩余可撤回步骤为" + (Index + 1) + "步");
            }
            else
            {
                subsystemCommandDef.ShowSubmitTips("没有记录的可撤回步骤");
            }
        }

        public static void Clear()
        {
            if (GameManager.m_worldInfo != null && GameManager.m_worldInfo.DirectoryName != WorldDirectoryName)
            {
                WithdrawEntitys.Clear();
                Index = -1;
                WorldDirectoryName = GameManager.m_worldInfo.DirectoryName;
            }
        }
    }
}