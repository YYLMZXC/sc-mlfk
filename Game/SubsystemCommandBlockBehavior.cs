using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemCommandBlockBehavior : SubsystemBlockBehavior, IDrawable
	{
		private class Chartlet
		{
			public Vector3 Position;

			public Vector3 P1;

			public Vector3 P2;

			public Vector3 P3;

			public Vector3 P4;

			public Color Color;

			public WorkingMode WorkingMode;
		}

		private Dictionary<Point3, CommandData> m_commandDatas = new Dictionary<Point3, CommandData>();

		private Dictionary<Point3, Chartlet[]> m_chartlets = new Dictionary<Point3, Chartlet[]>();

		private PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

		private TexturedBatch3D[] m_batchesByType = new TexturedBatch3D[4];

		private TexturedBatch3D[] m_batchesByType2 = new TexturedBatch3D[4];

		public SubsystemSky m_subsystemSky;

		public SubsystemTerrain m_subsystemTerrain;

		public Action<CommandData> OnCommandBlockGenerated;

		public static int[] m_drawOrders = new int[1] { 110 };

		public override int[] HandledBlocks
		{
			get
			{
				return new int[1] { 333 };
			}
		}

		public int[] DrawOrders
		{
			get
			{
				return m_drawOrders;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			CommandEditWidget.ScrollPosition.Clear();
			foreach (ValuesDictionary value3 in valuesDictionary.GetValue<ValuesDictionary>("Commands").Values)
			{
				Point3 value = value3.GetValue<Point3>("Position");
				string value2 = value3.GetValue("Line", "");
				SetCommandData(value, value2);
			}
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(true);
			m_batchesByType[0] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp0"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType[1] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp0"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType[2] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp0"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType[3] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp0"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType2[0] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp1"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType2[1] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp2"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType2[2] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp3"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			m_batchesByType2[3] = m_primitivesRenderer.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Csharp4"), false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			int num = 0;
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Commands", valuesDictionary2);
			foreach (CommandData value in m_commandDatas.Values)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary3.SetValue("Position", value.Position);
				valuesDictionary3.SetValue("Line", value.Line);
				valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				num++;
			}
		}

		public override void Dispose()
		{
			OnCommandBlockGenerated = null;
			try
			{
				CommandConfManager.SaveWhenDispose();
				InstructionManager.SaveHistoryItems();
			}
			catch (Exception ex)
			{
				Log.Warning("CommandBlockBehaviorDispose:" + ex.Message);
			}
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			if (!SubsystemCommand.InteractEnable)
			{
				return false;
			}
			AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			Point3 position = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
			if (componentMiner.ComponentPlayer != null)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CommandEditWidget(base.Project, componentMiner.ComponentPlayer, position);
				return true;
			}
			return false;
		}

		public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
		{
			Point3 key = new Point3(x, y, z);
			Chartlet[] value2;
			if (m_chartlets.TryGetValue(key, out value2))
			{
				Chartlet[] array = value2;
				foreach (Chartlet chartlet in array)
				{
					int? color = CommandBlock.GetColor(Terrain.ExtractData(value));
					chartlet.Color = (color.HasValue ? DataHandle.GetCommandColor(color.Value) : Color.Green);
					chartlet.WorkingMode = CommandBlock.GetWorkingMode(value);
				}
			}
		}

		public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
		{
			Point3 point = new Point3(x, y, z);
			CommandData commandData = GetCommandData(point);
			if (commandData == null)
			{
				commandData = SetCommandData(point, "");
			}
			if (OnCommandBlockGenerated != null)
			{
				OnCommandBlockGenerated(commandData);
			}
			int? color = CommandBlock.GetColor(Terrain.ExtractData(value));
			Color color2 = (color.HasValue ? DataHandle.GetCommandColor(color.Value) : Color.Green);
			Chartlet[] array = new Chartlet[4];
			for (int i = 0; i < array.Length; i++)
			{
				Chartlet chartlet = new Chartlet();
				Vector3 vector = Vector3.Cross(CellFace.FaceToVector3(i), Vector3.UnitY);
				chartlet.Position = new Vector3(point) + new Vector3(0.5f) + 0.5075f * CellFace.FaceToVector3(i);
				chartlet.P1 = chartlet.Position - 0.52f * (vector + Vector3.UnitY);
				chartlet.P2 = chartlet.Position + 0.52f * (vector - Vector3.UnitY);
				chartlet.P3 = chartlet.Position + 0.52f * (vector + Vector3.UnitY);
				chartlet.P4 = chartlet.Position - 0.52f * (vector - Vector3.UnitY);
				chartlet.Color = color2;
				chartlet.WorkingMode = CommandBlock.GetWorkingMode(value);
				array[i] = chartlet;
			}
			m_chartlets[point] = array;
		}

		public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
		{
			OnBlockGenerated(value, x, y, z, false);
		}

		public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
		{
			Point3 key = new Point3(x, y, z);
			m_commandDatas.Remove(key);
			m_chartlets.Remove(key);
		}

		public CommandData GetCommandData(Point3 position)
		{
			CommandData value;
			if (m_commandDatas.TryGetValue(position, out value))
			{
				return value;
			}
			return null;
		}

		public CommandData SetCommandData(Point3 position, string line)
		{
			CommandData commandData = new CommandData(position, line);
			string tip = commandData.TrySetValue();
			if (commandData.Name == null)
			{
				commandData.Line = string.Empty;
			}
			base.Project.FindSubsystem<SubsystemCommand>(true).ShowEditedTips(tip);
			m_commandDatas[position] = commandData;
			return commandData;
		}

		public void Draw(Camera camera, int drawOrder)
		{
			if (!SubsystemCommand.ChartletDraw)
			{
				return;
			}
			foreach (Point3 key in m_chartlets.Keys)
			{
				for (int i = 0; i < m_chartlets[key].Length; i++)
				{
					Chartlet chartlet = m_chartlets[key][i];
					if (chartlet.Color.A <= 0)
					{
						continue;
					}
					Vector3 vector = chartlet.Position - camera.ViewPosition;
					float num = Vector3.Dot(vector, camera.ViewDirection);
					Vector3 vector2 = 0.03f * num / vector.Length() * vector;
					if (num > 0.01f && vector.Length() < m_subsystemSky.ViewFogRange.Y)
					{
						if (chartlet.WorkingMode != WorkingMode.Variable)
						{
							m_batchesByType[i].QueueQuad(chartlet.P1 - vector2, chartlet.P2 - vector2, chartlet.P3 - vector2, chartlet.P4 - vector2, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), chartlet.Color);
						}
						else
						{
							m_batchesByType2[i].QueueQuad(chartlet.P1 - vector2, chartlet.P2 - vector2, chartlet.P3 - vector2, chartlet.P4 - vector2, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), chartlet.Color);
						}
					}
				}
			}
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}
	}
}
