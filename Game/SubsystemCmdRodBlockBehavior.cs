using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;
using Game;
using Mlfk;
using LibPixz2;
namespace Mlfk
{
	public class SubsystemCmdRodBlockBehavior : SubsystemBlockBehavior, IDrawable, IUpdateable
	{
		public class CommandPointData
		{
			public string Pos;

			public string Value;

			public bool Lock;

			public BevelledButtonWidget Button;
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemProjectiles m_subsystemProjectiles;

		public SubsystemCommand m_subsystemCommand;

		public SubsystemTime m_subsystemTime;

		public PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

		public PrimitivesRenderer3D m_primitivesRenderer3D2 = new PrimitivesRenderer3D();

		public Point3? m_recordPosition;

		public int? m_recordBlockValue;

		public Point2? m_recordEyes;

		public int? m_recordfurnitureId;

		public int? m_recordclothesId;

		public string m_recordEntityName = null;

		public string m_commandLine = string.Empty;

		private Color m_color = Color.Yellow;

		private float m_lastGameTime;

		private float m_aimTime;

		private bool m_firstAim = true;

		private Vector3 m_targetPos;

		private Texture2D m_texture;

		private Color m_sightColor;

		public static bool QuickMode = false;

		public int m_pointIndex;

		public StackPanelWidget m_pointDataWidget;

		public BitmapButtonWidget m_withdrawButton;

		public BitmapButtonWidget m_recoveryButton;

		public Dictionary<string, CommandPointData> m_commandPoints = new Dictionary<string, CommandPointData>();

		public static bool ShowRay = true;

		public static bool ShowChunk = false;

		public override int[] HandledBlocks
		{
			get
			{
				return new int[1] { 334 };
			}
		}

		public int[] DrawOrders
		{
			get
			{
				return new int[1] { 200 };
			}
		}

		public UpdateOrder UpdateOrder
		{
			get
			{
				return UpdateOrder.Default;
			}
		}

