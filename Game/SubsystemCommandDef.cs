using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Command;
using Engine;
using Engine.Audio;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;
using XmlUtilities;

namespace Game
{
	public class SubsystemCommandDef : SubsystemCommand, IUpdateable, IDrawable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemBlockEntities m_subsystemBlockEntities;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemMovingBlocks m_subsystemMovingBlocks;

		public SubsystemFurnitureBlockBehavior m_subsystemFurnitureBlockBehavior;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemSky m_subsystemSky;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemTime m_subsystemTime;

		public static Dictionary<Color, int> ColorIndexCaches = new Dictionary<Color, int>();

		public static Dictionary<Point3, Pattern> PatternPoints = new Dictionary<Point3, Pattern>();

		public static Dictionary<Point3, PatternFont> PatternFonts = new Dictionary<Point3, PatternFont>();

		public static Dictionary<int, float[]> OriginFirmBlockList = new Dictionary<int, float[]>();

		public static Dictionary<string, string> CreatureTextures = new Dictionary<string, string>();

		public static Dictionary<string, string> CreatureModels = new Dictionary<string, string>();

		public static Dictionary<string, ScreenPattern> ScreenPatterns = new Dictionary<string, ScreenPattern>();

		public static Dictionary<string, string> Notes = new Dictionary<string, string>();

		public static CommandMusic m_commandMusic;

		public List<int> m_firmBlockList = new List<int>();

		public bool m_firmAllBlocks = false;

		public static List<ComponentCreature> DeadCreatureList = new List<ComponentCreature>();

		public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

		public TexturedBatch3D[] m_batches = new TexturedBatch3D[2];

		public bool m_enterDeathScreen = false;

		public float m_aimDurationTime = 0f;

		public float m_worldRunTime = 0f;

		public bool m_shapeshifter;

		public string m_playerBoxStage;

		public Color m_rainColor;

		public Color m_skyColor;

		public bool m_onCapture = false;

		public Point2? m_eatItem;

		public CanvasWidget m_screenPatternsWidget;

		public ContainerWidget m_screenLabelCanvasWidget;

		public float m_screenLabelCloseTime;

		public List<BaseBatch> m_aimingSightsBatches;

		public static CopyBlockManager CopyBlockManager;

		public static bool DisplayColorBlock = false;

		public float m_recordTime = 0f;

		public List<Point2> m_terrainChunks007 = new List<Point2>();

		public Dictionary<string, List<Point3>> m_waitingMoveSets = new Dictionary<string, List<Point3>>();

		public Dictionary<string, List<MovingEntityBlock>> m_movingEntityBlocks = new Dictionary<string, List<MovingEntityBlock>>();

		public static Dictionary<string, MovingCollision> m_movingCollisions = new Dictionary<string, MovingCollision>();

		public object m_interactResult;

		public bool m_interactTest;

		public float m_moveResetTime;

		public UpdateOrder UpdateOrder
		{
			get
			{
				return UpdateOrder.Default;
			}
		}

		public int[] DrawOrders
		{
			get
			{
				return new int[1] { 1000 };
			}
		}

		public void Update(float dt)
		{
			if (m_componentPlayer == null)
			{
				return;
			}
			if (m_enterDeathScreen)
			{
				SetDeathScreen();
			}
			if (m_componentPlayer.ComponentInput.PlayerInput.Aim.HasValue)
			{
				m_aimDurationTime += dt;
			}
			else
			{
				m_aimDurationTime = 0f;
			}
			if (m_worldRunTime == 0f)
			{
				Initialize();
			}
			m_worldRunTime += dt;
			if (m_moveResetTime > 0.5f)
			{
				m_moveResetTime = 0f;
				m_movingCollisions.Clear();
			}
			m_moveResetTime += dt;
			if (m_screenLabelCloseTime > 0f)
			{
				m_screenLabelCloseTime -= dt;
				if (m_screenLabelCloseTime < 0.1f)
				{
					m_screenLabelCanvasWidget.IsVisible = false;
					m_screenLabelCloseTime = 0f;
				}
			}
			foreach (ScreenPattern value2 in ScreenPatterns.Values)
			{
				value2.OutTime = MathUtils.Max(0f, value2.OutTime - dt);
				if (value2.Widget is BitmapButtonWidget && ((ButtonWidget)value2.Widget).IsClicked)
				{
					value2.OutTime = 0.1f;
				}
			}
			if (m_interactTest && m_componentPlayer.ComponentInput.PlayerInput.Interact.HasValue)
			{
				Ray3 value = m_componentPlayer.ComponentInput.PlayerInput.Interact.Value;
				m_interactResult = DataHandle.Raycast(value, m_componentPlayer.ComponentMiner);
			}
			else
			{
				m_interactResult = null;
			}
			m_recordTime += dt;
			if (RecordManager.Recording && m_recordTime >= RecordManager.FrameTime)
			{
				m_recordTime = 0f;
				RecordManager.AddRecordPlayerStats(m_componentPlayer, (float)m_subsystemTime.GameTime);
			}
			if (RecordManager.Replaying && m_recordTime >= RecordManager.FrameTime)
			{
				m_recordTime = 0f;
				RecordManager.Replay(m_componentPlayer, dt);
			}
		}

		public void Draw(Camera camera, int drawOrder)
		{
			DrawPatternPoints(camera, drawOrder);
		}

		public void Initialize()
		{
			LoadNotes();
			WithdrawBlockManager.Clear();
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(true);
			m_subsystemBlockEntities = base.Project.FindSubsystem<SubsystemBlockEntities>(true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(true);
			m_subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>(true);
			m_subsystemFurnitureBlockBehavior = base.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(true);
			m_aimDurationTime = 0f;
			m_worldRunTime = 0f;
			m_screenLabelCloseTime = 0f;
			m_moveResetTime = 0f;
			m_shapeshifter = valuesDictionary.GetValue<bool>("Shapeshifter");
			m_playerBoxStage = valuesDictionary.GetValue<string>("PlayerBoxStage");
			m_commandMusic = new CommandMusic(valuesDictionary.GetValue<string>("CommandMusic"), null);
			string value = valuesDictionary.GetValue<string>("FirmBlocks");
			if (!string.IsNullOrEmpty(value))
			{
				m_firmBlockList.Clear();
				string[] array = value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string s in array)
				{
					m_firmBlockList.Add(int.Parse(s));
				}
			}
			if (SubsystemCommandExt.LoadAction == null)
			{
				SubsystemCommandExt.LoadAction = (Action)Delegate.Combine(SubsystemCommandExt.LoadAction, (Action)delegate
				{
					foreach (int firmBlock in m_firmBlockList)
					{
						SetFirmBlocks(firmBlock, true, null);
					}
				});
			}
			m_interactTest = false;
			SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = base.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
			subsystemCommandBlockBehavior.OnCommandBlockGenerated = (Action<CommandData>)Delegate.Combine(subsystemCommandBlockBehavior.OnCommandBlockGenerated, (Action<CommandData>)delegate(CommandData commandData)
			{
				if (commandData.Name == "clickinteract")
				{
					m_interactTest = true;
				}
			});
			m_rainColor = valuesDictionary.GetValue<Color>("RainColor");
			m_skyColor = valuesDictionary.GetValue<Color>("SkyColor");
			DeadCreatureList.Clear();
			ColorIndexCaches.Clear();
			ScreenPatterns.Clear();
			LoadChunks007(valuesDictionary);
			LoadCreatureTextureOrModels(valuesDictionary);
			LoadPatternPoints(valuesDictionary);
			LoadWaitMoveSet(valuesDictionary);
			LoadMoveEntityBlocks(valuesDictionary);
			MoveBlockCollidedAction();
			Function();
			Condition();
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			base.Save(valuesDictionary);
			valuesDictionary.SetValue("Shapeshifter", m_shapeshifter);
			valuesDictionary.SetValue("PlayerBoxStage", m_playerBoxStage);
			valuesDictionary.SetValue("CommandMusic", m_commandMusic.Name);
			string text = string.Empty;
			foreach (int firmBlock in m_firmBlockList)
			{
				text = text + firmBlock + ",";
			}
			valuesDictionary.SetValue("FirmBlocks", text);
			valuesDictionary.SetValue("RainColor", m_rainColor);
			valuesDictionary.SetValue("SkyColor", m_skyColor);
			SaveChunks007(valuesDictionary);
			SaveCreatureTextureOrModels(valuesDictionary);
			SavePatternPoints(valuesDictionary);
			SaveWaitMoveSet(valuesDictionary);
			SaveMoveEntityBlocks(valuesDictionary);
		}

