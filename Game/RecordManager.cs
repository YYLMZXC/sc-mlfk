using System.Collections.Generic;
using Engine;

namespace Game
{
	public class RecordManager
	{
		public class RecordPlayerAction
		{
			public string ActionName;

			public float ActionTime;

			public Point3 Point;

			public int Value;

			public void MakeAction(ComponentPlayer componentPlayer)
			{
				componentPlayer.m_subsystemTerrain.DestroyCell(0, Point.X, Point.Y, Point.Z, Value, true, false, (ComponentMiner)null);
				componentPlayer.m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(Point.X, Point.Y, Point.Z), 5f, false);
				componentPlayer.Entity.FindComponent<ComponentHumanModel>().m_handAngles2 = new Vector2(-0.2f, 0.3f);
			}
		}

		public static Dictionary<Point3, int> ChangeBlocks = new Dictionary<Point3, int>();

		public static List<RecordPlayerStats> RecordPlayerStats = new List<RecordPlayerStats>();

		public static List<RecordPlayerAction> RecordPlayerActions = new List<RecordPlayerAction>();

		public static bool Recording = false;

		public static bool Replaying = false;

		public static float FrameTime = 0.01f;

		public static int StatsIndex = 0;

		public static int ActionIndex = 0;

		public static Vector3 FirstPosition;

		public static Vector3 FirstDirection;

		public static float FirstTime;

		public static float ReplayTime;

		public static void Replay(ComponentPlayer componentPlayer, float dt)
		{
			ReplayTime += dt;
			if (StatsIndex >= RecordPlayerStats.Count)
			{
				Replaying = false;
				componentPlayer.GameWidget.ActiveCamera = componentPlayer.GameWidget.FindCamera<FppCamera>();
			}
			RecordPlayerStats recordPlayerStats = RecordPlayerStats[StatsIndex];
			if (recordPlayerStats.Time - FirstTime - ReplayTime <= 0.02f)
			{
				componentPlayer.ComponentBody.Position = recordPlayerStats.Position;
				componentPlayer.ComponentBody.Rotation = recordPlayerStats.Rotation;
				componentPlayer.ComponentLocomotion.LookAngles = recordPlayerStats.LookAngles;
				int activeSlotIndex = componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex;
				int slotCount = componentPlayer.ComponentMiner.Inventory.GetSlotCount(activeSlotIndex);
				componentPlayer.ComponentMiner.Inventory.RemoveSlotItems(activeSlotIndex, slotCount);
				componentPlayer.ComponentMiner.Inventory.AddSlotItems(activeSlotIndex, recordPlayerStats.ActiveBlockValue, slotCount);
				componentPlayer.ComponentBody.IsSneaking = recordPlayerStats.Sneaking;
				StatsIndex++;
			}
			while (ActionIndex < RecordPlayerActions.Count)
			{
				float num = RecordPlayerActions[ActionIndex].ActionTime - FirstTime - ReplayTime;
				if (num <= 0.02f)
				{
					RecordPlayerActions[ActionIndex].MakeAction(componentPlayer);
					ActionIndex++;
					continue;
				}
				break;
			}
		}

		public static void AddRecordPlayerStats(ComponentPlayer componentPlayer, float time)
		{
			RecordPlayerStats recordPlayerStats = new RecordPlayerStats();
			recordPlayerStats.Time = time;
			recordPlayerStats.Position = componentPlayer.ComponentBody.Position;
			recordPlayerStats.Rotation = componentPlayer.ComponentBody.Rotation;
			recordPlayerStats.LookAngles = componentPlayer.ComponentLocomotion.LookAngles;
			recordPlayerStats.ActiveBlockValue = componentPlayer.ComponentMiner.ActiveBlockValue;
			recordPlayerStats.Sneaking = componentPlayer.ComponentBody.IsSneaking;
			RecordPlayerStats.Add(recordPlayerStats);
		}
	}
}