		public void Draw(Camera camera, int drawOrder)
		{
			if (m_subsystemCommand.m_componentPlayer == null)
			{
				return;
			}
			if (m_targetPos != Vector3.Zero)
			{
				float num = 2f;
				Vector3 eyePosition = m_subsystemCommand.m_componentPlayer.ComponentCreatureModel.EyePosition;
				Vector3 vector = Vector3.Normalize(m_targetPos - eyePosition);
				Vector3 vector2 = eyePosition + vector * 50f;
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector, Vector3.UnitY));
				Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector, vector3));
				Vector3 p = vector2 + num * (-vector3 - vector4);
				Vector3 p2 = vector2 + num * (vector3 - vector4);
				Vector3 p3 = vector2 + num * (vector3 + vector4);
				Vector3 p4 = vector2 + num * (-vector3 + vector4);
				TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(m_texture, false, 0, DepthStencilState.None);
				int count = texturedBatch3D.TriangleVertices.Count;
				texturedBatch3D.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), m_sightColor);
				texturedBatch3D.TransformTriangles(camera.ViewMatrix, count);
				m_primitivesRenderer3D.Flush(camera.ProjectionMatrix);
			}
			else
			{
				m_primitivesRenderer3D.Clear();
			}
			if (m_recordPosition.HasValue)
			{
				Vector3 vector5 = new Vector3(m_recordPosition.Value);
				FlatBatch3D flatBatch3D = m_primitivesRenderer3D2.FlatBatch();
				BoundingBox boundingBox = new BoundingBox(vector5, vector5 + new Vector3(1f));
				Color green = Color.Green;
				if (ShowRay)
				{
					flatBatch3D.QueueLine(vector5 + new Vector3(-100.5f, 0.5f, 0.5f), vector5 + new Vector3(100.5f, 0.5f, 0.5f), Color.Yellow);
					flatBatch3D.QueueLine(vector5 + new Vector3(0.5f, -100.5f, 0.5f), vector5 + new Vector3(0.5f, 100.5f, 0.5f), green);
					flatBatch3D.QueueLine(vector5 + new Vector3(0.5f, 0.5f, -100.5f), vector5 + new Vector3(0.5f, 0.5f, 100.5f), green);
				}
				flatBatch3D.QueueBoundingBox(boundingBox, green);
				flatBatch3D.QueueLine(vector5 + new Vector3(1f, 0f, 0f), vector5 + new Vector3(0.9f, 0f, 0.05f), Color.Yellow);
				flatBatch3D.QueueLine(vector5 + new Vector3(1f, 0f, 0f), vector5 + new Vector3(0.9f, 0f, -0.05f), Color.Yellow);
				flatBatch3D.QueueLine(vector5 + new Vector3(0f, 1f, 0f), vector5 + new Vector3(0.05f, 0.9f, 0f), green);
				flatBatch3D.QueueLine(vector5 + new Vector3(0f, 1f, 0f), vector5 + new Vector3(-0.05f, 0.9f, 0f), green);
				flatBatch3D.QueueLine(vector5 + new Vector3(0f, 0f, 1f), vector5 + new Vector3(0.05f, 0f, 0.9f), green);
				flatBatch3D.QueueLine(vector5 + new Vector3(0f, 0f, 1f), vector5 + new Vector3(-0.05f, 0f, 0.9f), green);
				if (ShowChunk)
				{
					Color blue = Color.Blue;
					Point2 point = Terrain.ToChunk(new Vector2(vector5.X, vector5.Z));
					Vector3 vector6 = new Vector3(point.X * 16, 0f, point.Y * 16);
					flatBatch3D.QueueLine(vector6, vector6 + new Vector3(0f, 255f, 0f), blue);
					flatBatch3D.QueueLine(vector6 + new Vector3(0f, 0f, 16f), vector6 + new Vector3(0f, 255f, 16f), blue);
					flatBatch3D.QueueLine(vector6 + new Vector3(16f, 0f, 0f), vector6 + new Vector3(16f, 255f, 0f), blue);
					flatBatch3D.QueueLine(vector6 + new Vector3(16f, 0f, 16f), vector6 + new Vector3(16f, 255f, 16f), blue);
				}
				m_primitivesRenderer3D2.Flush(camera.ViewProjectionMatrix);
			}
			else
			{
				m_primitivesRenderer3D2.Clear();
			}
		}

		public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
		{
			if (m_firstAim)
			{
				m_lastGameTime = (float)m_subsystemTime.GameTime;
				m_aimTime = 0f;
				m_firstAim = false;
				m_targetPos = Vector3.Zero;
				m_sightColor = Color.Red;
			}
			switch (state)
			{
			case AimState.InProgress:
				m_aimTime = (float)m_subsystemTime.GameTime - m_lastGameTime;
				if (m_aimTime > 1f)
				{
					object obj = DataHandle.Raycast(aim, componentMiner);
					if (obj is TerrainRaycastResult)
					{
						CellFace cellFace = ((TerrainRaycastResult)obj).CellFace;
						m_targetPos = new Vector3(cellFace.Point) + new Vector3(0.5f) - aim.Direction * 2f;
						m_sightColor = Color.Green;
					}
					else if (obj is BodyRaycastResult)
					{
						ComponentBody componentBody = ((BodyRaycastResult)obj).ComponentBody;
						m_targetPos = componentBody.Position - aim.Direction * 2f;
						m_sightColor = Color.Green;
					}
					else if (obj is Ray3)
					{
						Ray3 ray = (Ray3)obj;
						m_targetPos = ray.Position + ray.Direction * 100f;
						m_sightColor = Color.Red;
					}
				}
				break;
			case AimState.Completed:
				m_firstAim = true;
				if (m_sightColor != Color.Red)
				{
					componentMiner.ComponentPlayer.ComponentBody.Position = m_targetPos;
					m_targetPos = Vector3.Zero;
					m_sightColor = Color.Red;
					return true;
				}
				m_targetPos = Vector3.Zero;
				break;
			case AimState.Cancelled:
				m_firstAim = true;
				m_targetPos = Vector3.Zero;
				break;
			}
			return false;
		}

		public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
		{
			object obj = componentMiner.Raycast(ray, RaycastMode.Digging);
			Point2 playerEyesAngle = DataHandle.GetPlayerEyesAngle(componentMiner.ComponentPlayer);
			string text = string.Format("\n视角: 水平方向:{0},垂直方向:{1}", playerEyesAngle.X, playerEyesAngle.Y);
			if (obj is TerrainRaycastResult)
			{
				CellFace cellFace = ((TerrainRaycastResult)obj).CellFace;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				int num = Terrain.ReplaceLight(cellValue, 0);
				int num2 = Terrain.ExtractContents(cellValue);
				int num3 = Terrain.ExtractData(cellValue);
				int num4 = -1;
				foreach (Pickable pickable in m_subsystemPickables.Pickables)
				{
					float num5 = Vector3.Distance(pickable.Position, new Vector3(cellFace.Point));
					if (num5 <= 1.5f)
					{
						num4 = pickable.Value;
						break;
					}
				}
				string empty = string.Empty;
				string text2 = string.Empty;
				string text3 = string.Empty;
				empty = string.Format("方块ID:{0}；特殊值:{1}；方块值:{2}\n标准坐标:({3},{4},{5})；方块面:{6}", num2, num3, num, cellFace.X, cellFace.Y, cellFace.Z, cellFace.Face);
				switch (num2)
				{
				case 333:
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(empty, Color.LightBlue, false, false);
					return false;
				case 227:
				{
					int designIndex = FurnitureBlock.GetDesignIndex(num3);
					int rotation = FurnitureBlock.GetRotation(num3);
					int num6 = ((cellFace.Face - rotation >= 0) ? (cellFace.Face - rotation) : (cellFace.Face - rotation + 4));
					if (cellFace.Face == 4 || cellFace.Face == 5)
					{
						num6 = cellFace.Face;
					}
					m_recordfurnitureId = designIndex;
					text2 = string.Format("\n家具序号:{0}；家具面:{1}", designIndex, num6);
					break;
				}
				}
				if (num4 != -1)
				{
					int num7 = Terrain.ExtractContents(num4);
					int num8 = Terrain.ExtractData(num4);
					text3 = string.Format("\n检测到掉落物的ID:{0}；特殊值:{1}；方块值:{2}", num7, num8, num4);
					if (num7 == 203)
					{
						int clothingIndex = ClothingBlock.GetClothingIndex(num8);
						m_recordclothesId = clothingIndex;
						text3 = text3 + "；衣物序号:" + clothingIndex;
					}
				}
				SetPointData(cellFace.Point);
				m_recordPosition = cellFace.Point;
				m_recordBlockValue = ((num4 != -1) ? num4 : num);
				m_recordEyes = playerEyesAngle;
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(empty + text + text2 + text3, m_color, false, false);
				return true;
			}
			if (obj is BodyRaycastResult)
			{
				ComponentBody componentBody = ((BodyRaycastResult)obj).ComponentBody;
				if (componentBody != null)
				{
					string text4 = componentBody.Entity.ValuesDictionary.DatabaseObject.Name.ToLower();
					ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
					ComponentDamage componentDamage = componentBody.Entity.FindComponent<ComponentDamage>();
					string text5 = "生物实体名:" + text4;
					Point3 bodyPoint = DataHandle.GetBodyPoint(componentBody);
					if (componentCreature != null)
					{
						string displayName = componentCreature.DisplayName;
						float num9 = componentCreature.ComponentHealth.Health * 100f;
						string text6 = componentBody.Mass.ToString();
						string text7 = componentBody.BoxSize.ToString();
						float flySpeed = componentCreature.ComponentLocomotion.FlySpeed;
						float walkSpeed = componentCreature.ComponentLocomotion.WalkSpeed;
						float jumpSpeed = componentCreature.ComponentLocomotion.JumpSpeed;
						componentCreature.ComponentLocomotion.SwimSpeed.ToString();
						text5 += string.Format("；名称:{0}；\n血量:{1}%；质量:{2}；\n位置:({3})；碰撞箱:{4}；", displayName, num9, text6, bodyPoint.ToString(), text7);
						text5 += string.Format("\n飞行速度:{0}；行走速度:{1}；跳跃速度:{2}", MathUtils.Round(flySpeed * 10f) / 10f, MathUtils.Round(walkSpeed * 10f) / 10f, MathUtils.Round(jumpSpeed * 10f) / 10f);
					}
					else if (componentDamage != null)
					{
						float num10 = componentDamage.Hitpoints * 100f;
						float attackResilience = componentDamage.AttackResilience;
						string text8 = componentBody.Mass.ToString();
						string text9 = componentBody.BoxSize.ToString();
						text5 += string.Format("；血量:{0}%；\n攻击抗性:{1}；质量:{2}；\n位置:({3})；碰撞箱:{4}；", num10, attackResilience, text8, bodyPoint.ToString(), text9);
					}
					SetPointData(bodyPoint);
					m_recordPosition = bodyPoint;
					m_recordEntityName = text4;
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text5, m_color, false, false);
					return true;
				}
			}
			else
			{
				if (obj is MovingBlocksRaycastResult)
				{
					IMovingBlockSet movingBlockSet = ((MovingBlocksRaycastResult)obj).MovingBlockSet;
					string text10 = ((movingBlockSet.Tag != null) ? ("该运动方块标签名为:" + movingBlockSet.Tag.ToString()) : "该运动方块标签名不存在");
					text10 = text10 + "；所在位置:" + string.Format("({0})", movingBlockSet.Position.ToString());
					SetPointData(new Point3(movingBlockSet.Position));
					m_recordPosition = new Point3(movingBlockSet.Position);
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text10, m_color, false, false);
					return true;
				}
				Point3 bodyPoint2 = DataHandle.GetBodyPoint(componentMiner.ComponentPlayer.ComponentBody);
				SetPointData(bodyPoint2);
				m_recordPosition = bodyPoint2;
				m_recordEntityName = "player";
				m_recordEyes = playerEyesAngle;
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(string.Format("玩家名:player; 玩家坐标:({0})", bodyPoint2.ToString()) + text, m_color, false, false);
			}
			return false;
		}

		public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
		{
			if (componentPlayer.ComponentGui.ModalPanelWidget is CommandEditWidget)
			{
				CommandEditWidget commandEditWidget = (CommandEditWidget)componentPlayer.ComponentGui.ModalPanelWidget;
				commandEditWidget.ParentWidget.ClearChildren();
				CommandEditWidget.GuiWidgetControl(componentPlayer, true);
				componentPlayer.ComponentGui.ModalPanelWidget = null;
			}
			else
			{
				componentPlayer.ComponentGui.ModalPanelWidget = new CommandEditWidget(base.Project, componentPlayer, Point3.Zero, true);
			}
			return true;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(true);
			m_subsystemProjectiles = base.Project.FindSubsystem<SubsystemProjectiles>(true);
			m_subsystemCommand = base.Project.FindSubsystem<SubsystemCommand>(true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(true);
			m_texture = ContentManager.Get<Texture2D>("Textures/Gui/Sights");
			m_commandLine = valuesDictionary.GetValue<string>("CommandLine");
			m_pointIndex = 0;
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			valuesDictionary.SetValue("CommandLine", m_commandLine);
		}


		public void Update(float dt)
		{
			if (m_pointDataWidget == null && !string.IsNullOrEmpty(m_commandLine))
			{
				InitPointDataWidget();
			}
            if (m_subsystemCommand.m_componentPlayer != null)
            {
                // 此处不再需要设置 TerrainCollidable
                // 如果是想要判断方块是否可碰撞，使用 block.IsCollidable
            }

            if (m_pointDataWidget == null || m_subsystemCommand.m_componentPlayer == null)
			{
				return;
			}
			Vector2 actualSize = m_subsystemCommand.m_componentPlayer.GameWidget.ActualSize;
			m_subsystemCommand.m_componentPlayer.GameWidget.SetWidgetPosition(m_withdrawButton, new Vector2(actualSize.X - 180f, 23f));
			m_subsystemCommand.m_componentPlayer.GameWidget.SetWidgetPosition(m_recoveryButton, new Vector2(actualSize.X - 130f, 23f));
			bool flag = Terrain.ExtractContents(m_subsystemCommand.m_componentPlayer.ComponentMiner.ActiveBlockValue) == 334;
			bool flag2 = m_subsystemCommand.m_componentPlayer.ComponentGui.ModalPanelWidget is CommandEditWidget;
			m_pointDataWidget.IsVisible = QuickMode && flag && !flag2;
			m_withdrawButton.IsVisible = m_pointDataWidget.IsVisible;
			m_recoveryButton.IsVisible = m_pointDataWidget.IsVisible;
			if (!m_pointDataWidget.IsVisible)
			{
				return;
			}
			if (m_withdrawButton.IsClicked)
			{
				if (WithdrawBlockManager.WithdrawMode)
				{
					WithdrawBlockManager.CarryOut(base.Project.FindSubsystem<SubsystemCommandDef>());
				}
				else
				{
					m_subsystemCommand.m_componentPlayer.ComponentGui.DisplaySmallMessage("请开启撤回模式", m_color, false, false);
				}
			}
			if (m_recoveryButton.IsClicked)
			{
				if (WithdrawBlockManager.WithdrawMode)
				{
					WithdrawBlockManager.Recovery(base.Project.FindSubsystem<SubsystemCommandDef>());
				}
				else
				{
					m_subsystemCommand.m_componentPlayer.ComponentGui.DisplaySmallMessage("请开启撤回模式", m_color, false, false);
				}
			}
			foreach (string key in m_commandPoints.Keys)
			{
				if (m_commandPoints[key].Button.IsClicked)
				{
					m_commandPoints[key].Lock = !m_commandPoints[key].Lock;
					SetPointColor(key);
					string text = key.Replace("pos", "点") + (m_commandPoints[key].Lock ? "已锁定" : "已解锁");
					m_subsystemCommand.m_componentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, false, false);
				}
			}
		}

		public void InitPointDataWidget()
		{
			if (m_pointDataWidget == null)
			{
				m_withdrawButton = new BitmapButtonWidget();
				m_withdrawButton.IsVisible = false;
				m_withdrawButton.Size = new Vector2(40f, 40f);
				m_withdrawButton.NormalSubtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/Withdraw1"), Vector2.Zero, Vector2.One);
				m_withdrawButton.ClickedSubtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/Withdraw2"), Vector2.Zero, Vector2.One);
				m_subsystemCommand.m_componentPlayer.GameWidget.Children.Add(m_withdrawButton);
				m_recoveryButton = new BitmapButtonWidget();
				m_recoveryButton.IsVisible = false;
				m_recoveryButton.Size = new Vector2(40f, 40f);
				m_recoveryButton.NormalSubtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/Withdraw3"), Vector2.Zero, Vector2.One);
				m_recoveryButton.ClickedSubtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/Withdraw4"), Vector2.Zero, Vector2.One);
				m_subsystemCommand.m_componentPlayer.GameWidget.Children.Add(m_recoveryButton);
				m_pointDataWidget = new StackPanelWidget();
				m_pointDataWidget.Name = "PointDataWidget";
				m_pointDataWidget.Direction = LayoutDirection.Vertical;
				m_subsystemCommand.m_componentPlayer.GameWidget.SetWidgetPosition(m_pointDataWidget, new Vector2(100f, 20f));
				m_pointDataWidget.IsHitTestVisible = false;
				m_pointDataWidget.ClampToBounds = true;
				m_pointDataWidget.IsVisible = false;
				m_subsystemCommand.m_componentPlayer.GameWidget.Children.Add(m_pointDataWidget);
			}
			m_pointDataWidget.Children.Clear();
			m_commandPoints.Clear();
			string[] array = m_commandLine.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.StartsWith("pos"))
				{
					string[] array3 = text.Split(':');
					if (array3[0] == "pos")
					{
						array3[0] = "pos1";
					}
					CommandPointData commandPointData = new CommandPointData();
					commandPointData.Pos = array3[0];
					commandPointData.Value = array3[1];
					commandPointData.Lock = false;
					m_commandPoints[commandPointData.Pos] = commandPointData;
				}
			}
			if (m_commandPoints.Count <= 0)
			{
				return;
			}
			if (m_pointIndex >= m_commandPoints.Count)
			{
				m_pointIndex = m_commandPoints.Count - 1;
			}
			foreach (string key in m_commandPoints.Keys)
			{
				BevelledButtonWidget bevelledButtonWidget = new BevelledButtonWidget();
				m_commandPoints[key].Button = bevelledButtonWidget;
				bevelledButtonWidget.Name = key;
				bevelledButtonWidget.Text = key.Replace("pos", "点") + ":" + m_commandPoints[key].Value;
				bevelledButtonWidget.Size = new Vector2(200f, 50f);
				bevelledButtonWidget.CenterColor = new Color(64, 64, 64, 32);
				bevelledButtonWidget.BevelSize = 0.7f;
				SetPointColor(key);
				m_pointDataWidget.Children.Add(bevelledButtonWidget);
				CanvasWidget canvasWidget = new CanvasWidget();
				canvasWidget.Size = new Vector2(0f, 5f);
				m_pointDataWidget.Children.Add(canvasWidget);
			}
			SetPointSize();
		}

		public void SetPointData(Point3 point)
		{
			try
			{
				if (!QuickMode || m_commandPoints.Count == 0 || string.IsNullOrEmpty(m_commandLine))
				{
					return;
				}
				if (m_recordPosition.HasValue)
				{
					Point3 value = point;
					Point3? recordPosition = m_recordPosition;
					if (value == recordPosition)
					{
						Time.QueueTimeDelayedExecution(Time.RealTime + 0.10000000149011612, delegate
						{
							CommandData commandData = new CommandData(Point3.Zero, m_commandLine);
							commandData.TrySetValue();
							m_subsystemCommand.Submit(commandData.Name, commandData, false);
							m_subsystemCommand.m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format("已提交指令:{0}${1}", commandData.Name, commandData.Type), Color.Yellow, false, false);
						});
						return;
					}
				}
				if (m_pointIndex >= m_commandPoints.Count)
				{
					m_pointIndex = m_commandPoints.Count - 1;
				}
				foreach (string key in m_commandPoints.Keys)
				{
					m_commandPoints[key].Button.Color = (m_commandPoints[key].Lock ? Color.Red : Color.White);
					m_commandPoints[key].Button.BevelColor = m_commandPoints[key].Button.Color;
				}
				bool flag = true;
				foreach (string key2 in m_commandPoints.Keys)
				{
					if (!m_commandPoints[key2].Lock)
					{
						flag = false;
						break;
					}
				}
				if (!flag)
				{
					do
					{
						m_pointIndex = (m_pointIndex + 1) % m_commandPoints.Count;
					}
					while (m_commandPoints["pos" + (m_pointIndex + 1)].Lock);
					string text = "pos" + (m_pointIndex + 1);
					if (m_commandPoints.Count == 1)
					{
						m_commandLine = m_commandLine.Replace("pos:" + m_commandPoints[text].Value, "pos:" + point.ToString());
					}
					else
					{
						m_commandLine = m_commandLine.Replace(text + ":" + m_commandPoints[text].Value, text + ":" + point.ToString());
					}
					m_commandPoints[text].Value = point.ToString();
					m_commandPoints[text].Button.Text = text.Replace("pos", "点") + ":" + point.ToString();
					SetPointSize();
					SetPointColor(text);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("DebugPoint:" + ex.Message);
			}
		}

		public void SetPointColor(string p)
		{
			m_commandPoints[p].Button.Color = ((p == "pos" + (m_pointIndex + 1)) ? Color.Green : Color.White);
			if (m_commandPoints[p].Lock)
			{
				m_commandPoints[p].Button.Color = Color.Red;
			}
			m_commandPoints[p].Button.BevelColor = m_commandPoints[p].Button.Color;
		}

		public void SetPointSize()
		{
			int num = 0;
			foreach (string key in m_commandPoints.Keys)
			{
				if (num < m_commandPoints[key].Button.Text.Length)
				{
					num = m_commandPoints[key].Button.Text.Length;
				}
			}
			foreach (string key2 in m_commandPoints.Keys)
			{
				m_commandPoints[key2].Button.Size = new Vector2((float)num * 13.3f, 50f);
			}
		}
	}
}
