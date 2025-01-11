using Command;
using Engine;
using Game;
namespace Mlfk
{
    public class CommandModLoader : ModLoader
    {
        public SubsystemCommandDef m_subsystemCommandDef;

        public SubsystemTime m_subsystemTime;

        public override void __ModInitialize()
        {
            ModsManager.RegisterHook("OnProjectLoaded", this);
            ModsManager.RegisterHook("SetRainAndSnowColor", this);
            ModsManager.RegisterHook("ChangeSkyColor", this);
            ModsManager.RegisterHook("ClothingProcessSlotItems", this);
            ModsManager.RegisterHook("OnCapture", this);
            ModsManager.RegisterHook("OnBlockExploded", this);
            ModsManager.RegisterHook("TerrainChangeCell", this);
            ModsManager.RegisterHook("ToFreeChunks", this);
            ModsManager.RegisterHook("ToAllocateChunks", this);
            ModsManager.RegisterHook("SetFurnitureDesignColor", this);
        }

        public override bool SetRainAndSnowColor(ref Color rainColor, ref Color snowColor)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            if (m_subsystemCommandDef.m_rainColor == Color.White)
            {
                return base.SetRainAndSnowColor(ref rainColor, ref snowColor);
            }

            rainColor = m_subsystemCommandDef.m_rainColor;
            snowColor = m_subsystemCommandDef.m_rainColor;
            return true;
        }

        public override Color ChangeSkyColor(Color oldColor, Vector3 direction, float timeOfDay, float precipitationIntensity, int temperature)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            if (m_subsystemCommandDef.m_skyColor == Color.White)
            {
                return base.ChangeSkyColor(oldColor, direction, timeOfDay, precipitationIntensity, temperature);
            }

            return m_subsystemCommandDef.m_skyColor;
        }

        public override bool ClothingProcessSlotItems(ComponentPlayer componentPlayer, Block block, int slotIndex, int value, int count)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            m_subsystemCommandDef.m_eatItem = new Point2(value, count);
            Time.QueueTimeDelayedExecution(Time.RealTime + 0.10000000149011612, delegate
            {
                m_subsystemCommandDef.m_eatItem = null;
            });
            return base.ClothingProcessSlotItems(componentPlayer, block, slotIndex, value, count);
        }

        public override void OnCapture()
        {
            m_subsystemCommandDef.m_onCapture = true;
            Time.QueueTimeDelayedExecution(Time.RealTime + 0.10000000149011612, delegate
            {
                m_subsystemCommandDef.m_onCapture = false;
            });
        }

        public override void OnBlockExploded(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            int item = Terrain.ExtractContents(value);
            if (m_subsystemCommandDef.m_firmAllBlocks || m_subsystemCommandDef.m_firmBlockList.Contains(item))
            {
                Time.QueueTimeDelayedExecution(Time.RealTime + 0.05000000074505806, delegate
                {
                    subsystemTerrain.ChangeCell(x, y, z, value);
                });
            }
        }

        public override void TerrainChangeCell(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value, out bool Skip)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            if (RecordManager.Recording)
            {
                if (!RecordManager.ChangeBlocks.ContainsKey(new Point3(x, y, z)))
                {
                    RecordManager.ChangeBlocks.Add(new Point3(x, y, z), subsystemTerrain.Terrain.GetCellValue(x, y, z));
                }

                RecordManager.RecordPlayerAction recordPlayerAction = new RecordManager.RecordPlayerAction();
                recordPlayerAction.ActionName = "Place";
                recordPlayerAction.Point = new Point3(x, y, z);
                recordPlayerAction.Value = value;
                recordPlayerAction.ActionTime = (float)m_subsystemTime.GameTime;
                RecordManager.RecordPlayerActions.Add(recordPlayerAction);
            }

            Skip = false;
        }

        public override void SetFurnitureDesignColor(FurnitureDesign design, Block block, int value, ref int FaceTextureSlot, ref Color Color)
        {
            if (m_subsystemCommandDef == null)
            {
                m_subsystemCommandDef = GameManager.Project.FindSubsystem<SubsystemCommandDef>();
            }

            Color commandColor = Command.ClayBlock.GetCommandColor(Terrain.ExtractData(value));
            if (!Command.ClayBlock.IsDefaultColor(commandColor))
            {
                if (block is Command.ClayBlock || block is LeavesBlock)
                {
                    Color = commandColor;
                }
                else if (block is Command.GlassBlock)
                {
                    FaceTextureSlot = 67;
                    Color = commandColor;
                }
            }
        }
    }
}