		public override void OnEntityAdded(Entity entity)
		{
			base.OnEntityAdded(entity);
			if (m_shapeshifter)
			{
				SetShapeshifter(entity);
			}
			if (CreatureTextures.Count > 0 || CreatureModels.Count > 0)
			{
				SetCreatureTextureOrModels(entity);
			}
			if (entity.FindComponent<ComponentPlayer>() != null)
			{
				SetPlayerBoxStage(m_playerBoxStage);
				InitScreenLabelCanvas();
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			SaveNotes();
			if (m_commandMusic != null && m_commandMusic.Sound != null)
			{
				m_commandMusic.Sound.Dispose();
			}
		}

		public void Function()
		{
			AddFunction("book", delegate(CommandData commandData)
			{
				object value;
				if (commandData.DIYPara.TryGetValue("BufferTime", out value) && m_subsystemTime.GameTime - (double)value < 3.0)
				{
					ShowSubmitTips("你已经看过了，歇2秒再看吧");
					return SubmitResult.Fail;
				}
				commandData.DIYPara["BufferTime"] = m_subsystemTime.GameTime;
				m_componentPlayer.ComponentGui.ModalPanelWidget = new ManualTopicWidget(m_componentPlayer, 0f);
				CommandEditWidget.GuiWidgetControl(m_componentPlayer, true);
				return SubmitResult.Success;
			});
			AddFunction("message", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text = (string)commandData.GetValue("text");
					Color color = (Color)commandData.GetValue("color");
					bool playNotificationSound = (bool)commandData.GetValue("con");
					m_componentPlayer.ComponentGui.DisplaySmallMessage(text, color, true, playNotificationSound);
				}
				else if (commandData.Type == "float")
				{
					string largeText = (string)commandData.GetValue("text1");
					string smallText = (string)commandData.GetValue("text2");
					m_componentPlayer.ComponentGui.DisplayLargeMessage(largeText, smallText, 3f, 0f);
				}
				return SubmitResult.Success;
			});
			AddFunction("place", delegate(CommandData commandData)
			{
				WithdrawBlockManager wbManager = null;
				if (WithdrawBlockManager.WithdrawMode)
				{
					wbManager = new WithdrawBlockManager();
				}
				if (commandData.Type == "default")
				{
					Point3 onePoint = GetOnePoint("pos", commandData);
					int value2 = (int)commandData.GetValue("id");
					ChangeBlockValue(wbManager, onePoint.X, onePoint.Y, onePoint.Z, value2, false);
					PlaceReprocess(wbManager, commandData, false, onePoint, onePoint);
				}
				else if (commandData.Type == "line")
				{
					Point3[] twoPoint = GetTwoPoint("pos1", "pos2", commandData);
					int value3 = (int)commandData.GetValue("id");
					CubeArea cubeArea = new CubeArea(twoPoint[0], twoPoint[1]);
					int num = MathUtils.Max(MathUtils.Max(cubeArea.LengthX, cubeArea.LengthY), cubeArea.LengthZ);
					for (int i = 0; i <= num; i++)
					{
						int x = twoPoint[0].X + (int)MathUtils.Round((float)i / (float)num * (float)(twoPoint[1].X - twoPoint[0].X));
						int y = twoPoint[0].Y + (int)MathUtils.Round((float)i / (float)num * (float)(twoPoint[1].Y - twoPoint[0].Y));
						int z = twoPoint[0].Z + (int)MathUtils.Round((float)i / (float)num * (float)(twoPoint[1].Z - twoPoint[0].Z));
						ChangeBlockValue(wbManager, x, y, z, value3, false);
					}
					PlaceReprocess(wbManager, commandData, false, cubeArea.MinPoint, cubeArea.MaxPoint);
				}
				else if (commandData.Type == "frame")
				{
					Point3[] twoPoint2 = GetTwoPoint("pos1", "pos2", commandData);
					int value4 = (int)commandData.GetValue("id");
					CubeArea cubeArea2 = new CubeArea(twoPoint2[0], twoPoint2[1]);
					for (int j = 0; j < cubeArea2.LengthX; j++)
					{
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X + j, cubeArea2.MinPoint.Y, cubeArea2.MinPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X + j, cubeArea2.MaxPoint.Y, cubeArea2.MinPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X + j, cubeArea2.MinPoint.Y, cubeArea2.MaxPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X + j, cubeArea2.MaxPoint.Y, cubeArea2.MaxPoint.Z, value4, false);
					}
					for (int k = 0; k < cubeArea2.LengthY; k++)
					{
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X, cubeArea2.MinPoint.Y + k, cubeArea2.MinPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MaxPoint.X, cubeArea2.MinPoint.Y + k, cubeArea2.MinPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X, cubeArea2.MinPoint.Y + k, cubeArea2.MaxPoint.Z, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MaxPoint.X, cubeArea2.MinPoint.Y + k, cubeArea2.MaxPoint.Z, value4, false);
					}
					for (int l = 0; l < cubeArea2.LengthZ; l++)
					{
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X, cubeArea2.MinPoint.Y, cubeArea2.MinPoint.Z + l, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MaxPoint.X, cubeArea2.MinPoint.Y, cubeArea2.MinPoint.Z + l, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MinPoint.X, cubeArea2.MaxPoint.Y, cubeArea2.MinPoint.Z + l, value4, false);
						ChangeBlockValue(wbManager, cubeArea2.MaxPoint.X, cubeArea2.MaxPoint.Y, cubeArea2.MinPoint.Z + l, value4, false);
					}
					PlaceReprocess(wbManager, commandData, false, cubeArea2.MinPoint, cubeArea2.MaxPoint);
				}
				else if (commandData.Type == "triangle")
				{
					Point3 onePoint2 = GetOnePoint("pos1", commandData);
					Point3 onePoint3 = GetOnePoint("pos2", commandData);
					Point3 onePoint4 = GetOnePoint("pos3", commandData);
					int value5 = (int)commandData.GetValue("id");
					Point3[] array = new Point3[3];
					for (int m = 0; m < 3; m++)
					{
						switch (m)
						{
						case 0:
							array = new Point3[3] { onePoint2, onePoint3, onePoint4 };
							break;
						case 1:
							array = new Point3[3] { onePoint2, onePoint4, onePoint3 };
							break;
						case 2:
							array = new Point3[3] { onePoint3, onePoint4, onePoint2 };
							break;
						}
						List<Point3> list = new List<Point3>();
						CubeArea cubeArea3 = new CubeArea(array[0], array[1]);
						int num2 = MathUtils.Max(MathUtils.Max(cubeArea3.LengthX, cubeArea3.LengthY), cubeArea3.LengthZ);
						for (int n = 0; n <= num2; n++)
						{
							int x2 = array[0].X + (int)MathUtils.Round((float)n / (float)num2 * (float)(array[1].X - array[0].X));
							int y2 = array[0].Y + (int)MathUtils.Round((float)n / (float)num2 * (float)(array[1].Y - array[0].Y));
							int z2 = array[0].Z + (int)MathUtils.Round((float)n / (float)num2 * (float)(array[1].Z - array[0].Z));
							list.Add(new Point3(x2, y2, z2));
							ChangeBlockValue(wbManager, x2, y2, z2, value5);
						}
						foreach (Point3 item2 in list)
						{
							CubeArea cubeArea4 = new CubeArea(array[2], item2);
							int num3 = MathUtils.Max(MathUtils.Max(cubeArea4.LengthX, cubeArea4.LengthY), cubeArea4.LengthZ);
							for (int num4 = 0; num4 <= num3; num4++)
							{
								int x3 = array[2].X + (int)MathUtils.Round((float)num4 / (float)num3 * (float)(item2.X - array[2].X));
								int y3 = array[2].Y + (int)MathUtils.Round((float)num4 / (float)num3 * (float)(item2.Y - array[2].Y));
								int z3 = array[2].Z + (int)MathUtils.Round((float)num4 / (float)num3 * (float)(item2.Z - array[2].Z));
								ChangeBlockValue(wbManager, x3, y3, z3, value5);
							}
						}
					}
					CubeArea cubeArea5 = new CubeArea(onePoint2, onePoint3);
					int x4 = MathUtils.Max(onePoint4.X, cubeArea5.MaxPoint.X);
					int y4 = MathUtils.Max(onePoint4.Y, cubeArea5.MaxPoint.Y);
					int z4 = MathUtils.Max(onePoint4.Z, cubeArea5.MaxPoint.Z);
					int x5 = MathUtils.Min(onePoint4.X, cubeArea5.MinPoint.X);
					int y5 = MathUtils.Min(onePoint4.Y, cubeArea5.MinPoint.Y);
					int z5 = MathUtils.Min(onePoint4.Z, cubeArea5.MinPoint.Z);
					PlaceReprocess(wbManager, commandData, true, new Point3(x5, y5, z5), new Point3(x4, y4, z4));
				}
				else if (commandData.Type == "cube")
				{
					Point3[] twoPoint3 = GetTwoPoint("pos1", "pos2", commandData);
					int id = (int)commandData.GetValue("id");
					bool con = (bool)commandData.GetValue("con");
					CubeArea cube = new CubeArea(twoPoint3[0], twoPoint3[1]);
					CubeArea cube2 = new CubeArea(cube.MinPoint + Point3.One, cube.MaxPoint - Point3.One);
					cube.Ergodic(delegate
					{
						if (con)
						{
							if (!cube2.Exist(cube.Current))
							{
								ChangeBlockValue(wbManager, cube.Current.X, cube.Current.Y, cube.Current.Z, id);
							}
						}
						else
						{
							ChangeBlockValue(wbManager, cube.Current.X, cube.Current.Y, cube.Current.Z, id);
						}
						return false;
					});
					PlaceReprocess(wbManager, commandData, true, cube.MinPoint, cube.MaxPoint);
				}
				else if (commandData.Type == "sphere")
				{
					Point3 onePoint5 = GetOnePoint("pos", commandData);
					int id2 = (int)commandData.GetValue("id");
					int num5 = (int)commandData.GetValue("r");
					bool con2 = (bool)commandData.GetValue("con");
					SphereArea sphere = new SphereArea(num5, onePoint5);
					SphereArea sphere2 = new SphereArea(num5 - 1, onePoint5);
					sphere.Ergodic(delegate
					{
						if (con2)
						{
							if (!sphere2.Exist(sphere.Current))
							{
								ChangeBlockValue(wbManager, sphere.Current.X, sphere.Current.Y, sphere.Current.Z, id2);
							}
						}
						else
						{
							ChangeBlockValue(wbManager, sphere.Current.X, sphere.Current.Y, sphere.Current.Z, id2);
						}
					});
					PlaceReprocess(wbManager, commandData, true, sphere.MinPoint, sphere.MaxPoint);
				}
				else if (commandData.Type == "column")
				{
					Point3 onePoint6 = GetOnePoint("pos", commandData);
					int id3 = (int)commandData.GetValue("id");
					int num6 = (int)commandData.GetValue("r");
					int num7 = (int)commandData.GetValue("h");
					string text2 = (string)commandData.GetValue("opt");
					bool con3 = (bool)commandData.GetValue("con");
					CoordDirection coord2 = CoordDirection.PY;
					Point3 point = Point3.Zero;
					switch (text2)
					{
					case "+x":
						coord2 = CoordDirection.PX;
						point = new Point3(1, 0, 0);
						break;
					case "+y":
						coord2 = CoordDirection.PY;
						point = new Point3(0, 1, 0);
						break;
					case "+z":
						coord2 = CoordDirection.PZ;
						point = new Point3(0, 0, 1);
						break;
					case "-x":
						coord2 = CoordDirection.NX;
						point = new Point3(-1, 0, 0);
						break;
					case "-y":
						coord2 = CoordDirection.NY;
						point = new Point3(0, -1, 0);
						break;
					case "-z":
						coord2 = CoordDirection.NZ;
						point = new Point3(0, 0, -1);
						break;
					}
					ColumnArea column = new ColumnArea(num6, num7, onePoint6, coord2);
					ColumnArea column2 = new ColumnArea(num6 - 1, num7 - 2, onePoint6 + point, coord2);
					column.Ergodic(delegate
					{
						if (con3)
						{
							if (!column2.Exist(column.Current))
							{
								ChangeBlockValue(wbManager, column.Current.X, column.Current.Y, column.Current.Z, id3);
							}
						}
						else
						{
							ChangeBlockValue(wbManager, column.Current.X, column.Current.Y, column.Current.Z, id3);
						}
					});
					PlaceReprocess(wbManager, commandData, true, column.MinPoint, column.MaxPoint);
				}
				else if (commandData.Type == "cone")
				{
					Point3 onePoint7 = GetOnePoint("pos", commandData);
					int id4 = (int)commandData.GetValue("id");
					int num8 = (int)commandData.GetValue("r");
					int num9 = (int)commandData.GetValue("h");
					string text3 = (string)commandData.GetValue("opt");
					bool con4 = (bool)commandData.GetValue("con");
					CoordDirection coord3 = CoordDirection.PY;
					Point3 point2 = Point3.Zero;
					switch (text3)
					{
					case "+x":
						coord3 = CoordDirection.PX;
						point2 = new Point3(1, 0, 0);
						break;
					case "+y":
						coord3 = CoordDirection.PY;
						point2 = new Point3(0, 1, 0);
						break;
					case "+z":
						coord3 = CoordDirection.PZ;
						point2 = new Point3(0, 0, 1);
						break;
					case "-x":
						coord3 = CoordDirection.NX;
						point2 = new Point3(-1, 0, 0);
						break;
					case "-y":
						coord3 = CoordDirection.NY;
						point2 = new Point3(0, -1, 0);
						break;
					case "-z":
						coord3 = CoordDirection.NZ;
						point2 = new Point3(0, 0, -1);
						break;
					}
					ConeArea cone = new ConeArea(num8, num9, onePoint7, coord3);
					ConeArea cone2 = new ConeArea((int)(0.8f * (float)num8), num9 - 2, onePoint7 + point2, coord3);
					cone.Ergodic(delegate
					{
						if (con4)
						{
							if (!cone2.Exist(cone.Current))
							{
								ChangeBlockValue(wbManager, cone.Current.X, cone.Current.Y, cone.Current.Z, id4);
							}
						}
						else
						{
							ChangeBlockValue(wbManager, cone.Current.X, cone.Current.Y, cone.Current.Z, id4);
						}
					});
					PlaceReprocess(wbManager, commandData, true, cone.MinPoint, cone.MaxPoint);
				}
				else if (commandData.Type == "function")
				{
					Point3 pos = GetOnePoint("pos", commandData);
					int id5 = (int)commandData.GetValue("id");
					int v = (int)commandData.GetValue("v");
					string func1 = (string)commandData.GetValue("func1");
					string func2 = (string)commandData.GetValue("func2");
					string str = (string)commandData.GetValue("limx");
					string str2 = (string)commandData.GetValue("limy");
					string str3 = (string)commandData.GetValue("limz");
					bool funcPass1 = func1 == "null";
					bool funcPass2 = func2 == "null";
					string[] funcArray1 = new string[3];
					string[] funcArray2 = new string[3];
					if (!funcPass1)
					{
						func1 = func1.Replace(" ", "");
						if (func1.Contains(">"))
						{
							funcArray1[0] = func1.Split('>')[0];
							funcArray1[1] = func1.Split('>')[1];
							funcArray1[2] = ">";
						}
						else if (func1.Contains("<"))
						{
							funcArray1[0] = func1.Split('<')[0];
							funcArray1[1] = func1.Split('<')[1];
							funcArray1[2] = "<";
						}
						else
						{
							if (!func1.Contains("="))
							{
								ShowSubmitTips("表达式有且只能包括'>','<','='其中一个符号");
								return SubmitResult.Fail;
							}
							funcArray1[0] = func1.Split('=')[0];
							funcArray1[1] = func1.Split('=')[1];
							funcArray1[2] = "=";
						}
					}
					if (!funcPass2)
					{
						func2 = func2.Replace(" ", "");
						if (func2.Contains(">"))
						{
							funcArray2[0] = func2.Split('>')[0];
							funcArray2[1] = func2.Split('>')[1];
							funcArray2[2] = ">";
						}
						else if (func2.Contains("<"))
						{
							funcArray2[0] = func2.Split('<')[0];
							funcArray2[1] = func2.Split('<')[1];
							funcArray2[2] = "<";
						}
						else
						{
							if (!func2.Contains("="))
							{
								ShowSubmitTips("表达式有且只能包括'>','<','='其中一个符号");
								return SubmitResult.Fail;
							}
							funcArray2[0] = func2.Split('=')[0];
							funcArray2[1] = func2.Split('=')[1];
							funcArray2[2] = "=";
						}
					}
					Point2 point2Value = DataHandle.GetPoint2Value(str);
					Point2 point2Value2 = DataHandle.GetPoint2Value(str2);
					Point2 point2Value3 = DataHandle.GetPoint2Value(str3);
					CubeArea cube3 = new CubeArea(new Point3(point2Value.X, point2Value2.X, point2Value3.X), new Point3(point2Value.Y, point2Value2.Y, point2Value3.Y));
					Task.Run(delegate
					{
						try
						{
							int count = 0;
							int eh = (int)((float)(cube3.LengthX * cube3.LengthY * cube3.LengthZ) / 10f) + 1;
							ShowSubmitTips(string.Format("{0}&{1}\n正在计算生成中,请等待片刻！", func1, func2));
							cube3.Ergodic(delegate
							{
								count++;
								if (count % eh == 0)
								{
									ShowSubmitTips(string.Format("{0}&{1}\n方块生成进度:{2}%", func1, func2, (int)((float)(count / eh) * 10f)));
								}
								bool flag = funcPass1;
								bool flag2 = funcPass2;
								if (!funcPass1)
								{
									ExpressionCalculator expressionCalculator = new ExpressionCalculator(funcArray1[0]);
									ExpressionCalculator expressionCalculator2 = new ExpressionCalculator(funcArray1[1]);
									int num10 = expressionCalculator.Calculate(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
									int num11 = expressionCalculator2.Calculate(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
									switch (funcArray1[2])
									{
									case "<":
										flag = num10 < num11;
										break;
									case ">":
										flag = num10 > num11;
										break;
									case "=":
										flag = MathUtils.Abs(num10 - num11) <= v;
										break;
									}
									if (num10 == int.MinValue || num11 == int.MinValue)
									{
										flag = false;
									}
								}
								if (!funcPass2)
								{
									ExpressionCalculator expressionCalculator3 = new ExpressionCalculator(funcArray2[0]);
									ExpressionCalculator expressionCalculator4 = new ExpressionCalculator(funcArray2[1]);
									int num12 = expressionCalculator3.Calculate(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
									int num13 = expressionCalculator4.Calculate(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
									switch (funcArray2[2])
									{
									case "<":
										flag2 = num12 < num13;
										break;
									case ">":
										flag2 = num12 > num13;
										break;
									case "=":
										flag2 = MathUtils.Abs(num12 - num13) <= v;
										break;
									}
									if (num12 == int.MinValue || num13 == int.MinValue)
									{
										flag2 = false;
									}
								}
								if (flag && flag2)
								{
									Point3 point3 = pos + cube3.Current - cube3.MinPoint;
									ChangeBlockValue(wbManager, point3.X, point3.Y, point3.Z, id5);
								}
								return false;
							});
							PlaceReprocess(wbManager, commandData, true, pos, pos + cube3.MaxPoint - cube3.MinPoint);
							ShowSubmitTips(string.Format("{0}&{1}\n生成方块完成！", func1, func2));
						}
						catch (Exception ex)
						{
							Log.Warning(string.Format("{0}&{1}:{2}", func1, func2, ex.Message));
							ShowSubmitTips(string.Format("{0}&{1}\n生成方块失败！请检查表达式以及定义域", func1, func2));
						}
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("dig", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint8 = GetOnePoint("pos", commandData);
					m_subsystemTerrain.DestroyCell(1, onePoint8.X, onePoint8.Y, onePoint8.Z, 0, false, false, (ComponentMiner)null);
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint4 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cube4 = new CubeArea(twoPoint4[0], twoPoint4[1]);
					cube4.Ergodic(delegate
					{
						m_subsystemTerrain.DestroyCell(1, cube4.Current.X, cube4.Current.Y, cube4.Current.Z, 0, false, false, (ComponentMiner)null);
						return false;
					});
				}
				else if (commandData.Type == "limit")
				{
					Point3[] twoPoint5 = GetTwoPoint("pos1", "pos2", commandData);
					int id6 = (int)commandData.GetValue("id");
					CubeArea cube5 = new CubeArea(twoPoint5[0], twoPoint5[1]);
					cube5.Ergodic(delegate
					{
						int limitValue = GetLimitValue(cube5.Current.X, cube5.Current.Y, cube5.Current.Z);
						if (limitValue == id6)
						{
							m_subsystemTerrain.DestroyCell(1, cube5.Current.X, cube5.Current.Y, cube5.Current.Z, 0, false, false, (ComponentMiner)null);
						}
						return false;
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("replace", delegate(CommandData commandData)
			{
				WithdrawBlockManager wbManager2 = null;
				if (WithdrawBlockManager.WithdrawMode)
				{
					wbManager2 = new WithdrawBlockManager();
				}
				if (commandData.Type == "default")
				{
					Point3[] twoPoint6 = GetTwoPoint("pos1", "pos2", commandData);
					int id7 = (int)commandData.GetValue("id1");
					int id8 = (int)commandData.GetValue("id2");
					CubeArea cube6 = new CubeArea(twoPoint6[0], twoPoint6[1]);
					cube6.Ergodic(delegate
					{
						int limitValue2 = GetLimitValue(cube6.Current.X, cube6.Current.Y, cube6.Current.Z);
						if (limitValue2 == id7)
						{
							ChangeBlockValue(wbManager2, cube6.Current.X, cube6.Current.Y, cube6.Current.Z, id8);
						}
						return false;
					});
					PlaceReprocess(wbManager2, commandData, true, cube6.MinPoint, cube6.MaxPoint);
				}
				else if (commandData.Type == "fuzzycolor")
				{
					Point3[] twoPoint7 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cube7 = new CubeArea(twoPoint7[0], twoPoint7[1]);
					cube7.Ergodic(delegate
					{
						int limitValue3 = GetLimitValue(cube7.Current.X, cube7.Current.Y, cube7.Current.Z);
						if (Terrain.ExtractContents(limitValue3) == 72)
						{
							Color commandColor = Command.ClayBlock.GetCommandColor(Terrain.ExtractData(limitValue3));
							if (!Command.ClayBlock.IsDefaultColor(commandColor))
							{
								int value6 = DataHandle.GetColorIndex(commandColor) * 32768 + 16456;
								ChangeBlockValue(wbManager2, cube7.Current.X, cube7.Current.Y, cube7.Current.Z, value6);
							}
						}
						return false;
					});
					PlaceReprocess(wbManager2, commandData, true, cube7.MinPoint, cube7.MaxPoint);
				}
				else if (commandData.Type == "padding")
				{
					Point3[] twoPoint8 = GetTwoPoint("pos1", "pos2", commandData);
					int value7 = (int)commandData.GetValue("id");
					string text4 = (string)commandData.GetValue("opt");
					CubeArea cubeArea6 = new CubeArea(twoPoint8[0], twoPoint8[1]);
					if (text4 == "+x" || text4 == "-x")
					{
						for (int num14 = 0; num14 < cubeArea6.LengthY; num14++)
						{
							for (int num15 = 0; num15 < cubeArea6.LengthZ; num15++)
							{
								if (text4 == "+x")
								{
									for (int num16 = 0; num16 < cubeArea6.LengthX; num16++)
									{
										Point3 point4 = new Point3(num16 + cubeArea6.MinPoint.X, num14 + cubeArea6.MinPoint.Y, num15 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point4.X, point4.Y, point4.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point4.X, point4.Y, point4.Z, value7);
									}
								}
								else
								{
									for (int num17 = cubeArea6.LengthX - 1; num17 > 0; num17--)
									{
										Point3 point5 = new Point3(num17 + cubeArea6.MinPoint.X, num14 + cubeArea6.MinPoint.Y, num15 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point5.X, point5.Y, point5.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point5.X, point5.Y, point5.Z, value7);
									}
								}
							}
						}
					}
					else if (text4 == "+y" || text4 == "-y")
					{
						for (int num18 = 0; num18 < cubeArea6.LengthX; num18++)
						{
							for (int num19 = 0; num19 < cubeArea6.LengthZ; num19++)
							{
								if (text4 == "+y")
								{
									for (int num20 = 0; num20 < cubeArea6.LengthY; num20++)
									{
										Point3 point6 = new Point3(num18 + cubeArea6.MinPoint.X, num20 + cubeArea6.MinPoint.Y, num19 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point6.X, point6.Y, point6.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point6.X, point6.Y, point6.Z, value7);
									}
								}
								else
								{
									for (int num21 = cubeArea6.LengthY - 1; num21 > 0; num21--)
									{
										Point3 point7 = new Point3(num18 + cubeArea6.MinPoint.X, num21 + cubeArea6.MinPoint.Y, num19 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point7.X, point7.Y, point7.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point7.X, point7.Y, point7.Z, value7);
									}
								}
							}
						}
					}
					else if (text4 == "+z" || text4 == "-z")
					{
						for (int num22 = 0; num22 < cubeArea6.LengthX; num22++)
						{
							for (int num23 = 0; num23 < cubeArea6.LengthY; num23++)
							{
								if (text4 == "+z")
								{
									for (int num24 = 0; num24 < cubeArea6.LengthZ; num24++)
									{
										Point3 point8 = new Point3(num22 + cubeArea6.MinPoint.X, num23 + cubeArea6.MinPoint.Y, num24 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point8.X, point8.Y, point8.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point8.X, point8.Y, point8.Z, value7);
									}
								}
								else
								{
									for (int num25 = cubeArea6.LengthZ - 1; num25 > 0; num25--)
									{
										Point3 point9 = new Point3(num22 + cubeArea6.MinPoint.X, num23 + cubeArea6.MinPoint.Y, num25 + cubeArea6.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point9.X, point9.Y, point9.Z) != 0)
										{
											break;
										}
										ChangeBlockValue(wbManager2, point9.X, point9.Y, point9.Z, value7);
									}
								}
							}
						}
					}
					PlaceReprocess(wbManager2, commandData, true, cubeArea6.MinPoint, cubeArea6.MaxPoint);
				}
				else if (commandData.Type == "overlay")
				{
					Point3[] twoPoint9 = GetTwoPoint("pos1", "pos2", commandData);
					int value8 = (int)commandData.GetValue("id");
					string text5 = (string)commandData.GetValue("opt");
					bool flag3 = (bool)commandData.GetValue("con1");
					bool flag4 = (bool)commandData.GetValue("con2");
					CubeArea cubeArea7 = new CubeArea(twoPoint9[0], twoPoint9[1]);
					bool flag5 = true;
					if (text5 == "+x" || text5 == "-x")
					{
						for (int num26 = 0; num26 < cubeArea7.LengthY; num26++)
						{
							for (int num27 = 0; num27 < cubeArea7.LengthZ; num27++)
							{
								flag5 = true;
								if (text5 == "+x")
								{
									for (int num28 = 0; num28 < cubeArea7.LengthX; num28++)
									{
										Point3 point10 = new Point3(num28 + cubeArea7.MinPoint.X, num26 + cubeArea7.MinPoint.Y, num27 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point10.X, point10.Y, point10.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, flag4 ? point10.X : (point10.X - 1), point10.Y, point10.Z, value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
								else
								{
									for (int num29 = cubeArea7.LengthX - 1; num29 > 0; num29--)
									{
										Point3 point11 = new Point3(num29 + cubeArea7.MinPoint.X, num26 + cubeArea7.MinPoint.Y, num27 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point11.X, point11.Y, point11.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, flag4 ? point11.X : (point11.X + 1), point11.Y, point11.Z, value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
							}
						}
					}
					else if (text5 == "+y" || text5 == "-y")
					{
						for (int num30 = 0; num30 < cubeArea7.LengthX; num30++)
						{
							for (int num31 = 0; num31 < cubeArea7.LengthZ; num31++)
							{
								flag5 = true;
								if (text5 == "+y")
								{
									for (int num32 = 0; num32 < cubeArea7.LengthY; num32++)
									{
										Point3 point12 = new Point3(num30 + cubeArea7.MinPoint.X, num32 + cubeArea7.MinPoint.Y, num31 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point12.X, point12.Y, point12.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, point12.X, flag4 ? point12.Y : (point12.Y - 1), point12.Z, value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
								else
								{
									for (int num33 = cubeArea7.LengthY - 1; num33 > 0; num33--)
									{
										Point3 point13 = new Point3(num30 + cubeArea7.MinPoint.X, num33 + cubeArea7.MinPoint.Y, num31 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point13.X, point13.Y, point13.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, point13.X, flag4 ? point13.Y : (point13.Y + 1), point13.Z, value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
							}
						}
					}
					else if (text5 == "+z" || text5 == "-z")
					{
						for (int num34 = 0; num34 < cubeArea7.LengthX; num34++)
						{
							for (int num35 = 0; num35 < cubeArea7.LengthY; num35++)
							{
								flag5 = true;
								if (text5 == "+z")
								{
									for (int num36 = 0; num36 < cubeArea7.LengthZ; num36++)
									{
										Point3 point14 = new Point3(num34 + cubeArea7.MinPoint.X, num35 + cubeArea7.MinPoint.Y, num36 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point14.X, point14.Y, point14.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, point14.X, point14.Y, flag4 ? point14.Z : (point14.Z - 1), value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
								else
								{
									for (int num37 = cubeArea7.LengthZ - 1; num37 > 0; num37--)
									{
										Point3 point15 = new Point3(num34 + cubeArea7.MinPoint.X, num35 + cubeArea7.MinPoint.Y, num37 + cubeArea7.MinPoint.Z);
										if (m_subsystemTerrain.Terrain.GetCellContents(point15.X, point15.Y, point15.Z) != 0)
										{
											if (flag5)
											{
												ChangeBlockValue(wbManager2, point15.X, point15.Y, flag4 ? point15.Z : (point15.Z + 1), value8);
											}
											if (!flag3)
											{
												break;
											}
											flag5 = false;
										}
										else
										{
											flag5 = true;
										}
									}
								}
							}
						}
					}
					PlaceReprocess(wbManager2, commandData, true, cubeArea7.MinPoint, cubeArea7.MaxPoint);
				}
				return SubmitResult.Success;
			});
			AddFunction("addnpc", delegate(CommandData commandData)
			{
				Point3 onePoint9 = GetOnePoint("pos", commandData);
				string obj = (string)commandData.GetValue("obj");
				string entityName = EntityInfoManager.GetEntityName(obj);
				if (entityName == "MalePlayer")
				{
					ShowSubmitTips("不能添加玩家");
					return SubmitResult.Fail;
				}
				Entity entity = DatabaseManager.CreateEntity(base.Project, entityName, true);
				ComponentFrame componentFrame = entity.FindComponent<ComponentFrame>();
				ComponentSpawn componentSpawn = entity.FindComponent<ComponentSpawn>();
				if (componentFrame != null && componentSpawn != null)
				{
					componentFrame.Position = new Vector3(onePoint9) + new Vector3(0.5f, 0f, 0.5f);
					componentFrame.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, new Random().Float(0f, (float)Math.PI * 2f));
					componentSpawn.SpawnDuration = 0f;
					base.Project.AddEntity(entity);
				}
				return SubmitResult.Success;
			});
			AddFunction("removenpc", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text6 = (string)commandData.GetValue("obj");
					if (text6 == "player")
					{
						ShowSubmitTips("不能移除玩家");
						return SubmitResult.Fail;
					}
					ErgodicBody(text6, delegate(ComponentBody body)
					{
						base.Project.RemoveEntity(body.Entity, true);
						return false;
					});
				}
				else if (commandData.Type == "all")
				{
					ErgodicBody("npc", delegate(ComponentBody body)
					{
						base.Project.RemoveEntity(body.Entity, true);
						return false;
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("injure", delegate(CommandData commandData)
			{
				string target = (string)commandData.GetValue("obj");
				int num38 = (int)commandData.GetValue("v");
				float amount = (float)num38 / 100f;
				ErgodicBody(target, delegate(ComponentBody body)
				{
					ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
					ComponentDamage componentDamage = body.Entity.FindComponent<ComponentDamage>();
					if (componentCreature != null)
					{
						componentCreature.ComponentHealth.Injure(amount, null, true, "不知道谁输的指令");
					}
					if (componentDamage != null)
					{
						componentDamage.Damage(amount);
					}
					return false;
				});
				return SubmitResult.Success;
			});
			AddFunction("kill", delegate(CommandData commandData)
			{
				string value9 = (string)commandData.GetValue("obj");
				commandData.Data.Add("v", 100);
				CommandData commandData2 = new CommandData(commandData.Position, commandData.Line);
				commandData2.Type = "default";
				commandData2.Data["obj"] = value9;
				commandData2.Data["v"] = 100;
				return Submit("injure", commandData2, false);
			});
			AddFunction("heal", delegate(CommandData commandData)
			{
				string target2 = (string)commandData.GetValue("obj");
				int num39 = (int)commandData.GetValue("v");
				float amount2 = (float)num39 / 100f;
				ErgodicBody(target2, delegate(ComponentBody body)
				{
					ComponentCreature componentCreature2 = body.Entity.FindComponent<ComponentCreature>();
					ComponentDamage componentDamage2 = body.Entity.FindComponent<ComponentDamage>();
					if (componentCreature2 != null)
					{
						componentCreature2.ComponentHealth.Heal(amount2);
					}
					if (componentDamage2 != null)
					{
						componentDamage2.Hitpoints = MathUtils.Min(componentDamage2.Hitpoints + amount2, 1f);
					}
					return false;
				});
				return SubmitResult.Success;
			});
			AddFunction("catchfire", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string target3 = (string)commandData.GetValue("obj");
					int v2 = (int)commandData.GetValue("v");
					ErgodicBody(target3, delegate(ComponentBody body)
					{
						ComponentOnFire componentOnFire = body.Entity.FindComponent<ComponentOnFire>();
						if (componentOnFire != null)
						{
							componentOnFire.SetOnFire(null, v2);
						}
						return false;
					});
				}
				else if (commandData.Type == "block")
				{
					Point3 onePoint10 = GetOnePoint("pos", commandData);
					int num40 = (int)commandData.GetValue("v");
					float num41 = (float)num40 / 10f;
					base.Project.FindSubsystem<SubsystemFireBlockBehavior>().SetCellOnFire(onePoint10.X, onePoint10.Y, onePoint10.Z, num41, (ComponentMiner)null);
				}
				return SubmitResult.Success;
			});
			AddFunction("teleport", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint11 = GetOnePoint("pos", commandData);
					m_componentPlayer.ComponentBody.Position = new Vector3(onePoint11) + new Vector3(0.5f, 0f, 0.5f);
				}
				if (commandData.Type == "spawn")
				{
					m_componentPlayer.ComponentBody.Position = m_componentPlayer.PlayerData.SpawnPosition;
				}
				return SubmitResult.Success;
			});
			AddFunction("spawn", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint12 = GetOnePoint("pos", commandData);
					m_componentPlayer.PlayerData.SpawnPosition = new Vector3(onePoint12) + new Vector3(0.5f, 0f, 0.5f);
				}
				else if (commandData.Type == "playerpos")
				{
					m_componentPlayer.PlayerData.SpawnPosition = m_componentPlayer.ComponentBody.Position;
				}
				return SubmitResult.Success;
			});
			AddFunction("boxstage", delegate(CommandData commandData)
			{
				if (SetPlayerBoxStage(commandData.Type))
				{
					m_playerBoxStage = commandData.Type;
				}
				return SubmitResult.Success;
			});
			AddFunction("level", delegate(CommandData commandData)
			{
				int num42 = (int)commandData.GetValue("v");
				m_componentPlayer.PlayerData.Level = num42;
				return SubmitResult.Success;
			});
			AddFunction("stats", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					float x6 = m_componentPlayer.ComponentHealth.Health * 100f;
					float x7 = m_componentPlayer.ComponentVitalStats.Food * 100f;
					float x8 = m_componentPlayer.ComponentVitalStats.Sleep * 100f;
					float x9 = m_componentPlayer.ComponentVitalStats.Stamina * 100f;
					float x10 = m_componentPlayer.ComponentVitalStats.Wetness * 100f;
					float temperature = m_componentPlayer.ComponentVitalStats.Temperature;
					float attackPower = m_componentPlayer.ComponentMiner.AttackPower;
					float attackResilience = m_componentPlayer.ComponentHealth.AttackResilience;
					float num43 = m_componentPlayer.ComponentLocomotion.WalkSpeed * 10f;
					string text7 = string.Format("生命值:{0}%，饥饿度:{1}%，疲劳度:{2}%，", MathUtils.Round(x6), MathUtils.Round(x7), MathUtils.Round(x8));
					text7 += string.Format("\n耐力值:{0}%，体湿:{1}%, 体温:{2}", MathUtils.Round(x9), MathUtils.Round(x10), temperature);
					text7 += string.Format("\n攻击力:{0}，防御值:{1}，行走速度:{2}", attackPower, attackResilience, num43);
					m_componentPlayer.ComponentGui.DisplaySmallMessage(text7, Color.White, false, false);
					return SubmitResult.Success;
				}
				int num44 = (int)commandData.GetValue("v");
				if (commandData.Type == "health")
				{
					m_componentPlayer.ComponentHealth.Health = (float)num44 / 100f;
				}
				else if (commandData.Type == "food")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令stats类型food在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					m_componentPlayer.ComponentVitalStats.Food = (float)num44 / 100f;
				}
				else if (commandData.Type == "sleep")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令stats类型sleep在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					m_componentPlayer.ComponentVitalStats.Sleep = (float)num44 / 100f;
				}
				else if (commandData.Type == "stamina")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令stats类型stamina在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					m_componentPlayer.ComponentVitalStats.Stamina = (float)num44 / 100f;
				}
				else if (commandData.Type == "wetness")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令stats类型wetness在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					m_componentPlayer.ComponentVitalStats.Wetness = (float)num44 / 100f;
				}
				else if (commandData.Type == "temperature")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令stats类型temperature在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					m_componentPlayer.ComponentVitalStats.Temperature = num44;
				}
				else if (commandData.Type == "attack")
				{
					m_componentPlayer.ComponentMiner.AttackPower = num44;
				}
				else if (commandData.Type == "defense")
				{
					m_componentPlayer.ComponentHealth.AttackResilience = num44;
					m_componentPlayer.ComponentHealth.FallResilience = num44;
					m_componentPlayer.ComponentHealth.FireResilience = 2 * num44;
				}
				else if (commandData.Type == "speed")
				{
					float num45 = (float)num44 / 10f;
					m_componentPlayer.ComponentLocomotion.WalkSpeed = 2f * num45;
					m_componentPlayer.ComponentLocomotion.JumpSpeed = 3f * MathUtils.Sqrt(num45);
					m_componentPlayer.ComponentLocomotion.LadderSpeed = 1.5f * num45;
					m_componentPlayer.ComponentLocomotion.SwimSpeed = 1.5f * num45;
				}
				return SubmitResult.Success;
			});
			AddFunction("action", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					ComponentFirstPersonModel componentFirstPersonModel = m_componentPlayer.Entity.FindComponent<ComponentFirstPersonModel>(true);
					componentFirstPersonModel.m_pokeAnimationTime = 5f;
				}
				else if (commandData.Type.StartsWith("move"))
				{
					int num46 = (int)commandData.GetValue("v");
					Vector3 playerEyesDirection = DataHandle.GetPlayerEyesDirection(m_componentPlayer);
					Vector3 vector = Vector3.Zero;
					switch (commandData.Type)
					{
					case "moveup":
						vector = new Vector3(playerEyesDirection.X, playerEyesDirection.Y, playerEyesDirection.Z);
						break;
					case "movedown":
						vector = new Vector3(0f - playerEyesDirection.X, 0f - playerEyesDirection.Y, 0f - playerEyesDirection.Z);
						break;
					case "moveleft":
						vector = new Vector3(playerEyesDirection.Z, 0f, 0f - playerEyesDirection.X);
						break;
					case "moveright":
						vector = new Vector3(0f - playerEyesDirection.Z, 0f, playerEyesDirection.X);
						break;
					}
					m_componentPlayer.ComponentBody.Velocity = vector / vector.Length() * ((float)num46 / 10f);
				}
				else if (commandData.Type.StartsWith("look"))
				{
					int num47 = (int)commandData.GetValue("v");
					float x11 = m_componentPlayer.ComponentBody.Rotation.ToYawPitchRoll().X;
					switch (commandData.Type)
					{
					case "lookup":
						m_componentPlayer.ComponentLocomotion.LookAngles += new Vector2(0f, MathUtils.DegToRad(num47));
						break;
					case "lookdown":
						m_componentPlayer.ComponentLocomotion.LookAngles += new Vector2(0f, 0f - MathUtils.DegToRad(num47));
						break;
					case "lookleft":
						m_componentPlayer.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x11 + MathUtils.DegToRad(num47));
						break;
					case "lookright":
						m_componentPlayer.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x11 - MathUtils.DegToRad(num47));
						break;
					}
				}
				else if (commandData.Type == "jump")
				{
					int num48 = (int)commandData.GetValue("v");
					Vector3 velocity = m_componentPlayer.ComponentBody.Velocity;
					m_componentPlayer.ComponentBody.Velocity = new ValueTuple<float, float, float>(velocity.X, (float)num48 / 10f, velocity.Z);
				}
				else if (commandData.Type == "rider")
				{
					bool flag6 = (bool)commandData.GetValue("con");
					if (flag6 && m_componentPlayer.ComponentRider.Mount == null)
					{
						ComponentMount componentMount = m_componentPlayer.ComponentRider.FindNearestMount();
						if (componentMount != null)
						{
							m_componentPlayer.ComponentRider.StartMounting(componentMount);
						}
					}
					if (!flag6 && m_componentPlayer.ComponentRider.Mount != null)
					{
						m_componentPlayer.ComponentRider.StartDismounting();
					}
				}
				else if (commandData.Type == "sneak")
				{
					bool isSneaking = (bool)commandData.GetValue("con");
					m_componentPlayer.ComponentBody.IsSneaking = isSneaking;
				}
				else if (commandData.Type == "sleep")
				{
					if ((bool)commandData.GetValue("con"))
					{
						m_componentPlayer.ComponentSleep.Sleep(false);
					}
					else
					{
						m_componentPlayer.ComponentSleep.WakeUp();
					}
				}
				else if (commandData.Type == "cough")
				{
					if ((bool)commandData.GetValue("con"))
					{
						m_componentPlayer.ComponentFlu.Cough();
					}
					else
					{
						m_componentPlayer.ComponentFlu.m_coughDuration = 0f;
					}
				}
				else if (commandData.Type == "sneeze")
				{
					if ((bool)commandData.GetValue("con"))
					{
						m_componentPlayer.ComponentFlu.Sneeze();
					}
					else
					{
						m_componentPlayer.ComponentFlu.m_sneezeDuration = 0f;
					}
				}
				else if (commandData.Type == "hasflu")
				{
					if ((bool)commandData.GetValue("con"))
					{
						m_componentPlayer.ComponentFlu.StartFlu();
					}
					else
					{
						m_componentPlayer.ComponentFlu.m_fluDuration = 0f;
					}
				}
				else if (commandData.Type == "sick")
				{
					if ((bool)commandData.GetValue("con"))
					{
						m_componentPlayer.ComponentSickness.StartSickness();
					}
					else
					{
						m_componentPlayer.ComponentSickness.m_sicknessDuration = 0f;
					}
				}
				else if (commandData.Type == "invincible")
				{
					bool isInvulnerable = (bool)commandData.GetValue("con");
					m_componentPlayer.ComponentHealth.IsInvulnerable = isInvulnerable;
				}
				else if (commandData.Type == "fixed")
				{
					bool flag7 = (bool)commandData.GetValue("con");
					m_componentPlayer.ComponentBody.AirDrag = (flag7 ? new Vector2(1000f, 1000f) : new Vector2(0.25f, 0.25f));
				}
				else if (commandData.Type == "breath")
				{
					bool flag8 = (bool)commandData.GetValue("con");
					m_componentPlayer.ComponentHealth.AirCapacity = (flag8 ? (-1) : 10);
				}
				return SubmitResult.Success;
			});
			AddFunction("clothes", delegate(CommandData commandData)
			{
				if (commandData.Type == "default" || commandData.Type == "removeid")
				{
					int num49 = (int)commandData.GetValue("id");
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(num49)];
					bool flag9 = commandData.Type == "removeid";
					if (!(block is ClothingBlock))
					{
						ShowSubmitTips(string.Format("id为{0}的物品不是衣物，请选择衣物", num49));
						return SubmitResult.Fail;
					}
					ClothingData clothingData = block.GetClothingData(num49);
					if (clothingData == null)
					{
						ShowSubmitTips(string.Format("id为{0}的衣物数据不存在", num49));
						return SubmitResult.Fail;
					}
					List<int> list2 = new List<int>();
					if (!flag9)
					{
						foreach (int clothe in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
						{
							list2.Add(clothe);
						}
						list2.Add(num49);
					}
					else
					{
						foreach (int clothe2 in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
						{
							if (num49 != clothe2)
							{
								list2.Add(clothe2);
							}
						}
					}
					m_componentPlayer.ComponentClothing.SetClothes(clothingData.Slot, list2);
				}
				else if (commandData.Type == "removeslot")
				{
					int num50 = (int)commandData.GetValue("v");
					ClothingSlot slot = (ClothingSlot)(num50 - 1);
					m_componentPlayer.ComponentClothing.SetClothes(slot, new List<int>());
				}
				else if (commandData.Type == "removeall")
				{
					m_componentPlayer.ComponentClothing.SetClothes(ClothingSlot.Head, new List<int>());
					m_componentPlayer.ComponentClothing.SetClothes(ClothingSlot.Torso, new List<int>());
					m_componentPlayer.ComponentClothing.SetClothes(ClothingSlot.Legs, new List<int>());
					m_componentPlayer.ComponentClothing.SetClothes(ClothingSlot.Feet, new List<int>());
				}
				return SubmitResult.Success;
			});
			AddFunction("interact", delegate(CommandData commandData)
			{
				Point3 pos2 = GetOnePoint("pos", commandData);
				if (commandData.Type == "default")
				{
					SubsystemBlockBehaviors subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>();
					Vector3[] array2 = new Vector3[6]
					{
						new Vector3(1f, 0f, 0f),
						new Vector3(0f, 1f, 0f),
						new Vector3(0f, 0f, 1f),
						new Vector3(-1f, 0f, 0f),
						new Vector3(0f, -1f, 0f),
						new Vector3(0f, 0f, -1f)
					};
					Vector3[] array3 = array2;
					foreach (Vector3 direction in array3)
					{
						Ray3 ray = new Ray3(new Vector3(pos2) + new Vector3(0.5f), direction);
						TerrainRaycastResult? terrainRaycastResult = m_componentPlayer.ComponentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
						if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.CellFace.Point == pos2)
						{
							TerrainRaycastResult value10 = terrainRaycastResult.Value;
							SubsystemBlockBehavior[] blockBehaviors = subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(value10.Value), (ComponentMiner)null, (Point3?)null);
							for (int num52 = 0; num52 < blockBehaviors.Length; num52++)
							{
								blockBehaviors[num52].OnInteract(value10, m_componentPlayer.ComponentMiner);
							}
							break;
						}
					}
				}
				else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
				{
					ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(pos2.X, pos2.Y, pos2.Z);
					if (blockEntity != null && m_componentPlayer != null)
					{
						IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
						switch (commandData.Type)
						{
						case "chest":
						{
							ComponentChest componentChest = blockEntity.Entity.FindComponent<ComponentChest>();
							if (componentChest != null)
							{
								m_componentPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(inventory, componentChest);
							}
							break;
						}
						case "table":
						{
							ComponentCraftingTable componentCraftingTable = blockEntity.Entity.FindComponent<ComponentCraftingTable>();
							if (componentCraftingTable != null)
							{
								m_componentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(inventory, componentCraftingTable);
							}
							break;
						}
						case "dispenser":
						{
							ComponentDispenser componentDispenser = blockEntity.Entity.FindComponent<ComponentDispenser>();
							if (componentDispenser != null)
							{
								m_componentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(inventory, componentDispenser);
							}
							break;
						}
						case "furnace":
						{
							ComponentFurnace componentFurnace = blockEntity.Entity.FindComponent<ComponentFurnace>();
							if (componentFurnace != null)
							{
								m_componentPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget(inventory, componentFurnace);
							}
							break;
						}
						}
					}
				}
				else if (commandData.Type == "memorybank" || commandData.Type == "truthcircuit" || commandData.Type == "delaygate" || commandData.Type == "battery")
				{
					SubsystemElectricity subsystemElectricity = base.Project.FindSubsystem<SubsystemElectricity>();
					int value11 = m_subsystemTerrain.Terrain.GetCellValue(pos2.X, pos2.Y, pos2.Z);
					switch (commandData.Type)
					{
					case "memorybank":
						if (Terrain.ExtractContents(value11) == 186)
						{
							SubsystemMemoryBankBlockBehavior memoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
							MemoryBankData memoryBankData = memoryBankBlockBehavior.GetBlockData(pos2) ?? new MemoryBankData();
							DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditMemoryBankDialog(memoryBankData, delegate
							{
								memoryBankBlockBehavior.SetBlockData(pos2, memoryBankData);
								int face3 = ((MemoryBankBlock)BlocksManager.Blocks[186]).GetFace(value11);
								ElectricElement electricElement4 = subsystemElectricity.GetElectricElement(pos2.X, pos2.Y, pos2.Z, face3);
								if (electricElement4 != null)
								{
									subsystemElectricity.QueueElectricElementForSimulation(electricElement4, subsystemElectricity.CircuitStep + 1);
								}
							}));
						}
						break;
					case "truthcircuit":
						if (Terrain.ExtractContents(value11) == 188)
						{
							SubsystemTruthTableCircuitBlockBehavior circuitBlockBehavior = base.Project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>();
							TruthTableData truthTableData = circuitBlockBehavior.GetBlockData(pos2) ?? new TruthTableData();
							DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditTruthTableDialog(truthTableData, delegate
							{
								circuitBlockBehavior.SetBlockData(pos2, truthTableData);
								int face = ((TruthTableCircuitBlock)BlocksManager.Blocks[188]).GetFace(value11);
								ElectricElement electricElement2 = subsystemElectricity.GetElectricElement(pos2.X, pos2.Y, pos2.Z, face);
								if (electricElement2 != null)
								{
									subsystemElectricity.QueueElectricElementForSimulation(electricElement2, subsystemElectricity.CircuitStep + 1);
								}
							}));
						}
						break;
					case "delaygate":
						if (Terrain.ExtractContents(value11) == 224)
						{
							int data2 = Terrain.ExtractData(value11);
							int delay = AdjustableDelayGateBlock.GetDelay(data2);
							DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditAdjustableDelayGateDialog(delay, delegate(int newDelay)
							{
								int num55 = AdjustableDelayGateBlock.SetDelay(data2, newDelay);
								if (num55 != data2)
								{
									int num56 = Terrain.ReplaceData(value11, num55);
									m_subsystemTerrain.ChangeCell(pos2.X, pos2.Y, pos2.Z, num56, true, (ComponentMiner)null);
									int face2 = ((AdjustableDelayGateBlock)BlocksManager.Blocks[224]).GetFace(value11);
									ElectricElement electricElement3 = subsystemElectricity.GetElectricElement(pos2.X, pos2.Y, pos2.Z, face2);
									if (electricElement3 != null)
									{
										subsystemElectricity.QueueElectricElementForSimulation(electricElement3, subsystemElectricity.CircuitStep + 1);
									}
								}
							}));
						}
						break;
					case "battery":
						if (Terrain.ExtractContents(value11) == 138)
						{
							int data = Terrain.ExtractData(value11);
							int voltageLevel = BatteryBlock.GetVoltageLevel(data);
							DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditBatteryDialog(voltageLevel, delegate(int newVoltageLevel)
							{
								int num53 = BatteryBlock.SetVoltageLevel(data, newVoltageLevel);
								if (num53 != data)
								{
									int num54 = Terrain.ReplaceData(value11, num53);
									m_subsystemTerrain.ChangeCell(pos2.X, pos2.Y, pos2.Z, num54, true, (ComponentMiner)null);
									ElectricElement electricElement = subsystemElectricity.GetElectricElement(pos2.X, pos2.Y, pos2.Z, 4);
									if (electricElement != null)
									{
										subsystemElectricity.QueueElectricElementConnectionsForSimulation(electricElement, subsystemElectricity.CircuitStep + 1);
									}
								}
							}));
						}
						break;
					}
				}
				else if (commandData.Type == "sign")
				{
					int cellContents = m_subsystemTerrain.Terrain.GetCellContents(pos2.X, pos2.Y, pos2.Z);
					if (cellContents == 97 || cellContents == 210)
					{
						SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>(true);
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditSignDialog(subsystemSignBlockBehavior, pos2));
					}
				}
				else if (commandData.Type == "command")
				{
					int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(pos2.X, pos2.Y, pos2.Z);
					if (cellContents2 == 333)
					{
						m_componentPlayer.ComponentGui.ModalPanelWidget = new CommandEditWidget(base.Project, m_componentPlayer, pos2);
					}
				}
				else if (commandData.Type == "button")
				{
					SubsystemElectricity subsystemElectricity2 = base.Project.FindSubsystem<SubsystemElectricity>();
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(pos2.X, pos2.Y, pos2.Z);
					if (Terrain.ExtractContents(cellValue) == 142)
					{
						int face4 = ((ButtonBlock)BlocksManager.Blocks[142]).GetFace(cellValue);
						ElectricElement electricElement5 = subsystemElectricity2.GetElectricElement(pos2.X, pos2.Y, pos2.Z, face4);
						if (electricElement5 != null)
						{
							((ButtonElectricElement)electricElement5).Press();
						}
					}
				}
				else if (commandData.Type == "switch")
				{
					int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(pos2.X, pos2.Y, pos2.Z);
					if (Terrain.ExtractContents(cellValue2) == 141)
					{
						int num57 = SwitchBlock.SetLeverState(cellValue2, !SwitchBlock.GetLeverState(cellValue2));
						m_subsystemTerrain.ChangeCell(pos2.X, pos2.Y, pos2.Z, num57, true, (ComponentMiner)null);
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("widget", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					switch ((string)commandData.GetValue("opt"))
					{
					case "clothing":
						if (!(m_componentPlayer.ComponentGui.ModalPanelWidget is ClothingWidget))
						{
							m_componentPlayer.ComponentGui.ModalPanelWidget = new ClothingWidget(m_componentPlayer);
						}
						break;
					case "stats":
						if (!(m_componentPlayer.ComponentGui.ModalPanelWidget is VitalStatsWidget))
						{
							m_componentPlayer.ComponentGui.ModalPanelWidget = new VitalStatsWidget(m_componentPlayer);
						}
						break;
					case "inventory":
						if (m_gameMode == GameMode.Creative)
						{
							if (!(m_componentPlayer.ComponentGui.ModalPanelWidget is CreativeInventoryWidget))
							{
								m_componentPlayer.ComponentGui.ModalPanelWidget = new CreativeInventoryWidget(m_componentPlayer.Entity);
							}
						}
						else if (!(m_componentPlayer.ComponentGui.ModalPanelWidget is FullInventoryWidget))
						{
							ComponentCraftingTable componentCraftingTable2 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(true);
							m_componentPlayer.ComponentGui.ModalPanelWidget = new FullInventoryWidget(m_componentPlayer.ComponentMiner.Inventory, componentCraftingTable2);
						}
						break;
					}
				}
				else if (commandData.Type == "close")
				{
					if (m_componentPlayer.ComponentGui.ModalPanelWidget != null)
					{
						m_componentPlayer.ComponentGui.ModalPanelWidget = null;
					}
				}
				else if (commandData.Type == "hidegui")
				{
					if ((bool)commandData.GetValue("con"))
					{
						if (m_aimingSightsBatches == null)
						{
							m_aimingSightsBatches = m_componentPlayer.ComponentAimingSights.m_primitivesRenderer3D.m_allBatches.ToList();
						}
						m_componentPlayer.ComponentAimingSights.m_primitivesRenderer3D.m_allBatches.Clear();
						m_componentPlayer.ComponentGui.ShortInventoryWidget.IsDrawEnabled = false;
						m_componentPlayer.ComponentGui.m_moveContainerWidget.IsDrawEnabled = false;
						m_componentPlayer.ComponentGui.m_lookContainerWidget.IsDrawEnabled = false;
						m_componentPlayer.ComponentGui.m_leftControlsContainerWidget.IsDrawEnabled = false;
						m_componentPlayer.ComponentGui.m_rightControlsContainerWidget.IsDrawEnabled = false;
					}
					else
					{
						if (m_aimingSightsBatches != null)
						{
							m_componentPlayer.ComponentAimingSights.m_primitivesRenderer3D.m_allBatches = m_aimingSightsBatches.ToList();
						}
						m_componentPlayer.ComponentGui.ShortInventoryWidget.IsDrawEnabled = true;
						m_componentPlayer.ComponentGui.m_moveContainerWidget.IsDrawEnabled = true;
						m_componentPlayer.ComponentGui.m_lookContainerWidget.IsDrawEnabled = true;
						m_componentPlayer.ComponentGui.m_leftControlsContainerWidget.IsDrawEnabled = true;
						m_componentPlayer.ComponentGui.m_rightControlsContainerWidget.IsDrawEnabled = true;
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("adddrop", delegate(CommandData commandData)
			{
				Point3 onePoint13 = GetOnePoint("pos", commandData);
				int value12 = (int)commandData.GetValue("id");
				int num58 = (int)commandData.GetValue("v");
				for (int num59 = 0; num59 < num58; num59++)
				{
					m_subsystemPickables.AddPickable(value12, 1, new Vector3(onePoint13) + new Vector3(new Random().Float(0.4f, 0.6f)), null, null);
				}
				return SubmitResult.Success;
			});
			AddFunction("removedrop", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					int num60 = (int)commandData.GetValue("id");
					foreach (Pickable pickable in m_subsystemPickables.Pickables)
					{
						if (pickable.Value == num60)
						{
							pickable.ToRemove = true;
						}
					}
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint10 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea8 = new CubeArea(twoPoint10[0], twoPoint10[1]);
					foreach (Pickable pickable2 in m_subsystemPickables.Pickables)
					{
						if (cubeArea8.Exist(pickable2.Position))
						{
							pickable2.ToRemove = true;
						}
					}
				}
				else if (commandData.Type == "limarea")
				{
					Point3[] twoPoint11 = GetTwoPoint("pos1", "pos2", commandData);
					int num61 = (int)commandData.GetValue("id");
					CubeArea cubeArea9 = new CubeArea(twoPoint11[0], twoPoint11[1]);
					foreach (Pickable pickable3 in m_subsystemPickables.Pickables)
					{
						if (cubeArea9.Exist(pickable3.Position) && pickable3.Value == num61)
						{
							pickable3.ToRemove = true;
						}
					}
				}
				else if (commandData.Type == "all")
				{
					foreach (Pickable pickable4 in m_subsystemPickables.Pickables)
					{
						pickable4.ToRemove = true;
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("launchdrop", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint14 = GetOnePoint("pos", commandData);
					int value13 = (int)commandData.GetValue("id");
					Vector3 value14 = (Vector3)commandData.GetValue("vec3");
					m_subsystemPickables.AddPickable(value13, 1, new Vector3(onePoint14) + new Vector3(0.5f), value14, null);
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint12 = GetTwoPoint("pos1", "pos2", commandData);
					int num62 = (int)commandData.GetValue("id");
					Vector3 velocity2 = (Vector3)commandData.GetValue("vec3");
					CubeArea cubeArea10 = new CubeArea(twoPoint12[0], twoPoint12[1]);
					foreach (Pickable pickable5 in m_subsystemPickables.Pickables)
					{
						if (cubeArea10.Exist(pickable5.Position) && pickable5.Value == num62)
						{
							pickable5.Velocity = velocity2;
						}
					}
				}
				else if (commandData.Type == "gather" || commandData.Type == "spread")
				{
					Point3 onePoint15 = GetOnePoint("pos", commandData);
					int num63 = (int)commandData.GetValue("v");
					bool flag10 = commandData.Type == "gather";
					foreach (Pickable pickable6 in m_subsystemPickables.Pickables)
					{
						Vector3 vector2 = new Vector3(onePoint15) - pickable6.Position;
						if (!flag10)
						{
							vector2 = -vector2;
						}
						pickable6.Velocity = vector2 / vector2.Length() * ((float)num63 / 10f);
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("additem", delegate(CommandData commandData)
			{
				int num64 = (int)commandData.GetValue("id");
				int num65 = (int)commandData.GetValue("v");
				if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
				{
					int num66 = (int)commandData.GetValue("s");
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令additem类型" + commandData.Type + "在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					bool flag11 = commandData.Type == "craft";
					int index = -1;
					ComponentInventoryBase componentInventoryBase = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
					ComponentCraftingTable componentCraftingTable3 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
					if (componentInventoryBase != null && componentCraftingTable3 != null)
					{
						List<ComponentInventoryBase.Slot> list3 = ((!flag11) ? componentInventoryBase.m_slots : componentCraftingTable3.m_slots);
						switch (commandData.Type)
						{
						case "default":
							index = num66 - 1;
							break;
						case "inventory":
							index = num66 + 9;
							break;
						case "craft":
							index = num66 - 1;
							break;
						}
						if (list3[index].Value == num64)
						{
							ComponentInventoryBase.Slot slot2 = list3[index];
							slot2.Count += num65;
						}
						else
						{
							list3[index].Value = num64;
							list3[index].Count = num65;
						}
					}
				}
				else if (commandData.Type == "hand")
				{
					ComponentInventoryBase componentInventoryBase2 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
					int slotValue = componentInventoryBase2.GetSlotValue(componentInventoryBase2.ActiveSlotIndex);
					int slotCount = componentInventoryBase2.GetSlotCount(componentInventoryBase2.ActiveSlotIndex);
					if (slotValue == num64)
					{
						componentInventoryBase2.RemoveSlotItems(componentInventoryBase2.ActiveSlotIndex, slotCount);
						componentInventoryBase2.AddSlotItems(componentInventoryBase2.ActiveSlotIndex, num64, slotCount + num65);
					}
					else
					{
						componentInventoryBase2.RemoveSlotItems(componentInventoryBase2.ActiveSlotIndex, slotCount);
						componentInventoryBase2.AddSlotItems(componentInventoryBase2.ActiveSlotIndex, num64, num65);
					}
				}
				else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
				{
					int num67 = (int)commandData.GetValue("s");
					Point3 onePoint16 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity2 = m_subsystemBlockEntities.GetBlockEntity(onePoint16.X, onePoint16.Y, onePoint16.Z);
					if (blockEntity2 == null)
					{
						return SubmitResult.Fail;
					}
					List<ComponentInventoryBase.Slot> list4 = null;
					try
					{
						switch (commandData.Type)
						{
						case "chest":
							list4 = blockEntity2.Entity.FindComponent<ComponentChest>().m_slots;
							break;
						case "table":
							list4 = blockEntity2.Entity.FindComponent<ComponentCraftingTable>().m_slots;
							break;
						case "dispenser":
							list4 = blockEntity2.Entity.FindComponent<ComponentDispenser>().m_slots;
							break;
						case "furnace":
							list4 = blockEntity2.Entity.FindComponent<ComponentFurnace>().m_slots;
							break;
						}
					}
					catch
					{
						return SubmitResult.Fail;
					}
					if (list4[num67 - 1].Value == num64)
					{
						ComponentInventoryBase.Slot slot3 = list4[num67 - 1];
						slot3.Count += num65;
					}
					else
					{
						list4[num67 - 1].Value = num64;
						list4[num67 - 1].Count = num65;
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("removeitem", delegate(CommandData commandData)
			{
				int num68 = (int)commandData.GetValue("s");
				int num69 = (int)commandData.GetValue("v");
				if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令removeitem类型" + commandData.Type + "在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					bool flag12 = commandData.Type == "craft";
					int index2 = -1;
					ComponentInventoryBase componentInventoryBase3 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
					ComponentCraftingTable componentCraftingTable4 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
					if (componentInventoryBase3 != null && componentCraftingTable4 != null)
					{
						List<ComponentInventoryBase.Slot> list5 = ((!flag12) ? componentInventoryBase3.m_slots : componentCraftingTable4.m_slots);
						switch (commandData.Type)
						{
						case "default":
							index2 = num68 - 1;
							break;
						case "inventory":
							index2 = num68 + 9;
							break;
						case "craft":
							index2 = num68 - 1;
							break;
						}
						if (list5[index2].Count < num69)
						{
							list5[index2].Count = 0;
						}
						else
						{
							list5[index2].Count = list5[index2].Count - num69;
						}
					}
				}
				else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
				{
					Point3 onePoint17 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity3 = m_subsystemBlockEntities.GetBlockEntity(onePoint17.X, onePoint17.Y, onePoint17.Z);
					if (blockEntity3 == null)
					{
						return SubmitResult.Fail;
					}
					List<ComponentInventoryBase.Slot> list6 = null;
					try
					{
						switch (commandData.Type)
						{
						case "chest":
							list6 = blockEntity3.Entity.FindComponent<ComponentChest>().m_slots;
							break;
						case "table":
							list6 = blockEntity3.Entity.FindComponent<ComponentCraftingTable>().m_slots;
							break;
						case "dispenser":
							list6 = blockEntity3.Entity.FindComponent<ComponentDispenser>().m_slots;
							break;
						case "furnace":
							list6 = blockEntity3.Entity.FindComponent<ComponentFurnace>().m_slots;
							break;
						}
					}
					catch
					{
						return SubmitResult.Fail;
					}
					if (list6[num68 - 1].Count < num69)
					{
						list6[num68 - 1].Count = 0;
					}
					else
					{
						list6[num68 - 1].Count = list6[num68 - 1].Count - num69;
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("clearitem", delegate(CommandData commandData)
			{
				if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
				{
					if (m_gameMode == GameMode.Creative)
					{
						ShowSubmitTips("指令clearitem类型" + commandData.Type + "在非创造模式下提交才有效");
						return SubmitResult.Fail;
					}
					bool flag13 = commandData.Type == "craft";
					int num70 = 0;
					int num71 = 0;
					ComponentInventoryBase componentInventoryBase4 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
					ComponentCraftingTable componentCraftingTable5 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
					if (componentInventoryBase4 != null && componentCraftingTable5 != null)
					{
						List<ComponentInventoryBase.Slot> list7 = ((!flag13) ? componentInventoryBase4.m_slots : componentCraftingTable5.m_slots);
						switch (commandData.Type)
						{
						case "default":
							num70 = 0;
							num71 = 10;
							break;
						case "inventory":
							num70 = 10;
							num71 = 16;
							break;
						case "craft":
							num70 = 0;
							num71 = 6;
							break;
						}
						for (int num72 = 0; num72 < num71; num72++)
						{
							list7[num70 + num72].Count = 0;
						}
					}
				}
				else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
				{
					Point3 onePoint18 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity4 = m_subsystemBlockEntities.GetBlockEntity(onePoint18.X, onePoint18.Y, onePoint18.Z);
					if (blockEntity4 == null)
					{
						return SubmitResult.Fail;
					}
					List<ComponentInventoryBase.Slot> list8 = null;
					try
					{
						switch (commandData.Type)
						{
						case "chest":
							list8 = blockEntity4.Entity.FindComponent<ComponentChest>().m_slots;
							break;
						case "table":
							list8 = blockEntity4.Entity.FindComponent<ComponentCraftingTable>().m_slots;
							break;
						case "dispenser":
							list8 = blockEntity4.Entity.FindComponent<ComponentDispenser>().m_slots;
							break;
						case "furnace":
							list8 = blockEntity4.Entity.FindComponent<ComponentFurnace>().m_slots;
							break;
						}
					}
					catch
					{
						return SubmitResult.Fail;
					}
					foreach (ComponentInventoryBase.Slot item3 in list8)
					{
						item3.Count = 0;
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("explosion", delegate(CommandData commandData)
			{
				Point3 onePoint19 = GetOnePoint("pos", commandData);
				int num73 = (int)commandData.GetValue("v");
				SubsystemExplosions subsystemExplosions = base.Project.FindSubsystem<SubsystemExplosions>();
				subsystemExplosions.AddExplosion(onePoint19.X, onePoint19.Y, onePoint19.Z, (float)num73, false, false, (PlayerData)null);
				return SubmitResult.Success;
			});
			AddFunction("lightning", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint20 = GetOnePoint("pos", commandData);
					base.Project.FindSubsystem<SubsystemSky>().MakeLightningStrike(new Vector3(onePoint20) + new Vector3(0.5f));
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint13 = GetTwoPoint("pos1", "pos2", commandData);
					Color color2 = (Color)commandData.GetValue("color");
					int num74 = (int)commandData.GetValue("v1");
					int v3 = (int)commandData.GetValue("v2");
					CubeArea cubeArea11 = new CubeArea(twoPoint13[0], twoPoint13[1]);
					int splitX = (int)((float)cubeArea11.LengthX * (1f - (float)num74 / 100f)) + 1;
					int splitZ = (int)((float)cubeArea11.LengthZ * (1f - (float)num74 / 100f)) + 1;
					SubsystemExplosions subsystemExplosions2 = base.Project.FindSubsystem<SubsystemExplosions>();
					cubeArea11.Ergodic(delegate
					{
						bool flag14 = cubeArea11.MinPoint.Y == cubeArea11.Current.Y;
						bool flag15 = (cubeArea11.Current.X - cubeArea11.MinPoint.X) % splitX == 0;
						bool flag16 = (cubeArea11.Current.Z - cubeArea11.MinPoint.Z) % splitZ == 0;
						if (flag14 && flag15 && flag16)
						{
							cubeArea11.Current.Y = m_subsystemTerrain.Terrain.GetTopHeight(cubeArea11.Current.X, cubeArea11.Current.Z);
							Vector3 position = new Vector3(cubeArea11.Current) + new Vector3(new Random().Float(0f, 1f));
							m_subsystemParticles.AddParticleSystem(new LightningStrikeParticleSystem(position, color2));
							subsystemExplosions2.AddExplosion(cubeArea11.Current.X, cubeArea11.Current.Y, cubeArea11.Current.Z, (float)v3, false, false, (PlayerData)null);
						}
						return false;
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("rain", delegate(CommandData commandData)
			{
				SubsystemWeather subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>();
				bool flag17 = (bool)commandData.GetValue("con");
				Color rainColor = (Color)commandData.GetValue("color");
				m_rainColor = rainColor;
				if (flag17)
				{
					subsystemWeather.GlobalPrecipitationIntensity = 1f;
					subsystemWeather.m_precipitationStartTime = 0.0;
				}
				else
				{
					subsystemWeather.GlobalPrecipitationIntensity = 0f;
					subsystemWeather.m_precipitationEndTime = 0.0;
				}
				return SubmitResult.Success;
			});
			AddFunction("skycolor", delegate(CommandData commandData)
			{
				Color skyColor = (Color)commandData.GetValue("color");
				m_skyColor = skyColor;
				return SubmitResult.Success;
			});
			AddFunction("temperature", delegate(CommandData commandData)
			{
				Point3[] twoPoint14 = GetTwoPoint("pos1", "pos2", commandData);
				int temperature2 = (int)commandData.GetValue("v");
				CubeArea cubeArea12 = new CubeArea(twoPoint14[0], twoPoint14[1]);
				Point2 point16 = Terrain.ToChunk(cubeArea12.MinPoint.X, cubeArea12.MinPoint.Z);
				Point2 point17 = Terrain.ToChunk(cubeArea12.MaxPoint.X, cubeArea12.MaxPoint.Z);
				for (int num75 = point16.X; num75 <= point17.X; num75++)
				{
					for (int num76 = point16.Y; num76 <= point17.Y; num76++)
					{
						TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(num75, num76);
						for (int num77 = 0; num77 < 16; num77++)
						{
							for (int num78 = 0; num78 < 16; num78++)
							{
								if (cubeArea12.Exist(new Vector3(num77 + chunkAtCoords.Origin.X, cubeArea12.Center.Y, num78 + chunkAtCoords.Origin.Y)))
								{
									chunkAtCoords.SetTemperatureFast(num77, num78, temperature2);
								}
							}
						}
					}
				}
				Time.QueueTimeDelayedExecution(Time.RealTime + 1.0, delegate
				{
					m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, false);
				});
				return SubmitResult.Success;
			});
			AddFunction("humidity", delegate(CommandData commandData)
			{
				Point3[] twoPoint15 = GetTwoPoint("pos1", "pos2", commandData);
				int humidity = (int)commandData.GetValue("v");
				CubeArea cubeArea13 = new CubeArea(twoPoint15[0], twoPoint15[1]);
				Point2 point18 = Terrain.ToChunk(cubeArea13.MinPoint.X, cubeArea13.MinPoint.Z);
				Point2 point19 = Terrain.ToChunk(cubeArea13.MaxPoint.X, cubeArea13.MaxPoint.Z);
				for (int num79 = point18.X; num79 <= point19.X; num79++)
				{
					for (int num80 = point18.Y; num80 <= point19.Y; num80++)
					{
						TerrainChunk chunkAtCoords2 = m_subsystemTerrain.Terrain.GetChunkAtCoords(num79, num80);
						for (int num81 = 0; num81 < 16; num81++)
						{
							for (int num82 = 0; num82 < 16; num82++)
							{
								if (cubeArea13.Exist(new Vector3(num81 + chunkAtCoords2.Origin.X, cubeArea13.Center.Y, num82 + chunkAtCoords2.Origin.Y)))
								{
									chunkAtCoords2.SetHumidityFast(num81, num82, humidity);
								}
							}
						}
					}
				}
				Time.QueueTimeDelayedExecution(Time.RealTime + 1.0, delegate
				{
					m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, false);
				});
				return SubmitResult.Success;
			});
			AddFunction("copyblock", delegate(CommandData commandData)
			{
				WithdrawBlockManager wbManager3 = null;
				if (WithdrawBlockManager.WithdrawMode)
				{
					wbManager3 = new WithdrawBlockManager();
				}
				Point3[] twoPoint16 = GetTwoPoint("pos1", "pos2", commandData);
				Point3 pos3 = GetOnePoint("pos3", commandData);
				CubeArea cubeArea14 = new CubeArea(twoPoint16[0], twoPoint16[1]);
				if (commandData.Type == "default")
				{
					Point3 point20 = (Point3)commandData.GetValue("pos4");
					bool flag18 = (bool)commandData.GetValue("con1");
					bool flag19 = (bool)commandData.GetValue("con2");
					CopyBlockManager copyBlockManager = new CopyBlockManager(this, wbManager3, cubeArea14.MinPoint, cubeArea14.MaxPoint, false, flag18)
					{
						CopyOrigin = pos3
					};
					if (flag19)
					{
						copyBlockManager.ClearBlockArea();
						PlaceReprocess(wbManager3, commandData, true, cubeArea14.MinPoint, cubeArea14.MaxPoint);
					}
					copyBlockManager.DirectCopy(point20, flag18);
					PlaceReprocess(wbManager3, commandData, true, point20 - pos3 + cubeArea14.MinPoint, point20 - pos3 + cubeArea14.MaxPoint);
				}
				else if (commandData.Type == "copycache")
				{
					CopyBlockManager = new CopyBlockManager(this, wbManager3, cubeArea14.MinPoint, cubeArea14.MaxPoint, true);
					CopyBlockManager.CopyOrigin = pos3;
					ShowSubmitTips("建筑已复制到缓存区，可以过视距或跨存档生成\n建筑生成指令build");
				}
				else if (commandData.Type == "rotate")
				{
					string axis = (string)commandData.GetValue("opt1");
					string angle = (string)commandData.GetValue("opt2");
					bool flag20 = (bool)commandData.GetValue("con");
					CopyBlockManager copyBlockManager2 = new CopyBlockManager(this, wbManager3, cubeArea14.MinPoint, cubeArea14.MaxPoint);
					if (flag20)
					{
						copyBlockManager2.ClearBlockArea();
						PlaceReprocess(wbManager3, commandData, true, cubeArea14.MinPoint, cubeArea14.MaxPoint);
					}
					copyBlockManager2.RotateCopy(pos3, axis, angle);
					Point3 rotatePoint = copyBlockManager2.GetRotatePoint(copyBlockManager2.CubeArea.MinPoint, pos3, axis, angle);
					Point3 rotatePoint2 = copyBlockManager2.GetRotatePoint(copyBlockManager2.CubeArea.MaxPoint, pos3, axis, angle);
					CubeArea cubeArea15 = new CubeArea(rotatePoint, rotatePoint2);
					PlaceReprocess(wbManager3, commandData, true, cubeArea15.MinPoint, cubeArea15.MaxPoint);
				}
				else if (commandData.Type == "mirror")
				{
					string plane = (string)commandData.GetValue("opt");
					bool flag21 = (bool)commandData.GetValue("con1");
					bool laminate = (bool)commandData.GetValue("con2");
					CopyBlockManager copyBlockManager3 = new CopyBlockManager(this, wbManager3, cubeArea14.MinPoint, cubeArea14.MaxPoint);
					if (flag21)
					{
						copyBlockManager3.ClearBlockArea();
						PlaceReprocess(wbManager3, commandData, true, cubeArea14.MinPoint, cubeArea14.MaxPoint);
					}
					copyBlockManager3.MirrorCopy(pos3, plane, laminate);
					Point3 mirrorPoint = copyBlockManager3.GetMirrorPoint(copyBlockManager3.CubeArea.MinPoint, pos3, plane, laminate);
					Point3 mirrorPoint2 = copyBlockManager3.GetMirrorPoint(copyBlockManager3.CubeArea.MaxPoint, pos3, plane, laminate);
					CubeArea cubeArea16 = new CubeArea(mirrorPoint, mirrorPoint2);
					PlaceReprocess(wbManager3, commandData, true, cubeArea16.MinPoint, cubeArea16.MaxPoint);
				}
				else if (commandData.Type == "enlarge")
				{
					cubeArea14.Ergodic(delegate
					{
						int num83 = cubeArea14.Current.X - cubeArea14.MinPoint.X;
						int num84 = cubeArea14.Current.Y - cubeArea14.MinPoint.Y;
						int num85 = cubeArea14.Current.Z - cubeArea14.MinPoint.Z;
						int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea14.Current.X, cubeArea14.Current.Y, cubeArea14.Current.Z);
						int num86 = Terrain.ExtractData(cellValue3);
						Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue3)];
						bool flag22 = block2 is CubeBlock;
						bool flag23 = block2 is StairsBlock;
						bool flag24 = block2 is SlabBlock;
						if (flag22 || flag23 || flag24)
						{
							int num87 = 0;
							int value15 = 0;
							for (int num88 = 0; num88 < 2; num88++)
							{
								for (int num89 = 0; num89 < 2; num89++)
								{
									for (int num90 = 0; num90 < 2; num90++)
									{
										if (flag22)
										{
											value15 = cellValue3;
										}
										else if (flag23)
										{
											value15 = DataHandle.GetStairValue(cellValue3, num87);
										}
										else if (flag24)
										{
											value15 = DataHandle.GetSlabValue(cellValue3, num87);
										}
										ChangeBlockValue(wbManager3, pos3.X + 2 * num83 + num89, pos3.Y + 2 * num84 + num88, pos3.Z + 2 * num85 + num90, value15);
										num87++;
									}
								}
							}
						}
						return false;
					});
					PlaceReprocess(wbManager3, commandData, true, pos3, pos3 + new Point3(cubeArea14.LengthX * 2, cubeArea14.LengthY * 2, cubeArea14.LengthZ * 2));
				}
				else if (commandData.Type == "aroundaxis")
				{
					string opt = (string)commandData.GetValue("opt");
					Task.Run(delegate
					{
						Dictionary<int, Dictionary<int, int>> hightLens = new Dictionary<int, Dictionary<int, int>>();
						cubeArea14.Ergodic(delegate
						{
							int cellValue4 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea14.Current.X, cubeArea14.Current.Y, cubeArea14.Current.Z);
							if (Terrain.ExtractContents(cellValue4) != 0)
							{
								int key = cubeArea14.Current.Y;
								int num91 = cubeArea14.Current.X - pos3.X;
								int num92 = cubeArea14.Current.Z - pos3.Z;
								if (opt == "x")
								{
									key = cubeArea14.Current.X;
									num91 = cubeArea14.Current.Y - pos3.Y;
								}
								else if (opt == "z")
								{
									key = cubeArea14.Current.Z;
									num92 = cubeArea14.Current.Y - pos3.Y;
								}
								if (!hightLens.ContainsKey(key))
								{
									hightLens[key] = new Dictionary<int, int>();
								}
								int num93 = num91 * num91 + num92 * num92;
								num93 = (int)MathUtils.Sqrt(num93);
								if (!hightLens[key].ContainsKey(num93))
								{
									hightLens[key][num93] = cellValue4;
								}
							}
							return false;
						});
						int num94 = 0;
						foreach (int key2 in hightLens.Keys)
						{
							foreach (int key3 in hightLens[key2].Keys)
							{
								if (key3 > num94)
								{
									num94 = key3;
								}
								for (int num95 = -key3; num95 <= key3; num95++)
								{
									for (int num96 = -key3; num96 <= key3; num96++)
									{
										if (MathUtils.Abs(num95 * num95 + num96 * num96 - key3 * key3) <= key3)
										{
											switch (opt)
											{
											case "x":
												ChangeBlockValue(wbManager3, key2, num95 + pos3.Y, num96 + pos3.Z, hightLens[key2][key3]);
												break;
											case "y":
												ChangeBlockValue(wbManager3, num95 + pos3.X, key2, num96 + pos3.Z, hightLens[key2][key3]);
												break;
											case "z":
												ChangeBlockValue(wbManager3, num95 + pos3.X, num96 + pos3.Y, key2, hightLens[key2][key3]);
												break;
											}
										}
									}
								}
							}
						}
						Point3 minPoint = new Point3(pos3.X - num94 - 1, 0, pos3.Z - num94 - 1);
						Point3 maxPoint = new Point3(pos3.X + num94 + 1, 0, pos3.Z + num94 + 1);
						PlaceReprocess(wbManager3, commandData, true, minPoint, maxPoint);
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("moveblock", delegate(CommandData commandData)
			{
				Point3[] twoPoint17 = GetTwoPoint("pos1", "pos2", commandData);
				Vector3 vector3 = (Vector3)commandData.GetValue("vec3");
				int num97 = (int)commandData.GetValue("v");
				string id9 = string.Empty;
				switch (commandData.Type)
				{
				case "default":
					id9 = "moveblock$default";
					break;
				case "dig":
					id9 = "moveblock$dig";
					break;
				case "limit":
					id9 = "moveblock$limit";
					break;
				}
				CubeArea cubeArea17 = new CubeArea(twoPoint17[0], twoPoint17[1]);
				List<MovingBlock> list9 = new List<MovingBlock>();
				cubeArea17.Ergodic(delegate
				{
					list9.Add(new MovingBlock
					{
						Value = m_subsystemTerrain.Terrain.GetCellValue(cubeArea17.Current.X, cubeArea17.Current.Y, cubeArea17.Current.Z),
						Offset = cubeArea17.Current - cubeArea17.MinPoint
					});
					return false;
				});
				Vector3 vector4 = new Vector3(cubeArea17.MinPoint.X, cubeArea17.MinPoint.Y, cubeArea17.MinPoint.Z);
				Vector3 targetPosition = vector4 + vector3;
				SubsystemMovingBlocks subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>();
				IMovingBlockSet movingBlockSet = subsystemMovingBlocks.AddMovingBlockSet(vector4, targetPosition, (float)num97 / 10f, 0f, 0f, new Vector2(1f, 1f), list9, id9, twoPoint17[0], true);
				if (movingBlockSet == null)
				{
					ShowSubmitTips("运动方块添加失败，发生未知错误");
					return SubmitResult.Fail;
				}
				foreach (MovingBlock item4 in list9)
				{
					m_subsystemTerrain.ChangeCell(cubeArea17.MinPoint.X + item4.Offset.X, cubeArea17.MinPoint.Y + item4.Offset.Y, cubeArea17.MinPoint.Z + item4.Offset.Z, 0, true, (ComponentMiner)null);
				}
				return SubmitResult.Success;
			});
			AddFunction("moveset", delegate(CommandData commandData)
			{
				string text8 = (string)commandData.GetValue("n");
				if (commandData.Type == "default")
				{
					string face5 = (string)commandData.GetValue("opt");
					Point3[] twoPoint18 = GetTwoPoint("pos1", "pos2", commandData);
					Point3 onePoint21 = GetOnePoint("pos3", commandData);
					CubeArea cubeArea18 = new CubeArea(twoPoint18[0], twoPoint18[1]);
					string[] array4 = new string[5] { "$", "|", ":", "@", "&" };
					string[] array5 = array4;
					foreach (string value16 in array5)
					{
						if (text8.Contains(value16))
						{
							ShowSubmitTips("运动设计名称不能包含特殊符号且不能为纯数字");
							return SubmitResult.Fail;
						}
					}
					if (GetMovingBlockTagLine(text8) != null || ExistWaitMoveSet(text8))
					{
						ShowSubmitTips("名为" + text8 + "的运动方块设计已存在");
						return SubmitResult.Fail;
					}
					string tag = SetMovingBlockTagLine(text8, face5, onePoint21 - cubeArea18.MinPoint);
					List<MovingBlock> list10 = new List<MovingBlock>();
					cubeArea18.Ergodic(delegate
					{
						int cellValue5 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea18.Current.X, cubeArea18.Current.Y, cubeArea18.Current.Z);
						int id10 = Terrain.ExtractContents(cellValue5);
						GetMoveEntityBlocks(tag, id10, cubeArea18.Current, cubeArea18.Current - cubeArea18.MinPoint);
						list10.Add(new MovingBlock
						{
							Value = cellValue5,
							Offset = cubeArea18.Current - cubeArea18.MinPoint
						});
						return false;
					});
					Vector3 vector5 = new Vector3(cubeArea18.MinPoint);
					IMovingBlockSet movingBlockSet2 = m_subsystemMovingBlocks.AddMovingBlockSet(vector5, vector5, 0f, 0f, 0f, new Vector2(1f, 1f), list10, "moveset", tag, true);
					if (movingBlockSet2 == null)
					{
						ShowSubmitTips("名为" + text8 + "的运动方块设计添加失败，发生未知错误");
						return SubmitResult.Fail;
					}
					foreach (MovingBlock item5 in list10)
					{
						m_subsystemTerrain.ChangeCell(cubeArea18.MinPoint.X + item5.Offset.X, cubeArea18.MinPoint.Y + item5.Offset.Y, cubeArea18.MinPoint.Z + item5.Offset.Z, 0, true, (ComponentMiner)null);
					}
				}
				else if (commandData.Type == "append")
				{
					Point3[] twoPoint19 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea19 = new CubeArea(twoPoint19[0], twoPoint19[1]);
					string tag2 = GetMovingBlockTagLine(text8);
					IMovingBlockSet movingBlockSet3 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag2);
					if (tag2 == null)
					{
						List<Point3> value17;
						if (!FindWaitMoveSet(text8, out tag2, out value17))
						{
							goto IL_1082;
						}
						movingBlockSet3 = WaitMoveSetTurnToWork(tag2, value17);
					}
					List<MovingBlock> list11 = new List<MovingBlock>();
					cubeArea19.Ergodic(delegate
					{
						int cellValue6 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea19.Current.X, cubeArea19.Current.Y, cubeArea19.Current.Z);
						movingBlockSet3.SetBlock(cubeArea19.Current - new Point3(movingBlockSet3.Position), cellValue6);
						m_subsystemTerrain.ChangeCell(cubeArea19.Current.X, cubeArea19.Current.Y, cubeArea19.Current.Z, 0, true, (ComponentMiner)null);
						return false;
					});
				}
				else if (commandData.Type == "move")
				{
					int num99 = (int)commandData.GetValue("v");
					string tag3 = GetMovingBlockTagLine(text8);
					Vector3 vector6 = (Vector3)commandData.GetValue("vec3");
					bool flag25 = (bool)commandData.GetValue("con");
					IMovingBlockSet movingBlockSet4 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag3);
					if (tag3 == null)
					{
						List<Point3> value18;
						if (!FindWaitMoveSet(text8, out tag3, out value18))
						{
							goto IL_1082;
						}
						movingBlockSet4 = WaitMoveSetTurnToWork(tag3, value18);
					}
					MovingBlockTag movingBlockTag = FindMovingBlockTag(text8);
					if (flag25)
					{
						switch (movingBlockTag.Face)
						{
						case CoordDirection.NX:
							vector6 = new Vector3(0f - vector6.X, vector6.Y, 0f - vector6.Z);
							break;
						case CoordDirection.PZ:
							vector6 = new Vector3(0f - vector6.Z, vector6.Y, vector6.X);
							break;
						case CoordDirection.NZ:
							vector6 = new Vector3(vector6.Z, vector6.Y, 0f - vector6.X);
							break;
						}
					}
					try
					{
						m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet4);
						Vector3 targetPosition2 = movingBlockSet4.Position + vector6;
						((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet4).Stop = false;
						m_subsystemMovingBlocks.AddMovingBlockSet(movingBlockSet4.Position, targetPosition2, (float)num99 / 10f, 0f, 0f, new Vector2(1f, 1f), movingBlockSet4.Blocks, "moveset", tag3, true);
					}
					catch
					{
						ShowSubmitTips("名为" + text8 + "的运动方块移动失败，发生未知错误");
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type == "turn")
				{
					string text9 = (string)commandData.GetValue("opt");
					string tag4 = GetMovingBlockTagLine(text8);
					IMovingBlockSet movingBlockSet5 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag4);
					if (tag4 == null)
					{
						List<Point3> value19;
						if (!FindWaitMoveSet(text8, out tag4, out value19))
						{
							goto IL_1082;
						}
						movingBlockSet5 = WaitMoveSetTurnToWork(tag4, value19);
					}
					MovingBlockTag movingBlockTag2 = FindMovingBlockTag(text8);
					try
					{
						Point3 point21 = new Point3((int)MathUtils.Round(movingBlockSet5.Position.X), (int)MathUtils.Round(movingBlockSet5.Position.Y), (int)MathUtils.Round(movingBlockSet5.Position.Z));
						Point3 point22 = Point3.Zero;
						m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet5);
						foreach (MovingBlock block5 in movingBlockSet5.Blocks)
						{
							point22 = point21 + block5.Offset;
							m_subsystemTerrain.ChangeCell(point22.X, point22.Y, point22.Z, block5.Value, true, (ComponentMiner)null);
						}
						SetMoveEntityBlocks(movingBlockSet5);
						int num100 = 0;
						string angle2;
						switch (text9)
						{
						case "back":
							angle2 = "+180";
							num100 = 2;
							break;
						case "left":
							angle2 = "+270";
							num100 = 3;
							break;
						case "right":
							angle2 = "+90";
							num100 = 1;
							break;
						default:
							return SubmitResult.Fail;
						}
						CopyBlockManager copyBlockManager4 = new CopyBlockManager(this, null, point21, point22);
						copyBlockManager4.ClearBlockArea(true);
						copyBlockManager4.RotateCopy(movingBlockTag2.Axis + point21, "+y", angle2, true);
						Point3 rotatePoint3 = copyBlockManager4.GetRotatePoint(copyBlockManager4.CubeArea.MinPoint, movingBlockTag2.Axis + point21, "+y", angle2);
						Point3 rotatePoint4 = copyBlockManager4.GetRotatePoint(copyBlockManager4.CubeArea.MaxPoint, movingBlockTag2.Axis + point21, "+y", angle2);
						string text10 = "+x";
						switch (movingBlockTag2.Face)
						{
						case CoordDirection.PX:
							text10 = "+x";
							break;
						case CoordDirection.NX:
							text10 = "-x";
							break;
						case CoordDirection.PZ:
							text10 = "+z";
							break;
						case CoordDirection.NZ:
							text10 = "-z";
							break;
						}
						string[] array6 = new string[4] { "+x", "+z", "-x", "-z" };
						for (int num101 = 0; num101 < array6.Length; num101++)
						{
							if (text10 == array6[num101])
							{
								text10 = array6[(num101 + num100) % array6.Length];
								break;
							}
						}
						CubeArea cubeArea20 = new CubeArea(rotatePoint3, rotatePoint4);
						tag4 = SetMovingBlockTagLine(movingBlockTag2.Name, text10, movingBlockTag2.Axis + point21 - cubeArea20.MinPoint);
						List<MovingBlock> list12 = new List<MovingBlock>();
						cubeArea20.Ergodic(delegate
						{
							int cellValue7 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea20.Current.X, cubeArea20.Current.Y, cubeArea20.Current.Z);
							int id11 = Terrain.ExtractContents(cellValue7);
							GetMoveEntityBlocks(tag4, id11, cubeArea20.Current, cubeArea20.Current - cubeArea20.MinPoint);
							list12.Add(new MovingBlock
							{
								Value = cellValue7,
								Offset = cubeArea20.Current - cubeArea20.MinPoint
							});
							return false;
						});
						Vector3 vector7 = new Vector3(cubeArea20.MinPoint);
						Vector3 vector8 = ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet5).TargetPosition - ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet5).Position;
						switch (text9)
						{
						case "back":
							vector8 = new Vector3(0f - vector8.X, vector8.Y, 0f - vector8.Z);
							break;
						case "left":
							vector8 = new Vector3(vector8.Z, vector8.Y, 0f - vector8.X);
							break;
						case "right":
							vector8 = new Vector3(0f - vector8.Z, vector8.Y, vector8.X);
							break;
						default:
							vector8 = Vector3.Zero;
							break;
						}
						movingBlockSet5 = m_subsystemMovingBlocks.AddMovingBlockSet(vector7, vector7 + vector8, ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet5).Speed, 0f, 0f, new Vector2(1f, 1f), list12, "moveset", tag4, true);
						if (movingBlockSet5 != null)
						{
							foreach (MovingBlock item6 in list12)
							{
								m_subsystemTerrain.ChangeCell(cubeArea20.MinPoint.X + item6.Offset.X, cubeArea20.MinPoint.Y + item6.Offset.Y, cubeArea20.MinPoint.Z + item6.Offset.Z, 0, true, (ComponentMiner)null);
							}
						}
					}
					catch
					{
						ShowSubmitTips("名为" + text8 + "的运动方块转弯失败，发生未知错误");
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type == "pause")
				{
					string tag5 = GetMovingBlockTagLine(text8);
					IMovingBlockSet movingBlockSet6 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag5);
					if (tag5 == null)
					{
						List<Point3> value20;
						if (!FindWaitMoveSet(text8, out tag5, out value20))
						{
							goto IL_1082;
						}
						movingBlockSet6 = WaitMoveSetTurnToWork(tag5, value20);
					}
					try
					{
						movingBlockSet6.Stop();
						m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet6);
						m_subsystemMovingBlocks.AddMovingBlockSet(movingBlockSet6.Position, movingBlockSet6.Position, 0f, 0f, 0f, new Vector2(1f, 1f), movingBlockSet6.Blocks, "moveset", tag5, true);
					}
					catch
					{
						ShowSubmitTips("名为" + text8 + "的运动方块移除失败，发生未知错误");
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type == "stop")
				{
					string tag6 = GetMovingBlockTagLine(text8);
					IMovingBlockSet movingBlockSet7 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag6);
					if (tag6 == null)
					{
						List<Point3> value21;
						if (FindWaitMoveSet(text8, out tag6, out value21))
						{
							return SubmitResult.Success;
						}
						goto IL_1082;
					}
					try
					{
						movingBlockSet7.Stop();
						m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet7);
						m_waitingMoveSets[tag6] = new List<Point3>();
						foreach (MovingBlock block6 in movingBlockSet7.Blocks)
						{
							Point3 item = new Point3((int)MathUtils.Round(movingBlockSet7.Position.X), (int)MathUtils.Round(movingBlockSet7.Position.Y), (int)MathUtils.Round(movingBlockSet7.Position.Z)) + block6.Offset;
							m_waitingMoveSets[tag6].Add(item);
							m_subsystemTerrain.ChangeCell(item.X, item.Y, item.Z, block6.Value, true, (ComponentMiner)null);
						}
						SetMoveEntityBlocks(movingBlockSet7);
					}
					catch
					{
						ShowSubmitTips("名为" + text8 + "的运动方块无法转为普通方块，发生未知错误");
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type == "remove")
				{
					string tag7 = GetMovingBlockTagLine(text8);
					IMovingBlockSet movingBlockSet8 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag7);
					if (tag7 == null)
					{
						List<Point3> value22;
						if (FindWaitMoveSet(text8, out tag7, out value22))
						{
							m_waitingMoveSets.Remove(tag7);
							return SubmitResult.Success;
						}
						goto IL_1082;
					}
					try
					{
						m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet8);
						foreach (MovingBlock block7 in movingBlockSet8.Blocks)
						{
							Point3 point23 = new Point3((int)MathUtils.Round(movingBlockSet8.Position.X), (int)MathUtils.Round(movingBlockSet8.Position.Y), (int)MathUtils.Round(movingBlockSet8.Position.Z)) + block7.Offset;
							m_subsystemTerrain.ChangeCell(point23.X, point23.Y, point23.Z, block7.Value, true, (ComponentMiner)null);
						}
						SetMoveEntityBlocks(movingBlockSet8);
					}
					catch
					{
						ShowSubmitTips("名为" + text8 + "的运动方块移除失败，发生未知错误");
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type == "removeall")
				{
					List<IMovingBlockSet> list13 = new List<IMovingBlockSet>();
					foreach (IMovingBlockSet movingBlockSet9 in m_subsystemMovingBlocks.MovingBlockSets)
					{
						list13.Add(movingBlockSet9);
					}
					foreach (IMovingBlockSet item7 in list13)
					{
						if (item7.Id == "moveset")
						{
							m_subsystemMovingBlocks.RemoveMovingBlockSet(item7);
							foreach (MovingBlock block8 in item7.Blocks)
							{
								Point3 point24 = new Point3((int)MathUtils.Round(item7.Position.X), (int)MathUtils.Round(item7.Position.Y), (int)MathUtils.Round(item7.Position.Z)) + block8.Offset;
								m_subsystemTerrain.ChangeCell(point24.X, point24.Y, point24.Z, block8.Value, true, (ComponentMiner)null);
							}
							SetMoveEntityBlocks(item7);
						}
					}
					m_waitingMoveSets.Clear();
				}
				return SubmitResult.Success;
				IL_1082:
				ShowEditedTips("提示:名为" + text8 + "的运动方块设计不存在");
				return SubmitResult.Fail;
			});
			AddFunction("furniture", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint22 = GetOnePoint("pos", commandData);
					CellFace start = new CellFace(onePoint22.X, onePoint22.Y, onePoint22.Z, 4);
					m_subsystemFurnitureBlockBehavior.ScanDesign(start, Vector3.Zero, m_componentPlayer.ComponentMiner);
				}
				else if (commandData.Type == "hammer")
				{
					Point3[] twoPoint20 = GetTwoPoint("pos1", "pos2", commandData);
					Point3 pos4 = GetOnePoint("pos3", commandData);
					int num102 = (int)commandData.GetValue("v1");
					int num103 = (int)commandData.GetValue("v2");
					CubeArea cubeArea21 = new CubeArea(twoPoint20[0], twoPoint20[1]);
					int division = num102;
					int rotate = num103;
					cubeArea21.Ergodic(division, 0.1f, delegate(Point3 origin, Point3 coord, Point3 finalCoord)
					{
						List<int> list14 = new List<int>();
						int num104 = 0;
						bool flag26 = false;
						for (int num105 = 0; num105 < division; num105++)
						{
							for (int num106 = 0; num106 < division; num106++)
							{
								for (int num107 = 0; num107 < division; num107++)
								{
									int limitValue4 = GetLimitValue(origin.X + num107, origin.Y + num106, origin.Z + num105);
									list14.Add(limitValue4);
									flag26 = flag26 || limitValue4 != 0;
									num104++;
								}
							}
						}
						Point3 point25 = new Point3(pos4.X + coord.X, pos4.Y + coord.Y, pos4.Z + coord.Z);
						switch (rotate)
						{
						case 1:
							point25 = new Point3(pos4.X - coord.Z, pos4.Y + coord.Y, pos4.Z + coord.X);
							break;
						case 2:
							point25 = new Point3(pos4.X - coord.X, pos4.Y + coord.Y, pos4.Z - coord.Z);
							break;
						case 3:
							point25 = new Point3(pos4.X + coord.Z, pos4.Y + coord.Y, pos4.Z - coord.X);
							break;
						}
						if (flag26)
						{
							try
							{
								FurnitureDesign furnitureDesign = new FurnitureDesign(m_subsystemTerrain);
								furnitureDesign.SetValues(division, list14.ToArray());
								furnitureDesign.Rotate(1, rotate);
								FurnitureDesign furnitureDesign2 = m_subsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign);
								int num108 = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter));
								m_subsystemPickables.AddPickable(num108, 1, new Vector3(commandData.Position) + new Vector3(0.5f, 1f, 0.5f), null, null);
								m_subsystemTerrain.ChangeCell(point25.X, point25.Y, point25.Z, num108, true, (ComponentMiner)null);
								return;
							}
							catch
							{
								ShowSubmitTips(string.Format("处理区域({0})-({1})时发生未知错误", origin.ToString(), (origin + new Point3(division)).ToString()));
								m_subsystemTerrain.ChangeCell(point25.X, point25.Y, point25.Z, 0, true, (ComponentMiner)null);
								return;
							}
						}
						m_subsystemTerrain.ChangeCell(point25.X, point25.Y, point25.Z, 0, true, (ComponentMiner)null);
					});
				}
				else if (commandData.Type == "slotreduct")
				{
					int index3 = (int)commandData.GetValue("fid");
					Point3 onePoint23 = GetOnePoint("pos", commandData);
					FurnitureDesign design = m_subsystemFurnitureBlockBehavior.GetDesign(index3);
					if (design == null)
					{
						ShowSubmitTips("找不到对应家具");
						return SubmitResult.Fail;
					}
					int num109 = 0;
					for (int num110 = 0; num110 < design.Resolution; num110++)
					{
						for (int num111 = 0; num111 < design.Resolution; num111++)
						{
							for (int num112 = 0; num112 < design.Resolution; num112++)
							{
								m_subsystemTerrain.ChangeCell(onePoint23.X - num110, onePoint23.Y + num111, onePoint23.Z + num112, design.m_values[num109++], true, (ComponentMiner)null);
							}
						}
					}
				}
				else if (commandData.Type == "posreduct")
				{
					Point3[] twoPoint21 = GetTwoPoint("pos1", "pos2", commandData);
					Point3 pos5 = GetOnePoint("pos3", commandData);
					CubeArea cubeArea22 = new CubeArea(twoPoint21[0], twoPoint21[1]);
					cubeArea22.Ergodic(delegate
					{
						int cellValue8 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea22.Current.X, cubeArea22.Current.Y, cubeArea22.Current.Z);
						if (Terrain.ExtractContents(cellValue8) == 227)
						{
							int data3 = Terrain.ExtractData(cellValue8);
							int designIndex = FurnitureBlock.GetDesignIndex(data3);
							int rotation = FurnitureBlock.GetRotation(data3);
							FurnitureDesign furnitureDesign3 = m_subsystemFurnitureBlockBehavior.GetDesign(designIndex).Clone();
							furnitureDesign3.Rotate(1, rotation);
							if (rotation == 1 || rotation == 3)
							{
								furnitureDesign3.Mirror(0);
							}
							int num113 = 0;
							Point3 point26 = (cubeArea22.Current - cubeArea22.MinPoint) * furnitureDesign3.Resolution + pos5;
							Point3 point27 = Point3.Zero;
							for (int num114 = 0; num114 < furnitureDesign3.Resolution; num114++)
							{
								for (int num115 = 0; num115 < furnitureDesign3.Resolution; num115++)
								{
									for (int num116 = 0; num116 < furnitureDesign3.Resolution; num116++)
									{
										switch (rotation)
										{
										case 0:
											point27 = new Point3(num116, num115, num114);
											break;
										case 1:
											point27 = new Point3(furnitureDesign3.Resolution - num116, num115, num114);
											break;
										case 2:
											point27 = new Point3(num116, num115, num114);
											break;
										case 3:
											point27 = new Point3(furnitureDesign3.Resolution - num116, num115, num114);
											break;
										}
										m_subsystemTerrain.ChangeCell(point26.X + point27.X, point26.Y + point27.Y, point26.Z + point27.Z, furnitureDesign3.m_values[num113++], true, (ComponentMiner)null);
									}
								}
							}
						}
						return false;
					});
				}
				else if (commandData.Type == "replace")
				{
					Point3[] twoPoint22 = GetTwoPoint("pos1", "pos2", commandData);
					int id12 = (int)commandData.GetValue("id1");
					int id13 = (int)commandData.GetValue("id2");
					CubeArea cubeArea23 = new CubeArea(twoPoint22[0], twoPoint22[1]);
					cubeArea23.Ergodic(delegate
					{
						int cellValue9 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea23.Current.X, cubeArea23.Current.Y, cubeArea23.Current.Z);
						int num117 = Terrain.ExtractContents(cellValue9);
						int data4 = Terrain.ExtractData(cellValue9);
						if (num117 == 227)
						{
							int designIndex2 = FurnitureBlock.GetDesignIndex(data4);
							FurnitureDesign design2 = m_subsystemFurnitureBlockBehavior.GetDesign(designIndex2);
							if (design2 != null)
							{
								List<FurnitureDesign> list15 = design2.CloneChain();
								foreach (FurnitureDesign item8 in list15)
								{
									int[] array7 = new int[design2.m_values.Length];
									for (int num118 = 0; num118 < design2.m_values.Length; num118++)
									{
										int num119 = Terrain.ReplaceLight(design2.m_values[num118], 0);
										if (num119 == id12)
										{
											array7[num118] = id13;
										}
										else
										{
											array7[num118] = design2.m_values[num118];
										}
									}
									item8.SetValues(design2.m_resolution, array7);
									FurnitureDesign furnitureDesign4 = m_subsystemFurnitureBlockBehavior.TryAddDesignChain(list15[0], true);
									if (furnitureDesign4 != null)
									{
										int data5 = FurnitureBlock.SetDesignIndex(data4, furnitureDesign4.Index, furnitureDesign4.ShadowStrengthFactor, furnitureDesign4.IsLightEmitter);
										int num120 = Terrain.ReplaceData(cellValue9, data5);
										m_subsystemTerrain.ChangeCell(cubeArea23.Current.X, cubeArea23.Current.Y, cubeArea23.Current.Z, num120, true, (ComponentMiner)null);
									}
								}
							}
						}
						return false;
					});
				}
				else if (commandData.Type == "link+x" || commandData.Type == "wire+x")
				{
					Point3 onePoint24 = GetOnePoint("pos", commandData);
					bool flag27 = (bool)commandData.GetValue("con");
					bool flag28 = commandData.Type == "wire+x";
					List<int> list16 = new List<int>();
					List<FurnitureDesign> list17 = new List<FurnitureDesign>();
					int num121 = 0;
					while (num121 < 1024)
					{
						int cellValue10 = m_subsystemTerrain.Terrain.GetCellValue(onePoint24.X + num121++, onePoint24.Y, onePoint24.Z);
						if (Terrain.ExtractContents(cellValue10) != 227)
						{
							break;
						}
						list16.Add(FurnitureBlock.GetDesignIndex(Terrain.ExtractData(cellValue10)));
					}
					if (list16.Count < 2)
					{
						ShowSubmitTips("家具数量少于2，无法链接");
						return SubmitResult.Fail;
					}
					foreach (int item9 in list16)
					{
						FurnitureDesign design3 = m_subsystemFurnitureBlockBehavior.GetDesign(item9);
						if (design3 != null)
						{
							if (flag27 && design3.LinkedDesign != null)
							{
								foreach (FurnitureDesign item10 in design3.ListChain())
								{
									list17.Add(item10.Clone());
								}
							}
							else
							{
								list17.Add(design3.Clone());
							}
						}
					}
					for (int num122 = 0; num122 < list17.Count; num122++)
					{
						list17[num122].InteractionMode = ((!flag28) ? FurnitureInteractionMode.Multistate : FurnitureInteractionMode.ConnectedMultistate);
						list17[num122].LinkedDesign = list17[(num122 + 1) % list17.Count];
					}
					FurnitureDesign furnitureDesign5 = m_subsystemFurnitureBlockBehavior.TryAddDesignChain(list17[0], true);
					if (furnitureDesign5 != null)
					{
						int value23 = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign5.Index, furnitureDesign5.ShadowStrengthFactor, furnitureDesign5.IsLightEmitter));
						Vector3 vector9 = ((commandData.Position == Point3.Zero) ? m_componentPlayer.ComponentBody.Position : new Vector3(commandData.Position));
						m_subsystemPickables.AddPickable(value23, 1, vector9 + new Vector3(0.5f, 1f, 0.5f), null, null);
					}
				}
				else if (commandData.Type == "find")
				{
					int fid = (int)commandData.GetValue("fid");
					SubsystemSpawn subsystemSpawn = base.Project.FindSubsystem<SubsystemSpawn>();
					Point2[] chunkPoints = subsystemSpawn.m_chunks.Keys.ToArray();
					int l2 = 0;
					float num123 = 0f;
					List<Point3> points = new List<Point3>();
					foreach (Point2 key4 in subsystemSpawn.m_chunks.Keys)
					{
						Time.QueueTimeDelayedExecution(Time.RealTime + (double)num123, delegate
						{
							int num124 = chunkPoints[l2].X * 16;
							int num125 = chunkPoints[l2].Y * 16;
							m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
							CommandCamera commandCamera = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
							commandCamera.m_position = new Vector3(num124, commandCamera.m_position.Y, num125);
							ShowSubmitTips(string.Format("正在扫描区块({0})-({1}),进度:{2}/{3}", new Point3(num124, 0, num125).ToString(), new Point3(num124 + 16, 255, num125 + 16).ToString(), l2, chunkPoints.Length - 1));
						});
						Time.QueueTimeDelayedExecution(Time.RealTime + (double)num123 + 0.5, delegate
						{
							TerrainChunk chunkAtCoords3 = m_subsystemTerrain.Terrain.GetChunkAtCoords(chunkPoints[l2].X, chunkPoints[l2].Y);
							for (int num126 = 0; num126 < 16; num126++)
							{
								for (int num127 = 0; num127 < 16; num127++)
								{
									for (int num128 = 0; num128 < 256; num128++)
									{
										int x12 = chunkAtCoords3.Origin.X + num126;
										int y6 = num128;
										int z6 = chunkAtCoords3.Origin.Y + num127;
										int cellContents3 = m_subsystemTerrain.Terrain.GetCellContents(x12, y6, z6);
										if (cellContents3 == 227)
										{
											int cellValue11 = m_subsystemTerrain.Terrain.GetCellValue(x12, y6, z6);
											if (fid == FurnitureBlock.GetDesignIndex(Terrain.ExtractData(cellValue11)))
											{
												points.Add(new Point3(x12, y6, z6));
												ShowSubmitTips(string.Format("点({0})发现目标家具!", new Point3(x12, y6, z6).ToString()));
											}
										}
									}
								}
							}
							if (l2 == chunkPoints.Length - 1)
							{
								string text11 = "查找完毕,";
								if (points.Count != 0)
								{
									text11 += "查找结果已复制到剪切板:\n";
									foreach (Point3 item11 in points)
									{
										text11 = text11 + item11.ToString() + "  ";
									}
									ClipboardManager.ClipboardString = text11;
								}
								else
								{
									text11 += "未找到目标家具";
								}
								m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
								ShowSubmitTips(text11);
							}
							l2++;
						});
						num123 += 0.6f;
					}
				}
				else if (commandData.Type == "remove")
				{
					for (int num129 = 0; num129 < m_subsystemFurnitureBlockBehavior.m_furnitureDesigns.Length; num129++)
					{
						FurnitureDesign furnitureDesign6 = m_subsystemFurnitureBlockBehavior.m_furnitureDesigns[num129];
						if (furnitureDesign6 != null && furnitureDesign6.FurnitureSet == null)
						{
							m_subsystemFurnitureBlockBehavior.m_furnitureDesigns[num129] = null;
						}
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("camera", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint25 = GetOnePoint("pos", commandData);
					Point2 eyes = (Point2)commandData.GetValue("eyes");
					bool flag29 = (bool)commandData.GetValue("con");
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, (!flag29) ? CommandCamera.CameraType.Aero : CommandCamera.CameraType.Lock);
					CommandCamera commandCamera2 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
					commandCamera2.m_position = new Vector3(onePoint25);
					commandCamera2.m_direction = DataHandle.EyesToDirection(eyes);
				}
				else if (commandData.Type == "lock")
				{
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Lock);
				}
				else if (commandData.Type == "aero")
				{
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
				}
				else if (commandData.Type == "move")
				{
					Vector3 vector10 = (Vector3)commandData.GetValue("vec3");
					int num130 = (int)commandData.GetValue("v");
					bool flag30 = (bool)commandData.GetValue("con");
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MovePos);
					CommandCamera commandCamera3 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
					commandCamera3.m_targetPosition = vector10 + commandCamera3.m_position;
					commandCamera3.m_speed = (float)num130 / 10f;
					commandCamera3.m_skipToAero = !flag30;
				}
				else if (commandData.Type == "direct")
				{
					Point2 eyes2 = (Point2)commandData.GetValue("eyes");
					int num131 = (int)commandData.GetValue("v");
					bool flag31 = (bool)commandData.GetValue("con");
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MoveDirect);
					CommandCamera commandCamera4 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
					commandCamera4.m_targetDirection = DataHandle.EyesToDirection(eyes2);
					commandCamera4.m_speed = (float)num131 / 10f;
					commandCamera4.m_skipToAero = !flag31;
				}
				else if (commandData.Type.StartsWith("byo"))
				{
					switch (commandData.Type)
					{
					case "byo-fpp":
						m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
						break;
					case "byo-tpp":
						m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<TppCamera>();
						break;
					case "byo-orbit":
						m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<OrbitCamera>();
						break;
					case "byo-fixed":
						m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FixedCamera>();
						break;
					}
				}
				else if (commandData.Type == "relative")
				{
					Vector3 relativePosition = (Vector3)commandData.GetValue("vec3");
					Vector2 vector11 = (Vector2)commandData.GetValue("vec2");
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MoveWithPlayer);
					CommandCamera commandCamera5 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
					commandCamera5.m_relativePosition = relativePosition;
					commandCamera5.m_relativeAngle = new Point2((int)vector11.X, (int)vector11.Y);
				}
				else if (commandData.Type == "record")
				{
					RecordManager.Recording = true;
					RecordManager.Replaying = false;
					RecordManager.FirstTime = (float)m_subsystemTime.GameTime;
					RecordManager.ReplayTime = 0f;
					RecordManager.ActionIndex = 0;
					RecordManager.StatsIndex = 0;
					RecordManager.FirstPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
					RecordManager.FirstDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
					RecordManager.RecordPlayerActions.Clear();
					RecordManager.RecordPlayerStats.Clear();
					RecordManager.ChangeBlocks.Clear();
				}
				else if (commandData.Type == "replay")
				{
					RecordManager.Recording = false;
					RecordManager.Replaying = true;
					foreach (Point3 key5 in RecordManager.ChangeBlocks.Keys)
					{
						m_subsystemTerrain.ChangeCell(key5.X, key5.Y, key5.Z, RecordManager.ChangeBlocks[key5], true, (ComponentMiner)null);
					}
					foreach (Pickable pickable7 in m_subsystemPickables.Pickables)
					{
						pickable7.ToRemove = true;
					}
					m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
					CommandCamera commandCamera6 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
					commandCamera6.m_position = RecordManager.FirstPosition;
					commandCamera6.m_direction = RecordManager.FirstDirection;
				}
				return SubmitResult.Success;
			});
			AddFunction("gametime", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					SubsystemTimeOfDay subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>();
					int num132 = (int)(subsystemTimeOfDay.TimeOfDay * 4096f);
					ShowSubmitTips(string.Format("当前游戏时间{0}/{1}", num132, 4096));
				}
				else if (commandData.Type.StartsWith("byo"))
				{
					SubsystemTimeOfDay subsystemTimeOfDay2 = base.Project.FindSubsystem<SubsystemTimeOfDay>();
					int num133 = 0;
					switch (commandData.Type)
					{
					case "byo-dawn":
						num133 = 1;
						break;
					case "byo-noon":
						num133 = 2;
						break;
					case "byo-dusk":
						num133 = 3;
						break;
					case "byo-night":
						num133 = 4;
						break;
					}
					subsystemTimeOfDay2.TimeOfDayOffset += MathUtils.Remainder(MathUtils.Remainder(0.25f * (float)num133, 1f) - subsystemTimeOfDay2.TimeOfDay, 1f);
				}
				else if (commandData.Type == "accelerate")
				{
					int num134 = (int)commandData.GetValue("v");
					if (num134 == 10)
					{
						num134 = 255;
					}
					base.Project.FindSubsystem<SubsystemTime>().GameTimeFactor = num134;
				}
				else if (commandData.Type == "slow")
				{
					int num135 = (int)commandData.GetValue("v");
					if (num135 == 10)
					{
						num135 = 1000;
					}
					base.Project.FindSubsystem<SubsystemTime>().GameTimeFactor = 1f / (float)num135;
				}
				return SubmitResult.Success;
			});
			AddFunction("gamemode", delegate(CommandData commandData)
			{
				GameMode gameMode = GameMode.Creative;
				if (commandData.Type != "cruel")
				{
					switch (commandData.Type)
					{
					case "default":
						gameMode = m_gameMode;
						break;
					case "creative":
						gameMode = GameMode.Creative;
						break;
					case "harmless":
						gameMode = GameMode.Harmless;
						break;
					case "challenge":
						gameMode = GameMode.Challenging;
						break;
					case "adventure":
						gameMode = GameMode.Adventure;
						break;
					}
				}
				else
				{
					string text12 = (string)commandData.GetValue("text");
					if (!(text12 == "LEURC"))
					{
						ShowSubmitTips("密码错误，请重新输入");
						return SubmitResult.Fail;
					}
					gameMode = GameMode.Cruel;
				}
				m_subsystemGameInfo.WorldSettings.GameMode = gameMode;
				WorldInfo worldInfo = GameManager.WorldInfo;
				GameManager.SaveProject(true, true);
				GameManager.DisposeProject();
				ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
				return SubmitResult.Success;
			});
			AddFunction("settings", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					ScreensManager.SwitchScreen("Settings");
				}
				else if (commandData.Type == "visibility")
				{
					int num136 = (int)commandData.GetValue("v");
					if (num136 < 0)
					{
						ShowSubmitTips("游戏视距不能小于0");
						return SubmitResult.Fail;
					}
					SettingsManager.VisibilityRange = num136;
				}
				else if (commandData.Type == "brightness")
				{
					int num137 = (int)commandData.GetValue("v");
					SettingsManager.Brightness = (float)num137 / 100f;
				}
				else if (commandData.Type == "skymode")
				{
					string text13 = (string)commandData.GetValue("opt");
					switch (text13)
					{
					case "full":
						SettingsManager.SkyRenderingMode = SkyRenderingMode.Full;
						break;
					case "nocloud":
						SettingsManager.SkyRenderingMode = SkyRenderingMode.NoClouds;
						break;
					case "disable":
						SettingsManager.SkyRenderingMode = SkyRenderingMode.Disabled;
						break;
					default:
						ShowSubmitTips("指令settings类型skymode找不到天空模式：" + text13);
						return SubmitResult.Fail;
					}
				}
				else if (commandData.Type.StartsWith("vol-") || commandData.Type.StartsWith("sen-"))
				{
					int num138 = (int)commandData.GetValue("v");
					switch (commandData.Type)
					{
					case "vol-sound":
						SettingsManager.SoundsVolume = (float)num138 / 10f;
						break;
					case "vol-music":
						SettingsManager.MusicVolume = (float)num138 / 10f;
						break;
					case "sen-move":
						SettingsManager.MoveSensitivity = (float)num138 / 10f;
						break;
					case "sen-look":
						SettingsManager.LookSensitivity = (float)num138 / 10f;
						break;
					}
				}
				else if (commandData.Type == "advsurvive")
				{
					bool areAdventureSurvivalMechanicsEnabled = (bool)commandData.GetValue("con");
					m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled = areAdventureSurvivalMechanicsEnabled;
				}
				else if (commandData.Type == "livingmode")
				{
					bool flag32 = (bool)commandData.GetValue("con");
					m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode = ((!flag32) ? EnvironmentBehaviorMode.Static : EnvironmentBehaviorMode.Living);
				}
				else if (commandData.Type == "weathereffect")
				{
					bool areWeatherEffectsEnabled = (bool)commandData.GetValue("con");
					m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled = areWeatherEffectsEnabled;
				}
				else if (commandData.Type == "daymode")
				{
					string text14 = (string)commandData.GetValue("opt");
					TimeOfDayMode timeOfDayMode = TimeOfDayMode.Day;
					switch (text14)
					{
					case "day":
						timeOfDayMode = TimeOfDayMode.Day;
						break;
					case "night":
						timeOfDayMode = TimeOfDayMode.Night;
						break;
					case "change":
						timeOfDayMode = TimeOfDayMode.Changing;
						break;
					case "sunrise":
						timeOfDayMode = TimeOfDayMode.Sunrise;
						break;
					case "sunset":
						timeOfDayMode = TimeOfDayMode.Sunset;
						break;
					default:
						ShowSubmitTips("指令settings类型daymode找不到时间模式：" + text14);
						return SubmitResult.Fail;
					}
					m_subsystemGameInfo.WorldSettings.TimeOfDayMode = timeOfDayMode;
				}
				return SubmitResult.Success;
			});
			AddFunction("shapeshifter", delegate(CommandData commandData)
			{
				bool con5 = (bool)commandData.GetValue("con");
				m_shapeshifter = con5;
				string target4 = (con5 ? "Wolf_Gray" : "Werewolf");
				string name2 = (con5 ? "Werewolf" : "Wolf_Gray");
				ErgodicBody(target4, delegate(ComponentBody body)
				{
					ComponentShapeshifter componentShapeshifter = body.Entity.FindComponent<ComponentShapeshifter>();
					if (componentShapeshifter != null)
					{
						componentShapeshifter.IsEnabled = true;
						componentShapeshifter.ShapeshiftTo(name2);
					}
					return false;
				});
				Time.QueueTimeDelayedExecution(Time.RealTime + 5.0, delegate
				{
					ErgodicBody(name2, delegate(ComponentBody body)
					{
						ComponentShapeshifter componentShapeshifter2 = body.Entity.FindComponent<ComponentShapeshifter>();
						if (componentShapeshifter2 != null)
						{
							componentShapeshifter2.IsEnabled = !con5;
						}
						return false;
					});
				});
				return SubmitResult.Success;
			});
			AddFunction("lockscreen", delegate(CommandData commandData)
			{
				bool flag33 = (bool)commandData.GetValue("con");
				m_componentPlayer.ComponentLocomotion.LookSpeed = (flag33 ? 8E-08f : 8f);
				m_componentPlayer.ComponentLocomotion.TurnSpeed = (flag33 ? 8E-08f : 8f);
				return SubmitResult.Success;
			});
			AddFunction("deathscreen", delegate(CommandData commandData)
			{
				if ((bool)commandData.GetValue("con"))
				{
					m_enterDeathScreen = true;
					m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<DeathCamera>();
				}
				else
				{
					m_enterDeathScreen = false;
					m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				}
				return SubmitResult.Success;
			});
			AddFunction("blockfirm", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					bool flag34 = (bool)commandData.GetValue("con");
					m_firmAllBlocks = flag34;
					if (flag34)
					{
						SubsystemCommandExt.BlockDataChange = true;
						for (int num139 = 1; num139 < BlocksManager.Blocks.Length; num139++)
						{
							try
							{
								Block block3 = BlocksManager.Blocks[num139];
								block3.DigResilience = float.PositiveInfinity;
								block3.ExplosionResilience = float.PositiveInfinity;
								block3.ProjectileResilience = float.PositiveInfinity;
								block3.FireDuration = 0f;
								block3.DefaultDropCount = 0f;
								block3.DefaultExperienceCount = 0f;
							}
							catch
							{
							}
						}
					}
					else
					{
						SubsystemCommandExt.BlockDataChange = false;
						foreach (ModEntity mod in ModsManager.ModList)
						{
							mod.LoadBlocksData();
						}
					}
				}
				else if (commandData.Type == "limit")
				{
					int value24 = (int)commandData.GetValue("id");
					bool flag35 = (bool)commandData.GetValue("con");
					value24 = Terrain.ExtractContents(value24);
					if (flag35)
					{
						SetFirmBlocks(value24, true, null);
						if (!m_firmBlockList.Contains(value24))
						{
							m_firmBlockList.Add(value24);
						}
					}
					else
					{
						float[] value25;
						if (OriginFirmBlockList.TryGetValue(value24, out value25))
						{
							SetFirmBlocks(value24, false, value25);
						}
						if (m_firmBlockList.Contains(value24))
						{
							m_firmBlockList.Remove(value24);
						}
					}
				}
				else if (commandData.Type == "nosqueeze")
				{
					bool flag36 = (bool)commandData.GetValue("con");
				}
				return SubmitResult.Success;
			});
			AddFunction("getcell", delegate(CommandData commandData)
			{
				Point3[] twoPoint23 = GetTwoPoint("pos1", "pos2", commandData);
				string f = (string)commandData.GetValue("f");
				bool flag37 = commandData.Type == "default";
				bool isGlobal = commandData.Type == "global";
				bool flag38 = commandData.Type == "chunk";
				CubeArea cube8 = new CubeArea(twoPoint23[0], twoPoint23[1]);
				Stream stream = GetCommandFileStream(f, OpenFileMode.CreateOrOpen);
				StreamWriter streamwriter = new StreamWriter(stream);
				if (flag37 || isGlobal)
				{
					cube8.Ergodic(delegate
					{
						int num140 = (isGlobal ? cube8.Current.X : (cube8.Current.X - cube8.MinPoint.X));
						int num141 = (isGlobal ? cube8.Current.Y : (cube8.Current.Y - cube8.MinPoint.Y));
						int num142 = (isGlobal ? cube8.Current.Z : (cube8.Current.Z - cube8.MinPoint.Z));
						int cellValue12 = m_subsystemTerrain.Terrain.GetCellValue(cube8.Current.X, cube8.Current.Y, cube8.Current.Z);
						if (Terrain.ExtractContents(cellValue12) != 0)
						{
							string value26 = string.Format("{0},{1},{2},{3}", num140, num141, num142, cellValue12);
							streamwriter.WriteLine(value26);
						}
						return false;
					});
					streamwriter.Flush();
					stream.Dispose();
					ShowSubmitTips("方块文件已生成，路径：\n" + DataHandle.GetCommandResPathName(f));
				}
				else if (flag38)
				{
					cube8.ErgodicByChunk(3f, 0.1f, delegate(Point3 origin, Point2 coord, Point2 finalCoord)
					{
						m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
						CommandCamera commandCamera7 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
						commandCamera7.m_position = new Vector3(origin) + new Vector3(0f, 100f, 0f);
						if (coord.X == -1 && coord.Y == -1)
						{
							ShowSubmitTips("方块文件正在生成，请耐心等候");
						}
						else
						{
							string value27 = string.Format("#CHUNK:{0},{1}", coord.X, coord.Y);
							streamwriter.WriteLine(value27);
							for (int num143 = 0; num143 < 16; num143++)
							{
								for (int num144 = 0; num144 < cube8.LengthY; num144++)
								{
									for (int num145 = 0; num145 < 16; num145++)
									{
										Point3 point28 = new Point3(origin.X + num143, origin.Y + num144, origin.Z + num145);
										int cellValue13 = m_subsystemTerrain.Terrain.GetCellValue(point28.X, point28.Y, point28.Z);
										if (Terrain.ExtractContents(cellValue13) != 0)
										{
											string value28 = string.Format("{0},{1},{2},{3}", point28.X, point28.Y, point28.Z, cellValue13);
											streamwriter.WriteLine(value28);
										}
									}
								}
							}
							streamwriter.WriteLine("###\n");
							ShowSubmitTips(string.Format("区块({0})-({1})的方块信息已生成完毕", origin.ToString(), (origin + new Point3(16, cube8.LengthY, 16)).ToString()));
							if (coord.X == finalCoord.X && coord.Y == finalCoord.Y)
							{
								streamwriter.Flush();
								stream.Dispose();
								m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
								ShowSubmitTips("方块文件已生成，路径：\n" + DataHandle.GetCommandResPathName(f));
							}
						}
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("memorydata", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint26 = GetOnePoint("pos", commandData);
					string data6 = (string)commandData.GetValue("text");
					SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>(true);
					SubsystemEditableItemBehavior<MemoryBankData> subsystemEditableItemBehavior = base.Project.FindSubsystem<SubsystemEditableItemBehavior<MemoryBankData>>(true);
					int cellValue14 = m_subsystemTerrain.Terrain.GetCellValue(onePoint26.X, onePoint26.Y, onePoint26.Z);
					if (Terrain.ExtractContents(cellValue14) == 186)
					{
						MemoryBankData memoryBankData2 = subsystemMemoryBankBlockBehavior.GetBlockData(onePoint26);
						if (memoryBankData2 == null)
						{
							memoryBankData2 = new MemoryBankData();
							subsystemEditableItemBehavior.SetBlockData(onePoint26, memoryBankData2);
						}
						memoryBankData2.LoadString(data6);
						memoryBankData2.SaveString();
					}
				}
				else if (commandData.Type == "imagemul" || commandData.Type == "imagefour")
				{
					string f2 = (string)commandData.GetValue("f1");
					string f3 = (string)commandData.GetValue("f2");
					Stream commandFileStream = GetCommandFileStream(f2, OpenFileMode.ReadWrite);
					if (commandFileStream == null)
					{
						return SubmitResult.Fail;
					}
					if (!Png.IsPngStream(commandFileStream))
					{
						ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
					}
					Image image = Image.Load(commandFileStream);
					string text15 = string.Empty;
					if (commandData.Type == "imagemul")
					{
						for (int num146 = 0; num146 < image.Height; num146++)
						{
							for (int num147 = 0; num147 < image.Width; num147++)
							{
								Color pixel = image.GetPixel(num147, num146);
								byte r = pixel.R;
								byte g = pixel.G;
								byte b = pixel.B;
								if (pixel.A < 20)
								{
									text15 += 0;
								}
								else if (pixel.R == 111 && pixel.G == 111 && pixel.B == 111)
								{
									text15 += 1;
								}
								else
								{
									int n2 = DataHandle.GetColorIndex(pixel, 1) + 7;
									text15 += DataHandle.NumberToSignal(n2);
								}
							}
							text15 += "\n";
						}
					}
					else
					{
						for (int num148 = 0; num148 < image.Height; num148 += 2)
						{
							for (int num149 = 0; num149 < image.Width; num149 += 2)
							{
								int[] array8 = new int[4];
								for (int num150 = 0; num150 < 4; num150++)
								{
									Color c = Color.Black;
									switch (num150)
									{
									case 0:
										c = image.GetPixel(num149, num148);
										break;
									case 1:
										c = image.GetPixel(num149 + 1, num148);
										break;
									case 2:
										c = image.GetPixel(num149, num148 + 1);
										break;
									case 3:
										c = image.GetPixel(num149 + 1, num148 + 1);
										break;
									}
									if ((c.R == 111 && c.G == 111 && c.B == 111) || c.A < 20)
									{
										array8[num150] = 0;
									}
									else
									{
										array8[num150] = ((DataHandle.GetColorIndex(c, 1) != 0) ? 1 : 0);
									}
								}
								int n3 = array8[0] + array8[1] * 2 + array8[2] * 4 + array8[3] * 8;
								text15 += DataHandle.NumberToSignal(n3);
							}
							text15 += "\n";
						}
					}
					text15 += "###\n";
					Stream commandFileStream2 = GetCommandFileStream(f3, OpenFileMode.CreateOrOpen);
					commandFileStream2.Position = commandFileStream2.Length;
					using (StreamWriter streamWriter = new StreamWriter(commandFileStream2))
					{
						streamWriter.WriteLine(text15);
						streamWriter.Flush();
					}
					commandFileStream.Dispose();
					commandFileStream2.Dispose();
				}
				else if (commandData.Type == "rank")
				{
					string f4 = (string)commandData.GetValue("f1");
					string f5 = (string)commandData.GetValue("f2");
					Stream commandFileStream3 = GetCommandFileStream(f4, OpenFileMode.ReadWrite);
					if (commandFileStream3 == null)
					{
						return SubmitResult.Fail;
					}
					int num151 = 0;
					int num152 = 0;
					string empty = string.Empty;
					StreamReader streamReader = new StreamReader(commandFileStream3);
					while ((empty = streamReader.ReadLine()) != null)
					{
						if (!(empty == "") && !(empty == " "))
						{
							if (num152 == 0)
							{
								num152 = empty.Length;
							}
							if (empty == "###")
							{
								break;
							}
							num151++;
						}
					}
					string[] array9 = new string[num151 * num152];
					int num153 = 0;
					commandFileStream3.Position = 0L;
					StreamReader streamReader2 = new StreamReader(commandFileStream3);
					while ((empty = streamReader2.ReadLine()) != null)
					{
						if (!(empty == "") && !(empty == " "))
						{
							if (empty == "###")
							{
								num153 = 0;
							}
							else
							{
								for (int num154 = 0; num154 < num152; num154++)
								{
									if (array9[num153 * num152 + num154] == null)
									{
										array9[num153 * num152 + num154] = "";
									}
									array9[num153 * num152 + num154] += empty[num154];
								}
								num153++;
							}
						}
					}
					string empty2 = string.Empty;
					Stream commandFileStream4 = GetCommandFileStream(f5, OpenFileMode.CreateOrOpen);
					StreamWriter streamWriter2 = new StreamWriter(commandFileStream4);
					for (int num155 = 0; num155 < array9.Length; num155++)
					{
						empty2 = array9[num155] + ((num155 % num152 == num152 - 1) ? "\n###" : "");
						streamWriter2.WriteLine(empty2);
					}
					streamWriter2.Flush();
					commandFileStream3.Dispose();
					commandFileStream4.Dispose();
				}
				else if (commandData.Type.StartsWith("load"))
				{
					Point3 onePoint27 = GetOnePoint("pos", commandData);
					int num156 = (int)commandData.GetValue("r");
					int num157 = (int)commandData.GetValue("c");
					string f6 = (string)commandData.GetValue("f");
					SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior2 = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>(true);
					SubsystemEditableItemBehavior<MemoryBankData> subsystemEditableItemBehavior2 = base.Project.FindSubsystem<SubsystemEditableItemBehavior<MemoryBankData>>(true);
					Stream commandFileStream5 = GetCommandFileStream(f6, OpenFileMode.ReadWrite);
					if (commandFileStream5 == null)
					{
						return SubmitResult.Fail;
					}
					StreamReader streamReader3 = new StreamReader(commandFileStream5);
					string empty3 = string.Empty;
					int num158 = 0;
					int num159 = onePoint27.X;
					int num160 = onePoint27.Y;
					int num161 = onePoint27.Z;
					while ((empty3 = streamReader3.ReadLine()) != null)
					{
						if (!(empty3 == "") && !(empty3 == " "))
						{
							if (empty3.StartsWith("###"))
							{
								switch (commandData.Type)
								{
								case "load+x-y":
									num159 = onePoint27.X;
									num160 -= num156;
									break;
								case "load-x-y":
									num159 = onePoint27.X;
									num160 -= num156;
									break;
								case "load+z-y":
									num161 = onePoint27.Z;
									num160 -= num156;
									break;
								case "load-z-y":
									num161 = onePoint27.Z;
									num160 -= num156;
									break;
								case "load+x+z":
									num159 = onePoint27.X;
									num161 += num156;
									break;
								case "load-x-z":
									num159 = onePoint27.X;
									num161 -= num156;
									break;
								}
							}
							else
							{
								int cellValue15 = m_subsystemTerrain.Terrain.GetCellValue(num159, num160, num161);
								if (Terrain.ExtractContents(cellValue15) == 186)
								{
									MemoryBankData memoryBankData3 = subsystemMemoryBankBlockBehavior2.GetBlockData(new Point3(num159, num160, num161));
									if (memoryBankData3 == null)
									{
										memoryBankData3 = new MemoryBankData();
										subsystemEditableItemBehavior2.SetBlockData(new Point3(num159, num160, num161), memoryBankData3);
									}
									memoryBankData3.LoadString(empty3);
									memoryBankData3.SaveString();
								}
								else
								{
									num158++;
								}
								switch (commandData.Type)
								{
								case "load+x-y":
									num159 += num157;
									break;
								case "load-x-y":
									num159 -= num157;
									break;
								case "load+z-y":
									num161 += num157;
									break;
								case "load-z-y":
									num161 -= num157;
									break;
								case "load+x+z":
									num159 += num157;
									break;
								case "load-x-z":
									num159 -= num157;
									break;
								}
							}
						}
					}
					if (num158 == 0)
					{
						ShowSubmitTips("M板数据已全部写入");
					}
					else
					{
						ShowSubmitTips(num158 + "个坐标位置不存在M板，请检查指令输入是否正确");
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("world", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text16 = (string)commandData.GetValue("f");
					WorldInfo worldInfo2 = null;
					foreach (WorldInfo worldInfo3 in WorldsManager.m_worldInfos)
					{
						if (text16 == worldInfo3.WorldSettings.Name)
						{
							worldInfo2 = worldInfo3;
							GameManager.SaveProject(true, true);
							GameManager.DisposeProject();
							ScreensManager.SwitchScreen("GameLoading", worldInfo2, null);
							return SubmitResult.Success;
						}
					}
					string text17 = Storage.CombinePaths(DataHandle.GetCommandPath(), text16);
					if (!Storage.DirectoryExists(text17))
					{
						ShowSubmitTips("找不到文件目录" + text17);
						return SubmitResult.Fail;
					}
					worldInfo2 = WorldsManager.GetWorldInfo(text17);
					GameManager.SaveProject(true, true);
					GameManager.DisposeProject();
					ScreensManager.SwitchScreen("GameLoading", worldInfo2, null);
				}
				else if (commandData.Type == "create")
				{
					string text18 = (string)commandData.GetValue("f");
					string text19 = Storage.CombinePaths(DataHandle.GetCommandPath(), text18);
					if (Storage.DirectoryExists(text19))
					{
						ShowSubmitTips("Command目录已存在该世界");
						return SubmitResult.Fail;
					}
					Storage.CreateDirectory(text19);
					WorldSettings worldSettings = GameManager.m_worldInfo.WorldSettings;
					worldSettings.Seed = text18.Replace("/", "$");
					int num162 = 0;
					int num163 = 1;
					string seed = worldSettings.Seed;
					foreach (char c2 in seed)
					{
						num162 += c2 * num163;
						num163 += 29;
					}
					ValuesDictionary valuesDictionary = new ValuesDictionary();
					worldSettings.Save(valuesDictionary, false);
					valuesDictionary.SetValue("WorldDirectoryName", text19);
					valuesDictionary.SetValue("WorldSeed", num162);
					ValuesDictionary valuesDictionary2 = new ValuesDictionary();
					valuesDictionary2.SetValue("Players", new ValuesDictionary());
					DatabaseObject databaseObject = DatabaseManager.GameDatabase.Database.FindDatabaseObject("GameProject", DatabaseManager.GameDatabase.ProjectTemplateType, true);
					XElement xElement = new XElement("Project");
					XmlUtils.SetAttributeValue(xElement, "Guid", databaseObject.Guid);
					XmlUtils.SetAttributeValue(xElement, "Name", "GameProject");
					XmlUtils.SetAttributeValue(xElement, "Version", VersionsManager.SerializationVersion);
					XElement xElement2 = new XElement("Subsystems");
					xElement.Add(xElement2);
					XElement xElement3 = new XElement("Values");
					XmlUtils.SetAttributeValue(xElement3, "Name", "GameInfo");
					valuesDictionary.Save(xElement3);
					xElement2.Add(xElement3);
					XElement xElement4 = new XElement("Values");
					XmlUtils.SetAttributeValue(xElement4, "Name", "Players");
					valuesDictionary2.Save(xElement4);
					xElement2.Add(xElement4);
					using (Stream stream2 = Storage.OpenFile(Storage.CombinePaths(text19, "Project.xml"), OpenFileMode.Create))
					{
						XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
					}
					ShowSubmitTips("已在Command目录创建世界:" + text18);
				}
				else if (commandData.Type == "remove")
				{
					string text20 = (string)commandData.GetValue("f");
					string text21 = Storage.CombinePaths(DataHandle.GetCommandPath(), text20);
					if (!Storage.DirectoryExists(text21))
					{
						ShowSubmitTips("找不到文件目录" + text21);
						return SubmitResult.Fail;
					}
					WorldsManager.DeleteWorld(text21);
					ShowSubmitTips("已删除Command目录中的世界:" + text20);
				}
				else if (commandData.Type == "unzip")
				{
					string text22 = (string)commandData.GetValue("f");
					string path = Storage.CombinePaths(GameManager.m_worldInfo.DirectoryName, text22);
					if (!Storage.FileExists(path))
					{
						ShowSubmitTips("当前存档中找不到子存档:" + text22);
						return SubmitResult.Fail;
					}
					Stream stream3 = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
					string text23 = text22.Replace(Storage.GetExtension(text22), "");
					string text24 = Storage.CombinePaths(DataHandle.GetCommandPath(), text23);
					if (Storage.DirectoryExists(text24))
					{
						ShowSubmitTips("Command目录已经存在世界:" + text23);
						return SubmitResult.Fail;
					}
					Storage.CreateDirectory(text24);
					WorldsManager.UnpackWorld(text24, stream3, true);
					stream3.Dispose();
					ShowSubmitTips("已成功在Command目录中创建世界:" + text23);
				}
				else if (commandData.Type == "delcurrent")
				{
					string directoryName = GameManager.m_worldInfo.DirectoryName;
					GameManager.SaveProject(true, true);
					GameManager.DisposeProject();
					WorldsManager.DeleteWorld(directoryName);
					ScreensManager.SwitchScreen("MainMenu");
				}
				else if (commandData.Type == "decipher")
				{
					string text25 = (string)commandData.GetValue("f");
					string path2 = Storage.CombinePaths(GameManager.m_worldInfo.DirectoryName, text25);
					if (!Storage.FileExists(path2))
					{
						ShowSubmitTips("当前存档中找不到:" + text25);
						return SubmitResult.Fail;
					}
					CommandData commandData3 = new CommandData(commandData.Position, commandData.Line);
					commandData3.Type = "unzip";
					commandData3.Data["f"] = text25;
					Submit("world", commandData3, false);
				}
				return SubmitResult.Success;
			});
			AddFunction("texture", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					base.Project.FindSubsystem<SubsystemBlocksTexture>().BlocksTexture = BlocksTexturesManager.DefaultBlocksTexture;
					UpdateAllChunks(0f, TerrainChunkState.InvalidLight);
				}
				else if (commandData.Type == "block")
				{
					string f7 = (string)commandData.GetValue("f");
					Stream commandFileStream6 = GetCommandFileStream(f7, OpenFileMode.ReadWrite);
					if (commandFileStream6 == null)
					{
						return SubmitResult.Fail;
					}
					if (!Png.IsPngStream(commandFileStream6))
					{
						ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
					}
					Texture2D texture2D = Texture2D.Load(commandFileStream6);
					if (!MathUtils.IsPowerOf2(texture2D.Width) || !MathUtils.IsPowerOf2(texture2D.Height))
					{
						ShowSubmitTips("材质图长和宽需为2的指数倍");
						return SubmitResult.Fail;
					}
					base.Project.FindSubsystem<SubsystemBlocksTexture>().BlocksTexture = texture2D;
					UpdateAllChunks(0f, TerrainChunkState.InvalidLight);
					commandFileStream6.Dispose();
				}
				else if (commandData.Type == "cmdcreature")
				{
					string text26 = (string)commandData.GetValue("obj");
					string text27 = (string)commandData.GetValue("f");
					Stream commandFileStream7 = GetCommandFileStream(text27, OpenFileMode.ReadWrite);
					if (commandFileStream7 == null)
					{
						return SubmitResult.Fail;
					}
					if (!Png.IsPngStream(commandFileStream7))
					{
						ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
					}
					Texture2D texture = Texture2D.Load(commandFileStream7);
					ErgodicBody(text26, delegate(ComponentBody body)
					{
						ComponentModel componentModel = body.Entity.FindComponent<ComponentModel>();
						if (componentModel != null)
						{
							componentModel.TextureOverride = texture;
						}
						return false;
					});
					commandFileStream7.Dispose();
					CreatureTextures[text26] = "$" + text27;
				}
				else if (commandData.Type == "pakcreature")
				{
					string text28 = (string)commandData.GetValue("obj");
					string text29 = (string)commandData.GetValue("opt");
					string text30 = Storage.CombinePaths("Textures/Creatures", text29);
					Texture2D texture2 = null;
					try
					{
						texture2 = ContentManager.Get<Texture2D>(text30);
					}
					catch
					{
						ShowSubmitTips("Content资源包不存在文件:" + text30);
						return SubmitResult.Fail;
					}
					ErgodicBody(text28, delegate(ComponentBody body)
					{
						ComponentModel componentModel2 = body.Entity.FindComponent<ComponentModel>();
						if (componentModel2 != null)
						{
							componentModel2.TextureOverride = texture2;
						}
						return false;
					});
					CreatureTextures[text28] = text30;
				}
				else if (commandData.Type == "resetcreature")
				{
					string text31 = (string)commandData.GetValue("obj");
					Texture2D texture3 = null;
					try
					{
						texture3 = ContentManager.Get<Texture2D>(EntityInfoManager.GetEntityInfo(text31).Texture);
					}
					catch
					{
						ShowSubmitTips(text31 + "材质恢复失败，发生未知错误");
						return SubmitResult.Fail;
					}
					ErgodicBody(text31, delegate(ComponentBody body)
					{
						ComponentModel componentModel3 = body.Entity.FindComponent<ComponentModel>();
						if (componentModel3 != null)
						{
							componentModel3.TextureOverride = texture3;
						}
						return false;
					});
					if (CreatureTextures.ContainsKey(text31))
					{
						CreatureTextures.Remove(text31);
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("model", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text32 = (string)commandData.GetValue("obj");
					bool flag39 = (bool)commandData.GetValue("con");
					string text33 = (string)commandData.GetValue("f");
					Stream commandFileStream8 = GetCommandFileStream(text33, OpenFileMode.ReadWrite);
					if (commandFileStream8 == null)
					{
						return SubmitResult.Fail;
					}
					Model model = Model.Load(commandFileStream8, true);
					commandFileStream8.Dispose();
					string modelType = EntityInfoManager.GetModelType(model);
					string modelType2 = EntityInfoManager.GetModelType(text32);
					if (modelType != modelType2 && !flag39 && text32 != "boat")
					{
						string modelTypeDisplayName = EntityInfoManager.GetModelTypeDisplayName(modelType);
						string modelTypeDisplayName2 = EntityInfoManager.GetModelTypeDisplayName(modelType2);
						ShowSubmitTips(string.Format("导入模型为{0},当前生物为{1},\n模型不匹配,请选择其他生物对象", modelTypeDisplayName, modelTypeDisplayName2));
						return SubmitResult.Fail;
					}
					ErgodicBody(text32, delegate(ComponentBody body)
					{
						ComponentModel componentModel4 = body.Entity.FindComponent<ComponentModel>();
						if (componentModel4 != null)
						{
							try
							{
								componentModel4.Model = model;
							}
							catch
							{
							}
						}
						return false;
					});
					CreatureModels[text32] = "$" + text33;
				}
				else if (commandData.Type == "pakmodel")
				{
					string text34 = (string)commandData.GetValue("obj");
					bool flag40 = (bool)commandData.GetValue("con");
					string text35 = (string)commandData.GetValue("opt");
					string text36 = Storage.CombinePaths("Models", text35);
					Model model2 = ContentManager.Get<Model>(text36);
					string modelType3 = EntityInfoManager.GetModelType(model2);
					string modelType4 = EntityInfoManager.GetModelType(text34);
					if (modelType3 != modelType4 && !flag40 && text34 != "boat")
					{
						string modelTypeDisplayName3 = EntityInfoManager.GetModelTypeDisplayName(modelType3);
						string modelTypeDisplayName4 = EntityInfoManager.GetModelTypeDisplayName(modelType4);
						ShowSubmitTips(string.Format("导入模型为{0},当前生物为{1},\n模型不匹配,请选择其他生物对象", modelTypeDisplayName3, modelTypeDisplayName4));
						return SubmitResult.Fail;
					}
					ErgodicBody(text34, delegate(ComponentBody body)
					{
						ComponentModel componentModel5 = body.Entity.FindComponent<ComponentModel>();
						if (componentModel5 != null)
						{
							try
							{
								componentModel5.Model = model2;
							}
							catch
							{
							}
						}
						return false;
					});
					CreatureModels[text34] = text36;
				}
				else if (commandData.Type == "resetall")
				{
					CreatureModels.Clear();
					ErgodicBody("all", delegate(ComponentBody body)
					{
						string name3 = body.Entity.ValuesDictionary.DatabaseObject.Name.ToLower();
						EntityInfo entityInfo = EntityInfoManager.GetEntityInfo(name3);
						if (entityInfo != null)
						{
							ComponentModel componentModel6 = body.Entity.FindComponent<ComponentModel>();
							if (componentModel6 != null)
							{
								componentModel6.Model = ContentManager.Get<Model>(entityInfo.Model);
							}
						}
						return false;
					});
				}
				return SubmitResult.Success;
			});
			AddFunction("image", delegate(CommandData commandData)
			{
				WithdrawBlockManager wbManager4 = null;
				if (WithdrawBlockManager.WithdrawMode)
				{
					wbManager4 = new WithdrawBlockManager();
				}
				Point3 onePoint28 = GetOnePoint("pos", commandData);
				bool flag41 = (bool)commandData.GetValue("con");
				string f8 = (string)commandData.GetValue("f");
				Stream commandFileStream9 = GetCommandFileStream(f8, OpenFileMode.ReadWrite);
				if (commandFileStream9 == null)
				{
					return SubmitResult.Fail;
				}
				if (!Png.IsPngStream(commandFileStream9))
				{
					ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
				}
				Image image2 = Image.Load(commandFileStream9);
				commandFileStream9.Dispose();
				bool flag42 = commandData.Type == "default";
				bool flag43 = commandData.Type == "tile";
				bool flag44 = commandData.Type == "rotate";
				for (int num165 = 0; num165 < image2.Height; num165++)
				{
					for (int num166 = 0; num166 < image2.Width; num166++)
					{
						Color pixel2 = image2.GetPixel(num166, num165);
						Point3 point29 = Point3.Zero;
						int value29 = 0;
						if (flag42)
						{
							point29 = new Point3(onePoint28.X - num166, onePoint28.Y - num165, onePoint28.Z);
						}
						else if (flag43)
						{
							point29 = new Point3(onePoint28.X - num166, onePoint28.Y, onePoint28.Z - num165);
						}
						else if (flag44)
						{
							point29 = new Point3(onePoint28.X, onePoint28.Y - num165, onePoint28.Z - num166);
						}
						if (point29.Y > 0 && point29.Y < 255)
						{
							if (pixel2.A >= 20)
							{
								if (flag41)
								{
									value29 = Command.ClayBlock.SetCommandColor(72, pixel2);
								}
								else if (!ColorIndexCaches.TryGetValue(pixel2, out value29))
								{
									value29 = DataHandle.GetColorIndex(pixel2) * 32768 + 16456;
									if (ColorIndexCaches.Count < 1000)
									{
										ColorIndexCaches[pixel2] = value29;
									}
								}
							}
							ChangeBlockValue(wbManager4, point29.X, point29.Y, point29.Z, value29);
						}
					}
				}
				Point3 minPoint2 = onePoint28;
				if (flag42)
				{
					minPoint2 = onePoint28 - new Point3(image2.Width, image2.Height, 0);
				}
				else if (flag43)
				{
					minPoint2 = onePoint28 - new Point3(image2.Width, 0, image2.Height);
				}
				else if (flag44)
				{
					minPoint2 = onePoint28 - new Point3(0, image2.Height, image2.Width);
				}
				PlaceReprocess(wbManager4, commandData, true, minPoint2, onePoint28);
				return SubmitResult.Success;
			});
			AddFunction("pattern", delegate(CommandData commandData)
			{
				if (commandData.Type == "default" || commandData.Type == "online")
				{
					Point3 pos6 = GetOnePoint("pos", commandData);
					Color color3 = (Color)commandData.GetValue("color");
					Vector3 vector12 = (Vector3)commandData.GetValue("vec3");
					int num167 = (int)commandData.GetValue("v");
					string text37 = (string)commandData.GetValue("opt");
					bool flag45 = commandData.Type != "default";
					bool con6 = flag45 && (bool)commandData.GetValue("con");
					string text38 = (flag45 ? string.Empty : ((string)commandData.GetValue("f")));
					string fix = ((!flag45) ? string.Empty : ((string)commandData.GetValue("fix")));
					bool flag46 = text37 == "tile";
					bool flag47 = text37 == "rotate";
					Vector3 vector13 = vector12 / 10f;
					Pattern pattern = new Pattern();
					pattern.Point = pos6;
					pattern.Color = color3;
					pattern.Size = (float)num167 / 20.418f;
					pattern.TexName = text38;
					pattern.Position = vector13 + new Vector3(pos6) + new Vector3(0.5f, 0.5f, 0f);
					pattern.Up = new Vector3(0f, -1f, 0f);
					pattern.Right = new Vector3(-1f, 0f, 0f);
					if (num167 <= 0 && PatternPoints.ContainsKey(pos6))
					{
						PatternPoints.Remove(pos6);
					}
					if (flag46)
					{
						pattern.Up = new Vector3(0f, 0f, -1f);
						pattern.Position = vector13 + new Vector3(pos6) + new Vector3(0.5f, 1f, 0.5f);
					}
					else if (flag47)
					{
						pattern.Right = new Vector3(0f, 0f, -1f);
						pattern.Position = vector13 + new Vector3(pos6) + new Vector3(1f, 0.5f, 0.5f);
					}
					if (!flag45)
					{
						Stream commandFileStream10 = GetCommandFileStream(text38, OpenFileMode.ReadWrite);
						if (commandFileStream10 == null)
						{
							return SubmitResult.Fail;
						}
						if (!Png.IsPngStream(commandFileStream10))
						{
							ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
						}
						Texture2D texture2D2 = Texture2D.Load(commandFileStream10);
						pattern.LWratio = (float)texture2D2.Height / (float)texture2D2.Width;
						pattern.Texture = texture2D2;
						PatternPoints[pos6] = pattern;
						commandFileStream10.Dispose();
					}
					else
					{
						ShowSubmitTips("图片正在生成,请保证网络良好");
						Task.Run(delegate
						{
							CancellableProgress progress = new CancellableProgress();
							WebManager.Get(fix, null, null, progress, delegate(byte[] result)
							{
								Stream stream4 = new MemoryStream(result);
								if (stream4 != null)
								{
									StreamReader streamReader4 = new StreamReader(stream4);
									string pic = GetPictureURL(streamReader4.ReadToEnd());
									if (!string.IsNullOrEmpty(pic))
									{
										WebManager.Get(pic, null, null, progress, delegate(byte[] result2)
										{
											Stream stream5 = new MemoryStream(result2);
											if (stream5 != null)
											{
												if (con6)
												{
													string systemPath = Storage.GetSystemPath(DataHandle.GetCommandPath());
													pattern.TexName = pic.Substring(pic.LastIndexOf('/') + 1);
													FileStream fileStream = new FileStream(Storage.CombinePaths(systemPath, pattern.TexName), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
													fileStream.Write(result2, 0, result2.Length);
													fileStream.Flush();
													fileStream.Dispose();
												}
												Texture2D texture2D3 = Texture2D.Load(stream5);
												pattern.LWratio = (float)texture2D3.Height / (float)texture2D3.Width;
												pattern.Texture = texture2D3;
												PatternPoints[pos6] = pattern;
												stream5.Dispose();
												ShowSubmitTips("图片生成成功");
											}
											else
											{
												ShowSubmitTips("图片生成失败");
											}
										}, delegate
										{
											ShowSubmitTips("图片生成失败");
										});
									}
									else
									{
										ShowSubmitTips("图片URL获取失败");
									}
								}
							}, delegate
							{
								ShowSubmitTips("接口连接失败");
							});
						});
					}
				}
				else if (commandData.Type == "font")
				{
					string text39 = (string)commandData.GetValue("text");
					Point3 onePoint29 = GetOnePoint("pos", commandData);
					Color color4 = (Color)commandData.GetValue("color");
					Vector3 vector14 = (Vector3)commandData.GetValue("vec3");
					int num168 = (int)commandData.GetValue("v");
					string text40 = (string)commandData.GetValue("opt");
					PatternFont patternFont = new PatternFont
					{
						Point = onePoint29,
						Position = new Vector3(onePoint29) + vector14 / 100f,
						Text = text39,
						Size = (float)num168 / 1000f,
						Color = color4
					};
					switch (text40)
					{
					case "+x-y":
						patternFont.Right = new Vector3(1f, 0f, 0f);
						patternFont.Down = new Vector3(0f, -1f, 0f);
						patternFont.Position += new Vector3(0f, 1f, 0f);
						break;
					case "-x-y":
						patternFont.Right = new Vector3(-1f, 0f, 0f);
						patternFont.Down = new Vector3(0f, -1f, 0f);
						patternFont.Position += new Vector3(1f, 1f, 0f);
						break;
					case "+z-y":
						patternFont.Right = new Vector3(0f, 0f, 1f);
						patternFont.Down = new Vector3(0f, -1f, 0f);
						patternFont.Position += new Vector3(0f, 1f, 0f);
						break;
					case "-z-y":
						patternFont.Right = new Vector3(0f, 0f, -1f);
						patternFont.Down = new Vector3(0f, -1f, 0f);
						patternFont.Position += new Vector3(0f, 1f, 1f);
						break;
					case "+x+z":
						patternFont.Right = new Vector3(1f, 0f, 0f);
						patternFont.Down = new Vector3(0f, 0f, 1f);
						break;
					case "-x+z":
						patternFont.Right = new Vector3(-1f, 0f, 0f);
						patternFont.Down = new Vector3(0f, 0f, 1f);
						patternFont.Position += new Vector3(1f, 0f, 0f);
						break;
					default:
						patternFont.Right = new Vector3(1f, 0f, 0f);
						patternFont.Down = new Vector3(0f, -1f, 0f);
						patternFont.Position += new Vector3(0f, 1f, 0f);
						break;
					}
					PatternFonts[onePoint29] = patternFont;
				}
				else if (commandData.Type == "remove")
				{
					Point3[] twoPoint24 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea24 = new CubeArea(twoPoint24[0], twoPoint24[1]);
					List<Point3> list18 = new List<Point3>();
					List<Point3> list19 = new List<Point3>();
					bool flag48 = false;
					foreach (Point3 key6 in PatternPoints.Keys)
					{
						if (cubeArea24.Exist(PatternPoints[key6].Position))
						{
							list18.Add(key6);
							flag48 = true;
						}
					}
					foreach (Point3 item12 in list18)
					{
						PatternPoints.Remove(item12);
					}
					foreach (Point3 key7 in PatternFonts.Keys)
					{
						if (cubeArea24.Exist(PatternFonts[key7].Position))
						{
							list19.Add(key7);
							flag48 = true;
						}
					}
					foreach (Point3 item13 in list19)
					{
						PatternFonts.Remove(item13);
					}
					if (!flag48)
					{
						ShowSubmitTips("指定区域不存在光点贴图");
					}
				}
				else if (commandData.Type == "removeall")
				{
					PatternPoints.Clear();
					PatternFonts.Clear();
				}
				else if (commandData.Type == "screenadd")
				{
					Vector2 vector15 = (Vector2)commandData.GetValue("vec2");
					int layer = (int)commandData.GetValue("v");
					string str4 = (string)commandData.GetValue("size");
					bool flag49 = (bool)commandData.GetValue("con1");
					bool flag50 = (bool)commandData.GetValue("con2");
					string text41 = (string)commandData.GetValue("text");
					string text42 = (string)commandData.GetValue("f");
					Stream commandFileStream11 = GetCommandFileStream(text42, OpenFileMode.ReadWrite);
					if (commandFileStream11 == null)
					{
						return SubmitResult.Fail;
					}
					if (!Png.IsPngStream(commandFileStream11))
					{
						ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
					}
					Texture2D texture2D4 = Texture2D.Load(commandFileStream11);
					commandFileStream11.Close();
					if (ScreenPatterns.ContainsKey(text41))
					{
						ScreenPatterns.Remove(text41);
					}
					m_screenPatternsWidget.IsVisible = true;
					Vector2 size = DataHandle.GetVector2Value(str4);
					if (flag49)
					{
						size = new Vector2(size.X, size.X * (float)texture2D4.Height / (float)texture2D4.Width);
					}
					int num169 = (int)(m_componentPlayer.GameWidget.ActualSize.X / 2f) - (int)(size.X / 2f);
					int num170 = (int)(m_componentPlayer.GameWidget.ActualSize.Y / 2f) - (int)(size.Y / 2f);
					if (flag50)
					{
						BitmapButtonWidget widget = new BitmapButtonWidget
						{
							NormalSubtexture = new Subtexture(texture2D4, Vector2.Zero, Vector2.One),
							ClickedSubtexture = new Subtexture(texture2D4, Vector2.Zero, Vector2.One),
							Color = Color.White,
							Size = size,
							Margin = new Vector2(num169, num170) + vector15
						};
						ScreenPattern value30 = new ScreenPattern
						{
							Name = text41,
							Texture = text42,
							Layer = layer,
							OutTime = 0f,
							Widget = widget
						};
						ScreenPatterns.Add(text41, value30);
					}
					else
					{
						RectangleWidget widget2 = new RectangleWidget
						{
							Subtexture = new Subtexture(texture2D4, Vector2.Zero, Vector2.One),
							FillColor = Color.White,
							OutlineColor = new Color(0, 0, 0, 0),
							Size = size,
							Margin = new Vector2(num169, num170) + vector15
						};
						ScreenPattern value31 = new ScreenPattern
						{
							Name = text41,
							Texture = text42,
							Layer = layer,
							OutTime = 0f,
							Widget = widget2
						};
						ScreenPatterns.Add(text41, value31);
					}
					if (m_screenPatternsWidget.Children.Count > 0)
					{
						m_screenPatternsWidget.Children.Clear();
					}
					ScreenPattern[] array10 = ScreenPatterns.Values.ToArray();
					for (int num171 = 0; num171 < array10.Length - 1; num171++)
					{
						for (int num172 = 0; num172 < array10.Length - 1 - num171; num172++)
						{
							if (array10[num172].Layer > array10[num172 + 1].Layer)
							{
								ScreenPattern screenPattern = array10[num172 + 1];
								array10[num172 + 1] = array10[num172];
								array10[num172] = screenPattern;
							}
						}
					}
					ScreenPattern[] array11 = array10;
					foreach (ScreenPattern screenPattern2 in array11)
					{
						m_screenPatternsWidget.Children.Add(screenPattern2.Widget);
					}
				}
				else if (commandData.Type == "screenremove")
				{
					string text43 = (string)commandData.GetValue("text");
					ScreenPattern value32;
					if (ScreenPatterns.TryGetValue(text43, out value32))
					{
						m_screenPatternsWidget.IsVisible = true;
						m_screenPatternsWidget.Children.Remove(value32.Widget);
						ScreenPatterns.Remove(text43);
					}
					else
					{
						ShowSubmitTips(string.Format("屏幕中没有标识名为{0}的贴图", text43));
					}
				}
				else if (commandData.Type == "screenclear")
				{
					m_screenPatternsWidget.Children.Clear();
					ScreenPatterns.Clear();
				}
				return SubmitResult.Success;
			});
			AddFunction("music", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text44 = (string)commandData.GetValue("f");
					string text45 = Storage.GetExtension(text44).ToLower();
					if (text45 != ".wav")
					{
						ShowSubmitTips("目前仅支持wav格式的音频");
						return SubmitResult.Fail;
					}
					if (m_commandMusic.Sound == null || (m_commandMusic.Sound != null && m_commandMusic.Name != text44))
					{
						Stream commandFileStream12 = GetCommandFileStream(text44, OpenFileMode.ReadWrite);
						if (commandFileStream12 == null)
						{
							return SubmitResult.Fail;
						}
						SoundBuffer soundBuffer = SoundBuffer.Load(commandFileStream12);
						m_commandMusic = new CommandMusic(text44, new Sound(soundBuffer));
						m_commandMusic.Sound.Play();
						ShowSubmitTips("开始播放歌曲：" + m_commandMusic.Name);
						commandFileStream12.Dispose();
					}
					else if (m_commandMusic.Sound != null && m_commandMusic.Name == text44)
					{
						if (m_commandMusic.Sound.State == SoundState.Paused)
						{
							ShowSubmitTips("继续播放歌曲：" + m_commandMusic.Name);
						}
						else if (m_commandMusic.Sound.State == SoundState.Stopped)
						{
							ShowSubmitTips("开始播放歌曲：" + m_commandMusic.Name);
						}
						m_commandMusic.Sound.Play();
					}
				}
				else if (commandData.Type == "pause" || commandData.Type == "stop" || commandData.Type == "reset")
				{
					if (m_commandMusic.Sound == null)
					{
						ShowSubmitTips("当前后台无歌曲，请先播放歌曲");
						return SubmitResult.Fail;
					}
					switch (commandData.Type)
					{
					case "pause":
						if (m_commandMusic.Sound.State == SoundState.Playing)
						{
							m_commandMusic.Sound.Pause();
							ShowSubmitTips("已暂停后台歌曲:" + m_commandMusic.Name);
						}
						break;
					case "stop":
						m_commandMusic.Sound.Stop();
						ShowSubmitTips("已停止后台歌曲:" + m_commandMusic.Name);
						break;
					case "reset":
						m_commandMusic.Sound.Stop();
						m_commandMusic.Sound.Play();
						ShowSubmitTips("重新播放歌曲:" + m_commandMusic.Name);
						break;
					}
				}
				else if (commandData.Type == "volume")
				{
					int num174 = (int)commandData.GetValue("v");
					if (m_commandMusic.Sound == null)
					{
						ShowSubmitTips("当前后台无歌曲，请先播放歌曲");
						return SubmitResult.Fail;
					}
					m_commandMusic.Sound.Volume = (float)num174 / 10f;
				}
				return SubmitResult.Success;
			});
			AddFunction("audio", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text46 = (string)commandData.GetValue("f");
					int num175 = (int)commandData.GetValue("v");
					int num176 = (int)commandData.GetValue("p");
					string text47 = Storage.GetExtension(text46).ToLower();
					if (text47 != ".wav")
					{
						ShowSubmitTips("目前仅支持wav格式的音频");
						return SubmitResult.Fail;
					}
					if (num176 != 15 && num175 != 0)
					{
						Vector3 p = new Vector3(commandData.Position);
						float pitch = CommandMusic.GetPitch(num176);
						float num177 = (float)num175 / 15f;
						Stream commandFileStream13 = GetCommandFileStream(text46, OpenFileMode.ReadWrite);
						if (commandFileStream13 == null)
						{
							return SubmitResult.Fail;
						}
						SoundBuffer soundBuffer2 = SoundBuffer.Load(commandFileStream13);
						float num178 = m_subsystemAudio.CalculateVolume(m_subsystemAudio.CalculateListenerDistance(p), 0.5f + 5f * num177);
						new Sound(soundBuffer2, num177 * num178, pitch, 0f, false, true).Play();
						commandFileStream13.Dispose();
					}
				}
				else if (commandData.Type == "contentpak")
				{
					string text48 = (string)commandData.GetValue("opt");
					int num179 = (int)commandData.GetValue("v");
					int num180 = (int)commandData.GetValue("p");
					if (num180 != 15 && num179 != 0)
					{
						Vector3 position2 = new Vector3(commandData.Position);
						float pitch2 = CommandMusic.GetPitch(num180);
						float num181 = (float)num179 / 15f;
						m_subsystemAudio.PlaySound("Audio/" + text48, num181, pitch2, position2, 0.5f + 5f * num181, true);
					}
				}
				else if (commandData.Type == "piano")
				{
					int num182 = (int)commandData.GetValue("p");
					int o2 = (int)commandData.GetValue("o");
					int num183 = (int)commandData.GetValue("v");
					if (num182 != 15 && num183 != 0)
					{
						Vector3 vector16 = new Vector3(commandData.Position) + new Vector3(0.5f);
						float volume = (float)num183 / 15f;
						float pitch3 = CommandMusic.GetRealPitch(num182, ref o2);
						if (o2 > 7)
						{
							o2 = 7;
							pitch3 = 1f;
						}
						float minDistance = 0.5f + (float)num183 / 3f;
						m_subsystemAudio.PlaySound(string.Format("CommandPiano/PianoC{0}", o2), volume, pitch3, vector16, minDistance, true);
						SoundParticleSystem soundParticleSystem = new SoundParticleSystem(m_subsystemTerrain, vector16 + new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1f, 0f));
						if (soundParticleSystem.SubsystemParticles == null)
						{
							m_subsystemParticles.AddParticleSystem(soundParticleSystem);
						}
						Vector3 hsv = new Vector3(22.5f * (float)num182 + new Random().Float(0f, 22f), 0.5f + (float)num183 / 30f, 1f);
						soundParticleSystem.AddNote(new Color(Color.HsvToRgb(hsv)));
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("build", delegate(CommandData commandData)
			{
				WithdrawBlockManager wbManager5 = null;
				if (WithdrawBlockManager.WithdrawMode)
				{
					wbManager5 = new WithdrawBlockManager();
				}
				if (commandData.Type == "default")
				{
					Point3 pos7 = GetOnePoint("pos", commandData);
					string f9 = (string)commandData.GetValue("f");
					string opt2 = (string)commandData.GetValue("opt");
					Task.Run(delegate
					{
						Stream commandFileStream14 = GetCommandFileStream(f9, OpenFileMode.ReadWrite);
						StreamReader streamReader5 = new StreamReader(commandFileStream14);
						int num184 = 1;
						string empty4 = string.Empty;
						Point3 zero = Point3.Zero;
						int num185 = 0;
						try
						{
							while ((empty4 = streamReader5.ReadLine()) != null)
							{
								string[] array12 = empty4.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
								if (array12.Length == 4 || array12.Length == 6)
								{
									try
									{
										Point3 point30 = Point3.Zero;
										switch (opt2)
										{
										case "x-y-z":
											point30 = new Point3(int.Parse(array12[0]), int.Parse(array12[1]), int.Parse(array12[2]));
											break;
										case "x-z-y":
											point30 = new Point3(int.Parse(array12[0]), int.Parse(array12[2]), int.Parse(array12[1]));
											break;
										case "y-x-z":
											point30 = new Point3(int.Parse(array12[1]), int.Parse(array12[0]), int.Parse(array12[2]));
											break;
										case "y-z-x":
											point30 = new Point3(int.Parse(array12[1]), int.Parse(array12[2]), int.Parse(array12[0]));
											break;
										case "z-x-y":
											point30 = new Point3(int.Parse(array12[2]), int.Parse(array12[0]), int.Parse(array12[1]));
											break;
										case "z-y-x":
											point30 = new Point3(int.Parse(array12[2]), int.Parse(array12[1]), int.Parse(array12[0]));
											break;
										}
										point30 += pos7;
										if (point30.X > zero.X)
										{
											zero.X = point30.X;
										}
										if (point30.Y > zero.Y)
										{
											zero.Y = point30.Y;
										}
										if (point30.Z > zero.Z)
										{
											zero.Z = point30.Z;
										}
										num185 = ((array12.Length != 4) ? Command.ClayBlock.SetCommandColor(72, new Color(int.Parse(array12[3]), int.Parse(array12[4]), int.Parse(array12[5]))) : int.Parse(array12[3]));
										ChangeBlockValue(wbManager5, point30.X, point30.Y, point30.Z, num185);
									}
									catch
									{
										ShowSubmitTips(string.Format("方块生成发生错误，错误发生在第{0}行", num184));
									}
								}
								num184++;
							}
						}
						catch
						{
						}
						PlaceReprocess(wbManager5, commandData, true, pos7, zero);
						commandFileStream14.Dispose();
					});
				}
				else if (commandData.Type == "copycache")
				{
					Point3 onePoint30 = GetOnePoint("pos", commandData);
					if (CopyBlockManager == null)
					{
						ShowSubmitTips("缓存区不存在复制的建筑，\n请先复制区域，复制指令为copyblock$copycache");
						return SubmitResult.Fail;
					}
					CopyBlockManager.SubsystemCommandDef = this;
					CopyBlockManager.WBManager = wbManager5;
					CopyBlockManager.CopyFromCache(onePoint30);
					PlaceReprocess(wbManager5, commandData, true, onePoint30, onePoint30 + CopyBlockManager.CubeArea.MaxPoint - CopyBlockManager.CubeArea.MinPoint);
					ShowSubmitTips("已全部生成！\n如果复制工作已完成，可以重启游戏以清除后台方块缓存");
				}
				return SubmitResult.Success;
			});
			AddFunction("copyfile", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text49 = (string)commandData.GetValue("fix");
					string path3 = Storage.CombinePaths(DataHandle.GetCommandPath(), text49);
					if (!Storage.FileExists(path3))
					{
						ShowSubmitTips("Command目录不存在文件:" + text49);
						return SubmitResult.Fail;
					}
					string systemPath2 = Storage.GetSystemPath(path3);
					string path4 = Storage.CombinePaths(Storage.GetSystemPath(GameManager.m_worldInfo.DirectoryName), text49);
					FileStream fileStream2 = new FileStream(systemPath2, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
					FileStream fileStream3 = new FileStream(path4, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
					fileStream2.CopyTo(fileStream3);
					fileStream2.Dispose();
					fileStream3.Dispose();
					ShowSubmitTips("已将指定文件复制到存档工作目录");
				}
				else if (commandData.Type == "folder")
				{
					string text50 = (string)commandData.GetValue("fix");
					string path5 = Storage.CombinePaths(Storage.GetDirectoryName(DataHandle.GetCommandPath()), text50);
					if (!Storage.DirectoryExists(path5))
					{
						ShowSubmitTips("Command目录不存在文件夹:" + text50);
						return SubmitResult.Fail;
					}
					string systemPath3 = Storage.GetSystemPath(path5);
					string systemPath4 = Storage.GetSystemPath(GameManager.m_worldInfo.DirectoryName);
					foreach (string item14 in Storage.ListFileNames(path5))
					{
						string path6 = Storage.CombinePaths(systemPath3, item14);
						string path7 = Storage.CombinePaths(systemPath4, item14);
						FileStream fileStream4 = new FileStream(path6, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
						FileStream fileStream5 = new FileStream(path7, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
						fileStream4.CopyTo(fileStream5);
						fileStream4.Dispose();
						fileStream5.Dispose();
					}
					ShowSubmitTips("已将指定文件夹所有文件复制到存档工作目录");
				}
				return SubmitResult.Success;
			});
			AddFunction("capture", delegate
			{
				ScreenCaptureManager.CapturePhoto(delegate
				{
					ShowSubmitTips("图片已储存到图库");
				}, delegate
				{
					ShowSubmitTips("图片截取失败，发生未知错误");
				});
				return SubmitResult.Success;
			});
			AddFunction("website", delegate(CommandData commandData)
			{
				string url = (string)commandData.GetValue("text");
				WebBrowserManager.LaunchBrowser(url);
				return SubmitResult.Success;
			});
			AddFunction("note", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					m_componentPlayer.ComponentGui.ModalPanelWidget = new NotesWidget(m_componentPlayer, Notes);
				}
				else if (commandData.Type == "onlyread")
				{
					string text51 = (string)commandData.GetValue("text");
					string str5 = (string)commandData.GetValue("size");
					Vector2 vector17 = (Vector2)commandData.GetValue("vec2");
					int num186 = (int)commandData.GetValue("v1");
					Color color5 = (Color)commandData.GetValue("color1");
					int num187 = (int)commandData.GetValue("v2");
					Color fillColor = (Color)commandData.GetValue("color2");
					Color outlineColor = (Color)commandData.GetValue("color3");
					int num188 = (int)commandData.GetValue("v3");
					string value33;
					if (Notes.TryGetValue(text51, out value33))
					{
						m_screenLabelCanvasWidget.IsVisible = true;
						m_screenLabelCloseTime = num186;
						Vector2 vector2Value = DataHandle.GetVector2Value(str5);
						CanvasWidget canvasWidget = (CanvasWidget)m_screenLabelCanvasWidget;
						canvasWidget.Size = vector2Value;
						int num189 = (int)(m_componentPlayer.GameWidget.ActualSize.X / 2f) - (int)(vector2Value.X / 2f);
						int num190 = (int)(m_componentPlayer.GameWidget.ActualSize.Y / 2f) - (int)(vector2Value.Y / 2f);
						m_componentPlayer.GameWidget.SetWidgetPosition(m_screenLabelCanvasWidget, new Vector2(num189, num190) + vector17);
						LabelWidget labelWidget = m_screenLabelCanvasWidget.Children.Find<LabelWidget>("Content");
						labelWidget.Text = value33.Replace("[n]", "\n").Replace("\t", "");
						labelWidget.Color = color5;
						labelWidget.FontScale = (float)num187 / 50f;
						RectangleWidget rectangleWidget = m_screenLabelCanvasWidget.Children.Find<RectangleWidget>("ScreenLabelRectangle");
						rectangleWidget.FillColor = fillColor;
						rectangleWidget.OutlineColor = outlineColor;
						rectangleWidget.OutlineThickness = (float)num188 / 10f;
					}
					else
					{
						ShowSubmitTips(string.Format("没有标题名为{0}的笔记", text51));
					}
				}
				else if (commandData.Type == "close")
				{
					m_screenLabelCanvasWidget.IsVisible = false;
				}
				return SubmitResult.Success;
			});
			AddFunction("colorblock", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					int value34 = (int)commandData.GetValue("id");
					Color color6 = (Color)commandData.GetValue("color");
					bool flag51 = (bool)commandData.GetValue("con");
					value34 = Terrain.ExtractContents(value34);
					int num191 = 0;
					if (value34 != 72 && value34 != 12 && value34 != 13 && value34 != 14 && value34 != 225 && value34 != 256 && value34 != 15)
					{
						ShowSubmitTips("该功能目前只支持黏土、玻璃以及树叶");
						return SubmitResult.Fail;
					}
					switch (value34)
					{
					case 72:
						num191 = Command.ClayBlock.SetCommandColor(value34, color6);
						break;
					case 12:
						num191 = Command.OakLeavesBlock.SetCommandColor(value34, color6);
						break;
					case 13:
						num191 = Command.BirchLeavesBlock.SetCommandColor(value34, color6);
						break;
					case 14:
						num191 = Command.SpruceLeavesBlock.SetCommandColor(value34, color6);
						break;
					case 225:
						num191 = Command.TallSpruceLeavesBlock.SetCommandColor(value34, color6);
						break;
					case 256:
						num191 = Command.MimosaLeavesBlock.SetCommandColor(value34, color6);
						break;
					case 15:
						num191 = Command.GlassBlock.SetCommandColor(value34, color6);
						break;
					}
					if (value34 == 15)
					{
						num191 = Command.GlassBlock.SetCommandColorAlpha(num191, (int)((float)(int)color6.A / 16f));
					}
					if (flag51)
					{
						int activeSlotIndex = m_componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex;
						int slotsCount = m_componentPlayer.ComponentMiner.Inventory.SlotsCount;
						m_componentPlayer.ComponentMiner.Inventory.RemoveSlotItems(activeSlotIndex, slotsCount);
						m_componentPlayer.ComponentMiner.Inventory.AddSlotItems(activeSlotIndex, num191, slotsCount);
					}
					ShowSubmitTips(string.Format("方块id为{0},颜色为{1}的方块值为{2},\n已将该方块值复制到剪切板以及命令辅助棒记录方块值", value34, color6.ToString(), num191));
					ClipboardManager.ClipboardString = num191.ToString();
					base.Project.FindSubsystem<SubsystemCmdRodBlockBehavior>().m_recordBlockValue = num191;
				}
				else if (commandData.Type == "display")
				{
					bool displayColorBlock = (bool)commandData.GetValue("con");
					DisplayColorBlock = displayColorBlock;
					if (DisplayColorBlock)
					{
						BlocksManager.AddCategory(Command.ClayBlock.CommandCategory);
						BlocksManager.AddCategory(Command.BirchLeavesBlock.CommandCategory);
						BlocksManager.AddCategory(Command.OakLeavesBlock.CommandCategory);
						BlocksManager.AddCategory(Command.SpruceLeavesBlock.CommandCategory);
						BlocksManager.AddCategory(Command.TallSpruceLeavesBlock.CommandCategory);
						BlocksManager.AddCategory(Command.MimosaLeavesBlock.CommandCategory);
						BlocksManager.AddCategory(Command.GlassBlock.CommandCategory);
					}
					else
					{
						if (BlocksManager.m_categories.Contains(Command.ClayBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.ClayBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.BirchLeavesBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.BirchLeavesBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.OakLeavesBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.OakLeavesBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.SpruceLeavesBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.SpruceLeavesBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.TallSpruceLeavesBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.TallSpruceLeavesBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.MimosaLeavesBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.MimosaLeavesBlock.CommandCategory);
						}
						if (BlocksManager.m_categories.Contains(Command.GlassBlock.CommandCategory))
						{
							BlocksManager.m_categories.Remove(Command.GlassBlock.CommandCategory);
						}
					}
					ComponentCreativeInventory componentCreativeInventory = m_componentPlayer.Entity.FindComponent<ComponentCreativeInventory>();
					List<Order> list20 = new List<Order>();
					Block[] blocks = BlocksManager.Blocks;
					foreach (Block block4 in blocks)
					{
						foreach (int creativeValue in block4.GetCreativeValues())
						{
							list20.Add(new Order(block4, block4.GetDisplayOrder(creativeValue), creativeValue));
						}
					}
					IOrderedEnumerable<Order> orderedEnumerable = list20.OrderBy((Order o) => o.order);
					foreach (Order item15 in orderedEnumerable)
					{
						componentCreativeInventory.m_slots.Add(item15.value);
					}
					if (DisplayColorBlock)
					{
						ShowSubmitTips("操作成功，可查看背包中的命令方块彩色方块栏");
					}
					else
					{
						ShowSubmitTips("已移除背包中的命令方块彩色方块栏");
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("withdraw", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					int num193 = (int)commandData.GetValue("v");
					bool withdrawMode = (bool)commandData.GetValue("con");
					WithdrawBlockManager.WithdrawMode = withdrawMode;
					if (num193 < WithdrawBlockManager.MaxSteps)
					{
						WithdrawBlockManager.Clear();
					}
					WithdrawBlockManager.MaxSteps = num193;
					if (!WithdrawBlockManager.WithdrawMode)
					{
						WithdrawBlockManager.Clear();
					}
				}
				else if (commandData.Type == "carryout")
				{
					if (WithdrawBlockManager.WithdrawMode)
					{
						WithdrawBlockManager.CarryOut(this);
					}
					else
					{
						ShowSubmitTips("请先打开撤回模式");
					}
				}
				else if (commandData.Type == "recovery")
				{
					if (WithdrawBlockManager.WithdrawMode)
					{
						WithdrawBlockManager.Recovery(this);
					}
					else
					{
						ShowSubmitTips("请先打开撤回模式");
					}
				}
				return SubmitResult.Success;
			});
			AddFunction("chunkwork", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint31 = GetOnePoint("pos", commandData);
					Point2 point31 = Terrain.ToChunk(onePoint31.X, onePoint31.Z);
					bool flag52 = AddChunks007(point31.X, point31.Y);
					ShowSubmitTips(string.Format("点({0})对应的区块", onePoint31.ToString()) + (flag52 ? "已安排到007岗位" : "已在拼命工作中，无需重复安排"));
				}
				else if (commandData.Type == "areawork")
				{
					Point3[] twoPoint25 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cube9 = new CubeArea(twoPoint25[0], twoPoint25[1]);
					cube9.Ergodic(delegate
					{
						Point2 point32 = Terrain.ToChunk(cube9.Current.X, cube9.Current.Z);
						AddChunks007(point32.X, point32.Y);
						return false;
					});
					ShowSubmitTips(string.Format("区域[({0})-({1})]对应的所有区块已全部安排到007岗位", twoPoint25[0], twoPoint25[1]));
				}
				else if (commandData.Type == "reset")
				{
					Point3 onePoint32 = GetOnePoint("pos", commandData);
					Point2 point33 = Terrain.ToChunk(onePoint32.X, onePoint32.Z);
					RemoveChunks007(point33.X, point33.Y);
					ShowSubmitTips(string.Format("点({0})对应的区块给予享受8小时工作制福利", onePoint32.ToString()));
				}
				else if (commandData.Type == "resetall")
				{
					m_terrainChunks007.Clear();
					ShowSubmitTips("所有007区块已正常放假");
				}
				else if (commandData.Type == "show")
				{
					string text52 = ((m_terrainChunks007.Count == 0) ? "当前没有007区块" : "以下点对应区块正在拼命工作：\n");
					foreach (Point2 item16 in m_terrainChunks007)
					{
						text52 = text52 + "(" + new Point3(item16.X * 16 + 7, 64, item16.Y * 16 + 7).ToString() + "); ";
					}
					ShowSubmitTips(text52);
				}
				return SubmitResult.Success;
			});
			AddFunction("convertfile", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					string text53 = (string)commandData.GetValue("f");
					Stream commandFileStream15 = GetCommandFileStream(text53, OpenFileMode.ReadWrite);
					if (commandFileStream15 == null)
					{
						return SubmitResult.Fail;
					}
					try
					{
						string text54 = Storage.CombinePaths(DataHandle.GetCommandPath(), text53.Replace(".scworld", ""));
						if (!Storage.DirectoryExists(text54))
						{
							Storage.CreateDirectory(text54);
						}
						WorldsManager.UnpackWorld(text54, commandFileStream15, true);
						ConvertWorld(Storage.CombinePaths(text54, "Project.xml"), true);
						using (Stream targetStream = Storage.OpenFile(text54 + "(转换).scworld", OpenFileMode.Create))
						{
							WorldsManager.PackWorld(text54, targetStream, null, true);
						}
						if (Storage.DirectoryExists(text54))
						{
							DataHandle.DeleteAllDirectoryAndFile(text54);
						}
						ShowSubmitTips("存档转换完成！新存档路径名：" + text54 + "(转换).scworld");
					}
					catch (Exception ex2)
					{
						ShowSubmitTips("存档转换失败！错误信息：" + ex2.Message);
					}
				}
				else if (commandData.Type == "decrypt")
				{
					string text55 = (string)commandData.GetValue("text");
					string text56 = (string)commandData.GetValue("f");
					ShowSubmitTips("已停用");
				}
				return SubmitResult.Success;
			});
			AddFunction("moremode", delegate(CommandData commandData)
			{
				if (commandData.Type == "rodray")
				{
					bool showRay = (bool)commandData.GetValue("con1");
					bool showChunk = (bool)commandData.GetValue("con2");
					SubsystemCmdRodBlockBehavior.ShowRay = showRay;
					SubsystemCmdRodBlockBehavior.ShowChunk = showChunk;
				}
				return SubmitResult.Success;
			});
			AddFunction("exit", delegate(CommandData commandData)
			{
				GameManager.SaveProject(true, true);
				GameManager.DisposeProject();
				if (commandData.Type == "default")
				{
					Environment.Exit(0);
				}
				else if (commandData.Type == "world")
				{
					ScreensManager.SwitchScreen("MainMenu");
				}
				return SubmitResult.Success;
			});
		}

		public void Condition()
		{
			AddCondition("blockexist", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint = GetOnePoint("pos", commandData);
					int num = (int)commandData.GetValue("id");
					int limitValue = GetLimitValue(onePoint.X, onePoint.Y, onePoint.Z);
					if (limitValue == num)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint = GetTwoPoint("pos1", "pos2", commandData);
					int id = (int)commandData.GetValue("id");
					CubeArea cube = new CubeArea(twoPoint[0], twoPoint[1]);
					if (cube.Ergodic(delegate
					{
						int limitValue2 = GetLimitValue(cube.Current.X, cube.Current.Y, cube.Current.Z);
						return limitValue2 == id;
					}))
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "global")
				{
					Point3[] twoPoint2 = GetTwoPoint("pos1", "pos2", commandData);
					int id2 = (int)commandData.GetValue("id");
					CubeArea cube2 = new CubeArea(twoPoint2[0], twoPoint2[1]);
					if (!cube2.Ergodic(delegate
					{
						int limitValue3 = GetLimitValue(cube2.Current.X, cube2.Current.Y, cube2.Current.Z);
						return limitValue3 != id2;
					}))
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("blockchange", delegate(CommandData commandData)
			{
				Point3 onePoint2 = GetOnePoint("pos", commandData);
				int limitValue4 = GetLimitValue(onePoint2.X, onePoint2.Y, onePoint2.Z);
				object value;
				if (commandData.DIYPara.TryGetValue("lastLimitValue", out value))
				{
					if (limitValue4 != (int)value)
					{
						commandData.DIYPara["lastLimitValue"] = limitValue4;
						return SubmitResult.Success;
					}
				}
				else
				{
					commandData.DIYPara["lastLimitValue"] = limitValue4;
				}
				return SubmitResult.Fail;
			});
			AddCondition("blocklight", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint3 = GetOnePoint("pos", commandData);
					Vector2 vector = (Vector2)commandData.GetValue("vec2");
					int cellLight = m_subsystemTerrain.Terrain.GetCellLight(onePoint3.X, onePoint3.Y, onePoint3.Z);
					if ((float)cellLight >= vector.X && (float)cellLight <= vector.Y)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "area")
				{
					Point3[] twoPoint3 = GetTwoPoint("pos1", "pos2", commandData);
					Vector2 vector2 = (Vector2)commandData.GetValue("vec2");
					int maxLight = -1;
					CubeArea cube3 = new CubeArea(twoPoint3[0], twoPoint3[1]);
					cube3.Ergodic(delegate
					{
						int cellLight2 = m_subsystemTerrain.Terrain.GetCellLight(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
						if (cellLight2 > maxLight)
						{
							maxLight = cellLight2;
						}
						return false;
					});
					if ((float)maxLight >= vector2.X && (float)maxLight <= vector2.Y)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("entityexist", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3[] twoPoint4 = GetTwoPoint("pos1", "pos2", commandData);
					string target = (string)commandData.GetValue("obj");
					CubeArea cube4 = new CubeArea(twoPoint4[0], twoPoint4[1]);
					if (ErgodicBody(target, (ComponentBody body) => cube4.Exist(body.Position)))
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "limit")
				{
					Point3[] twoPoint5 = GetTwoPoint("pos1", "pos2", commandData);
					string text = (string)commandData.GetValue("obj");
					Vector2 vector3 = (Vector2)commandData.GetValue("vec2");
					CubeArea cubeArea = new CubeArea(twoPoint5[0], twoPoint5[1]);
					int num2 = 0;
					Vector2 vector4 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
					DynamicArray<ComponentBody> dynamicArray = m_subsystemBodies.Bodies.ToDynamicArray();
					foreach (ComponentBody item in dynamicArray)
					{
						if (text == item.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() && cubeArea.Exist(item.Position))
						{
							num2++;
						}
					}
					if ((float)num2 >= vector3.X && (float)num2 <= vector3.Y)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "count")
				{
					Point3[] twoPoint6 = GetTwoPoint("pos1", "pos2", commandData);
					string text2 = (string)commandData.GetValue("obj");
					Vector2 vector5 = (Vector2)commandData.GetValue("vec2");
					CubeArea cubeArea2 = new CubeArea(twoPoint6[0], twoPoint6[1]);
					int num3 = 0;
					Vector2 vector6 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
					DynamicArray<ComponentBody> dynamicArray2 = m_subsystemBodies.Bodies.ToDynamicArray();
					foreach (ComponentBody item2 in dynamicArray2)
					{
						if (cubeArea2.Exist(item2.Position))
						{
							num3++;
						}
					}
					if ((float)num3 >= vector5.X && (float)num3 <= vector5.Y)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("creaturedie", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3[] twoPoint7 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea3 = new CubeArea(twoPoint7[0], twoPoint7[1]);
					Vector2 vector7 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
					DynamicArray<ComponentBody> dynamicArray3 = m_subsystemBodies.Bodies.ToDynamicArray();
					foreach (ComponentBody item3 in dynamicArray3)
					{
						if (cubeArea3.Exist(item3.Position))
						{
							ComponentCreature componentCreature = item3.Entity.FindComponent<ComponentCreature>();
							if (componentCreature != null && componentCreature.ComponentHealth.Health <= 0f && !DeadCreatureList.Contains(componentCreature))
							{
								Time.QueueTimeDelayedExecution(Time.RealTime + 0.10000000149011612, delegate
								{
									if (!DeadCreatureList.Contains(componentCreature))
									{
										DeadCreatureList.Add(componentCreature);
									}
								});
								return SubmitResult.Success;
							}
						}
					}
				}
				else if (commandData.Type == "limit")
				{
					Point3[] twoPoint8 = GetTwoPoint("pos1", "pos2", commandData);
					string text3 = (string)commandData.GetValue("obj");
					CubeArea cubeArea4 = new CubeArea(twoPoint8[0], twoPoint8[1]);
					Vector2 vector8 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
					DynamicArray<ComponentBody> dynamicArray4 = m_subsystemBodies.Bodies.ToDynamicArray();
					foreach (ComponentBody item4 in dynamicArray4)
					{
						if (text3 == item4.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() && cubeArea4.Exist(item4.Position))
						{
							ComponentCreature componentCreature2 = item4.Entity.FindComponent<ComponentCreature>();
							if (componentCreature2 != null && componentCreature2.ComponentHealth.Health <= 0f && !DeadCreatureList.Contains(componentCreature2))
							{
								Time.QueueTimeDelayedExecution(Time.RealTime + 0.10000000149011612, delegate
								{
									if (!DeadCreatureList.Contains(componentCreature2))
									{
										DeadCreatureList.Add(componentCreature2);
									}
								});
								return SubmitResult.Success;
							}
						}
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("dropexist", delegate(CommandData commandData)
			{
				Point3[] twoPoint9 = GetTwoPoint("pos1", "pos2", commandData);
				int num4 = (int)commandData.GetValue("id");
				CubeArea cubeArea5 = new CubeArea(twoPoint9[0], twoPoint9[1]);
				foreach (Pickable pickable in m_subsystemPickables.Pickables)
				{
					if (pickable.Value == num4 && cubeArea5.Exist(pickable.Position))
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("itemexist", delegate(CommandData commandData)
			{
				if (commandData.Type == "default" || commandData.Type == "onlyid")
				{
					int num5 = (int)commandData.GetValue("id");
					bool flag = commandData.Type == "onlyid";
					ComponentInventoryBase componentInventoryBase = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>(true);
					ComponentCraftingTable componentCraftingTable = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(true);
					foreach (ComponentInventoryBase.Slot slot in componentInventoryBase.m_slots)
					{
						int num6 = (flag ? Terrain.ExtractContents(slot.Value) : slot.Value);
						if (num6 == num5 && slot.Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
					foreach (ComponentInventoryBase.Slot slot2 in componentCraftingTable.m_slots)
					{
						int num7 = (flag ? Terrain.ExtractContents(slot2.Value) : slot2.Value);
						if (num7 == num5 && slot2.Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type.Contains("main") || commandData.Type.Contains("inventory") || commandData.Type.Contains("craft"))
				{
					int num8 = (int)commandData.GetValue("id");
					int num9 = (int)commandData.GetValue("s");
					int num10 = -1;
					int num11 = -1;
					List<ComponentInventoryBase.Slot> list = null;
					switch (commandData.Type)
					{
					case "main":
						num10 = num9 - 1;
						break;
					case "limmain":
						num10 = num9 - 1;
						num11 = (int)commandData.GetValue("v");
						break;
					case "inventory":
						num10 = num9 + 9;
						break;
					case "liminventory":
						num10 = num9 + 9;
						num11 = (int)commandData.GetValue("v");
						break;
					case "craft":
						num10 = num9 - 1;
						break;
					case "limcraft":
						num10 = num9 - 1;
						num11 = (int)commandData.GetValue("v");
						break;
					default:
						return SubmitResult.Fail;
					}
					list = (commandData.Type.Contains("craft") ? m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(true).m_slots : m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>(true).m_slots);
					if (list[num10].Value == num8)
					{
						if (num11 == -1)
						{
							if (list[num10].Count >= 1)
							{
								return SubmitResult.Success;
							}
						}
						else if (list[num10].Count == num11)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type.Contains("chest"))
				{
					int num12 = (int)commandData.GetValue("id");
					Point3 onePoint4 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(onePoint4.X, onePoint4.Y, onePoint4.Z);
					if (blockEntity == null)
					{
						return SubmitResult.Fail;
					}
					ComponentChest componentChest = blockEntity.Entity.FindComponent<ComponentChest>();
					if (componentChest == null)
					{
						return SubmitResult.Fail;
					}
					if (commandData.Type == "chest")
					{
						foreach (ComponentInventoryBase.Slot slot3 in componentChest.m_slots)
						{
							if (slot3.Value == num12 && slot3.Count >= 1)
							{
								return SubmitResult.Success;
							}
						}
					}
					else if (commandData.Type == "slotchest")
					{
						int num13 = (int)commandData.GetValue("s");
						int index = num13 - 1;
						if (componentChest.m_slots[index].Value == num12 && componentChest.m_slots[index].Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
					else if (commandData.Type == "limchest")
					{
						int num14 = (int)commandData.GetValue("s");
						int num15 = (int)commandData.GetValue("v");
						int index2 = num14 - 1;
						if (componentChest.m_slots[index2].Value == num12 && componentChest.m_slots[index2].Count == num15)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type.Contains("table"))
				{
					int num16 = (int)commandData.GetValue("id");
					Point3 onePoint5 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity2 = m_subsystemBlockEntities.GetBlockEntity(onePoint5.X, onePoint5.Y, onePoint5.Z);
					if (blockEntity2 == null)
					{
						return SubmitResult.Fail;
					}
					ComponentCraftingTable componentCraftingTable2 = blockEntity2.Entity.FindComponent<ComponentCraftingTable>();
					if (componentCraftingTable2 == null)
					{
						return SubmitResult.Fail;
					}
					if (commandData.Type == "table")
					{
						foreach (ComponentInventoryBase.Slot slot4 in componentCraftingTable2.m_slots)
						{
							if (slot4.Value == num16 && slot4.Count >= 1)
							{
								return SubmitResult.Success;
							}
						}
					}
					else if (commandData.Type == "slottable")
					{
						int num17 = (int)commandData.GetValue("s");
						int index3 = num17 - 1;
						if (componentCraftingTable2.m_slots[index3].Value == num16 && componentCraftingTable2.m_slots[index3].Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
					else if (commandData.Type == "limtable")
					{
						int num18 = (int)commandData.GetValue("s");
						int num19 = (int)commandData.GetValue("v");
						int index4 = num18 - 1;
						if (componentCraftingTable2.m_slots[index4].Value == num16 && componentCraftingTable2.m_slots[index4].Count == num19)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type.Contains("dispenser"))
				{
					int num20 = (int)commandData.GetValue("id");
					Point3 onePoint6 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity3 = m_subsystemBlockEntities.GetBlockEntity(onePoint6.X, onePoint6.Y, onePoint6.Z);
					if (blockEntity3 == null)
					{
						return SubmitResult.Fail;
					}
					ComponentDispenser componentDispenser = blockEntity3.Entity.FindComponent<ComponentDispenser>();
					if (componentDispenser == null)
					{
						return SubmitResult.Fail;
					}
					if (commandData.Type == "dispenser")
					{
						foreach (ComponentInventoryBase.Slot slot5 in componentDispenser.m_slots)
						{
							if (slot5.Value == num20 && slot5.Count >= 1)
							{
								return SubmitResult.Success;
							}
						}
					}
					else if (commandData.Type == "slotdispenser")
					{
						int num21 = (int)commandData.GetValue("s");
						int index5 = num21 - 1;
						if (componentDispenser.m_slots[index5].Value == num20 && componentDispenser.m_slots[index5].Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
					else if (commandData.Type == "limdispenser")
					{
						int num22 = (int)commandData.GetValue("s");
						int num23 = (int)commandData.GetValue("v");
						int index6 = num22 - 1;
						if (componentDispenser.m_slots[index6].Value == num20 && componentDispenser.m_slots[index6].Count == num23)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type.Contains("furnace"))
				{
					int num24 = (int)commandData.GetValue("id");
					Point3 onePoint7 = GetOnePoint("pos", commandData);
					ComponentBlockEntity blockEntity4 = m_subsystemBlockEntities.GetBlockEntity(onePoint7.X, onePoint7.Y, onePoint7.Z);
					if (blockEntity4 == null)
					{
						return SubmitResult.Fail;
					}
					ComponentFurnace componentFurnace = blockEntity4.Entity.FindComponent<ComponentFurnace>();
					if (componentFurnace == null)
					{
						return SubmitResult.Fail;
					}
					if (commandData.Type == "furnace")
					{
						foreach (ComponentInventoryBase.Slot slot6 in componentFurnace.m_slots)
						{
							if (slot6.Value == num24 && slot6.Count >= 1)
							{
								return SubmitResult.Success;
							}
						}
					}
					else if (commandData.Type == "slotfurnace")
					{
						int num25 = (int)commandData.GetValue("s");
						int index7 = num25 - 1;
						if (componentFurnace.m_slots[index7].Value == num24 && componentFurnace.m_slots[index7].Count >= 1)
						{
							return SubmitResult.Success;
						}
					}
					else if (commandData.Type == "limfurnace")
					{
						int num26 = (int)commandData.GetValue("s");
						int num27 = (int)commandData.GetValue("v");
						int index8 = num26 - 1;
						if (componentFurnace.m_slots[index8].Value == num24 && componentFurnace.m_slots[index8].Count == num27)
						{
							return SubmitResult.Success;
						}
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("handitem", delegate(CommandData commandData)
			{
				int activeSlotIndex = m_componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex;
				int slotValue = m_componentPlayer.ComponentMiner.Inventory.GetSlotValue(activeSlotIndex);
				int slotCount = m_componentPlayer.ComponentMiner.Inventory.GetSlotCount(activeSlotIndex);
				if (commandData.Type == "default")
				{
					int num28 = (int)commandData.GetValue("id");
					if (slotValue == num28 && slotCount >= 1)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "limit")
				{
					int num29 = (int)commandData.GetValue("id");
					int num30 = (int)commandData.GetValue("v");
					if (slotValue == num29 && slotCount == num30)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "empty" && Terrain.ExtractContents(slotValue) == 0 && slotCount == 0)
				{
					return SubmitResult.Success;
				}
				return SubmitResult.Fail;
			});
			AddCondition("levelrange", delegate(CommandData commandData)
			{
				Vector2 vector9 = (Vector2)commandData.GetValue("vec2");
				int num31 = (int)m_componentPlayer.PlayerData.Level;
				return (!((float)num31 >= vector9.X) || !((float)num31 <= vector9.Y)) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("heightrange", delegate(CommandData commandData)
			{
				Vector2 vector10 = (Vector2)commandData.GetValue("vec2");
				float y = m_componentPlayer.ComponentBody.Position.Y;
				return (!(y >= vector10.X) || !(y <= vector10.Y)) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("eyesangle", delegate(CommandData commandData)
			{
				Point2 point = (Point2)commandData.GetValue("eyes1");
				Point2 point2 = (Point2)commandData.GetValue("eyes2");
				Point2 playerEyesAngle = DataHandle.GetPlayerEyesAngle(m_componentPlayer);
				return (playerEyesAngle.X < point.X || playerEyesAngle.X > point.Y || playerEyesAngle.Y < point2.X || playerEyesAngle.Y > point2.Y) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("statsrange", delegate(CommandData commandData)
			{
				if (commandData.Type != "default")
				{
					Vector2 vector11 = (Vector2)commandData.GetValue("vec2");
					float num32 = -1f;
					switch (commandData.Type)
					{
					case "health":
						num32 = m_componentPlayer.ComponentHealth.Health * 100f;
						break;
					case "food":
						num32 = m_componentPlayer.ComponentVitalStats.Food * 100f;
						break;
					case "stamina":
						num32 = m_componentPlayer.ComponentVitalStats.Stamina * 100f;
						break;
					case "sleep":
						num32 = m_componentPlayer.ComponentVitalStats.Sleep * 100f;
						break;
					case "attack":
						num32 = m_componentPlayer.ComponentMiner.AttackPower;
						break;
					case "defense":
						num32 = m_componentPlayer.ComponentHealth.AttackResilience;
						break;
					case "speed":
						num32 = m_componentPlayer.ComponentLocomotion.WalkSpeed * 10f;
						break;
					case "temperature":
						num32 = m_componentPlayer.ComponentVitalStats.Temperature;
						break;
					case "wetness":
						num32 = m_componentPlayer.ComponentVitalStats.Wetness * 100f;
						break;
					}
					if (num32 >= vector11.X && num32 <= vector11.Y)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("actionmake", delegate(CommandData commandData)
			{
				bool flag2 = false;
				switch (commandData.Type)
				{
				case "default":
					flag2 = m_componentPlayer.ComponentBody.Velocity.LengthSquared() > 0.0625f;
					break;
				case "sneak":
					flag2 = m_componentPlayer.ComponentBody.IsSneaking;
					break;
				case "rider":
					flag2 = m_componentPlayer.ComponentRider.Mount != null;
					break;
				case "sleep":
					flag2 = m_componentPlayer.ComponentSleep.IsSleeping;
					break;
				case "jump":
					flag2 = m_componentPlayer.ComponentInput.PlayerInput.Jump;
					break;
				case "hasflu":
					flag2 = m_componentPlayer.ComponentFlu.HasFlu;
					break;
				case "sick":
					flag2 = m_componentPlayer.ComponentSickness.IsSick;
					break;
				case "moveup":
					flag2 = m_componentPlayer.ComponentInput.PlayerInput.Move.Z > 0f;
					break;
				case "movedown":
					flag2 = m_componentPlayer.ComponentInput.PlayerInput.Move.Z < 0f;
					break;
				case "moveleft":
					flag2 = m_componentPlayer.ComponentInput.PlayerInput.Move.X < 0f;
					break;
				case "moveright":
					flag2 = m_componentPlayer.ComponentInput.PlayerInput.Move.X > 0f;
					break;
				default:
					if (commandData.Type.StartsWith("look"))
					{
						float num33 = MathUtils.Abs(m_componentPlayer.ComponentInput.PlayerInput.Look.X);
						float num34 = MathUtils.Abs(m_componentPlayer.ComponentInput.PlayerInput.Look.Y);
						int num35 = (int)((double)MathUtils.Atan(num34 / num33) / 3.14 * 180.0);
						if (commandData.Type == "lookup")
						{
							flag2 = m_componentPlayer.ComponentInput.PlayerInput.Look.Y > 0f && num35 > 30;
						}
						else if (commandData.Type == "lookdown")
						{
							flag2 = m_componentPlayer.ComponentInput.PlayerInput.Look.Y < 0f && num35 > 30;
						}
						else if (commandData.Type == "lookleft")
						{
							flag2 = m_componentPlayer.ComponentInput.PlayerInput.Look.X < 0f && num35 < 60;
						}
						else if (commandData.Type == "lookright")
						{
							flag2 = m_componentPlayer.ComponentInput.PlayerInput.Look.X > 0f && num35 < 60;
						}
					}
					break;
				}
				return (!flag2) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("openwidget", delegate(CommandData commandData)
			{
				bool flag3 = false;
				switch (commandData.Type)
				{
				case "default":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget == null && NoContainDialog();
					break;
				case "chest":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is ChestWidget;
					break;
				case "table":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is CraftingTableWidget;
					break;
				case "dispenser":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is DispenserWidget;
					break;
				case "furnace":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is FurnaceWidget;
					break;
				case "clothing":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is ClothingWidget;
					break;
				case "inventory":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is CreativeInventoryWidget || m_componentPlayer.ComponentGui.ModalPanelWidget is FullInventoryWidget;
					break;
				case "stats":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is VitalStatsWidget;
					break;
				case "command":
					flag3 = m_componentPlayer.ComponentGui.ModalPanelWidget is CommandEditWidget;
					break;
				case "sign":
					flag3 = IsContainDialog(commandData.Type);
					break;
				case "memorybank":
					flag3 = IsContainDialog(commandData.Type);
					break;
				case "truthcircuit":
					flag3 = IsContainDialog(commandData.Type);
					break;
				case "delaygate":
					flag3 = IsContainDialog(commandData.Type);
					break;
				case "battery":
					flag3 = IsContainDialog(commandData.Type);
					break;
				case "piston":
					flag3 = IsContainDialog(commandData.Type);
					break;
				}
				return (!flag3) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("gamemode", delegate(CommandData commandData)
			{
				if (commandData.Type != "default")
				{
					bool flag4 = false;
					switch (commandData.Type)
					{
					case "creative":
						flag4 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
						break;
					case "harmless":
						flag4 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless;
						break;
					case "challenge":
						flag4 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Challenging;
						break;
					case "cruel":
						flag4 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel;
						break;
					case "adventure":
						flag4 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure;
						break;
					}
					if (flag4)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("camerapos", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3[] twoPoint10 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea6 = new CubeArea(twoPoint10[0], twoPoint10[1]);
					if (cubeArea6.Exist(m_componentPlayer.GameWidget.ActiveCamera.ViewPosition))
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "direction")
				{
					Point2 point3 = (Point2)commandData.GetValue("eyes1");
					Point2 point4 = (Point2)commandData.GetValue("eyes2");
					Point2 point5 = DataHandle.DirectionToEyes(m_componentPlayer.PlayerData.GameWidget.ActiveCamera.ViewDirection);
					if (point5.X >= point3.X && point5.X <= point3.Y && point5.Y >= point4.X && point5.Y <= point4.Y)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("signtext", delegate(CommandData commandData)
			{
				Point3 onePoint8 = GetOnePoint("pos", commandData);
				string text4 = (string)commandData.GetValue("text");
				SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>();
				SignData signData = subsystemSignBlockBehavior.GetSignData(onePoint8);
				if (signData == null)
				{
					return SubmitResult.Fail;
				}
				if (commandData.Type == "default")
				{
					string[] lines = signData.Lines;
					foreach (string text5 in lines)
					{
						if (text5.Contains(text4))
						{
							return SubmitResult.Success;
						}
					}
				}
				else
				{
					bool flag5 = false;
					switch (commandData.Type)
					{
					case "lineone":
						flag5 = signData.Lines[0] == text4;
						break;
					case "linetwo":
						flag5 = signData.Lines[1] == text4;
						break;
					case "linethree":
						flag5 = signData.Lines[2] == text4;
						break;
					case "linefour":
						flag5 = signData.Lines[3] == text4;
						break;
					case "lineurl":
						flag5 = signData.Url == text4;
						break;
					}
					if (flag5)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("clickinteract", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Point3 onePoint9 = GetOnePoint("pos", commandData);
					int num36 = (int)commandData.GetValue("v");
					if (m_interactResult != null && m_interactResult is TerrainRaycastResult && ((TerrainRaycastResult)m_interactResult).Distance <= (float)num36 && ((TerrainRaycastResult)m_interactResult).CellFace.Point == onePoint9)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "limblock")
				{
					Point3 onePoint10 = GetOnePoint("pos", commandData);
					int num37 = (int)commandData.GetValue("id");
					int num38 = (int)commandData.GetValue("v");
					if (m_interactResult != null && m_interactResult is TerrainRaycastResult && ((TerrainRaycastResult)m_interactResult).Distance <= (float)num38 && ((TerrainRaycastResult)m_interactResult).CellFace.Point == onePoint10 && GetLimitValue(onePoint10.X, onePoint10.Y, onePoint10.Z) == num37)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "creature")
				{
					string text6 = (string)commandData.GetValue("obj");
					int num39 = (int)commandData.GetValue("v");
					if (m_interactResult != null && m_interactResult is BodyRaycastResult && ((BodyRaycastResult)m_interactResult).Distance <= (float)num39)
					{
						Entity entity = ((BodyRaycastResult)m_interactResult).ComponentBody.Entity;
						if (text6.ToLower() == entity.ValuesDictionary.DatabaseObject.Name.ToLower())
						{
							return SubmitResult.Success;
						}
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("longpress", delegate(CommandData commandData)
			{
				int num40 = (int)commandData.GetValue("v");
				return (!(m_aimDurationTime >= (float)num40)) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("timerange", delegate(CommandData commandData)
			{
				if (commandData.Type == "default")
				{
					Vector2 vector12 = (Vector2)commandData.GetValue("vec2");
					SubsystemTimeOfDay subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>();
					int num41 = (int)(subsystemTimeOfDay.TimeOfDay * 4096f);
					if ((float)num41 >= vector12.X && (float)num41 <= vector12.Y)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "system")
				{
					DateTime value2 = (DateTime)commandData.GetValue("time1");
					DateTime value3 = (DateTime)commandData.GetValue("time2");
					if (DateTime.Now.CompareTo(value2) >= 0 && DateTime.Now.CompareTo(value3) <= 0)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "worldrun")
				{
					Vector2 vector13 = (Vector2)commandData.GetValue("vec2");
					if (m_worldRunTime >= vector13.X && m_worldRunTime <= vector13.Y)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("fileexist", delegate(CommandData commandData)
			{
				string pathName = (string)commandData.GetValue("f");
				return (!Storage.FileExists(DataHandle.GetCommandResPathName(pathName))) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("modcount", delegate(CommandData commandData)
			{
				Vector2 vector14 = (Vector2)commandData.GetValue("vec2");
				return (!((float)ModsManager.ModList.Count >= vector14.X) || !((float)ModsManager.ModList.Count <= vector14.Y)) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("oncapture", (CommandData commandData) => (!m_onCapture) ? SubmitResult.Fail : SubmitResult.Success);
			AddCondition("eatorwear", delegate(CommandData commandData)
			{
				int num42 = (int)commandData.GetValue("id");
				return (!m_eatItem.HasValue || m_eatItem.Value.X != num42) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("clothes", delegate(CommandData commandData)
			{
				int num43 = (int)commandData.GetValue("id");
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(num43)];
				if (block is ClothingBlock)
				{
					ClothingData clothingData = block.GetClothingData(num43);
					if (clothingData != null)
					{
						foreach (int clothe in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
						{
							if (clothe == num43)
							{
								return SubmitResult.Success;
							}
						}
					}
				}
				return SubmitResult.Fail;
			});
			AddCondition("patternbutton", delegate(CommandData commandData)
			{
				string key = (string)commandData.GetValue("f");
				ScreenPattern value4;
				return (!ScreenPatterns.TryGetValue(key, out value4) || !(value4.OutTime > 0f)) ? SubmitResult.Fail : SubmitResult.Success;
			});
			AddCondition("moveset", delegate(CommandData commandData)
			{
				string n = (string)commandData.GetValue("n");
				string tag = GetMovingBlockTagLine(n);
				if (commandData.Type == "default")
				{
					Point3[] twoPoint11 = GetTwoPoint("pos1", "pos2", commandData);
					CubeArea cubeArea7 = new CubeArea(twoPoint11[0], twoPoint11[1]);
					if (tag == null)
					{
						List<Point3> value5;
						if (FindWaitMoveSet(n, out tag, out value5) && value5.Count > 0 && cubeArea7.Exist(new Vector3(value5[0])) && cubeArea7.Exist(new Vector3(value5[value5.Count - 1]) + Vector3.One))
						{
							return SubmitResult.Success;
						}
					}
					else
					{
						IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag);
						SubsystemMovingBlocks.MovingBlockSet movingBlockSet2 = (SubsystemMovingBlocks.MovingBlockSet)movingBlockSet;
						if (cubeArea7.Exist(movingBlockSet2.Position) && cubeArea7.Exist(movingBlockSet2.Position + new Vector3(movingBlockSet2.Box.Size)))
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type == "pausestop")
				{
					if (tag == null)
					{
						List<Point3> value6;
						if (FindWaitMoveSet(n, out tag, out value6))
						{
							return SubmitResult.Success;
						}
					}
					else
					{
						IMovingBlockSet movingBlockSet3 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag);
						if (movingBlockSet3.CurrentVelocity == Vector3.Zero)
						{
							return SubmitResult.Success;
						}
					}
				}
				else if (commandData.Type == "collideblock")
				{
					MovingCollision value7;
					if (tag != null && m_movingCollisions.TryGetValue(tag, out value7) && value7.Block != 0)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "collidelimblock")
				{
					int num44 = (int)commandData.GetValue("id");
					MovingCollision value8;
					if (tag != null && m_movingCollisions.TryGetValue(tag, out value8) && value8.Block == num44)
					{
						return SubmitResult.Success;
					}
				}
				else if (commandData.Type == "collideentity")
				{
					string obj = (string)commandData.GetValue("obj");
					string entityName = EntityInfoManager.GetEntityName(obj);
					MovingCollision value9;
					if (tag != null && m_movingCollisions.TryGetValue(tag, out value9) && value9.Creature == entityName)
					{
						return SubmitResult.Success;
					}
				}
				return SubmitResult.Fail;
			});
		}

		public int GetLimitValue(int x, int y, int z)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			return Terrain.ReplaceLight(cellValue, 0);
		}

		public void ChangeBlockValue(WithdrawBlockManager wbManager, int x, int y, int z, int value, bool fast = true)
		{
			m_subsystemTerrain.ChangeCell(x, y, z, value, true, (ComponentMiner)null);
		}

		public void PlaceReprocess(WithdrawBlockManager wbManager, CommandData commandData, bool updateChunk, Point3 minPoint, Point3 maxPoint)
		{
		}

		public bool ErgodicBody(string target, Func<ComponentBody, bool> action)
		{
			target = target.ToLower();
			Vector2 vector = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
			DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
			m_subsystemBodies.FindBodiesInArea(vector - new Vector2(64f), vector + new Vector2(64f), dynamicArray);
			foreach (ComponentBody item in dynamicArray)
			{
				switch (target)
				{
				case "player":
				{
					ComponentPlayer componentPlayer4 = item.Entity.FindComponent<ComponentPlayer>();
					if (componentPlayer4 != null && action(item))
					{
						return true;
					}
					break;
				}
				case "boat":
				{
					ComponentBoat componentBoat = item.Entity.FindComponent<ComponentBoat>();
					if (componentBoat != null && action(item))
					{
						return true;
					}
					break;
				}
				case "creature":
				{
					ComponentPlayer componentPlayer = item.Entity.FindComponent<ComponentPlayer>();
					ComponentCreature componentCreature = item.Entity.FindComponent<ComponentCreature>();
					if (componentPlayer == null && componentCreature != null && action(item))
					{
						return true;
					}
					break;
				}
				case "vehicle":
				{
					ComponentDamage componentDamage3 = item.Entity.FindComponent<ComponentDamage>();
					if (componentDamage3 != null && action(item))
					{
						return true;
					}
					break;
				}
				case "npc":
				{
					ComponentPlayer componentPlayer2 = item.Entity.FindComponent<ComponentPlayer>();
					ComponentCreature componentCreature2 = item.Entity.FindComponent<ComponentCreature>();
					ComponentDamage componentDamage = item.Entity.FindComponent<ComponentDamage>();
					if (componentPlayer2 == null && (componentCreature2 != null || componentDamage != null) && action(item))
					{
						return true;
					}
					break;
				}
				case "all":
				{
					ComponentPlayer componentPlayer3 = item.Entity.FindComponent<ComponentPlayer>();
					ComponentCreature componentCreature3 = item.Entity.FindComponent<ComponentCreature>();
					ComponentDamage componentDamage2 = item.Entity.FindComponent<ComponentDamage>();
					if ((componentPlayer3 != null || componentCreature3 != null || componentDamage2 != null) && action(item))
					{
						return true;
					}
					break;
				}
				default:
					if (target == item.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() && action(item))
					{
						return true;
					}
					break;
				}
			}
			return false;
		}

		public Point3 GetPlayerPoint()
		{
			int x = (int)MathUtils.Floor(m_componentPlayer.ComponentBody.Position.X);
			int y = (int)MathUtils.Floor(m_componentPlayer.ComponentBody.Position.Y);
			int z = (int)MathUtils.Floor(m_componentPlayer.ComponentBody.Position.Z);
			return new Point3(x, y, z);
		}

		public Point3 GetActualPosition(CommandData commandData)
		{
			Point3 result = Point3.Zero;
			if (commandData.Coordinate == CoordinateMode.Player)
			{
				result = GetPlayerPoint();
			}
			else if (commandData.Coordinate == CoordinateMode.Command)
			{
				result = commandData.Position;
			}
			return result;
		}

		public Point3 GetOnePoint(string pos, CommandData commandData)
		{
			return (Point3)commandData.GetValue(pos) + GetActualPosition(commandData);
		}

		public Point3[] GetTwoPoint(string pos1, string pos2, CommandData commandData)
		{
			Point3 actualPosition = GetActualPosition(commandData);
			return new Point3[2]
			{
				(Point3)commandData.GetValue(pos1) + actualPosition,
				(Point3)commandData.GetValue(pos2) + actualPosition
			};
		}

		public Stream GetCommandFileStream(string f, OpenFileMode fileMode)
		{
			string commandResPathName = DataHandle.GetCommandResPathName(f);
			bool flag = fileMode == OpenFileMode.Create || fileMode == OpenFileMode.CreateOrOpen;
			if (!Storage.FileExists(commandResPathName) && !flag)
			{
				ShowSubmitTips(string.Format("在{0}目录中找不到文件:{1}", DataHandle.GetCommandPath(), f));
				return null;
			}
			Stream stream = Storage.OpenFile(commandResPathName, fileMode);
			if (stream != null)
			{
				return stream;
			}
			ShowSubmitTips("创建流失败，引发文件：" + f);
			return null;
		}

		public void UpdateChunks(Point3 minPoint, Point3 maxPoint)
		{
			Point2 point = Terrain.ToChunk(minPoint.X, minPoint.Z);
			Point2 point2 = Terrain.ToChunk(maxPoint.X, maxPoint.Z);
			for (int i = point.X - 1; i <= point2.X + 1; i++)
			{
				for (int j = point.Y - 1; j <= point2.Y + 1; j++)
				{
					TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(i, j);
					if (chunkAtCoords != null)
					{
						if (chunkAtCoords.State > TerrainChunkState.InvalidLight)
						{
							chunkAtCoords.State = TerrainChunkState.InvalidLight;
						}
						chunkAtCoords.WasDowngraded = true;
						chunkAtCoords.ModificationCounter++;
					}
				}
			}
		}

		public void UpdateAllChunks(float time, TerrainChunkState chunkState)
		{
			if (time == 0f)
			{
				m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(chunkState, true);
			}
			Time.QueueTimeDelayedExecution(Time.RealTime + (double)time, delegate
			{
				m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(chunkState, true);
			});
		}

		public void InitScreenLabelCanvas()
		{
			m_screenPatternsWidget = new CanvasWidget();
			m_screenPatternsWidget.Name = "ScreenPatterns";
			m_componentPlayer.GameWidget.SetWidgetPosition(m_screenPatternsWidget, Vector2.Zero);
			m_screenPatternsWidget.IsHitTestVisible = false;
			m_screenPatternsWidget.ClampToBounds = true;
			m_screenPatternsWidget.IsVisible = false;
			m_componentPlayer.GameWidget.Children.Add(m_screenPatternsWidget);
			XElement node = ContentManager.Get<XElement>("Widgets/CommandLabelWidget");
			m_screenLabelCanvasWidget = (ContainerWidget)Widget.LoadWidget(m_componentPlayer.GameWidget, node, null);
			m_screenLabelCanvasWidget.IsVisible = false;
			m_componentPlayer.GameWidget.Children.Add(m_screenLabelCanvasWidget);
		}

		public void MoveBlockCollidedEntity(Entity entity)
		{
			ComponentBody componentBody = entity.FindComponent<ComponentBody>();
			if (componentBody == null)
			{
			}
		}

		public void MoveBlockCollidedAction()
		{
			m_subsystemMovingBlocks.CollidedWithTerrain += delegate(IMovingBlockSet movingBlockSet, Point3 pos)
			{
				if (movingBlockSet.Id != null)
				{
					if (movingBlockSet.Id == "moveblock$dig")
					{
						m_subsystemTerrain.DestroyCell(1, pos.X, pos.Y, pos.Z, 0, false, false, (ComponentMiner)null);
					}
					else if (movingBlockSet.Id == "moveblock$limit")
					{
						movingBlockSet.Stop();
					}
					else if (movingBlockSet.Id == "moveset")
					{
						if (m_movingCollisions.ContainsKey((string)movingBlockSet.Tag))
						{
							m_movingCollisions[(string)movingBlockSet.Tag].Block = m_subsystemTerrain.Terrain.GetCellValue(pos.X, pos.Y, pos.Z);
						}
						else
						{
							m_movingCollisions[(string)movingBlockSet.Tag] = new MovingCollision
							{
								Block = m_subsystemTerrain.Terrain.GetCellValue(pos.X, pos.Y, pos.Z)
							};
						}
					}
				}
			};
			m_subsystemMovingBlocks.Stopped += delegate(IMovingBlockSet movingBlockSet)
			{
				if (movingBlockSet.Id != null && movingBlockSet.Id.StartsWith("moveblock"))
				{
					foreach (MovingBlock block in movingBlockSet.Blocks)
					{
						Point3 point = new Point3((int)MathUtils.Round(movingBlockSet.Position.X), (int)MathUtils.Round(movingBlockSet.Position.Y), (int)MathUtils.Round(movingBlockSet.Position.Z));
						m_subsystemTerrain.ChangeCell(point.X + block.Offset.X, point.Y + block.Offset.Y, point.Z + block.Offset.Z, block.Value, true, (ComponentMiner)null);
					}
					m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet);
				}
			};
		}

		public IMovingBlockSet WaitMoveSetTurnToWork(string tag, List<Point3> list)
		{
			List<MovingBlock> list2 = new List<MovingBlock>();
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			int num3 = int.MaxValue;
			foreach (Point3 item in list)
			{
				if (num > item.X)
				{
					num = item.X;
				}
				if (num2 > item.Y)
				{
					num2 = item.Y;
				}
				if (num3 > item.Z)
				{
					num3 = item.Z;
				}
			}
			foreach (Point3 item2 in list)
			{
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(item2.X, item2.Y, item2.Z);
				int id = Terrain.ExtractContents(cellValue);
				GetMoveEntityBlocks(tag, id, item2, item2 - new Point3(num, num2, num3));
				list2.Add(new MovingBlock
				{
					Value = cellValue,
					Offset = item2 - new Point3(num, num2, num3)
				});
			}
			Vector3 vector = new Vector3(num, num2, num3);
			IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.AddMovingBlockSet(vector, vector, 0f, 0f, 0f, new Vector2(1f, 1f), list2, "moveset", tag, true);
			if (movingBlockSet != null)
			{
				foreach (Point3 item3 in list)
				{
					m_subsystemTerrain.ChangeCell(item3.X, item3.Y, item3.Z, 0, true, (ComponentMiner)null);
				}
			}
			m_waitingMoveSets.Remove(tag);
			return movingBlockSet;
		}

		public void GetMoveEntityBlocks(string tag, int id, Point3 p, Point3 offset)
		{
			if (!m_movingEntityBlocks.ContainsKey(tag))
			{
				m_movingEntityBlocks[tag] = new List<MovingEntityBlock>();
			}
			if (id == 27 || id == 45 || id == 64 || id == 216)
			{
				ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(p.X, p.Y, p.Z);
				if (blockEntity == null)
				{
					return;
				}
				List<ComponentInventoryBase.Slot> slots = blockEntity.Entity.FindComponent<ComponentInventoryBase>().m_slots;
				string text = string.Empty;
				foreach (ComponentInventoryBase.Slot item in slots)
				{
					text = text + item.Value + ":" + item.Count + ";";
				}
				blockEntity.Entity.FindComponent<ComponentInventoryBase>().m_slots.Clear();
				m_movingEntityBlocks[tag].Add(new MovingEntityBlock
				{
					Id = id,
					Offset = offset,
					Data = text
				});
				return;
			}
			if (id == 97 || id == 210 || id == 98 || id == 211)
			{
				SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>();
				SignData signData = subsystemSignBlockBehavior.GetSignData(p);
				if (signData != null)
				{
					string empty = string.Empty;
					empty += "Colors=";
					for (int i = 0; i < signData.Colors.Length; i++)
					{
						empty = empty + signData.Colors[i].ToString() + ";";
					}
					empty += "&Lines=";
					for (int j = 0; j < signData.Lines.Length; j++)
					{
						empty = empty + signData.Lines[j] + ";";
					}
					empty = empty + "&Url=" + signData.Url;
					m_movingEntityBlocks[tag].Add(new MovingEntityBlock
					{
						Id = id,
						Offset = offset,
						Data = empty
					});
				}
				return;
			}
			switch (id)
			{
			case 333:
			{
				SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = base.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
				CommandData commandData = subsystemCommandBlockBehavior.GetCommandData(p);
				if (commandData != null)
				{
					string empty3 = string.Empty;
					empty3 = commandData.Line;
					m_movingEntityBlocks[tag].Add(new MovingEntityBlock
					{
						Id = id,
						Offset = offset,
						Data = empty3
					});
				}
				break;
			}
			case 186:
			{
				SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
				MemoryBankData blockData = subsystemMemoryBankBlockBehavior.GetBlockData(p);
				if (blockData != null)
				{
					string empty2 = string.Empty;
					empty2 = blockData.SaveString();
					m_movingEntityBlocks[tag].Add(new MovingEntityBlock
					{
						Id = id,
						Offset = offset,
						Data = empty2
					});
				}
				break;
			}
			}
		}

		public void SetMoveEntityBlocks(IMovingBlockSet movingBlockSet)
		{
			string key = (string)movingBlockSet.Tag;
			if (!m_movingEntityBlocks.ContainsKey(key))
			{
				return;
			}
			foreach (MovingEntityBlock item in m_movingEntityBlocks[key])
			{
				Point3 point = new Point3((int)MathUtils.Round(movingBlockSet.Position.X), (int)MathUtils.Round(movingBlockSet.Position.Y), (int)MathUtils.Round(movingBlockSet.Position.Z)) + item.Offset;
				if (m_subsystemTerrain.Terrain.GetCellContents(point.X, point.Y, point.Z) != item.Id || item.Data == null)
				{
					continue;
				}
				if (item.Id == 27 || item.Id == 45 || item.Id == 64 || item.Id == 216)
				{
					ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
					if (blockEntity == null)
					{
						continue;
					}
					ComponentInventoryBase componentInventoryBase = blockEntity.Entity.FindComponent<ComponentInventoryBase>();
					string[] array = item.Data.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						string[] array2 = array[i].Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
						if (item.Id == 27)
						{
							if (i < array.Length - 2)
							{
								componentInventoryBase.AddSlotItems(i, int.Parse(array2[0]), int.Parse(array2[1]));
							}
						}
						else
						{
							componentInventoryBase.AddSlotItems(i, int.Parse(array2[0]), int.Parse(array2[1]));
						}
					}
				}
				else if (item.Id == 97 || item.Id == 210 || item.Id == 98 || item.Id == 211)
				{
					SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>();
					string[] array3 = item.Data.Split(new char[1] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					string[] array4 = array3[0].Replace("Colors=", "").Split(';');
					string[] array5 = array3[1].Replace("Lines=", "").Split(';');
					Color[] array6 = new Color[4];
					for (int j = 0; j < array6.Length; j++)
					{
						array6[j] = DataHandle.GetColorValue(array4[j]);
					}
					string[] array7 = new string[4];
					for (int k = 0; k < array7.Length; k++)
					{
						array7[k] = array5[k];
					}
					string url = array3[2].Replace("Url=", "");
					subsystemSignBlockBehavior.SetSignData(point, array7, array6, url);
				}
				else if (item.Id == 333)
				{
					SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = base.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
					string data = item.Data;
					subsystemCommandBlockBehavior.SetCommandData(point, data);
				}
				else if (item.Id == 186)
				{
					SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
					MemoryBankData memoryBankData = new MemoryBankData();
					memoryBankData.LoadString(item.Data);
					subsystemMemoryBankBlockBehavior.SetBlockData(point, memoryBankData);
				}
			}
			m_movingEntityBlocks.Remove(key);
		}

		public void LoadMoveEntityBlocks(ValuesDictionary valuesDictionary)
		{
			m_movingEntityBlocks.Clear();
			foreach (ValuesDictionary value3 in valuesDictionary.GetValue<ValuesDictionary>("MovingEntityBlocks").Values)
			{
				string value = value3.GetValue<string>("Tag");
				ValuesDictionary value2 = value3.GetValue<ValuesDictionary>("MovingEntity");
				List<MovingEntityBlock> list = new List<MovingEntityBlock>();
				m_movingEntityBlocks[value] = list;
				foreach (string key in value3.GetValue<ValuesDictionary>("MovingEntity").Keys)
				{
					MovingEntityBlock movingEntityBlock = new MovingEntityBlock();
					movingEntityBlock.Offset = DataHandle.GetPoint3Value(key);
					list.Add(movingEntityBlock);
				}
				foreach (MovingEntityBlock item in list)
				{
					string[] array = value2.GetValue<string>(item.Offset.ToString()).Split('@');
					item.Id = int.Parse(array[0]);
					item.Data = array[1];
				}
			}
		}

		public void SaveMoveEntityBlocks(ValuesDictionary valuesDictionary)
		{
			int num = 0;
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("MovingEntityBlocks", valuesDictionary2);
			foreach (string key in m_movingEntityBlocks.Keys)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				ValuesDictionary valuesDictionary4 = new ValuesDictionary();
				valuesDictionary3.SetValue("Tag", key);
				valuesDictionary3.SetValue("MovingEntity", valuesDictionary4);
				foreach (MovingEntityBlock item in m_movingEntityBlocks[key])
				{
					valuesDictionary4.SetValue(item.Offset.ToString(), item.Id + "@" + item.Data);
				}
				valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				num++;
			}
		}

		public void LoadWaitMoveSet(ValuesDictionary valuesDictionary)
		{
			m_waitingMoveSets.Clear();
			foreach (ValuesDictionary value3 in valuesDictionary.GetValue<ValuesDictionary>("WaitingMoveSets").Values)
			{
				string value = value3.GetValue<string>("Tag");
				string value2 = value3.GetValue<string>("Points");
				List<Point3> list = new List<Point3>();
				string[] array = value2.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string str in array)
				{
					list.Add(DataHandle.GetPoint3Value(str));
				}
				m_waitingMoveSets[value] = list;
			}
		}

		public void SaveWaitMoveSet(ValuesDictionary valuesDictionary)
		{
			int num = 0;
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("WaitingMoveSets", valuesDictionary2);
			foreach (string key in m_waitingMoveSets.Keys)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary3.SetValue("Tag", key);
				string text = string.Empty;
				foreach (Point3 item in m_waitingMoveSets[key])
				{
					text = text + item.ToString() + ";";
				}
				valuesDictionary3.SetValue("Points", text);
				valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				num++;
			}
		}

		public bool ExistWaitMoveSet(string n)
		{
			foreach (string key in m_waitingMoveSets.Keys)
			{
				string[] array = key.Split('$');
				if (array.Length != 0 && array[0] == n)
				{
					return true;
				}
			}
			return false;
		}

		public bool FindWaitMoveSet(string n, out string tag, out List<Point3> value)
		{
			foreach (string key in m_waitingMoveSets.Keys)
			{
				string[] array = key.Split('$');
				if (array.Length != 0 && array[0] == n)
				{
					tag = key;
					value = m_waitingMoveSets[key];
					return true;
				}
			}
			tag = null;
			value = null;
			return false;
		}

		public string SetMovingBlockTagLine(string n, string face, Point3 axis)
		{
			return n + "$" + face + "$" + axis.ToString();
		}

		public string GetMovingBlockTagLine(string n)
		{
			foreach (IMovingBlockSet movingBlockSet in m_subsystemMovingBlocks.MovingBlockSets)
			{
				if (!(movingBlockSet.Id == "moveset") || movingBlockSet.Tag == null)
				{
					continue;
				}
				string[] array = ((string)movingBlockSet.Tag).Split('$');
				if (array[0] == n)
				{
					if (array.Length < 3)
					{
						((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet).Tag = SetMovingBlockTagLine(n, "+x", Point3.Zero);
					}
					return (string)movingBlockSet.Tag;
				}
			}
			return null;
		}

		public MovingBlockTag FindMovingBlockTag(string n)
		{
			foreach (IMovingBlockSet movingBlockSet in m_subsystemMovingBlocks.MovingBlockSets)
			{
				if (movingBlockSet.Tag == null)
				{
					continue;
				}
				string[] array = ((string)movingBlockSet.Tag).Split('$');
				if (array[0] == n)
				{
					MovingBlockTag movingBlockTag = new MovingBlockTag();
					movingBlockTag.Name = n;
					switch (array[1])
					{
					case "+x":
						movingBlockTag.Face = CoordDirection.PX;
						break;
					case "-x":
						movingBlockTag.Face = CoordDirection.NX;
						break;
					case "+z":
						movingBlockTag.Face = CoordDirection.PZ;
						break;
					case "-z":
						movingBlockTag.Face = CoordDirection.NZ;
						break;
					default:
						movingBlockTag.Face = CoordDirection.PX;
						break;
					}
					movingBlockTag.Axis = DataHandle.GetPoint3Value(array[2]);
					return movingBlockTag;
				}
			}
			return null;
		}

		public void LoadPatternPoints(ValuesDictionary valuesDictionary)
		{
			PatternPoints.Clear();
			PatternFonts.Clear();
			foreach (ValuesDictionary value in valuesDictionary.GetValue<ValuesDictionary>("PatternPoints").Values)
			{
				Pattern pattern = new Pattern();
				pattern.Point = value.GetValue<Point3>("Key");
				pattern.Position = value.GetValue<Vector3>("Position");
				pattern.Up = value.GetValue<Vector3>("Up");
				pattern.Right = value.GetValue<Vector3>("Right");
				pattern.Color = value.GetValue<Color>("Color");
				pattern.TexName = value.GetValue<string>("Texture");
				pattern.Size = value.GetValue<float>("Size");
				Stream commandFileStream = GetCommandFileStream(pattern.TexName, OpenFileMode.ReadWrite);
				if (commandFileStream != null)
				{
					pattern.Texture = Texture2D.Load(commandFileStream);
					pattern.LWratio = (float)pattern.Texture.Height / (float)pattern.Texture.Width;
					commandFileStream.Dispose();
					PatternPoints[pattern.Point] = pattern;
				}
			}
			foreach (ValuesDictionary value2 in valuesDictionary.GetValue<ValuesDictionary>("PatternFonts").Values)
			{
				PatternFont patternFont = new PatternFont();
				patternFont.Point = value2.GetValue<Point3>("Key");
				patternFont.Position = value2.GetValue<Vector3>("Position");
				patternFont.Right = value2.GetValue<Vector3>("Right");
				patternFont.Down = value2.GetValue<Vector3>("Down");
				patternFont.Color = value2.GetValue<Color>("Color");
				patternFont.Text = value2.GetValue<string>("Text");
				patternFont.Size = value2.GetValue<float>("Size");
				PatternFonts[patternFont.Point] = patternFont;
			}
		}

		public void SavePatternPoints(ValuesDictionary valuesDictionary)
		{
			int num = 0;
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("PatternPoints", valuesDictionary2);
			foreach (Pattern value in PatternPoints.Values)
			{
				if (!string.IsNullOrEmpty(value.TexName))
				{
					ValuesDictionary valuesDictionary3 = new ValuesDictionary();
					valuesDictionary3.SetValue("Key", value.Point);
					valuesDictionary3.SetValue("Position", value.Position);
					valuesDictionary3.SetValue("Up", value.Up);
					valuesDictionary3.SetValue("Right", value.Right);
					valuesDictionary3.SetValue("Color", value.Color);
					valuesDictionary3.SetValue("Texture", value.TexName);
					valuesDictionary3.SetValue("Size", value.Size);
					valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
					num++;
				}
			}
			int num2 = 0;
			ValuesDictionary valuesDictionary4 = new ValuesDictionary();
			valuesDictionary.SetValue("PatternFonts", valuesDictionary4);
			foreach (PatternFont value2 in PatternFonts.Values)
			{
				ValuesDictionary valuesDictionary5 = new ValuesDictionary();
				valuesDictionary5.SetValue("Key", value2.Point);
				valuesDictionary5.SetValue("Position", value2.Position);
				valuesDictionary5.SetValue("Right", value2.Right);
				valuesDictionary5.SetValue("Down", value2.Down);
				valuesDictionary5.SetValue("Color", value2.Color);
				valuesDictionary5.SetValue("Text", value2.Text);
				valuesDictionary5.SetValue("Size", value2.Size);
				valuesDictionary4.SetValue(num2.ToString(CultureInfo.InvariantCulture), valuesDictionary5);
				num2++;
			}
		}

		public void DrawPatternPoints(Camera camera, int drawOrder)
		{
			foreach (Point3 key in PatternPoints.Keys)
			{
				Pattern pattern = PatternPoints[key];
				Vector3 vector = pattern.Position - camera.ViewPosition;
				float num = Vector3.Dot(vector, camera.ViewDirection);
				if (!(num > 0.01f))
				{
					continue;
				}
				float num2 = vector.Length();
				if (num2 < m_subsystemSky.ViewFogRange.Y)
				{
					Vector3 vector2 = (0f - (0.01f + 0.02f * num)) / num2 * vector;
					Vector3 vector3 = pattern.LWratio * pattern.Up;
					Vector3 vector4 = pattern.Position - pattern.Size * (pattern.Right + vector3) + vector2;
					Vector3 vector5 = pattern.Position + pattern.Size * (pattern.Right - vector3) + vector2;
					Vector3 vector6 = pattern.Position + pattern.Size * (pattern.Right + vector3) + vector2;
					Vector3 vector7 = pattern.Position - pattern.Size * (pattern.Right - vector3) + vector2;
					try
					{
						m_batches[0] = m_primitivesRenderer.TexturedBatch(pattern.Texture, true, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
						m_batches[0].QueueQuad(vector4, vector5, vector6, vector7, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), pattern.Color);
						m_batches[1] = m_batches[0];
						m_batches[1].QueueQuad(vector5, vector4, vector7, vector6, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), pattern.Color);
					}
					catch
					{
					}
				}
			}
			foreach (PatternFont value in PatternFonts.Values)
			{
				try
				{
					FontBatch3D fontBatch3D = m_primitivesRenderer.FontBatch(LabelWidget.BitmapFont, 1);
					fontBatch3D.QueueText(value.Text, value.Position, value.Right * value.Size, value.Down * value.Size, value.Color);
				}
				catch
				{
				}
			}
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}

		public void SetShapeshifter(Entity entity)
		{
			ComponentBody componentBody = entity.FindComponent<ComponentBody>();
			if (componentBody == null)
			{
				return;
			}
			string text = componentBody.Entity.ValuesDictionary.DatabaseObject.Name.ToLower();
			if (text == "wolf_gray")
			{
				base.Project.RemoveEntity(entity, true);
				Entity entity2 = DatabaseManager.CreateEntity(base.Project, "Werewolf", true);
				ComponentFrame componentFrame = entity2.FindComponent<ComponentFrame>();
				ComponentSpawn componentSpawn = entity2.FindComponent<ComponentSpawn>();
				if (componentFrame != null && componentSpawn != null)
				{
					componentFrame.Position = componentBody.Position;
					componentFrame.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, new Random().Float(0f, (float)Math.PI * 2f));
					componentSpawn.SpawnDuration = 0f;
					base.Project.AddEntity(entity2);
					ComponentShapeshifter componentShapeshifter = entity2.FindComponent<ComponentShapeshifter>();
					if (componentShapeshifter != null)
					{
						componentShapeshifter.IsEnabled = false;
					}
				}
			}
			else if (text == "Werewolf")
			{
				ComponentShapeshifter componentShapeshifter2 = componentBody.Entity.FindComponent<ComponentShapeshifter>();
				if (componentShapeshifter2 != null)
				{
					componentShapeshifter2.IsEnabled = false;
				}
			}
		}

		public bool IsContainDialog(string key)
		{
			foreach (Dialog dialog in DialogsManager.Dialogs)
			{
				if (key == "sign" && dialog is EditSignDialog)
				{
					return true;
				}
				if ((key == "memorybank" && dialog is EditMemoryBankDialog) || dialog is EditMemoryBankDialogAPI)
				{
					return true;
				}
				if (key == "truthcircuit" && dialog is EditTruthTableDialog)
				{
					return true;
				}
				if (key == "delaygate" && dialog is EditAdjustableDelayGateDialog)
				{
					return true;
				}
				if (key == "battery" && dialog is EditBatteryDialog)
				{
					return true;
				}
				if (key == "piston" && dialog is EditPistonDialog)
				{
					return true;
				}
			}
			return false;
		}

		public bool NoContainDialog()
		{
			return DialogsManager.Dialogs.Count == 0;
		}

		public void SetDeathScreen()
		{
			ComponentHumanModel componentHumanModel = m_componentPlayer.Entity.FindComponent<ComponentHumanModel>();
			if (componentHumanModel != null)
			{
				m_componentPlayer.ComponentScreenOverlays.RedoutFactor = 0.5f;
				componentHumanModel.m_lieDownFactorEye = 1f;
				componentHumanModel.m_lieDownFactorModel = 1f;
			}
		}

		public void SetFirmBlocks(int id, bool open, float[] value)
		{
			Block block = BlocksManager.Blocks[id];
			if (open)
			{
				if (!OriginFirmBlockList.ContainsKey(id))
				{
					float[] value2 = new float[6] { block.DigResilience, block.ExplosionResilience, block.ProjectileResilience, block.FireDuration, block.DefaultDropCount, block.DefaultExperienceCount };
					OriginFirmBlockList[id] = value2;
				}
				block.DigResilience = float.PositiveInfinity;
				block.ExplosionResilience = float.PositiveInfinity;
				block.ProjectileResilience = float.PositiveInfinity;
				block.FireDuration = 0f;
				block.DefaultDropCount = 0f;
				block.DefaultExperienceCount = 0f;
			}
			else
			{
				block.DigResilience = value[0];
				block.ExplosionResilience = value[1];
				block.ProjectileResilience = value[2];
				block.FireDuration = value[3];
				block.DefaultDropCount = value[4];
				block.DefaultExperienceCount = value[5];
			}
		}

		public bool SetPlayerBoxStage(string stage)
		{
			switch (stage)
			{
			case "default":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(0.5f, 1.77f, 0.5f);
				break;
			case "short":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(0.5f, 0.7f, 0.5f);
				break;
			case "wide":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(1.5f, 1.77f, 1.5f);
				break;
			case "flat":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(0.1f, 1.77f, 0.1f);
				break;
			case "high":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(0.5f, 2.8f, 0.5f);
				break;
			case "huge":
				m_componentPlayer.ComponentBody.BoxSize = new Vector3(1.5f, 2.8f, 1.5f);
				break;
			default:
				return false;
			}
			return true;
		}

		public void SetCreatureTextureOrModels(Entity entity)
		{
			try
			{
				ComponentBody componentBody = entity.FindComponent<ComponentBody>();
				if (componentBody == null)
				{
					return;
				}
				string key = componentBody.Entity.ValuesDictionary.DatabaseObject.Name.ToLower();
				string value;
				if (CreatureTextures.TryGetValue(key, out value))
				{
					if (value.StartsWith("$"))
					{
						Stream commandFileStream = GetCommandFileStream(value.Substring(1), OpenFileMode.ReadWrite);
						if (commandFileStream == null)
						{
							return;
						}
						entity.FindComponent<ComponentModel>().TextureOverride = Texture2D.Load(commandFileStream);
						commandFileStream.Dispose();
					}
					else
					{
						entity.FindComponent<ComponentModel>().TextureOverride = ContentManager.Get<Texture2D>(value);
					}
				}
				string value2;
				if (!CreatureModels.TryGetValue(key, out value2))
				{
					return;
				}
				if (value2.StartsWith("$"))
				{
					Stream commandFileStream2 = GetCommandFileStream(value2.Substring(1), OpenFileMode.ReadWrite);
					if (commandFileStream2 != null)
					{
						Model model = Model.Load(commandFileStream2, true);
						commandFileStream2.Dispose();
						entity.FindComponent<ComponentModel>().Model = model;
					}
				}
				else
				{
					entity.FindComponent<ComponentModel>().Model = ContentManager.Get<Model>(value2);
				}
			}
			catch (Exception)
			{
			}
		}

		public void LoadCreatureTextureOrModels(ValuesDictionary valuesDictionary)
		{
			CreatureTextures.Clear();
			string value = valuesDictionary.GetValue<string>("CreatureTextures");
			if (!string.IsNullOrEmpty(value))
			{
				string[] array = value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					string[] array2 = text.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length >= 2)
					{
						CreatureTextures[array2[0]] = array2[1];
					}
				}
			}
			CreatureModels.Clear();
			string value2 = valuesDictionary.GetValue<string>("CreatureModels");
			if (string.IsNullOrEmpty(value2))
			{
				return;
			}
			string[] array3 = value2.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text2 in array3)
			{
				string[] array4 = text2.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (array4.Length >= 2)
				{
					CreatureModels[array4[0]] = array4[1];
				}
			}
		}

		public void SaveCreatureTextureOrModels(ValuesDictionary valuesDictionary)
		{
			string text = string.Empty;
			foreach (KeyValuePair<string, string> creatureTexture in CreatureTextures)
			{
				text = text + creatureTexture.Key + ":" + creatureTexture.Value + ",";
			}
			valuesDictionary.SetValue("CreatureTextures", text);
			string text2 = string.Empty;
			foreach (KeyValuePair<string, string> creatureModel in CreatureModels)
			{
				text2 = text2 + creatureModel.Key + ":" + creatureModel.Value + ",";
			}
			valuesDictionary.SetValue("CreatureModels", text2);
		}

		public void LoadNotes()
		{
			Notes.Clear();
			string path = Storage.CombinePaths(GameManager.WorldInfo.DirectoryName, "Notes.xml");
			if (!Storage.FileExists(path))
			{
				return;
			}
			try
			{
				Stream stream = Storage.OpenFile(path, OpenFileMode.Read);
				XElement xElement = XmlUtils.LoadXmlFromStream(stream, Encoding.UTF8, true);
				stream.Dispose();
				foreach (XElement item in xElement.Elements("Topic").ToList())
				{
					Notes[item.Attribute("Title").Value] = item.Value.Trim('\n').Replace("\t", "");
				}
			}
			catch
			{
				Log.Warning("Load Notes Fail");
			}
		}

		public void SaveNotes()
		{
			if (GameManager.WorldInfo == null)
			{
				return;
			}
			string path = Storage.CombinePaths(GameManager.WorldInfo.DirectoryName, "Notes.xml");
			Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
			XElement xElement = new XElement("Notes");
			foreach (KeyValuePair<string, string> note in Notes)
			{
				XElement xElement2 = new XElement("Topic");
				XmlUtils.SetAttributeValue(xElement2, "Title", note.Key);
				xElement2.Value = note.Value;
				xElement.Add(xElement2);
			}
			XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, true);
			stream.Close();
		}

		public bool AddChunks007(int x, int y)
		{
			Point2 item = new Point2(x, y);
			if (!m_terrainChunks007.Contains(item))
			{
				m_terrainChunks007.Add(item);
				return true;
			}
			return false;
		}

		public bool RemoveChunks007(int x, int y)
		{
			Point2 item = new Point2(x, y);
			if (m_terrainChunks007.Contains(item))
			{
				m_terrainChunks007.Remove(item);
				return true;
			}
			return false;
		}

		public void LoadChunks007(ValuesDictionary valuesDictionary)
		{
			string value = valuesDictionary.GetValue<string>("WorkChunksLine");
			if (!string.IsNullOrEmpty(value))
			{
				string[] array = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string str in array)
				{
					Point2 point2Value = DataHandle.GetPoint2Value(str);
					AddChunks007(point2Value.X, point2Value.Y);
				}
			}
		}

		public void SaveChunks007(ValuesDictionary valuesDictionary)
		{
			string text = string.Empty;
			foreach (Point2 item in m_terrainChunks007)
			{
				text = text + item.ToString() + ";";
			}
			valuesDictionary.SetValue("WorkChunksLine", text);
		}

		public static void ConvertWorld(string path, bool removeAll)
		{
			try
			{
				bool flag = false;
				XElement xElement = null;
				using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
				{
					xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
				}
				if (xElement == null)
				{
					return;
				}
				foreach (XElement item in xElement.Element("Subsystems").Elements())
				{
					if (!(XmlUtils.GetAttributeValue<string>(item, "Name") == "Spawn"))
					{
						continue;
					}
					if (item.Element("Values") == null)
					{
						break;
					}
					foreach (XElement item2 in item.Element("Values").Elements("Values"))
					{
						if (removeAll)
						{
							foreach (XElement item3 in item2.Elements())
							{
								if (XmlUtils.GetAttributeValue<string>(item3, "Name") == "SpawnsData")
								{
									flag = true;
									item3.Remove();
									break;
								}
							}
							continue;
						}
						foreach (XElement item4 in item2.Elements("Value"))
						{
							if (XmlUtils.GetAttributeValue<string>(item4, "Name") == "SpawnsData")
							{
								flag = true;
								item4.Remove();
								break;
							}
						}
					}
					break;
				}
				if (!flag)
				{
					return;
				}
				using (Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create))
				{
					XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("CommandConvertWorld:" + ex.Message);
			}
		}

		public static string GetPictureURL(string json)
		{
			try
			{
				int num = json.IndexOf(".jpg\"");
				for (int num2 = num; num2 > 0; num2--)
				{
					if (json[num2] == '"' && json.Substring(num2, 6).Contains("http"))
					{
						return json.Substring(num2 + 1, num - num2 + 3).Replace("\\", "");
					}
				}
			}
			catch
			{
			}
			return string.Empty;
		}
	}
}
