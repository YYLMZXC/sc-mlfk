using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Engine;
using Engine.Audio;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;
using XmlUtilities;
using Game;
namespace Mlfk
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

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => new int[1] { 1000 };

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
            m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
            m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
            m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
            m_subsystemBlockEntities = base.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
            m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
            m_subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
            m_subsystemFurnitureBlockBehavior = base.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
            m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
            m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
            m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
            m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
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
                        SetFirmBlocks(firmBlock, open: true, null);
                    }
                });
            }

            m_interactTest = false;
            SubsystemCommandBlockBehavior subsystemCommandBlockBehavior = base.Project.FindSubsystem<SubsystemCommandBlockBehavior>();
            subsystemCommandBlockBehavior.OnCommandBlockGenerated = (Action<CommandData>)Delegate.Combine(subsystemCommandBlockBehavior.OnCommandBlockGenerated, (Action<CommandData>)delegate (CommandData commandData)
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
            AddFunction("book", delegate (CommandData commandData)
            {
                if (commandData.DIYPara.TryGetValue("BufferTime", out var value39) && m_subsystemTime.GameTime - (double)value39 < 3.0)
                {
                    ShowSubmitTips("你已经看过了，歇2秒再看吧");
                    return SubmitResult.Fail;
                }

                commandData.DIYPara["BufferTime"] = m_subsystemTime.GameTime;
                m_componentPlayer.ComponentGui.ModalPanelWidget = new ManualTopicWidget(m_componentPlayer, 0f);
                CommandEditWidget.GuiWidgetControl(m_componentPlayer, button: true);
                return SubmitResult.Success;
            });
            AddFunction("message", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text56 = (string)commandData.GetValue("text");
                    Color color6 = (Color)commandData.GetValue("color");
                    bool playNotificationSound = (bool)commandData.GetValue("con");
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(text56, color6, blinking: true, playNotificationSound);
                }
                else if (commandData.Type == "float")
                {
                    string largeText = (string)commandData.GetValue("text1");
                    string smallText = (string)commandData.GetValue("text2");
                    m_componentPlayer.ComponentGui.DisplayLargeMessage(largeText, smallText, 3f, 0f);
                }

                return SubmitResult.Success;
            });
            AddFunction("place", delegate (CommandData commandData)
            {
                WithdrawBlockManager wbManager5 = null;
                if (WithdrawBlockManager.WithdrawMode)
                {
                    wbManager5 = new WithdrawBlockManager();
                }

                if (commandData.Type == "default")
                {
                    Point3 onePoint26 = GetOnePoint("pos", commandData);
                    int value35 = (int)commandData.GetValue("id");
                    ChangeBlockValue(wbManager5, onePoint26.X, onePoint26.Y, onePoint26.Z, value35, fast: false);
                    PlaceReprocess(wbManager5, commandData, updateChunk: false, onePoint26, onePoint26);
                }
                else if (commandData.Type == "line")
                {
                    Point3[] twoPoint23 = GetTwoPoint("pos1", "pos2", commandData);
                    int value36 = (int)commandData.GetValue("id");
                    CubeArea cubeArea20 = new CubeArea(twoPoint23[0], twoPoint23[1]);
                    int num169 = MathUtils.Max(MathUtils.Max(cubeArea20.LengthX, cubeArea20.LengthY), cubeArea20.LengthZ);
                    for (int num170 = 0; num170 <= num169; num170++)
                    {
                        int x8 = twoPoint23[0].X + (int)MathUtils.Round((float)num170 / (float)num169 * (float)(twoPoint23[1].X - twoPoint23[0].X));
                        int y2 = twoPoint23[0].Y + (int)MathUtils.Round((float)num170 / (float)num169 * (float)(twoPoint23[1].Y - twoPoint23[0].Y));
                        int z2 = twoPoint23[0].Z + (int)MathUtils.Round((float)num170 / (float)num169 * (float)(twoPoint23[1].Z - twoPoint23[0].Z));
                        ChangeBlockValue(wbManager5, x8, y2, z2, value36, fast: false);
                    }

                    PlaceReprocess(wbManager5, commandData, updateChunk: false, cubeArea20.MinPoint, cubeArea20.MaxPoint);
                }
                else if (commandData.Type == "frame")
                {
                    Point3[] twoPoint24 = GetTwoPoint("pos1", "pos2", commandData);
                    int value37 = (int)commandData.GetValue("id");
                    CubeArea cubeArea21 = new CubeArea(twoPoint24[0], twoPoint24[1]);
                    for (int num171 = 0; num171 < cubeArea21.LengthX; num171++)
                    {
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X + num171, cubeArea21.MinPoint.Y, cubeArea21.MinPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X + num171, cubeArea21.MaxPoint.Y, cubeArea21.MinPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X + num171, cubeArea21.MinPoint.Y, cubeArea21.MaxPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X + num171, cubeArea21.MaxPoint.Y, cubeArea21.MaxPoint.Z, value37, fast: false);
                    }

                    for (int num172 = 0; num172 < cubeArea21.LengthY; num172++)
                    {
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X, cubeArea21.MinPoint.Y + num172, cubeArea21.MinPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MaxPoint.X, cubeArea21.MinPoint.Y + num172, cubeArea21.MinPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X, cubeArea21.MinPoint.Y + num172, cubeArea21.MaxPoint.Z, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MaxPoint.X, cubeArea21.MinPoint.Y + num172, cubeArea21.MaxPoint.Z, value37, fast: false);
                    }

                    for (int num173 = 0; num173 < cubeArea21.LengthZ; num173++)
                    {
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X, cubeArea21.MinPoint.Y, cubeArea21.MinPoint.Z + num173, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MaxPoint.X, cubeArea21.MinPoint.Y, cubeArea21.MinPoint.Z + num173, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MinPoint.X, cubeArea21.MaxPoint.Y, cubeArea21.MinPoint.Z + num173, value37, fast: false);
                        ChangeBlockValue(wbManager5, cubeArea21.MaxPoint.X, cubeArea21.MaxPoint.Y, cubeArea21.MinPoint.Z + num173, value37, fast: false);
                    }

                    PlaceReprocess(wbManager5, commandData, updateChunk: false, cubeArea21.MinPoint, cubeArea21.MaxPoint);
                }
                else if (commandData.Type == "triangle")
                {
                    Point3 onePoint27 = GetOnePoint("pos1", commandData);
                    Point3 onePoint28 = GetOnePoint("pos2", commandData);
                    Point3 onePoint29 = GetOnePoint("pos3", commandData);
                    int value38 = (int)commandData.GetValue("id");
                    Point3[] array12 = new Point3[3];
                    for (int num174 = 0; num174 < 3; num174++)
                    {
                        switch (num174)
                        {
                            case 0:
                                array12 = new Point3[3] { onePoint27, onePoint28, onePoint29 };
                                break;
                            case 1:
                                array12 = new Point3[3] { onePoint27, onePoint29, onePoint28 };
                                break;
                            case 2:
                                array12 = new Point3[3] { onePoint28, onePoint29, onePoint27 };
                                break;
                        }

                        List<Point3> list20 = new List<Point3>();
                        CubeArea cubeArea22 = new CubeArea(array12[0], array12[1]);
                        int num175 = MathUtils.Max(MathUtils.Max(cubeArea22.LengthX, cubeArea22.LengthY), cubeArea22.LengthZ);
                        for (int num176 = 0; num176 <= num175; num176++)
                        {
                            int x9 = array12[0].X + (int)MathUtils.Round((float)num176 / (float)num175 * (float)(array12[1].X - array12[0].X));
                            int y3 = array12[0].Y + (int)MathUtils.Round((float)num176 / (float)num175 * (float)(array12[1].Y - array12[0].Y));
                            int z3 = array12[0].Z + (int)MathUtils.Round((float)num176 / (float)num175 * (float)(array12[1].Z - array12[0].Z));
                            list20.Add(new Point3(x9, y3, z3));
                            ChangeBlockValue(wbManager5, x9, y3, z3, value38);
                        }

                        foreach (Point3 item2 in list20)
                        {
                            CubeArea cubeArea23 = new CubeArea(array12[2], item2);
                            int num177 = MathUtils.Max(MathUtils.Max(cubeArea23.LengthX, cubeArea23.LengthY), cubeArea23.LengthZ);
                            for (int num178 = 0; num178 <= num177; num178++)
                            {
                                int x10 = array12[2].X + (int)MathUtils.Round((float)num178 / (float)num177 * (float)(item2.X - array12[2].X));
                                int y4 = array12[2].Y + (int)MathUtils.Round((float)num178 / (float)num177 * (float)(item2.Y - array12[2].Y));
                                int z4 = array12[2].Z + (int)MathUtils.Round((float)num178 / (float)num177 * (float)(item2.Z - array12[2].Z));
                                ChangeBlockValue(wbManager5, x10, y4, z4, value38);
                            }
                        }
                    }

                    CubeArea cubeArea24 = new CubeArea(onePoint27, onePoint28);
                    int x11 = MathUtils.Max(onePoint29.X, cubeArea24.MaxPoint.X);
                    int y5 = MathUtils.Max(onePoint29.Y, cubeArea24.MaxPoint.Y);
                    int z5 = MathUtils.Max(onePoint29.Z, cubeArea24.MaxPoint.Z);
                    int x12 = MathUtils.Min(onePoint29.X, cubeArea24.MinPoint.X);
                    int y6 = MathUtils.Min(onePoint29.Y, cubeArea24.MinPoint.Y);
                    int z6 = MathUtils.Min(onePoint29.Z, cubeArea24.MinPoint.Z);
                    PlaceReprocess(wbManager5, commandData, updateChunk: true, new Point3(x12, y6, z6), new Point3(x11, y5, z5));
                }
                else if (commandData.Type == "cube")
                {
                    Point3[] twoPoint25 = GetTwoPoint("pos1", "pos2", commandData);
                    int id13 = (int)commandData.GetValue("id");
                    bool con6 = (bool)commandData.GetValue("con");
                    CubeArea cube8 = new CubeArea(twoPoint25[0], twoPoint25[1]);
                    CubeArea cube9 = new CubeArea(cube8.MinPoint + Point3.One, cube8.MaxPoint - Point3.One);
                    cube8.Ergodic(delegate
                    {
                        if (con6)
                        {
                            if (!cube9.Exist(cube8.Current))
                            {
                                ChangeBlockValue(wbManager5, cube8.Current.X, cube8.Current.Y, cube8.Current.Z, id13);
                            }
                        }
                        else
                        {
                            ChangeBlockValue(wbManager5, cube8.Current.X, cube8.Current.Y, cube8.Current.Z, id13);
                        }

                        return false;
                    });
                    PlaceReprocess(wbManager5, commandData, updateChunk: true, cube8.MinPoint, cube8.MaxPoint);
                }
                else if (commandData.Type == "sphere")
                {
                    Point3 onePoint30 = GetOnePoint("pos", commandData);
                    int id12 = (int)commandData.GetValue("id");
                    int num179 = (int)commandData.GetValue("r");
                    bool con5 = (bool)commandData.GetValue("con");
                    SphereArea sphere = new SphereArea(num179, onePoint30);
                    SphereArea sphere2 = new SphereArea(num179 - 1, onePoint30);
                    sphere.Ergodic(delegate
                    {
                        if (con5)
                        {
                            if (!sphere2.Exist(sphere.Current))
                            {
                                ChangeBlockValue(wbManager5, sphere.Current.X, sphere.Current.Y, sphere.Current.Z, id12);
                            }
                        }
                        else
                        {
                            ChangeBlockValue(wbManager5, sphere.Current.X, sphere.Current.Y, sphere.Current.Z, id12);
                        }
                    });
                    PlaceReprocess(wbManager5, commandData, updateChunk: true, sphere.MinPoint, sphere.MaxPoint);
                }
                else if (commandData.Type == "column")
                {
                    Point3 onePoint31 = GetOnePoint("pos", commandData);
                    int id11 = (int)commandData.GetValue("id");
                    int num180 = (int)commandData.GetValue("r");
                    int num181 = (int)commandData.GetValue("h");
                    string text54 = (string)commandData.GetValue("opt");
                    bool con4 = (bool)commandData.GetValue("con");
                    CoordDirection coord2 = CoordDirection.PY;
                    Point3 point31 = Point3.Zero;
                    switch (text54)
                    {
                        case "+x":
                            coord2 = CoordDirection.PX;
                            point31 = new Point3(1, 0, 0);
                            break;
                        case "+y":
                            coord2 = CoordDirection.PY;
                            point31 = new Point3(0, 1, 0);
                            break;
                        case "+z":
                            coord2 = CoordDirection.PZ;
                            point31 = new Point3(0, 0, 1);
                            break;
                        case "-x":
                            coord2 = CoordDirection.NX;
                            point31 = new Point3(-1, 0, 0);
                            break;
                        case "-y":
                            coord2 = CoordDirection.NY;
                            point31 = new Point3(0, -1, 0);
                            break;
                        case "-z":
                            coord2 = CoordDirection.NZ;
                            point31 = new Point3(0, 0, -1);
                            break;
                    }

                    ColumnArea column = new ColumnArea(num180, num181, onePoint31, coord2);
                    ColumnArea column2 = new ColumnArea(num180 - 1, num181 - 2, onePoint31 + point31, coord2);
                    column.Ergodic(delegate
                    {
                        if (con4)
                        {
                            if (!column2.Exist(column.Current))
                            {
                                ChangeBlockValue(wbManager5, column.Current.X, column.Current.Y, column.Current.Z, id11);
                            }
                        }
                        else
                        {
                            ChangeBlockValue(wbManager5, column.Current.X, column.Current.Y, column.Current.Z, id11);
                        }
                    });
                    PlaceReprocess(wbManager5, commandData, updateChunk: true, column.MinPoint, column.MaxPoint);
                }
                else if (commandData.Type == "cone")
                {
                    Point3 onePoint32 = GetOnePoint("pos", commandData);
                    int id10 = (int)commandData.GetValue("id");
                    int num182 = (int)commandData.GetValue("r");
                    int num183 = (int)commandData.GetValue("h");
                    string text55 = (string)commandData.GetValue("opt");
                    bool con3 = (bool)commandData.GetValue("con");
                    CoordDirection coord3 = CoordDirection.PY;
                    Point3 point32 = Point3.Zero;
                    switch (text55)
                    {
                        case "+x":
                            coord3 = CoordDirection.PX;
                            point32 = new Point3(1, 0, 0);
                            break;
                        case "+y":
                            coord3 = CoordDirection.PY;
                            point32 = new Point3(0, 1, 0);
                            break;
                        case "+z":
                            coord3 = CoordDirection.PZ;
                            point32 = new Point3(0, 0, 1);
                            break;
                        case "-x":
                            coord3 = CoordDirection.NX;
                            point32 = new Point3(-1, 0, 0);
                            break;
                        case "-y":
                            coord3 = CoordDirection.NY;
                            point32 = new Point3(0, -1, 0);
                            break;
                        case "-z":
                            coord3 = CoordDirection.NZ;
                            point32 = new Point3(0, 0, -1);
                            break;
                    }

                    ConeArea cone = new ConeArea(num182, num183, onePoint32, coord3);
                    ConeArea cone2 = new ConeArea((int)(0.8f * (float)num182), num183 - 2, onePoint32 + point32, coord3);
                    cone.Ergodic(delegate
                    {
                        if (con3)
                        {
                            if (!cone2.Exist(cone.Current))
                            {
                                ChangeBlockValue(wbManager5, cone.Current.X, cone.Current.Y, cone.Current.Z, id10);
                            }
                        }
                        else
                        {
                            ChangeBlockValue(wbManager5, cone.Current.X, cone.Current.Y, cone.Current.Z, id10);
                        }
                    });
                    PlaceReprocess(wbManager5, commandData, updateChunk: true, cone.MinPoint, cone.MaxPoint);
                }
                else if (commandData.Type == "function")
                {
                    Point3 pos7 = GetOnePoint("pos", commandData);
                    int id9 = (int)commandData.GetValue("id");
                    int v4 = (int)commandData.GetValue("v");
                    string func1 = (string)commandData.GetValue("func1");
                    string func2 = (string)commandData.GetValue("func2");
                    string str3 = (string)commandData.GetValue("limx");
                    string str4 = (string)commandData.GetValue("limy");
                    string str5 = (string)commandData.GetValue("limz");
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

                    Point2 point2Value = DataHandle.GetPoint2Value(str3);
                    Point2 point2Value2 = DataHandle.GetPoint2Value(str4);
                    Point2 point2Value3 = DataHandle.GetPoint2Value(str5);
                    CubeArea cube7 = new CubeArea(new Point3(point2Value.X, point2Value2.X, point2Value3.X), new Point3(point2Value.Y, point2Value2.Y, point2Value3.Y));
                    Task.Run(delegate
                    {
                        try
                        {
                            int count = 0;
                            int eh = (int)((float)(cube7.LengthX * cube7.LengthY * cube7.LengthZ) / 10f) + 1;
                            ShowSubmitTips($"{func1}&{func2}\n正在计算生成中,请等待片刻！");
                            cube7.Ergodic(delegate
                            {
                                count++;
                                if (count % eh == 0)
                                {
                                    ShowSubmitTips($"{func1}&{func2}\n方块生成进度:{(int)((float)(count / eh) * 10f)}%");
                                }

                                bool flag51 = funcPass1;
                                bool flag52 = funcPass2;
                                if (!funcPass1)
                                {
                                    ExpressionCalculator expressionCalculator = new ExpressionCalculator(funcArray1[0]);
                                    ExpressionCalculator expressionCalculator2 = new ExpressionCalculator(funcArray1[1]);
                                    int num184 = expressionCalculator.Calculate(cube7.Current.X, cube7.Current.Y, cube7.Current.Z);
                                    int num185 = expressionCalculator2.Calculate(cube7.Current.X, cube7.Current.Y, cube7.Current.Z);
                                    switch (funcArray1[2])
                                    {
                                        case "<":
                                            flag51 = num184 < num185;
                                            break;
                                        case ">":
                                            flag51 = num184 > num185;
                                            break;
                                        case "=":
                                            flag51 = MathUtils.Abs(num184 - num185) <= v4;
                                            break;
                                    }

                                    if (num184 == int.MinValue || num185 == int.MinValue)
                                    {
                                        flag51 = false;
                                    }
                                }

                                if (!funcPass2)
                                {
                                    ExpressionCalculator expressionCalculator3 = new ExpressionCalculator(funcArray2[0]);
                                    ExpressionCalculator expressionCalculator4 = new ExpressionCalculator(funcArray2[1]);
                                    int num186 = expressionCalculator3.Calculate(cube7.Current.X, cube7.Current.Y, cube7.Current.Z);
                                    int num187 = expressionCalculator4.Calculate(cube7.Current.X, cube7.Current.Y, cube7.Current.Z);
                                    switch (funcArray2[2])
                                    {
                                        case "<":
                                            flag52 = num186 < num187;
                                            break;
                                        case ">":
                                            flag52 = num186 > num187;
                                            break;
                                        case "=":
                                            flag52 = MathUtils.Abs(num186 - num187) <= v4;
                                            break;
                                    }

                                    if (num186 == int.MinValue || num187 == int.MinValue)
                                    {
                                        flag52 = false;
                                    }
                                }

                                if (flag51 && flag52)
                                {
                                    Point3 point33 = pos7 + cube7.Current - cube7.MinPoint;
                                    ChangeBlockValue(wbManager5, point33.X, point33.Y, point33.Z, id9);
                                }

                                return false;
                            });
                            PlaceReprocess(wbManager5, commandData, updateChunk: true, pos7, pos7 + cube7.MaxPoint - cube7.MinPoint);
                            ShowSubmitTips($"{func1}&{func2}\n生成方块完成！");
                        }
                        catch (Exception ex2)
                        {
                            Log.Warning($"{func1}&{func2}:{ex2.Message}");
                            ShowSubmitTips($"{func1}&{func2}\n生成方块失败！请检查表达式以及定义域");
                        }
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("dig", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint25 = GetOnePoint("pos", commandData);
                    m_subsystemTerrain.DestroyCell(1, onePoint25.X, onePoint25.Y, onePoint25.Z, 0, noDrop: false, noParticleSystem: false);
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint21 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cube6 = new CubeArea(twoPoint21[0], twoPoint21[1]);
                    cube6.Ergodic(delegate
                    {
                        m_subsystemTerrain.DestroyCell(1, cube6.Current.X, cube6.Current.Y, cube6.Current.Z, 0, noDrop: false, noParticleSystem: false);
                        return false;
                    });
                }
                else if (commandData.Type == "limit")
                {
                    Point3[] twoPoint22 = GetTwoPoint("pos1", "pos2", commandData);
                    int id8 = (int)commandData.GetValue("id");
                    CubeArea cube5 = new CubeArea(twoPoint22[0], twoPoint22[1]);
                    cube5.Ergodic(delegate
                    {
                        int limitValue4 = GetLimitValue(cube5.Current.X, cube5.Current.Y, cube5.Current.Z);
                        if (limitValue4 == id8)
                        {
                            m_subsystemTerrain.DestroyCell(1, cube5.Current.X, cube5.Current.Y, cube5.Current.Z, 0, noDrop: false, noParticleSystem: false);
                        }

                        return false;
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("replace", delegate (CommandData commandData)
            {
                WithdrawBlockManager wbManager4 = null;
                if (WithdrawBlockManager.WithdrawMode)
                {
                    wbManager4 = new WithdrawBlockManager();
                }

                if (commandData.Type == "default")
                {
                    Point3[] twoPoint17 = GetTwoPoint("pos1", "pos2", commandData);
                    int id6 = (int)commandData.GetValue("id1");
                    int id7 = (int)commandData.GetValue("id2");
                    CubeArea cube4 = new CubeArea(twoPoint17[0], twoPoint17[1]);
                    cube4.Ergodic(delegate
                    {
                        int limitValue3 = GetLimitValue(cube4.Current.X, cube4.Current.Y, cube4.Current.Z);
                        if (limitValue3 == id6)
                        {
                            ChangeBlockValue(wbManager4, cube4.Current.X, cube4.Current.Y, cube4.Current.Z, id7);
                        }

                        return false;
                    });
                    PlaceReprocess(wbManager4, commandData, updateChunk: true, cube4.MinPoint, cube4.MaxPoint);
                }
                else if (commandData.Type == "fuzzycolor")
                {
                    Point3[] twoPoint18 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cube3 = new CubeArea(twoPoint18[0], twoPoint18[1]);
                    cube3.Ergodic(delegate
                    {
                        int limitValue2 = GetLimitValue(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
                        if (Terrain.ExtractContents(limitValue2) == 72)
                        {
                            Color commandColor = Mlfk.ClayBlock.GetCommandColor(Terrain.ExtractData(limitValue2));
                            if (!Mlfk.ClayBlock.IsDefaultColor(commandColor))
                            {
                                int value34 = DataHandle.GetColorIndex(commandColor) * 32768 + 16456;
                                ChangeBlockValue(wbManager4, cube3.Current.X, cube3.Current.Y, cube3.Current.Z, value34);
                            }
                        }

                        return false;
                    });
                    PlaceReprocess(wbManager4, commandData, updateChunk: true, cube3.MinPoint, cube3.MaxPoint);
                }
                else if (commandData.Type == "padding")
                {
                    Point3[] twoPoint19 = GetTwoPoint("pos1", "pos2", commandData);
                    int value32 = (int)commandData.GetValue("id");
                    string text52 = (string)commandData.GetValue("opt");
                    CubeArea cubeArea18 = new CubeArea(twoPoint19[0], twoPoint19[1]);
                    if (text52 == "+x" || text52 == "-x")
                    {
                        for (int num145 = 0; num145 < cubeArea18.LengthY; num145++)
                        {
                            for (int num146 = 0; num146 < cubeArea18.LengthZ; num146++)
                            {
                                if (text52 == "+x")
                                {
                                    for (int num147 = 0; num147 < cubeArea18.LengthX; num147++)
                                    {
                                        Point3 point19 = new Point3(num147 + cubeArea18.MinPoint.X, num145 + cubeArea18.MinPoint.Y, num146 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point19.X, point19.Y, point19.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point19.X, point19.Y, point19.Z, value32);
                                    }
                                }
                                else
                                {
                                    for (int num148 = cubeArea18.LengthX - 1; num148 > 0; num148--)
                                    {
                                        Point3 point20 = new Point3(num148 + cubeArea18.MinPoint.X, num145 + cubeArea18.MinPoint.Y, num146 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point20.X, point20.Y, point20.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point20.X, point20.Y, point20.Z, value32);
                                    }
                                }
                            }
                        }
                    }
                    else if (text52 == "+y" || text52 == "-y")
                    {
                        for (int num149 = 0; num149 < cubeArea18.LengthX; num149++)
                        {
                            for (int num150 = 0; num150 < cubeArea18.LengthZ; num150++)
                            {
                                if (text52 == "+y")
                                {
                                    for (int num151 = 0; num151 < cubeArea18.LengthY; num151++)
                                    {
                                        Point3 point21 = new Point3(num149 + cubeArea18.MinPoint.X, num151 + cubeArea18.MinPoint.Y, num150 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point21.X, point21.Y, point21.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point21.X, point21.Y, point21.Z, value32);
                                    }
                                }
                                else
                                {
                                    for (int num152 = cubeArea18.LengthY - 1; num152 > 0; num152--)
                                    {
                                        Point3 point22 = new Point3(num149 + cubeArea18.MinPoint.X, num152 + cubeArea18.MinPoint.Y, num150 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point22.X, point22.Y, point22.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point22.X, point22.Y, point22.Z, value32);
                                    }
                                }
                            }
                        }
                    }
                    else if (text52 == "+z" || text52 == "-z")
                    {
                        for (int num153 = 0; num153 < cubeArea18.LengthX; num153++)
                        {
                            for (int num154 = 0; num154 < cubeArea18.LengthY; num154++)
                            {
                                if (text52 == "+z")
                                {
                                    for (int num155 = 0; num155 < cubeArea18.LengthZ; num155++)
                                    {
                                        Point3 point23 = new Point3(num153 + cubeArea18.MinPoint.X, num154 + cubeArea18.MinPoint.Y, num155 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point23.X, point23.Y, point23.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point23.X, point23.Y, point23.Z, value32);
                                    }
                                }
                                else
                                {
                                    for (int num156 = cubeArea18.LengthZ - 1; num156 > 0; num156--)
                                    {
                                        Point3 point24 = new Point3(num153 + cubeArea18.MinPoint.X, num154 + cubeArea18.MinPoint.Y, num156 + cubeArea18.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point24.X, point24.Y, point24.Z) != 0)
                                        {
                                            break;
                                        }

                                        ChangeBlockValue(wbManager4, point24.X, point24.Y, point24.Z, value32);
                                    }
                                }
                            }
                        }
                    }

                    PlaceReprocess(wbManager4, commandData, updateChunk: true, cubeArea18.MinPoint, cubeArea18.MaxPoint);
                }
                else if (commandData.Type == "overlay")
                {
                    Point3[] twoPoint20 = GetTwoPoint("pos1", "pos2", commandData);
                    int value33 = (int)commandData.GetValue("id");
                    string text53 = (string)commandData.GetValue("opt");
                    bool flag48 = (bool)commandData.GetValue("con1");
                    bool flag49 = (bool)commandData.GetValue("con2");
                    CubeArea cubeArea19 = new CubeArea(twoPoint20[0], twoPoint20[1]);
                    bool flag50 = true;
                    if (text53 == "+x" || text53 == "-x")
                    {
                        for (int num157 = 0; num157 < cubeArea19.LengthY; num157++)
                        {
                            for (int num158 = 0; num158 < cubeArea19.LengthZ; num158++)
                            {
                                flag50 = true;
                                if (text53 == "+x")
                                {
                                    for (int num159 = 0; num159 < cubeArea19.LengthX; num159++)
                                    {
                                        Point3 point25 = new Point3(num159 + cubeArea19.MinPoint.X, num157 + cubeArea19.MinPoint.Y, num158 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point25.X, point25.Y, point25.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, flag49 ? point25.X : (point25.X - 1), point25.Y, point25.Z, value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int num160 = cubeArea19.LengthX - 1; num160 > 0; num160--)
                                    {
                                        Point3 point26 = new Point3(num160 + cubeArea19.MinPoint.X, num157 + cubeArea19.MinPoint.Y, num158 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point26.X, point26.Y, point26.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, flag49 ? point26.X : (point26.X + 1), point26.Y, point26.Z, value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (text53 == "+y" || text53 == "-y")
                    {
                        for (int num161 = 0; num161 < cubeArea19.LengthX; num161++)
                        {
                            for (int num162 = 0; num162 < cubeArea19.LengthZ; num162++)
                            {
                                flag50 = true;
                                if (text53 == "+y")
                                {
                                    for (int num163 = 0; num163 < cubeArea19.LengthY; num163++)
                                    {
                                        Point3 point27 = new Point3(num161 + cubeArea19.MinPoint.X, num163 + cubeArea19.MinPoint.Y, num162 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point27.X, point27.Y, point27.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, point27.X, flag49 ? point27.Y : (point27.Y - 1), point27.Z, value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int num164 = cubeArea19.LengthY - 1; num164 > 0; num164--)
                                    {
                                        Point3 point28 = new Point3(num161 + cubeArea19.MinPoint.X, num164 + cubeArea19.MinPoint.Y, num162 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point28.X, point28.Y, point28.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, point28.X, flag49 ? point28.Y : (point28.Y + 1), point28.Z, value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (text53 == "+z" || text53 == "-z")
                    {
                        for (int num165 = 0; num165 < cubeArea19.LengthX; num165++)
                        {
                            for (int num166 = 0; num166 < cubeArea19.LengthY; num166++)
                            {
                                flag50 = true;
                                if (text53 == "+z")
                                {
                                    for (int num167 = 0; num167 < cubeArea19.LengthZ; num167++)
                                    {
                                        Point3 point29 = new Point3(num165 + cubeArea19.MinPoint.X, num166 + cubeArea19.MinPoint.Y, num167 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point29.X, point29.Y, point29.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, point29.X, point29.Y, flag49 ? point29.Z : (point29.Z - 1), value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int num168 = cubeArea19.LengthZ - 1; num168 > 0; num168--)
                                    {
                                        Point3 point30 = new Point3(num165 + cubeArea19.MinPoint.X, num166 + cubeArea19.MinPoint.Y, num168 + cubeArea19.MinPoint.Z);
                                        if (m_subsystemTerrain.Terrain.GetCellContents(point30.X, point30.Y, point30.Z) != 0)
                                        {
                                            if (flag50)
                                            {
                                                ChangeBlockValue(wbManager4, point30.X, point30.Y, flag49 ? point30.Z : (point30.Z + 1), value33);
                                            }

                                            if (!flag48)
                                            {
                                                break;
                                            }

                                            flag50 = false;
                                        }
                                        else
                                        {
                                            flag50 = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    PlaceReprocess(wbManager4, commandData, updateChunk: true, cubeArea19.MinPoint, cubeArea19.MaxPoint);
                }

                return SubmitResult.Success;
            });
            AddFunction("addnpc", delegate (CommandData commandData)
            {
                Point3 onePoint24 = GetOnePoint("pos", commandData);
                string obj17 = (string)commandData.GetValue("obj");
                string entityName = EntityInfoManager.GetEntityName(obj17);
                if (entityName == "MalePlayer")
                {
                    ShowSubmitTips("不能添加玩家");
                    return SubmitResult.Fail;
                }

                Entity entity = DatabaseManager.CreateEntity(base.Project, entityName, throwIfNotFound: true);
                ComponentFrame componentFrame = entity.FindComponent<ComponentFrame>();
                ComponentSpawn componentSpawn = entity.FindComponent<ComponentSpawn>();
                if (componentFrame != null && componentSpawn != null)
                {
                    componentFrame.Position = new Vector3(onePoint24) + new Vector3(0.5f, 0f, 0.5f);
                    componentFrame.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, new Game.Random().Float(0f, (float)Math.PI * 2f));
                    componentSpawn.SpawnDuration = 0f;
                    base.Project.AddEntity(entity);
                }

                return SubmitResult.Success;
            });
            AddFunction("removenpc", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text51 = (string)commandData.GetValue("obj");
                    if (text51 == "player")
                    {
                        ShowSubmitTips("不能移除玩家");
                        return SubmitResult.Fail;
                    }

                    ErgodicBody(text51, delegate (ComponentBody body)
                    {
                        base.Project.RemoveEntity(body.Entity, disposeEntity: true);
                        return false;
                    });
                }
                else if (commandData.Type == "all")
                {
                    ErgodicBody("npc", delegate (ComponentBody body)
                    {
                        base.Project.RemoveEntity(body.Entity, disposeEntity: true);
                        return false;
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("injure", delegate (CommandData commandData)
            {
                string target4 = (string)commandData.GetValue("obj");
                int num144 = (int)commandData.GetValue("v");
                float amount2 = (float)num144 / 100f;
                ErgodicBody(target4, delegate (ComponentBody body)
                {
                    ComponentCreature componentCreature2 = body.Entity.FindComponent<ComponentCreature>();
                    ComponentDamage componentDamage2 = body.Entity.FindComponent<ComponentDamage>();
                    componentCreature2?.ComponentHealth.Injure(amount2, null, ignoreInvulnerability: true, "不知道谁输的指令");
                    componentDamage2?.Damage(amount2);
                    return false;
                });
                return SubmitResult.Success;
            });
            AddFunction("kill", delegate (CommandData commandData)
            {
                string value31 = (string)commandData.GetValue("obj");
                commandData.Data.Add("v", 100);
                return Submit("injure", new CommandData(commandData.Position, commandData.Line)
                {
                    Type = "default",
                    Data =
                    {
                        ["obj"] = value31,
                        ["v"] = 100
                    }
                }, Judge: false);
            });
            AddFunction("heal", delegate (CommandData commandData)
            {
                string target3 = (string)commandData.GetValue("obj");
                int num143 = (int)commandData.GetValue("v");
                float amount = (float)num143 / 100f;
                ErgodicBody(target3, delegate (ComponentBody body)
                {
                    ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
                    ComponentDamage componentDamage = body.Entity.FindComponent<ComponentDamage>();
                    componentCreature?.ComponentHealth.Heal(amount);
                    if (componentDamage != null)
                    {
                        componentDamage.Hitpoints = MathUtils.Min(componentDamage.Hitpoints + amount, 1f);
                    }

                    return false;
                });
                return SubmitResult.Success;
            });
            AddFunction("catchfire", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string target2 = (string)commandData.GetValue("obj");
                    int v3 = (int)commandData.GetValue("v");
                    ErgodicBody(target2, delegate (ComponentBody body)
                    {
                        body.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(null, v3);
                        return false;
                    });
                }
                else if (commandData.Type == "block")
                {
                    Point3 onePoint23 = GetOnePoint("pos", commandData);
                    int num142 = (int)commandData.GetValue("v");
                    float fireExpandability = (float)num142 / 10f;
                    base.Project.FindSubsystem<SubsystemFireBlockBehavior>().SetCellOnFire(onePoint23.X, onePoint23.Y, onePoint23.Z, fireExpandability);
                }

                return SubmitResult.Success;
            });
            AddFunction("teleport", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint22 = GetOnePoint("pos", commandData);
                    m_componentPlayer.ComponentBody.Position = new Vector3(onePoint22) + new Vector3(0.5f, 0f, 0.5f);
                }

                if (commandData.Type == "spawn")
                {
                    m_componentPlayer.ComponentBody.Position = m_componentPlayer.PlayerData.SpawnPosition;
                }

                return SubmitResult.Success;
            });
            AddFunction("spawn", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint21 = GetOnePoint("pos", commandData);
                    m_componentPlayer.PlayerData.SpawnPosition = new Vector3(onePoint21) + new Vector3(0.5f, 0f, 0.5f);
                }
                else if (commandData.Type == "playerpos")
                {
                    m_componentPlayer.PlayerData.SpawnPosition = m_componentPlayer.ComponentBody.Position;
                }

                return SubmitResult.Success;
            });
            AddFunction("boxstage", delegate (CommandData commandData)
            {
                if (SetPlayerBoxStage(commandData.Type))
                {
                    m_playerBoxStage = commandData.Type;
                }

                return SubmitResult.Success;
            });
            AddFunction("level", delegate (CommandData commandData)
            {
                int num141 = (int)commandData.GetValue("v");
                m_componentPlayer.PlayerData.Level = num141;
                return SubmitResult.Success;
            });
            AddFunction("stats", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    float x3 = m_componentPlayer.ComponentHealth.Health * 100f;
                    float x4 = m_componentPlayer.ComponentVitalStats.Food * 100f;
                    float x5 = m_componentPlayer.ComponentVitalStats.Sleep * 100f;
                    float x6 = m_componentPlayer.ComponentVitalStats.Stamina * 100f;
                    float x7 = m_componentPlayer.ComponentVitalStats.Wetness * 100f;
                    float temperature2 = m_componentPlayer.ComponentVitalStats.Temperature;
                    float attackPower = m_componentPlayer.ComponentMiner.AttackPower;
                    float attackResilience = m_componentPlayer.ComponentHealth.AttackResilience;
                    float num138 = m_componentPlayer.ComponentLocomotion.WalkSpeed * 10f;
                    string text50 = $"生命值:{MathUtils.Round(x3)}%，饥饿度:{MathUtils.Round(x4)}%，疲劳度:{MathUtils.Round(x5)}%，";
                    text50 += $"\n耐力值:{MathUtils.Round(x6)}%，体湿:{MathUtils.Round(x7)}%, 体温:{temperature2}";
                    text50 += $"\n攻击力:{attackPower}，防御值:{attackResilience}，行走速度:{num138}";
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(text50, Color.White, blinking: false, playNotificationSound: false);
                    return SubmitResult.Success;
                }

                int num139 = (int)commandData.GetValue("v");
                if (commandData.Type == "health")
                {
                    m_componentPlayer.ComponentHealth.Health = (float)num139 / 100f;
                }
                else if (commandData.Type == "food")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令stats类型food在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    m_componentPlayer.ComponentVitalStats.Food = (float)num139 / 100f;
                }
                else if (commandData.Type == "sleep")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令stats类型sleep在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    m_componentPlayer.ComponentVitalStats.Sleep = (float)num139 / 100f;
                }
                else if (commandData.Type == "stamina")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令stats类型stamina在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    m_componentPlayer.ComponentVitalStats.Stamina = (float)num139 / 100f;
                }
                else if (commandData.Type == "wetness")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令stats类型wetness在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    m_componentPlayer.ComponentVitalStats.Wetness = (float)num139 / 100f;
                }
                else if (commandData.Type == "temperature")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令stats类型temperature在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    m_componentPlayer.ComponentVitalStats.Temperature = num139;
                }
                else if (commandData.Type == "attack")
                {
                    m_componentPlayer.ComponentMiner.AttackPower = num139;
                }
                else if (commandData.Type == "defense")
                {
                    m_componentPlayer.ComponentHealth.AttackResilience = num139;
                    m_componentPlayer.ComponentHealth.FallResilience = num139;
                    m_componentPlayer.ComponentHealth.FireResilience = 2 * num139;
                }
                else if (commandData.Type == "speed")
                {
                    float num140 = (float)num139 / 10f;
                    m_componentPlayer.ComponentLocomotion.WalkSpeed = 2f * num140;
                    m_componentPlayer.ComponentLocomotion.JumpSpeed = 3f * MathUtils.Sqrt(num140);
                    m_componentPlayer.ComponentLocomotion.LadderSpeed = 1.5f * num140;
                    m_componentPlayer.ComponentLocomotion.SwimSpeed = 1.5f * num140;
                }

                return SubmitResult.Success;
            });
            AddFunction("action", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    ComponentFirstPersonModel componentFirstPersonModel = m_componentPlayer.Entity.FindComponent<ComponentFirstPersonModel>(throwOnError: true);
                    componentFirstPersonModel.m_pokeAnimationTime = 5f;
                }
                else if (commandData.Type.StartsWith("move"))
                {
                    int num135 = (int)commandData.GetValue("v");
                    Vector3 playerEyesDirection = DataHandle.GetPlayerEyesDirection(m_componentPlayer);
                    Vector3 vector17 = Vector3.Zero;
                    switch (commandData.Type)
                    {
                        case "moveup":
                            vector17 = new Vector3(playerEyesDirection.X, playerEyesDirection.Y, playerEyesDirection.Z);
                            break;
                        case "movedown":
                            vector17 = new Vector3(0f - playerEyesDirection.X, 0f - playerEyesDirection.Y, 0f - playerEyesDirection.Z);
                            break;
                        case "moveleft":
                            vector17 = new Vector3(playerEyesDirection.Z, 0f, 0f - playerEyesDirection.X);
                            break;
                        case "moveright":
                            vector17 = new Vector3(0f - playerEyesDirection.Z, 0f, playerEyesDirection.X);
                            break;
                    }

                    m_componentPlayer.ComponentBody.Velocity = vector17 / vector17.Length() * ((float)num135 / 10f);
                }
                else if (commandData.Type.StartsWith("look"))
                {
                    int num136 = (int)commandData.GetValue("v");
                    float x2 = m_componentPlayer.ComponentBody.Rotation.ToYawPitchRoll().X;
                    switch (commandData.Type)
                    {
                        case "lookup":
                            m_componentPlayer.ComponentLocomotion.LookAngles += new Vector2(0f, MathUtils.DegToRad(num136));
                            break;
                        case "lookdown":
                            m_componentPlayer.ComponentLocomotion.LookAngles += new Vector2(0f, 0f - MathUtils.DegToRad(num136));
                            break;
                        case "lookleft":
                            m_componentPlayer.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x2 + MathUtils.DegToRad(num136));
                            break;
                        case "lookright":
                            m_componentPlayer.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x2 - MathUtils.DegToRad(num136));
                            break;
                    }
                }
                else if (commandData.Type == "jump")
                {
                    int num137 = (int)commandData.GetValue("v");
                    Vector3 velocity2 = m_componentPlayer.ComponentBody.Velocity;
                    m_componentPlayer.ComponentBody.Velocity = (velocity2.X, (float)num137 / 10f, velocity2.Z);
                }
                else if (commandData.Type == "rider")
                {
                    bool flag45 = (bool)commandData.GetValue("con");
                    if (flag45 && m_componentPlayer.ComponentRider.Mount == null)
                    {
                        ComponentMount componentMount = m_componentPlayer.ComponentRider.FindNearestMount();
                        if (componentMount != null)
                        {
                            m_componentPlayer.ComponentRider.StartMounting(componentMount);
                        }
                    }

                    if (!flag45 && m_componentPlayer.ComponentRider.Mount != null)
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
                        m_componentPlayer.ComponentSleep.Sleep(allowManualWakeup: false);
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
                    bool flag46 = (bool)commandData.GetValue("con");
                    m_componentPlayer.ComponentBody.AirDrag = (flag46 ? new Vector2(1000f, 1000f) : new Vector2(0.25f, 0.25f));
                }
                else if (commandData.Type == "breath")
                {
                    bool flag47 = (bool)commandData.GetValue("con");
                    m_componentPlayer.ComponentHealth.AirCapacity = (flag47 ? (-1) : 10);
                }

                return SubmitResult.Success;
            });
            AddFunction("clothes", delegate (CommandData commandData)
            {
                if (commandData.Type == "default" || commandData.Type == "removeid")
                {
                    int num133 = (int)commandData.GetValue("id");
                    Block block4 = BlocksManager.Blocks[Terrain.ExtractContents(num133)];
                    bool flag44 = commandData.Type == "removeid";
                    if (!(block4 is ClothingBlock))
                    {
                        ShowSubmitTips($"id为{num133}的物品不是衣物，请选择衣物");
                        return SubmitResult.Fail;
                    }

                    ClothingData clothingData = block4.GetClothingData(num133);
                    if (clothingData == null)
                    {
                        ShowSubmitTips($"id为{num133}的衣物数据不存在");
                        return SubmitResult.Fail;
                    }

                    List<int> list19 = new List<int>();
                    if (!flag44)
                    {
                        foreach (int clothe in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
                        {
                            list19.Add(clothe);
                        }

                        list19.Add(num133);
                    }
                    else
                    {
                        foreach (int clothe2 in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
                        {
                            if (num133 != clothe2)
                            {
                                list19.Add(clothe2);
                            }
                        }
                    }

                    m_componentPlayer.ComponentClothing.SetClothes(clothingData.Slot, list19);
                }
                else if (commandData.Type == "removeslot")
                {
                    int num134 = (int)commandData.GetValue("v");
                    ClothingSlot slot = (ClothingSlot)(num134 - 1);
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
            AddFunction("interact", delegate (CommandData commandData)
            {
                Point3 pos6 = GetOnePoint("pos", commandData);
                if (commandData.Type == "default")
                {
                    SubsystemBlockBehaviors subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>();
                    Vector3[] array10 = new Vector3[6]
                    {
                        new Vector3(1f, 0f, 0f),
                        new Vector3(0f, 1f, 0f),
                        new Vector3(0f, 0f, 1f),
                        new Vector3(-1f, 0f, 0f),
                        new Vector3(0f, -1f, 0f),
                        new Vector3(0f, 0f, -1f)
                    };
                    Vector3[] array11 = array10;
                    foreach (Vector3 direction in array11)
                    {
                        Ray3 ray = new Ray3(new Vector3(pos6) + new Vector3(0.5f), direction);
                        TerrainRaycastResult? terrainRaycastResult = m_componentPlayer.ComponentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
                        if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.CellFace.Point == pos6)
                        {
                            TerrainRaycastResult value27 = terrainRaycastResult.Value;
                            SubsystemBlockBehavior[] blockBehaviors = subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(value27.Value));
                            for (int num130 = 0; num130 < blockBehaviors.Length; num130++)
                            {
                                blockBehaviors[num130].OnInteract(value27, m_componentPlayer.ComponentMiner);
                            }

                            break;
                        }
                    }
                }
                else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
                {
                    ComponentBlockEntity blockEntity4 = m_subsystemBlockEntities.GetBlockEntity(pos6.X, pos6.Y, pos6.Z);
                    if (blockEntity4 != null && m_componentPlayer != null)
                    {
                        IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
                        switch (commandData.Type)
                        {
                            case "chest":
                                {
                                    ComponentChest componentChest = blockEntity4.Entity.FindComponent<ComponentChest>();
                                    if (componentChest != null)
                                    {
                                        m_componentPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(inventory, componentChest);
                                    }

                                    break;
                                }
                            case "table":
                                {
                                    ComponentCraftingTable componentCraftingTable5 = blockEntity4.Entity.FindComponent<ComponentCraftingTable>();
                                    if (componentCraftingTable5 != null)
                                    {
                                        m_componentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(inventory, componentCraftingTable5);
                                    }

                                    break;
                                }
                            case "dispenser":
                                {
                                    ComponentDispenser componentDispenser = blockEntity4.Entity.FindComponent<ComponentDispenser>();
                                    if (componentDispenser != null)
                                    {
                                        m_componentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(inventory, componentDispenser);
                                    }

                                    break;
                                }
                            case "furnace":
                                {
                                    ComponentFurnace componentFurnace = blockEntity4.Entity.FindComponent<ComponentFurnace>();
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
                    int value26 = m_subsystemTerrain.Terrain.GetCellValue(pos6.X, pos6.Y, pos6.Z);
                    switch (commandData.Type)
                    {
                        case "memorybank":
                            if (Terrain.ExtractContents(value26) == 186)
                            {
                                SubsystemMemoryBankBlockBehavior memoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>();
                                MemoryBankData memoryBankData3 = memoryBankBlockBehavior.GetBlockData(pos6) ?? new MemoryBankData();
                                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditMemoryBankDialog(memoryBankData3, delegate
                                {
                                    memoryBankBlockBehavior.SetBlockData(pos6, memoryBankData3);
                                    int face3 = ((MemoryBankBlock)BlocksManager.Blocks[186]).GetFace(value26);
                                    ElectricElement electricElement2 = subsystemElectricity.GetElectricElement(pos6.X, pos6.Y, pos6.Z, face3);
                                    if (electricElement2 != null)
                                    {
                                        subsystemElectricity.QueueElectricElementForSimulation(electricElement2, subsystemElectricity.CircuitStep + 1);
                                    }
                                }));
                            }

                            break;
                        case "truthcircuit":
                            if (Terrain.ExtractContents(value26) == 188)
                            {
                                SubsystemTruthTableCircuitBlockBehavior circuitBlockBehavior = base.Project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>();
                                TruthTableData truthTableData = circuitBlockBehavior.GetBlockData(pos6) ?? new TruthTableData();
                                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditTruthTableDialog(truthTableData, delegate
                                {
                                    circuitBlockBehavior.SetBlockData(pos6, truthTableData);
                                    int face5 = ((TruthTableCircuitBlock)BlocksManager.Blocks[188]).GetFace(value26);
                                    ElectricElement electricElement4 = subsystemElectricity.GetElectricElement(pos6.X, pos6.Y, pos6.Z, face5);
                                    if (electricElement4 != null)
                                    {
                                        subsystemElectricity.QueueElectricElementForSimulation(electricElement4, subsystemElectricity.CircuitStep + 1);
                                    }
                                }));
                            }

                            break;
                        case "delaygate":
                            if (Terrain.ExtractContents(value26) == 224)
                            {
                                int data5 = Terrain.ExtractData(value26);
                                int delay = AdjustableDelayGateBlock.GetDelay(data5);
                                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditAdjustableDelayGateDialog(delay, delegate (int newDelay)
                                {
                                    int num131 = AdjustableDelayGateBlock.SetDelay(data5, newDelay);
                                    if (num131 != data5)
                                    {
                                        int value29 = Terrain.ReplaceData(value26, num131);
                                        m_subsystemTerrain.ChangeCell(pos6.X, pos6.Y, pos6.Z, value29);
                                        int face4 = ((AdjustableDelayGateBlock)BlocksManager.Blocks[224]).GetFace(value26);
                                        ElectricElement electricElement3 = subsystemElectricity.GetElectricElement(pos6.X, pos6.Y, pos6.Z, face4);
                                        if (electricElement3 != null)
                                        {
                                            subsystemElectricity.QueueElectricElementForSimulation(electricElement3, subsystemElectricity.CircuitStep + 1);
                                        }
                                    }
                                }));
                            }

                            break;
                        case "battery":
                            if (Terrain.ExtractContents(value26) == 138)
                            {
                                int data6 = Terrain.ExtractData(value26);
                                int voltageLevel = BatteryBlock.GetVoltageLevel(data6);
                                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditBatteryDialog(voltageLevel, delegate (int newVoltageLevel)
                                {
                                    int num132 = BatteryBlock.SetVoltageLevel(data6, newVoltageLevel);
                                    if (num132 != data6)
                                    {
                                        int value30 = Terrain.ReplaceData(value26, num132);
                                        m_subsystemTerrain.ChangeCell(pos6.X, pos6.Y, pos6.Z, value30);
                                        ElectricElement electricElement5 = subsystemElectricity.GetElectricElement(pos6.X, pos6.Y, pos6.Z, 4);
                                        if (electricElement5 != null)
                                        {
                                            subsystemElectricity.QueueElectricElementConnectionsForSimulation(electricElement5, subsystemElectricity.CircuitStep + 1);
                                        }
                                    }
                                }));
                            }

                            break;
                    }
                }
                else if (commandData.Type == "sign")
                {
                    int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(pos6.X, pos6.Y, pos6.Z);
                    if (cellContents2 == 97 || cellContents2 == 210)
                    {
                        SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>(throwOnError: true);
                        DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new EditSignDialog(subsystemSignBlockBehavior, pos6));
                    }
                }
                else if (commandData.Type == "command")
                {
                    int cellContents3 = m_subsystemTerrain.Terrain.GetCellContents(pos6.X, pos6.Y, pos6.Z);
                    if (cellContents3 == 333)
                    {
                        m_componentPlayer.ComponentGui.ModalPanelWidget = new CommandEditWidget(base.Project, m_componentPlayer, pos6);
                    }
                }
                else if (commandData.Type == "button")
                {
                    SubsystemElectricity subsystemElectricity2 = base.Project.FindSubsystem<SubsystemElectricity>();
                    int cellValue14 = m_subsystemTerrain.Terrain.GetCellValue(pos6.X, pos6.Y, pos6.Z);
                    if (Terrain.ExtractContents(cellValue14) == 142)
                    {
                        int face2 = ((ButtonBlock)BlocksManager.Blocks[142]).GetFace(cellValue14);
                        ElectricElement electricElement = subsystemElectricity2.GetElectricElement(pos6.X, pos6.Y, pos6.Z, face2);
                        if (electricElement != null)
                        {
                            ((ButtonElectricElement)electricElement).Press();
                        }
                    }
                }
                else if (commandData.Type == "switch")
                {
                    int cellValue15 = m_subsystemTerrain.Terrain.GetCellValue(pos6.X, pos6.Y, pos6.Z);
                    if (Terrain.ExtractContents(cellValue15) == 141)
                    {
                        int value28 = SwitchBlock.SetLeverState(cellValue15, !SwitchBlock.GetLeverState(cellValue15));
                        m_subsystemTerrain.ChangeCell(pos6.X, pos6.Y, pos6.Z, value28);
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("widget", delegate (CommandData commandData)
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
                                ComponentCraftingTable componentCraftingTable4 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true);
                                m_componentPlayer.ComponentGui.ModalPanelWidget = new FullInventoryWidget(m_componentPlayer.ComponentMiner.Inventory, componentCraftingTable4);
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
            AddFunction("adddrop", delegate (CommandData commandData)
            {
                Point3 onePoint20 = GetOnePoint("pos", commandData);
                int value25 = (int)commandData.GetValue("id");
                int num127 = (int)commandData.GetValue("v");
                for (int num128 = 0; num128 < num127; num128++)
                {
                    m_subsystemPickables.AddPickable(value25, 1, new Vector3(onePoint20) + new Vector3(new Game.Random().Float(0.4f, 0.6f)), null, null);
                }

                return SubmitResult.Success;
            });
            AddFunction("removedrop", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    int num125 = (int)commandData.GetValue("id");
                    foreach (Pickable pickable in m_subsystemPickables.Pickables)
                    {
                        if (pickable.Value == num125)
                        {
                            pickable.ToRemove = true;
                        }
                    }
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint15 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea16 = new CubeArea(twoPoint15[0], twoPoint15[1]);
                    foreach (Pickable pickable2 in m_subsystemPickables.Pickables)
                    {
                        if (cubeArea16.Exist(pickable2.Position))
                        {
                            pickable2.ToRemove = true;
                        }
                    }
                }
                else if (commandData.Type == "limarea")
                {
                    Point3[] twoPoint16 = GetTwoPoint("pos1", "pos2", commandData);
                    int num126 = (int)commandData.GetValue("id");
                    CubeArea cubeArea17 = new CubeArea(twoPoint16[0], twoPoint16[1]);
                    foreach (Pickable pickable3 in m_subsystemPickables.Pickables)
                    {
                        if (cubeArea17.Exist(pickable3.Position) && pickable3.Value == num126)
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
            AddFunction("launchdrop", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint18 = GetOnePoint("pos", commandData);
                    int value23 = (int)commandData.GetValue("id");
                    Vector3 value24 = (Vector3)commandData.GetValue("vec3");
                    m_subsystemPickables.AddPickable(value23, 1, new Vector3(onePoint18) + new Vector3(0.5f), value24, null);
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint14 = GetTwoPoint("pos1", "pos2", commandData);
                    int num123 = (int)commandData.GetValue("id");
                    Vector3 velocity = (Vector3)commandData.GetValue("vec3");
                    CubeArea cubeArea15 = new CubeArea(twoPoint14[0], twoPoint14[1]);
                    foreach (Pickable pickable5 in m_subsystemPickables.Pickables)
                    {
                        if (cubeArea15.Exist(pickable5.Position) && pickable5.Value == num123)
                        {
                            pickable5.Velocity = velocity;
                        }
                    }
                }
                else if (commandData.Type == "gather" || commandData.Type == "spread")
                {
                    Point3 onePoint19 = GetOnePoint("pos", commandData);
                    int num124 = (int)commandData.GetValue("v");
                    bool flag43 = commandData.Type == "gather";
                    foreach (Pickable pickable6 in m_subsystemPickables.Pickables)
                    {
                        Vector3 vector16 = new Vector3(onePoint19) - pickable6.Position;
                        if (!flag43)
                        {
                            vector16 = -vector16;
                        }

                        pickable6.Velocity = vector16 / vector16.Length() * ((float)num124 / 10f);
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("additem", delegate (CommandData commandData)
            {
                int num119 = (int)commandData.GetValue("id");
                int num120 = (int)commandData.GetValue("v");
                if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
                {
                    int num121 = (int)commandData.GetValue("s");
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令additem类型" + commandData.Type + "在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    bool flag42 = commandData.Type == "craft";
                    int index3 = -1;
                    ComponentInventoryBase componentInventoryBase3 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
                    ComponentCraftingTable componentCraftingTable3 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
                    if (componentInventoryBase3 != null && componentCraftingTable3 != null)
                    {
                        List<ComponentInventoryBase.Slot> list17 = ((!flag42) ? componentInventoryBase3.m_slots : componentCraftingTable3.m_slots);
                        switch (commandData.Type)
                        {
                            case "default":
                                index3 = num121 - 1;
                                break;
                            case "inventory":
                                index3 = num121 + 9;
                                break;
                            case "craft":
                                index3 = num121 - 1;
                                break;
                        }

                        if (list17[index3].Value == num119)
                        {
                            list17[index3].Count += num120;
                        }
                        else
                        {
                            list17[index3].Value = num119;
                            list17[index3].Count = num120;
                        }
                    }
                }
                else if (commandData.Type == "hand")
                {
                    ComponentInventoryBase componentInventoryBase4 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
                    int slotValue = componentInventoryBase4.GetSlotValue(componentInventoryBase4.ActiveSlotIndex);
                    int slotCount = componentInventoryBase4.GetSlotCount(componentInventoryBase4.ActiveSlotIndex);
                    if (slotValue == num119)
                    {
                        componentInventoryBase4.RemoveSlotItems(componentInventoryBase4.ActiveSlotIndex, slotCount);
                        componentInventoryBase4.AddSlotItems(componentInventoryBase4.ActiveSlotIndex, num119, slotCount + num120);
                    }
                    else
                    {
                        componentInventoryBase4.RemoveSlotItems(componentInventoryBase4.ActiveSlotIndex, slotCount);
                        componentInventoryBase4.AddSlotItems(componentInventoryBase4.ActiveSlotIndex, num119, num120);
                    }
                }
                else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
                {
                    int num122 = (int)commandData.GetValue("s");
                    Point3 onePoint17 = GetOnePoint("pos", commandData);
                    ComponentBlockEntity blockEntity3 = m_subsystemBlockEntities.GetBlockEntity(onePoint17.X, onePoint17.Y, onePoint17.Z);
                    if (blockEntity3 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    List<ComponentInventoryBase.Slot> list18 = null;
                    try
                    {
                        switch (commandData.Type)
                        {
                            case "chest":
                                list18 = blockEntity3.Entity.FindComponent<ComponentChest>().m_slots;
                                break;
                            case "table":
                                list18 = blockEntity3.Entity.FindComponent<ComponentCraftingTable>().m_slots;
                                break;
                            case "dispenser":
                                list18 = blockEntity3.Entity.FindComponent<ComponentDispenser>().m_slots;
                                break;
                            case "furnace":
                                list18 = blockEntity3.Entity.FindComponent<ComponentFurnace>().m_slots;
                                break;
                        }
                    }
                    catch
                    {
                        return SubmitResult.Fail;
                    }

                    if (list18[num122 - 1].Value == num119)
                    {
                        list18[num122 - 1].Count += num120;
                    }
                    else
                    {
                        list18[num122 - 1].Value = num119;
                        list18[num122 - 1].Count = num120;
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("removeitem", delegate (CommandData commandData)
            {
                int num117 = (int)commandData.GetValue("s");
                int num118 = (int)commandData.GetValue("v");
                if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令removeitem类型" + commandData.Type + "在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    bool flag41 = commandData.Type == "craft";
                    int index2 = -1;
                    ComponentInventoryBase componentInventoryBase2 = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
                    ComponentCraftingTable componentCraftingTable2 = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
                    if (componentInventoryBase2 != null && componentCraftingTable2 != null)
                    {
                        List<ComponentInventoryBase.Slot> list15 = ((!flag41) ? componentInventoryBase2.m_slots : componentCraftingTable2.m_slots);
                        switch (commandData.Type)
                        {
                            case "default":
                                index2 = num117 - 1;
                                break;
                            case "inventory":
                                index2 = num117 + 9;
                                break;
                            case "craft":
                                index2 = num117 - 1;
                                break;
                        }

                        if (list15[index2].Count < num118)
                        {
                            list15[index2].Count = 0;
                        }
                        else
                        {
                            list15[index2].Count = list15[index2].Count - num118;
                        }
                    }
                }
                else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
                {
                    Point3 onePoint16 = GetOnePoint("pos", commandData);
                    ComponentBlockEntity blockEntity2 = m_subsystemBlockEntities.GetBlockEntity(onePoint16.X, onePoint16.Y, onePoint16.Z);
                    if (blockEntity2 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    List<ComponentInventoryBase.Slot> list16 = null;
                    try
                    {
                        switch (commandData.Type)
                        {
                            case "chest":
                                list16 = blockEntity2.Entity.FindComponent<ComponentChest>().m_slots;
                                break;
                            case "table":
                                list16 = blockEntity2.Entity.FindComponent<ComponentCraftingTable>().m_slots;
                                break;
                            case "dispenser":
                                list16 = blockEntity2.Entity.FindComponent<ComponentDispenser>().m_slots;
                                break;
                            case "furnace":
                                list16 = blockEntity2.Entity.FindComponent<ComponentFurnace>().m_slots;
                                break;
                        }
                    }
                    catch
                    {
                        return SubmitResult.Fail;
                    }

                    if (list16[num117 - 1].Count < num118)
                    {
                        list16[num117 - 1].Count = 0;
                    }
                    else
                    {
                        list16[num117 - 1].Count = list16[num117 - 1].Count - num118;
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("clearitem", delegate (CommandData commandData)
            {
                if (commandData.Type == "default" || commandData.Type == "inventory" || commandData.Type == "craft")
                {
                    if (m_gameMode == GameMode.Creative)
                    {
                        ShowSubmitTips("指令clearitem类型" + commandData.Type + "在非创造模式下提交才有效");
                        return SubmitResult.Fail;
                    }

                    bool flag40 = commandData.Type == "craft";
                    int num114 = 0;
                    int num115 = 0;
                    ComponentInventoryBase componentInventoryBase = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>();
                    ComponentCraftingTable componentCraftingTable = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>();
                    if (componentInventoryBase != null && componentCraftingTable != null)
                    {
                        List<ComponentInventoryBase.Slot> list13 = ((!flag40) ? componentInventoryBase.m_slots : componentCraftingTable.m_slots);
                        switch (commandData.Type)
                        {
                            case "default":
                                num114 = 0;
                                num115 = 10;
                                break;
                            case "inventory":
                                num114 = 10;
                                num115 = 16;
                                break;
                            case "craft":
                                num114 = 0;
                                num115 = 6;
                                break;
                        }

                        for (int num116 = 0; num116 < num115; num116++)
                        {
                            list13[num114 + num116].Count = 0;
                        }
                    }
                }
                else if (commandData.Type == "chest" || commandData.Type == "table" || commandData.Type == "dispenser" || commandData.Type == "furnace")
                {
                    Point3 onePoint15 = GetOnePoint("pos", commandData);
                    ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(onePoint15.X, onePoint15.Y, onePoint15.Z);
                    if (blockEntity == null)
                    {
                        return SubmitResult.Fail;
                    }

                    List<ComponentInventoryBase.Slot> list14 = null;
                    try
                    {
                        switch (commandData.Type)
                        {
                            case "chest":
                                list14 = blockEntity.Entity.FindComponent<ComponentChest>().m_slots;
                                break;
                            case "table":
                                list14 = blockEntity.Entity.FindComponent<ComponentCraftingTable>().m_slots;
                                break;
                            case "dispenser":
                                list14 = blockEntity.Entity.FindComponent<ComponentDispenser>().m_slots;
                                break;
                            case "furnace":
                                list14 = blockEntity.Entity.FindComponent<ComponentFurnace>().m_slots;
                                break;
                        }
                    }
                    catch
                    {
                        return SubmitResult.Fail;
                    }

                    foreach (ComponentInventoryBase.Slot item3 in list14)
                    {
                        item3.Count = 0;
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("explosion", delegate (CommandData commandData)
            {
                Point3 onePoint14 = GetOnePoint("pos", commandData);
                int num113 = (int)commandData.GetValue("v");
                SubsystemExplosions subsystemExplosions2 = base.Project.FindSubsystem<SubsystemExplosions>();
                subsystemExplosions2.AddExplosion(onePoint14.X, onePoint14.Y, onePoint14.Z, num113, isIncendiary: false, noExplosionSound: false);
                return SubmitResult.Success;
            });
            AddFunction("lightning", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint13 = GetOnePoint("pos", commandData);
                    base.Project.FindSubsystem<SubsystemSky>().MakeLightningStrike(new Vector3(onePoint13) + new Vector3(0.5f));
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint13 = GetTwoPoint("pos1", "pos2", commandData);
                    Color color5 = (Color)commandData.GetValue("color");
                    int num112 = (int)commandData.GetValue("v1");
                    int v2 = (int)commandData.GetValue("v2");
                    CubeArea cubeArea14 = new CubeArea(twoPoint13[0], twoPoint13[1]);
                    int splitX = (int)((float)cubeArea14.LengthX * (1f - (float)num112 / 100f)) + 1;
                    int splitZ = (int)((float)cubeArea14.LengthZ * (1f - (float)num112 / 100f)) + 1;
                    SubsystemExplosions subsystemExplosions = base.Project.FindSubsystem<SubsystemExplosions>();
                    cubeArea14.Ergodic(delegate
                    {
                        bool flag37 = cubeArea14.MinPoint.Y == cubeArea14.Current.Y;
                        bool flag38 = (cubeArea14.Current.X - cubeArea14.MinPoint.X) % splitX == 0;
                        bool flag39 = (cubeArea14.Current.Z - cubeArea14.MinPoint.Z) % splitZ == 0;
                        if (flag37 && flag38 && flag39)
                        {
                            cubeArea14.Current.Y = m_subsystemTerrain.Terrain.GetTopHeight(cubeArea14.Current.X, cubeArea14.Current.Z);
                            Vector3 position2 = new Vector3(cubeArea14.Current) + new Vector3(new Game.Random().Float(0f, 1f));
                            m_subsystemParticles.AddParticleSystem(new LightningStrikeParticleSystem(position2, color5));
                            subsystemExplosions.AddExplosion(cubeArea14.Current.X, cubeArea14.Current.Y, cubeArea14.Current.Z, v2, isIncendiary: false, noExplosionSound: false);
                        }

                        return false;
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("rain", delegate (CommandData commandData)
            {
                SubsystemWeather subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>();
                bool flag36 = (bool)commandData.GetValue("con");
                Color rainColor = (Color)commandData.GetValue("color");
                m_rainColor = rainColor;
                if (flag36)
                {
                    subsystemWeather.PrecipitationIntensity = 1f;
                    subsystemWeather.m_precipitationStartTime = 0.0;
                }
                else
                {
                    subsystemWeather.PrecipitationIntensity = 0f;
                    subsystemWeather.m_precipitationEndTime = 0.0;
                }

                return SubmitResult.Success;
            });
            AddFunction("skycolor", delegate (CommandData commandData)
            {
                Color skyColor = (Color)commandData.GetValue("color");
                m_skyColor = skyColor;
                return SubmitResult.Success;
            });
            AddFunction("temperature", delegate (CommandData commandData)
            {
                Point3[] twoPoint12 = GetTwoPoint("pos1", "pos2", commandData);
                int temperature = (int)commandData.GetValue("v");
                CubeArea cubeArea13 = new CubeArea(twoPoint12[0], twoPoint12[1]);
                Point2 point17 = Terrain.ToChunk(cubeArea13.MinPoint.X, cubeArea13.MinPoint.Z);
                Point2 point18 = Terrain.ToChunk(cubeArea13.MaxPoint.X, cubeArea13.MaxPoint.Z);
                for (int num108 = point17.X; num108 <= point18.X; num108++)
                {
                    for (int num109 = point17.Y; num109 <= point18.Y; num109++)
                    {
                        TerrainChunk chunkAtCoords3 = m_subsystemTerrain.Terrain.GetChunkAtCoords(num108, num109);
                        for (int num110 = 0; num110 < 16; num110++)
                        {
                            for (int num111 = 0; num111 < 16; num111++)
                            {
                                if (cubeArea13.Exist(new Vector3(num110 + chunkAtCoords3.Origin.X, cubeArea13.Center.Y, num111 + chunkAtCoords3.Origin.Y)))
                                {
                                    chunkAtCoords3.SetTemperatureFast(num110, num111, temperature);
                                }
                            }
                        }
                    }
                }

                Time.QueueTimeDelayedExecution(Time.RealTime + 1.0, delegate
                {
                    m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
                });
                return SubmitResult.Success;
            });
            AddFunction("humidity", delegate (CommandData commandData)
            {
                Point3[] twoPoint11 = GetTwoPoint("pos1", "pos2", commandData);
                int humidity = (int)commandData.GetValue("v");
                CubeArea cubeArea12 = new CubeArea(twoPoint11[0], twoPoint11[1]);
                Point2 point15 = Terrain.ToChunk(cubeArea12.MinPoint.X, cubeArea12.MinPoint.Z);
                Point2 point16 = Terrain.ToChunk(cubeArea12.MaxPoint.X, cubeArea12.MaxPoint.Z);
                for (int num104 = point15.X; num104 <= point16.X; num104++)
                {
                    for (int num105 = point15.Y; num105 <= point16.Y; num105++)
                    {
                        TerrainChunk chunkAtCoords2 = m_subsystemTerrain.Terrain.GetChunkAtCoords(num104, num105);
                        for (int num106 = 0; num106 < 16; num106++)
                        {
                            for (int num107 = 0; num107 < 16; num107++)
                            {
                                if (cubeArea12.Exist(new Vector3(num106 + chunkAtCoords2.Origin.X, cubeArea12.Center.Y, num107 + chunkAtCoords2.Origin.Y)))
                                {
                                    chunkAtCoords2.SetHumidityFast(num106, num107, humidity);
                                }
                            }
                        }
                    }
                }

                Time.QueueTimeDelayedExecution(Time.RealTime + 1.0, delegate
                {
                    m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
                });
                return SubmitResult.Success;
            });
            AddFunction("copyblock", delegate (CommandData commandData)
            {
                WithdrawBlockManager wbManager3 = null;
                if (WithdrawBlockManager.WithdrawMode)
                {
                    wbManager3 = new WithdrawBlockManager();
                }

                Point3[] twoPoint10 = GetTwoPoint("pos1", "pos2", commandData);
                Point3 pos5 = GetOnePoint("pos3", commandData);
                CubeArea cubeArea9 = new CubeArea(twoPoint10[0], twoPoint10[1]);
                if (commandData.Type == "default")
                {
                    Point3 point14 = (Point3)commandData.GetValue("pos4");
                    bool flag29 = (bool)commandData.GetValue("con1");
                    bool flag30 = (bool)commandData.GetValue("con2");
                    CopyBlockManager copyBlockManager2 = new CopyBlockManager(this, wbManager3, cubeArea9.MinPoint, cubeArea9.MaxPoint, handleFurniture: false, flag29)
                    {
                        CopyOrigin = pos5
                    };
                    if (flag30)
                    {
                        copyBlockManager2.ClearBlockArea();
                        PlaceReprocess(wbManager3, commandData, updateChunk: true, cubeArea9.MinPoint, cubeArea9.MaxPoint);
                    }

                    copyBlockManager2.DirectCopy(point14, flag29);
                    PlaceReprocess(wbManager3, commandData, updateChunk: true, point14 - pos5 + cubeArea9.MinPoint, point14 - pos5 + cubeArea9.MaxPoint);
                }
                else if (commandData.Type == "copycache")
                {
                    CopyBlockManager = new CopyBlockManager(this, wbManager3, cubeArea9.MinPoint, cubeArea9.MaxPoint, handleFurniture: true);
                    CopyBlockManager.CopyOrigin = pos5;
                    ShowSubmitTips("建筑已复制到缓存区，可以过视距或跨存档生成\n建筑生成指令build");
                }
                else if (commandData.Type == "rotate")
                {
                    string axis = (string)commandData.GetValue("opt1");
                    string angle2 = (string)commandData.GetValue("opt2");
                    bool flag31 = (bool)commandData.GetValue("con");
                    CopyBlockManager copyBlockManager3 = new CopyBlockManager(this, wbManager3, cubeArea9.MinPoint, cubeArea9.MaxPoint);
                    if (flag31)
                    {
                        copyBlockManager3.ClearBlockArea();
                        PlaceReprocess(wbManager3, commandData, updateChunk: true, cubeArea9.MinPoint, cubeArea9.MaxPoint);
                    }

                    copyBlockManager3.RotateCopy(pos5, axis, angle2);
                    Point3 rotatePoint3 = copyBlockManager3.GetRotatePoint(copyBlockManager3.CubeArea.MinPoint, pos5, axis, angle2);
                    Point3 rotatePoint4 = copyBlockManager3.GetRotatePoint(copyBlockManager3.CubeArea.MaxPoint, pos5, axis, angle2);
                    CubeArea cubeArea10 = new CubeArea(rotatePoint3, rotatePoint4);
                    PlaceReprocess(wbManager3, commandData, updateChunk: true, cubeArea10.MinPoint, cubeArea10.MaxPoint);
                }
                else if (commandData.Type == "mirror")
                {
                    string plane = (string)commandData.GetValue("opt");
                    bool flag32 = (bool)commandData.GetValue("con1");
                    bool laminate = (bool)commandData.GetValue("con2");
                    CopyBlockManager copyBlockManager4 = new CopyBlockManager(this, wbManager3, cubeArea9.MinPoint, cubeArea9.MaxPoint);
                    if (flag32)
                    {
                        copyBlockManager4.ClearBlockArea();
                        PlaceReprocess(wbManager3, commandData, updateChunk: true, cubeArea9.MinPoint, cubeArea9.MaxPoint);
                    }

                    copyBlockManager4.MirrorCopy(pos5, plane, laminate);
                    Point3 mirrorPoint = copyBlockManager4.GetMirrorPoint(copyBlockManager4.CubeArea.MinPoint, pos5, plane, laminate);
                    Point3 mirrorPoint2 = copyBlockManager4.GetMirrorPoint(copyBlockManager4.CubeArea.MaxPoint, pos5, plane, laminate);
                    CubeArea cubeArea11 = new CubeArea(mirrorPoint, mirrorPoint2);
                    PlaceReprocess(wbManager3, commandData, updateChunk: true, cubeArea11.MinPoint, cubeArea11.MaxPoint);
                }
                else if (commandData.Type == "enlarge")
                {
                    cubeArea9.Ergodic(delegate
                    {
                        int num96 = cubeArea9.Current.X - cubeArea9.MinPoint.X;
                        int num97 = cubeArea9.Current.Y - cubeArea9.MinPoint.Y;
                        int num98 = cubeArea9.Current.Z - cubeArea9.MinPoint.Z;
                        int cellValue13 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea9.Current.X, cubeArea9.Current.Y, cubeArea9.Current.Z);
                        int num99 = Terrain.ExtractData(cellValue13);
                        Block block3 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue13)];
                        bool flag33 = block3 is CubeBlock;
                        bool flag34 = block3 is StairsBlock;
                        bool flag35 = block3 is SlabBlock;
                        if (flag33 || flag34 || flag35)
                        {
                            int num100 = 0;
                            int value22 = 0;
                            for (int num101 = 0; num101 < 2; num101++)
                            {
                                for (int num102 = 0; num102 < 2; num102++)
                                {
                                    for (int num103 = 0; num103 < 2; num103++)
                                    {
                                        if (flag33)
                                        {
                                            value22 = cellValue13;
                                        }
                                        else if (flag34)
                                        {
                                            value22 = DataHandle.GetStairValue(cellValue13, num100);
                                        }
                                        else if (flag35)
                                        {
                                            value22 = DataHandle.GetSlabValue(cellValue13, num100);
                                        }

                                        ChangeBlockValue(wbManager3, pos5.X + 2 * num96 + num102, pos5.Y + 2 * num97 + num101, pos5.Z + 2 * num98 + num103, value22);
                                        num100++;
                                    }
                                }
                            }
                        }

                        return false;
                    });
                    PlaceReprocess(wbManager3, commandData, updateChunk: true, pos5, pos5 + new Point3(cubeArea9.LengthX * 2, cubeArea9.LengthY * 2, cubeArea9.LengthZ * 2));
                }
                else if (commandData.Type == "aroundaxis")
                {
                    string opt2 = (string)commandData.GetValue("opt");
                    Task.Run(delegate
                    {
                        Dictionary<int, Dictionary<int, int>> hightLens = new Dictionary<int, Dictionary<int, int>>();
                        cubeArea9.Ergodic(delegate
                        {
                            int cellValue12 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea9.Current.X, cubeArea9.Current.Y, cubeArea9.Current.Z);
                            if (Terrain.ExtractContents(cellValue12) != 0)
                            {
                                int key = cubeArea9.Current.Y;
                                int num93 = cubeArea9.Current.X - pos5.X;
                                int num94 = cubeArea9.Current.Z - pos5.Z;
                                if (opt2 == "x")
                                {
                                    key = cubeArea9.Current.X;
                                    num93 = cubeArea9.Current.Y - pos5.Y;
                                }
                                else if (opt2 == "z")
                                {
                                    key = cubeArea9.Current.Z;
                                    num94 = cubeArea9.Current.Y - pos5.Y;
                                }

                                if (!hightLens.ContainsKey(key))
                                {
                                    hightLens[key] = new Dictionary<int, int>();
                                }

                                int num95 = num93 * num93 + num94 * num94;
                                num95 = (int)MathUtils.Sqrt(num95);
                                if (!hightLens[key].ContainsKey(num95))
                                {
                                    hightLens[key][num95] = cellValue12;
                                }
                            }

                            return false;
                        });
                        int num90 = 0;
                        foreach (int key2 in hightLens.Keys)
                        {
                            foreach (int key3 in hightLens[key2].Keys)
                            {
                                if (key3 > num90)
                                {
                                    num90 = key3;
                                }

                                for (int num91 = -key3; num91 <= key3; num91++)
                                {
                                    for (int num92 = -key3; num92 <= key3; num92++)
                                    {
                                        if (MathUtils.Abs(num91 * num91 + num92 * num92 - key3 * key3) <= key3)
                                        {
                                            switch (opt2)
                                            {
                                                case "x":
                                                    ChangeBlockValue(wbManager3, key2, num91 + pos5.Y, num92 + pos5.Z, hightLens[key2][key3]);
                                                    break;
                                                case "y":
                                                    ChangeBlockValue(wbManager3, num91 + pos5.X, key2, num92 + pos5.Z, hightLens[key2][key3]);
                                                    break;
                                                case "z":
                                                    ChangeBlockValue(wbManager3, num91 + pos5.X, num92 + pos5.Y, key2, hightLens[key2][key3]);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Point3 minPoint2 = new Point3(pos5.X - num90 - 1, 0, pos5.Z - num90 - 1);
                        Point3 maxPoint = new Point3(pos5.X + num90 + 1, 0, pos5.Z + num90 + 1);
                        PlaceReprocess(wbManager3, commandData, updateChunk: true, minPoint2, maxPoint);
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("moveblock", delegate (CommandData commandData)
            {
                Point3[] twoPoint9 = GetTwoPoint("pos1", "pos2", commandData);
                Vector3 vector14 = (Vector3)commandData.GetValue("vec3");
                int num89 = (int)commandData.GetValue("v");
                string id5 = string.Empty;
                switch (commandData.Type)
                {
                    case "default":
                        id5 = "moveblock$default";
                        break;
                    case "dig":
                        id5 = "moveblock$dig";
                        break;
                    case "limit":
                        id5 = "moveblock$limit";
                        break;
                }

                CubeArea cubeArea8 = new CubeArea(twoPoint9[0], twoPoint9[1]);
                List<MovingBlock> list12 = new List<MovingBlock>();
                cubeArea8.Ergodic(delegate
                {
                    list12.Add(new MovingBlock
                    {
                        Value = m_subsystemTerrain.Terrain.GetCellValue(cubeArea8.Current.X, cubeArea8.Current.Y, cubeArea8.Current.Z),
                        Offset = cubeArea8.Current - cubeArea8.MinPoint
                    });
                    return false;
                });
                Vector3 vector15 = new Vector3(cubeArea8.MinPoint.X, cubeArea8.MinPoint.Y, cubeArea8.MinPoint.Z);
                Vector3 targetPosition2 = vector15 + vector14;
                SubsystemMovingBlocks subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>();
                IMovingBlockSet movingBlockSet8 = subsystemMovingBlocks.AddMovingBlockSet(vector15, targetPosition2, (float)num89 / 10f, 0f, 0f, new Vector2(1f, 1f), list12, id5, twoPoint9[0], testCollision: true);
                if (movingBlockSet8 == null)
                {
                    ShowSubmitTips("运动方块添加失败，发生未知错误");
                    return SubmitResult.Fail;
                }

                foreach (MovingBlock item4 in list12)
                {
                    m_subsystemTerrain.ChangeCell(cubeArea8.MinPoint.X + item4.Offset.X, cubeArea8.MinPoint.Y + item4.Offset.Y, cubeArea8.MinPoint.Z + item4.Offset.Z, 0);
                }

                return SubmitResult.Success;
            });
            AddFunction("moveset", delegate (CommandData commandData)
            {
                string text47 = (string)commandData.GetValue("n");
                if (commandData.Type == "default")
                {
                    string face = (string)commandData.GetValue("opt");
                    Point3[] twoPoint7 = GetTwoPoint("pos1", "pos2", commandData);
                    Point3 onePoint12 = GetOnePoint("pos3", commandData);
                    CubeArea cubeArea7 = new CubeArea(twoPoint7[0], twoPoint7[1]);
                    string[] array7 = new string[5] { "$", "|", ":", "@", "&" };
                    string[] array8 = array7;
                    foreach (string value15 in array8)
                    {
                        if (text47.Contains(value15))
                        {
                            ShowSubmitTips("运动设计名称不能包含特殊符号且不能为纯数字");
                            return SubmitResult.Fail;
                        }
                    }

                    if (GetMovingBlockTagLine(text47) != null || ExistWaitMoveSet(text47))
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块设计已存在");
                        return SubmitResult.Fail;
                    }

                    string tag2 = SetMovingBlockTagLine(text47, face, onePoint12 - cubeArea7.MinPoint);
                    List<MovingBlock> list9 = new List<MovingBlock>();
                    cubeArea7.Ergodic(delegate
                    {
                        int cellValue11 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea7.Current.X, cubeArea7.Current.Y, cubeArea7.Current.Z);
                        int id4 = Terrain.ExtractContents(cellValue11);
                        GetMoveEntityBlocks(tag2, id4, cubeArea7.Current, cubeArea7.Current - cubeArea7.MinPoint);
                        list9.Add(new MovingBlock
                        {
                            Value = cellValue11,
                            Offset = cubeArea7.Current - cubeArea7.MinPoint
                        });
                        return false;
                    });
                    Vector3 vector10 = new Vector3(cubeArea7.MinPoint);
                    IMovingBlockSet movingBlockSet2 = m_subsystemMovingBlocks.AddMovingBlockSet(vector10, vector10, 0f, 0f, 0f, new Vector2(1f, 1f), list9, "moveset", tag2, testCollision: true);
                    if (movingBlockSet2 == null)
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块设计添加失败，发生未知错误");
                        return SubmitResult.Fail;
                    }

                    foreach (MovingBlock item5 in list9)
                    {
                        m_subsystemTerrain.ChangeCell(cubeArea7.MinPoint.X + item5.Offset.X, cubeArea7.MinPoint.Y + item5.Offset.Y, cubeArea7.MinPoint.Z + item5.Offset.Z, 0);
                    }
                }
                else if (commandData.Type == "append")
                {
                    Point3[] twoPoint8 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea6 = new CubeArea(twoPoint8[0], twoPoint8[1]);
                    string tag3 = GetMovingBlockTagLine(text47);
                    IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag3);
                    if (tag3 == null)
                    {
                        if (!FindWaitMoveSet(text47, out tag3, out var value16))
                        {
                            goto IL_1082;
                        }

                        movingBlockSet = WaitMoveSetTurnToWork(tag3, value16);
                    }

                    List<MovingBlock> list10 = new List<MovingBlock>();
                    cubeArea6.Ergodic(delegate
                    {
                        int cellValue10 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea6.Current.X, cubeArea6.Current.Y, cubeArea6.Current.Z);
                        movingBlockSet.SetBlock(cubeArea6.Current - new Point3(movingBlockSet.Position), cellValue10);
                        m_subsystemTerrain.ChangeCell(cubeArea6.Current.X, cubeArea6.Current.Y, cubeArea6.Current.Z, 0);
                        return false;
                    });
                }
                else if (commandData.Type == "move")
                {
                    int num86 = (int)commandData.GetValue("v");
                    string tag4 = GetMovingBlockTagLine(text47);
                    Vector3 vector11 = (Vector3)commandData.GetValue("vec3");
                    bool flag28 = (bool)commandData.GetValue("con");
                    IMovingBlockSet movingBlockSet3 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag4);
                    if (tag4 == null)
                    {
                        if (!FindWaitMoveSet(text47, out tag4, out var value17))
                        {
                            goto IL_1082;
                        }

                        movingBlockSet3 = WaitMoveSetTurnToWork(tag4, value17);
                    }

                    MovingBlockTag movingBlockTag = FindMovingBlockTag(text47);
                    if (flag28)
                    {
                        switch (movingBlockTag.Face)
                        {
                            case CoordDirection.NX:
                                vector11 = new Vector3(0f - vector11.X, vector11.Y, 0f - vector11.Z);
                                break;
                            case CoordDirection.PZ:
                                vector11 = new Vector3(0f - vector11.Z, vector11.Y, vector11.X);
                                break;
                            case CoordDirection.NZ:
                                vector11 = new Vector3(vector11.Z, vector11.Y, 0f - vector11.X);
                                break;
                        }
                    }

                    try
                    {
                        m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet3);
                        Vector3 targetPosition = movingBlockSet3.Position + vector11;
                        ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet3).Stop = false;
                        m_subsystemMovingBlocks.AddMovingBlockSet(movingBlockSet3.Position, targetPosition, (float)num86 / 10f, 0f, 0f, new Vector2(1f, 1f), movingBlockSet3.Blocks, "moveset", tag4, testCollision: true);
                    }
                    catch
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块移动失败，发生未知错误");
                        return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type == "turn")
                {
                    string text48 = (string)commandData.GetValue("opt");
                    string tag = GetMovingBlockTagLine(text47);
                    IMovingBlockSet movingBlockSet4 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag);
                    if (tag == null)
                    {
                        if (!FindWaitMoveSet(text47, out tag, out var value18))
                        {
                            goto IL_1082;
                        }

                        movingBlockSet4 = WaitMoveSetTurnToWork(tag, value18);
                    }

                    MovingBlockTag movingBlockTag2 = FindMovingBlockTag(text47);
                    try
                    {
                        Point3 point10 = new Point3((int)MathUtils.Round(movingBlockSet4.Position.X), (int)MathUtils.Round(movingBlockSet4.Position.Y), (int)MathUtils.Round(movingBlockSet4.Position.Z));
                        Point3 point11 = Point3.Zero;
                        m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet4);
                        foreach (MovingBlock block5 in movingBlockSet4.Blocks)
                        {
                            point11 = point10 + block5.Offset;
                            m_subsystemTerrain.ChangeCell(point11.X, point11.Y, point11.Z, block5.Value);
                        }

                        SetMoveEntityBlocks(movingBlockSet4);
                        int num87 = 0;
                        string angle;
                        switch (text48)
                        {
                            case "back":
                                angle = "+180";
                                num87 = 2;
                                break;
                            case "left":
                                angle = "+270";
                                num87 = 3;
                                break;
                            case "right":
                                angle = "+90";
                                num87 = 1;
                                break;
                            default:
                                return SubmitResult.Fail;
                        }

                        CopyBlockManager copyBlockManager = new CopyBlockManager(this, null, point10, point11);
                        copyBlockManager.ClearBlockArea(applyChangeCell: true);
                        copyBlockManager.RotateCopy(movingBlockTag2.Axis + point10, "+y", angle, applyChangeCell: true);
                        Point3 rotatePoint = copyBlockManager.GetRotatePoint(copyBlockManager.CubeArea.MinPoint, movingBlockTag2.Axis + point10, "+y", angle);
                        Point3 rotatePoint2 = copyBlockManager.GetRotatePoint(copyBlockManager.CubeArea.MaxPoint, movingBlockTag2.Axis + point10, "+y", angle);
                        string text49 = "+x";
                        switch (movingBlockTag2.Face)
                        {
                            case CoordDirection.PX:
                                text49 = "+x";
                                break;
                            case CoordDirection.NX:
                                text49 = "-x";
                                break;
                            case CoordDirection.PZ:
                                text49 = "+z";
                                break;
                            case CoordDirection.NZ:
                                text49 = "-z";
                                break;
                        }

                        string[] array9 = new string[4] { "+x", "+z", "-x", "-z" };
                        for (int num88 = 0; num88 < array9.Length; num88++)
                        {
                            if (text49 == array9[num88])
                            {
                                text49 = array9[(num88 + num87) % array9.Length];
                                break;
                            }
                        }

                        CubeArea cubeArea5 = new CubeArea(rotatePoint, rotatePoint2);
                        tag = SetMovingBlockTagLine(movingBlockTag2.Name, text49, movingBlockTag2.Axis + point10 - cubeArea5.MinPoint);
                        List<MovingBlock> list8 = new List<MovingBlock>();
                        cubeArea5.Ergodic(delegate
                        {
                            int cellValue9 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea5.Current.X, cubeArea5.Current.Y, cubeArea5.Current.Z);
                            int id3 = Terrain.ExtractContents(cellValue9);
                            GetMoveEntityBlocks(tag, id3, cubeArea5.Current, cubeArea5.Current - cubeArea5.MinPoint);
                            list8.Add(new MovingBlock
                            {
                                Value = cellValue9,
                                Offset = cubeArea5.Current - cubeArea5.MinPoint
                            });
                            return false;
                        });
                        Vector3 vector12 = new Vector3(cubeArea5.MinPoint);
                        Vector3 vector13 = ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet4).TargetPosition - ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet4).Position;

                        switch (text48)
                        {
                            case "back":
                                vector13 = new Vector3(0f - vector13.X, vector13.Y, 0f - vector13.Z);
                                break;
                            case "left":
                                vector13 = new Vector3(vector13.Z, vector13.Y, 0f - vector13.X);
                                break;
                            case "right":
                                vector13 = new Vector3(0f - vector13.Z, vector13.Y, vector13.X);
                                break;
                            default:
                                vector13 = Vector3.Zero;
                                break;
                        }

                        movingBlockSet4 = m_subsystemMovingBlocks.AddMovingBlockSet(vector12, vector12 + vector13, ((SubsystemMovingBlocks.MovingBlockSet)movingBlockSet4).Speed, 0f, 0f, new Vector2(1f, 1f), list8, "moveset", tag, testCollision: true);
                        if (movingBlockSet4 != null)
                        {
                            foreach (MovingBlock item6 in list8)
                            {
                                m_subsystemTerrain.ChangeCell(cubeArea5.MinPoint.X + item6.Offset.X, cubeArea5.MinPoint.Y + item6.Offset.Y, cubeArea5.MinPoint.Z + item6.Offset.Z, 0);
                            }
                        }
                    }
                    catch
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块转弯失败，发生未知错误");
                        return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type == "pause")
                {
                    string tag5 = GetMovingBlockTagLine(text47);
                    IMovingBlockSet movingBlockSet5 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag5);
                    if (tag5 == null)
                    {
                        if (!FindWaitMoveSet(text47, out tag5, out var value19))
                        {
                            goto IL_1082;
                        }

                        movingBlockSet5 = WaitMoveSetTurnToWork(tag5, value19);
                    }

                    try
                    {
                        movingBlockSet5.Stop();
                        m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet5);
                        m_subsystemMovingBlocks.AddMovingBlockSet(movingBlockSet5.Position, movingBlockSet5.Position, 0f, 0f, 0f, new Vector2(1f, 1f), movingBlockSet5.Blocks, "moveset", tag5, testCollision: true);
                    }
                    catch
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块移除失败，发生未知错误");
                        return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type == "stop")
                {
                    string tag6 = GetMovingBlockTagLine(text47);
                    IMovingBlockSet movingBlockSet6 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag6);
                    if (tag6 == null)
                    {
                        if (FindWaitMoveSet(text47, out tag6, out var _))
                        {
                            return SubmitResult.Success;
                        }

                        goto IL_1082;
                    }

                    try
                    {
                        movingBlockSet6.Stop();
                        m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet6);
                        m_waitingMoveSets[tag6] = new List<Point3>();
                        foreach (MovingBlock block6 in movingBlockSet6.Blocks)
                        {
                            Point3 item = new Point3((int)MathUtils.Round(movingBlockSet6.Position.X), (int)MathUtils.Round(movingBlockSet6.Position.Y), (int)MathUtils.Round(movingBlockSet6.Position.Z)) + block6.Offset;
                            m_waitingMoveSets[tag6].Add(item);
                            m_subsystemTerrain.ChangeCell(item.X, item.Y, item.Z, block6.Value);
                        }

                        SetMoveEntityBlocks(movingBlockSet6);
                    }
                    catch
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块无法转为普通方块，发生未知错误");
                        return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type == "remove")
                {
                    string tag7 = GetMovingBlockTagLine(text47);
                    IMovingBlockSet movingBlockSet7 = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag7);
                    if (tag7 == null)
                    {
                        if (FindWaitMoveSet(text47, out tag7, out var _))
                        {
                            m_waitingMoveSets.Remove(tag7);
                            return SubmitResult.Success;
                        }

                        goto IL_1082;
                    }

                    try
                    {
                        m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet7);
                        foreach (MovingBlock block7 in movingBlockSet7.Blocks)
                        {
                            Point3 point12 = new Point3((int)MathUtils.Round(movingBlockSet7.Position.X), (int)MathUtils.Round(movingBlockSet7.Position.Y), (int)MathUtils.Round(movingBlockSet7.Position.Z)) + block7.Offset;
                            m_subsystemTerrain.ChangeCell(point12.X, point12.Y, point12.Z, block7.Value);
                        }

                        SetMoveEntityBlocks(movingBlockSet7);
                    }
                    catch
                    {
                        ShowSubmitTips("名为" + text47 + "的运动方块移除失败，发生未知错误");
                        return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type == "removeall")
                {
                    List<IMovingBlockSet> list11 = new List<IMovingBlockSet>();
                    foreach (IMovingBlockSet movingBlockSet9 in m_subsystemMovingBlocks.MovingBlockSets)
                    {
                        list11.Add(movingBlockSet9);
                    }

                    foreach (IMovingBlockSet item7 in list11)
                    {
                        if (item7.Id == "moveset")
                        {
                            m_subsystemMovingBlocks.RemoveMovingBlockSet(item7);
                            foreach (MovingBlock block8 in item7.Blocks)
                            {
                                Point3 point13 = new Point3((int)MathUtils.Round(item7.Position.X), (int)MathUtils.Round(item7.Position.Y), (int)MathUtils.Round(item7.Position.Z)) + block8.Offset;
                                m_subsystemTerrain.ChangeCell(point13.X, point13.Y, point13.Z, block8.Value);
                            }

                            SetMoveEntityBlocks(item7);
                        }
                    }

                    m_waitingMoveSets.Clear();
                }

                return SubmitResult.Success;
            IL_1082:
                ShowEditedTips("提示:名为" + text47 + "的运动方块设计不存在");
                return SubmitResult.Fail;
            });
            AddFunction("furniture", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint9 = GetOnePoint("pos", commandData);
                    CellFace start = new CellFace(onePoint9.X, onePoint9.Y, onePoint9.Z, 4);
                    m_subsystemFurnitureBlockBehavior.ScanDesign(start, Vector3.Zero, m_componentPlayer.ComponentMiner);
                }
                else if (commandData.Type == "hammer")
                {
                    Point3[] twoPoint4 = GetTwoPoint("pos1", "pos2", commandData);
                    Point3 pos4 = GetOnePoint("pos3", commandData);
                    int num59 = (int)commandData.GetValue("v1");
                    int num60 = (int)commandData.GetValue("v2");
                    CubeArea cubeArea4 = new CubeArea(twoPoint4[0], twoPoint4[1]);
                    int division = num59;
                    int rotate = num60;
                    cubeArea4.Ergodic(division, 0.1f, delegate (Point3 origin, Point3 coord, Point3 finalCoord)
                    {
                        List<int> list7 = new List<int>();
                        int num81 = 0;
                        bool flag27 = false;
                        for (int num82 = 0; num82 < division; num82++)
                        {
                            for (int num83 = 0; num83 < division; num83++)
                            {
                                for (int num84 = 0; num84 < division; num84++)
                                {
                                    int limitValue = GetLimitValue(origin.X + num84, origin.Y + num83, origin.Z + num82);
                                    list7.Add(limitValue);
                                    flag27 = flag27 || limitValue != 0;
                                    num81++;
                                }
                            }
                        }

                        Point3 point9 = new Point3(pos4.X + coord.X, pos4.Y + coord.Y, pos4.Z + coord.Z);
                        switch (rotate)
                        {
                            case 1:
                                point9 = new Point3(pos4.X - coord.Z, pos4.Y + coord.Y, pos4.Z + coord.X);
                                break;
                            case 2:
                                point9 = new Point3(pos4.X - coord.X, pos4.Y + coord.Y, pos4.Z - coord.Z);
                                break;
                            case 3:
                                point9 = new Point3(pos4.X + coord.Z, pos4.Y + coord.Y, pos4.Z - coord.X);
                                break;
                        }

                        if (flag27)
                        {
                            try
                            {
                                FurnitureDesign furnitureDesign5 = new FurnitureDesign(m_subsystemTerrain);
                                furnitureDesign5.SetValues(division, list7.ToArray());
                                furnitureDesign5.Rotate(1, rotate);
                                FurnitureDesign furnitureDesign6 = m_subsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign5);
                                int value14 = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign6.Index, furnitureDesign6.ShadowStrengthFactor, furnitureDesign6.IsLightEmitter));
                                m_subsystemPickables.AddPickable(value14, 1, new Vector3(commandData.Position) + new Vector3(0.5f, 1f, 0.5f), null, null);
                                m_subsystemTerrain.ChangeCell(point9.X, point9.Y, point9.Z, value14);
                                return;
                            }
                            catch
                            {
                                ShowSubmitTips($"处理区域({origin.ToString()})-({(origin + new Point3(division)).ToString()})时发生未知错误");
                                m_subsystemTerrain.ChangeCell(point9.X, point9.Y, point9.Z, 0);
                                return;
                            }
                        }

                        m_subsystemTerrain.ChangeCell(point9.X, point9.Y, point9.Z, 0);
                    });
                }
                else if (commandData.Type == "slotreduct")
                {
                    int index = (int)commandData.GetValue("fid");
                    Point3 onePoint10 = GetOnePoint("pos", commandData);
                    FurnitureDesign design = m_subsystemFurnitureBlockBehavior.GetDesign(index);
                    if (design == null)
                    {
                        ShowSubmitTips("找不到对应家具");
                        return SubmitResult.Fail;
                    }

                    int num61 = 0;
                    for (int num62 = 0; num62 < design.Resolution; num62++)
                    {
                        for (int num63 = 0; num63 < design.Resolution; num63++)
                        {
                            for (int num64 = 0; num64 < design.Resolution; num64++)
                            {
                                m_subsystemTerrain.ChangeCell(onePoint10.X - num62, onePoint10.Y + num63, onePoint10.Z + num64, design.m_values[num61++]);
                            }
                        }
                    }
                }
                else if (commandData.Type == "posreduct")
                {
                    Point3[] twoPoint5 = GetTwoPoint("pos1", "pos2", commandData);
                    Point3 pos3 = GetOnePoint("pos3", commandData);
                    CubeArea cubeArea3 = new CubeArea(twoPoint5[0], twoPoint5[1]);
                    cubeArea3.Ergodic(delegate
                    {
                        int cellValue8 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea3.Current.X, cubeArea3.Current.Y, cubeArea3.Current.Z);
                        if (Terrain.ExtractContents(cellValue8) == 227)
                        {
                            int data4 = Terrain.ExtractData(cellValue8);
                            int designIndex2 = FurnitureBlock.GetDesignIndex(data4);
                            int rotation = FurnitureBlock.GetRotation(data4);
                            FurnitureDesign furnitureDesign4 = m_subsystemFurnitureBlockBehavior.GetDesign(designIndex2).Clone();
                            furnitureDesign4.Rotate(1, rotation);
                            if (rotation == 1 || rotation == 3)
                            {
                                furnitureDesign4.Mirror(0);
                            }

                            int num77 = 0;
                            Point3 point7 = (cubeArea3.Current - cubeArea3.MinPoint) * furnitureDesign4.Resolution + pos3;
                            Point3 point8 = Point3.Zero;
                            for (int num78 = 0; num78 < furnitureDesign4.Resolution; num78++)
                            {
                                for (int num79 = 0; num79 < furnitureDesign4.Resolution; num79++)
                                {
                                    for (int num80 = 0; num80 < furnitureDesign4.Resolution; num80++)
                                    {
                                        switch (rotation)
                                        {
                                            case 0:
                                                point8 = new Point3(num80, num79, num78);
                                                break;
                                            case 1:
                                                point8 = new Point3(furnitureDesign4.Resolution - num80, num79, num78);
                                                break;
                                            case 2:
                                                point8 = new Point3(num80, num79, num78);
                                                break;
                                            case 3:
                                                point8 = new Point3(furnitureDesign4.Resolution - num80, num79, num78);
                                                break;
                                        }

                                        m_subsystemTerrain.ChangeCell(point7.X + point8.X, point7.Y + point8.Y, point7.Z + point8.Z, furnitureDesign4.m_values[num77++]);
                                    }
                                }
                            }
                        }

                        return false;
                    });
                }
                else if (commandData.Type == "replace")
                {
                    Point3[] twoPoint6 = GetTwoPoint("pos1", "pos2", commandData);
                    int id1 = (int)commandData.GetValue("id1");
                    int id2 = (int)commandData.GetValue("id2");
                    CubeArea cubeArea2 = new CubeArea(twoPoint6[0], twoPoint6[1]);
                    cubeArea2.Ergodic(delegate
                    {
                        int cellValue7 = m_subsystemTerrain.Terrain.GetCellValue(cubeArea2.Current.X, cubeArea2.Current.Y, cubeArea2.Current.Z);
                        int num74 = Terrain.ExtractContents(cellValue7);
                        int data2 = Terrain.ExtractData(cellValue7);
                        if (num74 == 227)
                        {
                            int designIndex = FurnitureBlock.GetDesignIndex(data2);
                            FurnitureDesign design3 = m_subsystemFurnitureBlockBehavior.GetDesign(designIndex);
                            if (design3 != null)
                            {
                                List<FurnitureDesign> list6 = design3.CloneChain();
                                foreach (FurnitureDesign item8 in list6)
                                {
                                    int[] array6 = new int[design3.m_values.Length];
                                    for (int num75 = 0; num75 < design3.m_values.Length; num75++)
                                    {
                                        int num76 = Terrain.ReplaceLight(design3.m_values[num75], 0);
                                        if (num76 == id1)
                                        {
                                            array6[num75] = id2;
                                        }
                                        else
                                        {
                                            array6[num75] = design3.m_values[num75];
                                        }
                                    }

                                    item8.SetValues(design3.m_resolution, array6);
                                    FurnitureDesign furnitureDesign3 = m_subsystemFurnitureBlockBehavior.TryAddDesignChain(list6[0], garbageCollectIfNeeded: true);
                                    if (furnitureDesign3 != null)
                                    {
                                        int data3 = FurnitureBlock.SetDesignIndex(data2, furnitureDesign3.Index, furnitureDesign3.ShadowStrengthFactor, furnitureDesign3.IsLightEmitter);
                                        int value13 = Terrain.ReplaceData(cellValue7, data3);
                                        m_subsystemTerrain.ChangeCell(cubeArea2.Current.X, cubeArea2.Current.Y, cubeArea2.Current.Z, value13);
                                    }
                                }
                            }
                        }

                        return false;
                    });
                }
                else if (commandData.Type == "link+x" || commandData.Type == "wire+x")
                {
                    Point3 onePoint11 = GetOnePoint("pos", commandData);
                    bool flag25 = (bool)commandData.GetValue("con");
                    bool flag26 = commandData.Type == "wire+x";
                    List<int> list4 = new List<int>();
                    List<FurnitureDesign> list5 = new List<FurnitureDesign>();
                    int num65 = 0;
                    while (num65 < 1024)
                    {
                        int cellValue5 = m_subsystemTerrain.Terrain.GetCellValue(onePoint11.X + num65++, onePoint11.Y, onePoint11.Z);
                        if (Terrain.ExtractContents(cellValue5) != 227)
                        {
                            break;
                        }

                        list4.Add(FurnitureBlock.GetDesignIndex(Terrain.ExtractData(cellValue5)));
                    }

                    if (list4.Count < 2)
                    {
                        ShowSubmitTips("家具数量少于2，无法链接");
                        return SubmitResult.Fail;
                    }

                    foreach (int item9 in list4)
                    {
                        FurnitureDesign design2 = m_subsystemFurnitureBlockBehavior.GetDesign(item9);
                        if (design2 != null)
                        {
                            if (flag25 && design2.LinkedDesign != null)
                            {
                                foreach (FurnitureDesign item10 in design2.ListChain())
                                {
                                    list5.Add(item10.Clone());
                                }
                            }
                            else
                            {
                                list5.Add(design2.Clone());
                            }
                        }
                    }

                    for (int num66 = 0; num66 < list5.Count; num66++)
                    {
                        list5[num66].InteractionMode = ((!flag26) ? FurnitureInteractionMode.Multistate : FurnitureInteractionMode.ConnectedMultistate);
                        list5[num66].LinkedDesign = list5[(num66 + 1) % list5.Count];
                    }

                    FurnitureDesign furnitureDesign = m_subsystemFurnitureBlockBehavior.TryAddDesignChain(list5[0], garbageCollectIfNeeded: true);
                    if (furnitureDesign != null)
                    {
                        int value12 = Terrain.MakeBlockValue(227, 0, FurnitureBlock.SetDesignIndex(0, furnitureDesign.Index, furnitureDesign.ShadowStrengthFactor, furnitureDesign.IsLightEmitter));
                        Vector3 vector9 = ((commandData.Position == Point3.Zero) ? m_componentPlayer.ComponentBody.Position : new Vector3(commandData.Position));
                        m_subsystemPickables.AddPickable(value12, 1, vector9 + new Vector3(0.5f, 1f, 0.5f), null, null);
                    }
                }
                else if (commandData.Type == "find")
                {
                    int fid = (int)commandData.GetValue("fid");
                    SubsystemSpawn subsystemSpawn = base.Project.FindSubsystem<SubsystemSpawn>();
                    Point2[] chunkPoints = subsystemSpawn.m_chunks.Keys.ToArray();
                    int l2 = 0;
                    float num67 = 0f;
                    List<Point3> points = new List<Point3>();
                    foreach (Point2 key4 in subsystemSpawn.m_chunks.Keys)
                    {
                        Time.QueueTimeDelayedExecution(Time.RealTime + (double)num67, delegate
                        {
                            int num72 = chunkPoints[l2].X * 16;
                            int num73 = chunkPoints[l2].Y * 16;
                            m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
                            CommandCamera commandCamera7 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                            commandCamera7.m_position = new Vector3(num72, commandCamera7.m_position.Y, num73);
                            ShowSubmitTips($"正在扫描区块({new Point3(num72, 0, num73).ToString()})-({new Point3(num72 + 16, 255, num73 + 16).ToString()}),进度:{l2}/{chunkPoints.Length - 1}");
                        });
                        Time.QueueTimeDelayedExecution(Time.RealTime + (double)num67 + 0.5, delegate
                        {
                            TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(chunkPoints[l2].X, chunkPoints[l2].Y);
                            for (int num69 = 0; num69 < 16; num69++)
                            {
                                for (int num70 = 0; num70 < 16; num70++)
                                {
                                    for (int num71 = 0; num71 < 256; num71++)
                                    {
                                        int x = chunkAtCoords.Origin.X + num69;
                                        int y = num71;
                                        int z = chunkAtCoords.Origin.Y + num70;
                                        int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
                                        if (cellContents == 227)
                                        {
                                            int cellValue6 = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
                                            if (fid == FurnitureBlock.GetDesignIndex(Terrain.ExtractData(cellValue6)))
                                            {
                                                points.Add(new Point3(x, y, z));
                                                ShowSubmitTips($"点({new Point3(x, y, z).ToString()})发现目标家具!");
                                            }
                                        }
                                    }
                                }
                            }

                            if (l2 == chunkPoints.Length - 1)
                            {
                                string text46 = "查找完毕,";
                                if (points.Count != 0)
                                {
                                    text46 += "查找结果已复制到剪切板:\n";
                                    foreach (Point3 item11 in points)
                                    {
                                        text46 = text46 + item11.ToString() + "  ";
                                    }

                                    ClipboardManager.ClipboardString = text46;
                                }
                                else
                                {
                                    text46 += "未找到目标家具";
                                }

                                m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
                                ShowSubmitTips(text46);
                            }

                            l2++;
                        });
                        num67 += 0.6f;
                    }
                }
                else if (commandData.Type == "remove")
                {
                    for (int num68 = 0; num68 < m_subsystemFurnitureBlockBehavior.m_furnitureDesigns.Length; num68++)
                    {
                        FurnitureDesign furnitureDesign2 = m_subsystemFurnitureBlockBehavior.m_furnitureDesigns[num68];
                        if (furnitureDesign2 != null && furnitureDesign2.FurnitureSet == null)
                        {
                            m_subsystemFurnitureBlockBehavior.m_furnitureDesigns[num68] = null;
                        }
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("camera", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint8 = GetOnePoint("pos", commandData);
                    Point2 eyes = (Point2)commandData.GetValue("eyes");
                    bool flag22 = (bool)commandData.GetValue("con");
                    m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, (!flag22) ? CommandCamera.CameraType.Aero : CommandCamera.CameraType.Lock);
                    CommandCamera commandCamera2 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                    commandCamera2.m_position = new Vector3(onePoint8);
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
                    Vector3 vector7 = (Vector3)commandData.GetValue("vec3");
                    int num57 = (int)commandData.GetValue("v");
                    bool flag23 = (bool)commandData.GetValue("con");
                    m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MovePos);
                    CommandCamera commandCamera3 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                    commandCamera3.m_targetPosition = vector7 + commandCamera3.m_position;
                    commandCamera3.m_speed = (float)num57 / 10f;
                    commandCamera3.m_skipToAero = !flag23;
                }
                else if (commandData.Type == "direct")
                {
                    Point2 eyes2 = (Point2)commandData.GetValue("eyes");
                    int num58 = (int)commandData.GetValue("v");
                    bool flag24 = (bool)commandData.GetValue("con");
                    m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MoveDirect);
                    CommandCamera commandCamera4 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                    commandCamera4.m_targetDirection = DataHandle.EyesToDirection(eyes2);
                    commandCamera4.m_speed = (float)num58 / 10f;
                    commandCamera4.m_skipToAero = !flag24;
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
                    Vector2 vector8 = (Vector2)commandData.GetValue("vec2");
                    m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.MoveWithPlayer);
                    CommandCamera commandCamera5 = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                    commandCamera5.m_relativePosition = relativePosition;
                    commandCamera5.m_relativeAngle = new Point2((int)vector8.X, (int)vector8.Y);
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
                        m_subsystemTerrain.ChangeCell(key5.X, key5.Y, key5.Z, RecordManager.ChangeBlocks[key5]);
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
            AddFunction("gametime", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    SubsystemTimeOfDay subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>();
                    int num53 = (int)(subsystemTimeOfDay.TimeOfDay * 4096f);
                    ShowSubmitTips($"当前游戏时间{num53}/{4096}");
                }
                else if (commandData.Type.StartsWith("byo"))
                {
                    SubsystemTimeOfDay subsystemTimeOfDay2 = base.Project.FindSubsystem<SubsystemTimeOfDay>();
                    int num54 = 0;
                    switch (commandData.Type)
                    {
                        case "byo-dawn":
                            num54 = 1;
                            break;
                        case "byo-noon":
                            num54 = 2;
                            break;
                        case "byo-dusk":
                            num54 = 3;
                            break;
                        case "byo-night":
                            num54 = 4;
                            break;
                    }

                    subsystemTimeOfDay2.TimeOfDayOffset += MathUtils.Remainder(MathUtils.Remainder(0.25f * (float)num54, 1f) - subsystemTimeOfDay2.TimeOfDay, 1f);
                }
                else if (commandData.Type == "accelerate")
                {
                    int num55 = (int)commandData.GetValue("v");
                    if (num55 == 10)
                    {
                        num55 = 255;
                    }

                    base.Project.FindSubsystem<SubsystemTime>().GameTimeFactor = num55;
                }
                else if (commandData.Type == "slow")
                {
                    int num56 = (int)commandData.GetValue("v");
                    if (num56 == 10)
                    {
                        num56 = 1000;
                    }

                    base.Project.FindSubsystem<SubsystemTime>().GameTimeFactor = 1f / (float)num56;
                }

                return SubmitResult.Success;
            });
            AddFunction("gamemode", delegate (CommandData commandData)
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
                    string text45 = (string)commandData.GetValue("text");
                    if (!(text45 == "LEURC"))
                    {
                        ShowSubmitTips("密码错误，请重新输入");
                        return SubmitResult.Fail;
                    }

                    gameMode = GameMode.Cruel;
                }

                m_subsystemGameInfo.WorldSettings.GameMode = gameMode;
                WorldInfo worldInfo2 = GameManager.WorldInfo;
                GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
                GameManager.DisposeProject();
                ScreensManager.SwitchScreen("GameLoading", worldInfo2, null);
                return SubmitResult.Success;
            });
            AddFunction("settings", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    ScreensManager.SwitchScreen("Settings");
                }
                else if (commandData.Type == "visibility")
                {
                    int num50 = (int)commandData.GetValue("v");
                    if (num50 < 0)
                    {
                        ShowSubmitTips("游戏视距不能小于0");
                        return SubmitResult.Fail;
                    }

                    SettingsManager.VisibilityRange = num50;
                }
                else if (commandData.Type == "brightness")
                {
                    int num51 = (int)commandData.GetValue("v");
                    SettingsManager.Brightness = (float)num51 / 100f;
                }
                else if (commandData.Type == "skymode")
                {
                    string text43 = (string)commandData.GetValue("opt");
                    switch (text43)
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
                            ShowSubmitTips("指令settings类型skymode找不到天空模式：" + text43);
                            return SubmitResult.Fail;
                    }
                }
                else if (commandData.Type.StartsWith("vol-") || commandData.Type.StartsWith("sen-"))
                {
                    int num52 = (int)commandData.GetValue("v");
                    switch (commandData.Type)
                    {
                        case "vol-sound":
                            SettingsManager.SoundsVolume = (float)num52 / 10f;
                            break;
                        case "vol-music":
                            SettingsManager.MusicVolume = (float)num52 / 10f;
                            break;
                        case "sen-move":
                            SettingsManager.MoveSensitivity = (float)num52 / 10f;
                            break;
                        case "sen-look":
                            SettingsManager.LookSensitivity = (float)num52 / 10f;
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
                    bool flag21 = (bool)commandData.GetValue("con");
                    m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode = ((!flag21) ? EnvironmentBehaviorMode.Static : EnvironmentBehaviorMode.Living);
                }
                else if (commandData.Type == "weathereffect")
                {
                    bool areWeatherEffectsEnabled = (bool)commandData.GetValue("con");
                    m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled = areWeatherEffectsEnabled;
                }
                else if (commandData.Type == "daymode")
                {
                    string text44 = (string)commandData.GetValue("opt");
                    TimeOfDayMode timeOfDayMode = TimeOfDayMode.Day;
                    switch (text44)
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
                            ShowSubmitTips("指令settings类型daymode找不到时间模式：" + text44);
                            return SubmitResult.Fail;
                    }

                    m_subsystemGameInfo.WorldSettings.TimeOfDayMode = timeOfDayMode;
                }

                return SubmitResult.Success;
            });
            AddFunction("shapeshifter", delegate (CommandData commandData)
            {
                bool con2 = (bool)commandData.GetValue("con");
                m_shapeshifter = con2;
                string target = (con2 ? "Wolf_Gray" : "Werewolf");
                string name2 = (con2 ? "Werewolf" : "Wolf_Gray");
                ErgodicBody(target, delegate (ComponentBody body)
                {
                    ComponentShapeshifter componentShapeshifter2 = body.Entity.FindComponent<ComponentShapeshifter>();
                    if (componentShapeshifter2 != null)
                    {
                        componentShapeshifter2.IsEnabled = true;
                        componentShapeshifter2.ShapeshiftTo(name2);
                    }

                    return false;
                });
                Time.QueueTimeDelayedExecution(Time.RealTime + 5.0, delegate
                {
                    ErgodicBody(name2, delegate (ComponentBody body)
                    {
                        ComponentShapeshifter componentShapeshifter = body.Entity.FindComponent<ComponentShapeshifter>();
                        if (componentShapeshifter != null)
                        {
                            componentShapeshifter.IsEnabled = !con2;
                        }

                        return false;
                    });
                });
                return SubmitResult.Success;
            });
            AddFunction("lockscreen", delegate (CommandData commandData)
            {
                bool flag20 = (bool)commandData.GetValue("con");
                m_componentPlayer.ComponentLocomotion.LookSpeed = (flag20 ? 8E-08f : 8f);
                m_componentPlayer.ComponentLocomotion.TurnSpeed = (flag20 ? 8E-08f : 8f);
                return SubmitResult.Success;
            });
            AddFunction("deathscreen", delegate (CommandData commandData)
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
            AddFunction("blockfirm", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    bool flag17 = (bool)commandData.GetValue("con");
                    m_firmAllBlocks = flag17;
                    if (flag17)
                    {
                        SubsystemCommandExt.BlockDataChange = true;
                        for (int num49 = 1; num49 < BlocksManager.Blocks.Length; num49++)
                        {
                            try
                            {
                                Block block2 = BlocksManager.Blocks[num49];
                                block2.DigResilience = float.PositiveInfinity;
                                block2.ExplosionResilience = float.PositiveInfinity;
                                block2.ProjectileResilience = float.PositiveInfinity;
                                block2.FireDuration = 0f;
                                block2.DefaultDropCount = 0f;
                                block2.DefaultExperienceCount = 0f;
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
                    int value10 = (int)commandData.GetValue("id");
                    bool flag18 = (bool)commandData.GetValue("con");
                    value10 = Terrain.ExtractContents(value10);
                    if (flag18)
                    {
                        SetFirmBlocks(value10, open: true, null);
                        if (!m_firmBlockList.Contains(value10))
                        {
                            m_firmBlockList.Add(value10);
                        }
                    }
                    else
                    {
                        if (OriginFirmBlockList.TryGetValue(value10, out var value11))
                        {
                            SetFirmBlocks(value10, open: false, value11);
                        }

                        if (m_firmBlockList.Contains(value10))
                        {
                            m_firmBlockList.Remove(value10);
                        }
                    }
                }
                else if (commandData.Type == "nosqueeze")
                {
                    bool flag19 = (bool)commandData.GetValue("con");
                }

                return SubmitResult.Success;
            });
            AddFunction("getcell", delegate (CommandData commandData)
            {
                Point3[] twoPoint3 = GetTwoPoint("pos1", "pos2", commandData);
                string f9 = (string)commandData.GetValue("f");
                bool flag15 = commandData.Type == "default";
                bool isGlobal = commandData.Type == "global";
                bool flag16 = commandData.Type == "chunk";
                CubeArea cube2 = new CubeArea(twoPoint3[0], twoPoint3[1]);
                Stream stream5 = GetCommandFileStream(f9, OpenFileMode.CreateOrOpen);
                StreamWriter streamwriter = new StreamWriter(stream5);
                if (flag15 || isGlobal)
                {
                    cube2.Ergodic(delegate
                    {
                        int num46 = (isGlobal ? cube2.Current.X : (cube2.Current.X - cube2.MinPoint.X));
                        int num47 = (isGlobal ? cube2.Current.Y : (cube2.Current.Y - cube2.MinPoint.Y));
                        int num48 = (isGlobal ? cube2.Current.Z : (cube2.Current.Z - cube2.MinPoint.Z));
                        int cellValue4 = m_subsystemTerrain.Terrain.GetCellValue(cube2.Current.X, cube2.Current.Y, cube2.Current.Z);
                        if (Terrain.ExtractContents(cellValue4) != 0)
                        {
                            string value9 = $"{num46},{num47},{num48},{cellValue4}";
                            streamwriter.WriteLine(value9);
                        }

                        return false;
                    });
                    streamwriter.Flush();
                    stream5.Dispose();
                    ShowSubmitTips("方块文件已生成，路径：\n" + DataHandle.GetCommandResPathName(f9));
                }
                else if (flag16)
                {
                    cube2.ErgodicByChunk(3f, 0.1f, delegate (Point3 origin, Point2 coord, Point2 finalCoord)
                    {
                        m_componentPlayer.GameWidget.ActiveCamera = new CommandCamera(m_componentPlayer.GameWidget, CommandCamera.CameraType.Aero);
                        CommandCamera commandCamera = m_componentPlayer.GameWidget.ActiveCamera as CommandCamera;
                        commandCamera.m_position = new Vector3(origin) + new Vector3(0f, 100f, 0f);
                        if (coord.X == -1 && coord.Y == -1)
                        {
                            ShowSubmitTips("方块文件正在生成，请耐心等候");
                        }
                        else
                        {
                            string value7 = $"#CHUNK:{coord.X},{coord.Y}";
                            streamwriter.WriteLine(value7);
                            for (int num43 = 0; num43 < 16; num43++)
                            {
                                for (int num44 = 0; num44 < cube2.LengthY; num44++)
                                {
                                    for (int num45 = 0; num45 < 16; num45++)
                                    {
                                        Point3 point6 = new Point3(origin.X + num43, origin.Y + num44, origin.Z + num45);
                                        int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(point6.X, point6.Y, point6.Z);
                                        if (Terrain.ExtractContents(cellValue3) != 0)
                                        {
                                            string value8 = $"{point6.X},{point6.Y},{point6.Z},{cellValue3}";
                                            streamwriter.WriteLine(value8);
                                        }
                                    }
                                }
                            }

                            streamwriter.WriteLine("###\n");
                            ShowSubmitTips($"区块({origin.ToString()})-({(origin + new Point3(16, cube2.LengthY, 16)).ToString()})的方块信息已生成完毕");
                            if (coord.X == finalCoord.X && coord.Y == finalCoord.Y)
                            {
                                streamwriter.Flush();
                                stream5.Dispose();
                                m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
                                ShowSubmitTips("方块文件已生成，路径：\n" + DataHandle.GetCommandResPathName(f9));
                            }
                        }
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("memorydata", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint6 = GetOnePoint("pos", commandData);
                    string data = (string)commandData.GetValue("text");
                    SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>(throwOnError: true);
                    SubsystemEditableItemBehavior<MemoryBankData> subsystemEditableItemBehavior = base.Project.FindSubsystem<SubsystemEditableItemBehavior<MemoryBankData>>(throwOnError: true);
                    int cellValue = m_subsystemTerrain.Terrain.GetCellValue(onePoint6.X, onePoint6.Y, onePoint6.Z);
                    if (Terrain.ExtractContents(cellValue) == 186)
                    {
                        MemoryBankData memoryBankData = subsystemMemoryBankBlockBehavior.GetBlockData(onePoint6);
                        if (memoryBankData == null)
                        {
                            memoryBankData = new MemoryBankData();
                            subsystemEditableItemBehavior.SetBlockData(onePoint6, memoryBankData);
                        }

                        memoryBankData.LoadString(data);
                        memoryBankData.SaveString();
                    }
                }
                else if (commandData.Type == "imagemul" || commandData.Type == "imagefour")
                {
                    string f4 = (string)commandData.GetValue("f1");
                    string f5 = (string)commandData.GetValue("f2");
                    Stream commandFileStream11 = GetCommandFileStream(f4, OpenFileMode.ReadWrite);
                    if (commandFileStream11 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    if (!Png.IsPngStream(commandFileStream11))
                    {
                        ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                    }

                    Image image2 = Image.Load(commandFileStream11);
                    string text42 = string.Empty;
                    if (commandData.Type == "imagemul")
                    {
                        for (int num27 = 0; num27 < image2.Height; num27++)
                        {
                            for (int num28 = 0; num28 < image2.Width; num28++)
                            {
                                Color pixel2 = image2.GetPixel(num28, num27);
                                byte r = pixel2.R;
                                byte g = pixel2.G;
                                byte b = pixel2.B;
                                if (pixel2.A < 20)
                                {
                                    text42 += 0;
                                }
                                else if (pixel2.R == 111 && pixel2.G == 111 && pixel2.B == 111)
                                {
                                    text42 += 1;
                                }
                                else
                                {
                                    int n2 = DataHandle.GetColorIndex(pixel2, 1) + 7;
                                    text42 += DataHandle.NumberToSignal(n2);
                                }
                            }

                            text42 += "\n";
                        }
                    }
                    else
                    {
                        for (int num29 = 0; num29 < image2.Height; num29 += 2)
                        {
                            for (int num30 = 0; num30 < image2.Width; num30 += 2)
                            {
                                int[] array4 = new int[4];
                                for (int num31 = 0; num31 < 4; num31++)
                                {
                                    Color c2 = Color.Black;
                                    switch (num31)
                                    {
                                        case 0:
                                            c2 = image2.GetPixel(num30, num29);
                                            break;
                                        case 1:
                                            c2 = image2.GetPixel(num30 + 1, num29);
                                            break;
                                        case 2:
                                            c2 = image2.GetPixel(num30, num29 + 1);
                                            break;
                                        case 3:
                                            c2 = image2.GetPixel(num30 + 1, num29 + 1);
                                            break;
                                    }

                                    if ((c2.R == 111 && c2.G == 111 && c2.B == 111) || c2.A < 20)
                                    {
                                        array4[num31] = 0;
                                    }
                                    else
                                    {
                                        array4[num31] = ((DataHandle.GetColorIndex(c2, 1) != 0) ? 1 : 0);
                                    }
                                }

                                int n3 = array4[0] + array4[1] * 2 + array4[2] * 4 + array4[3] * 8;
                                text42 += DataHandle.NumberToSignal(n3);
                            }

                            text42 += "\n";
                        }
                    }

                    text42 += "###\n";
                    Stream commandFileStream12 = GetCommandFileStream(f5, OpenFileMode.CreateOrOpen);
                    commandFileStream12.Position = commandFileStream12.Length;
                    using (StreamWriter streamWriter = new StreamWriter(commandFileStream12))
                    {
                        streamWriter.WriteLine(text42);
                        streamWriter.Flush();
                    }

                    commandFileStream11.Dispose();
                    commandFileStream12.Dispose();
                }
                else if (commandData.Type == "rank")
                {
                    string f6 = (string)commandData.GetValue("f1");
                    string f7 = (string)commandData.GetValue("f2");
                    Stream commandFileStream13 = GetCommandFileStream(f6, OpenFileMode.ReadWrite);
                    if (commandFileStream13 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    int num32 = 0;
                    int num33 = 0;
                    string empty2 = string.Empty;
                    StreamReader streamReader3 = new StreamReader(commandFileStream13);
                    while ((empty2 = streamReader3.ReadLine()) != null)
                    {
                        if (!(empty2 == "") && !(empty2 == " "))
                        {
                            if (num33 == 0)
                            {
                                num33 = empty2.Length;
                            }

                            if (empty2 == "###")
                            {
                                break;
                            }

                            num32++;
                        }
                    }

                    string[] array5 = new string[num32 * num33];
                    int num34 = 0;
                    commandFileStream13.Position = 0L;
                    StreamReader streamReader4 = new StreamReader(commandFileStream13);
                    while ((empty2 = streamReader4.ReadLine()) != null)
                    {
                        if (!(empty2 == "") && !(empty2 == " "))
                        {
                            if (empty2 == "###")
                            {
                                num34 = 0;
                            }
                            else
                            {
                                for (int num35 = 0; num35 < num33; num35++)
                                {
                                    if (array5[num34 * num33 + num35] == null)
                                    {
                                        array5[num34 * num33 + num35] = "";
                                    }

                                    array5[num34 * num33 + num35] += empty2[num35];
                                }

                                num34++;
                            }
                        }
                    }

                    string empty3 = string.Empty;
                    Stream commandFileStream14 = GetCommandFileStream(f7, OpenFileMode.CreateOrOpen);
                    StreamWriter streamWriter2 = new StreamWriter(commandFileStream14);
                    for (int num36 = 0; num36 < array5.Length; num36++)
                    {
                        empty3 = array5[num36] + ((num36 % num33 == num33 - 1) ? "\n###" : "");
                        streamWriter2.WriteLine(empty3);
                    }

                    streamWriter2.Flush();
                    commandFileStream13.Dispose();
                    commandFileStream14.Dispose();
                }
                else if (commandData.Type.StartsWith("load"))
                {
                    Point3 onePoint7 = GetOnePoint("pos", commandData);
                    int num37 = (int)commandData.GetValue("r");
                    int num38 = (int)commandData.GetValue("c");
                    string f8 = (string)commandData.GetValue("f");
                    SubsystemMemoryBankBlockBehavior subsystemMemoryBankBlockBehavior2 = base.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>(throwOnError: true);
                    SubsystemEditableItemBehavior<MemoryBankData> subsystemEditableItemBehavior2 = base.Project.FindSubsystem<SubsystemEditableItemBehavior<MemoryBankData>>(throwOnError: true);
                    Stream commandFileStream15 = GetCommandFileStream(f8, OpenFileMode.ReadWrite);
                    if (commandFileStream15 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    StreamReader streamReader5 = new StreamReader(commandFileStream15);
                    string empty4 = string.Empty;
                    int num39 = 0;
                    int num40 = onePoint7.X;
                    int num41 = onePoint7.Y;
                    int num42 = onePoint7.Z;
                    while ((empty4 = streamReader5.ReadLine()) != null)
                    {
                        if (!(empty4 == "") && !(empty4 == " "))
                        {
                            if (empty4.StartsWith("###"))
                            {
                                switch (commandData.Type)
                                {
                                    case "load+x-y":
                                        num40 = onePoint7.X;
                                        num41 -= num37;
                                        break;
                                    case "load-x-y":
                                        num40 = onePoint7.X;
                                        num41 -= num37;
                                        break;
                                    case "load+z-y":
                                        num42 = onePoint7.Z;
                                        num41 -= num37;
                                        break;
                                    case "load-z-y":
                                        num42 = onePoint7.Z;
                                        num41 -= num37;
                                        break;
                                    case "load+x+z":
                                        num40 = onePoint7.X;
                                        num42 += num37;
                                        break;
                                    case "load-x-z":
                                        num40 = onePoint7.X;
                                        num42 -= num37;
                                        break;
                                }
                            }
                            else
                            {
                                int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(num40, num41, num42);
                                if (Terrain.ExtractContents(cellValue2) == 186)
                                {
                                    MemoryBankData memoryBankData2 = subsystemMemoryBankBlockBehavior2.GetBlockData(new Point3(num40, num41, num42));
                                    if (memoryBankData2 == null)
                                    {
                                        memoryBankData2 = new MemoryBankData();
                                        subsystemEditableItemBehavior2.SetBlockData(new Point3(num40, num41, num42), memoryBankData2);
                                    }

                                    memoryBankData2.LoadString(empty4);
                                    memoryBankData2.SaveString();
                                }
                                else
                                {
                                    num39++;
                                }

                                switch (commandData.Type)
                                {
                                    case "load+x-y":
                                        num40 += num38;
                                        break;
                                    case "load-x-y":
                                        num40 -= num38;
                                        break;
                                    case "load+z-y":
                                        num42 += num38;
                                        break;
                                    case "load-z-y":
                                        num42 -= num38;
                                        break;
                                    case "load+x+z":
                                        num40 += num38;
                                        break;
                                    case "load-x-z":
                                        num40 -= num38;
                                        break;
                                }
                            }
                        }
                    }

                    if (num39 == 0)
                    {
                        ShowSubmitTips("M板数据已全部写入");
                    }
                    else
                    {
                        ShowSubmitTips(num39 + "个坐标位置不存在M板，请检查指令输入是否正确");
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("world", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text32 = (string)commandData.GetValue("f");
                    WorldInfo worldInfo = null;
                    foreach (WorldInfo worldInfo3 in WorldsManager.m_worldInfos)
                    {
                        if (text32 == worldInfo3.WorldSettings.Name)
                        {
                            worldInfo = worldInfo3;
                            GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
                            GameManager.DisposeProject();
                            ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
                            return SubmitResult.Success;
                        }
                    }

                    string text33 = Storage.CombinePaths(DataHandle.GetCommandPath(), text32);
                    if (!Storage.DirectoryExists(text33))
                    {
                        ShowSubmitTips("找不到文件目录" + text33);
                        return SubmitResult.Fail;
                    }

                    worldInfo = WorldsManager.GetWorldInfo(text33);
                    GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
                    GameManager.DisposeProject();
                    ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
                }
                else if (commandData.Type == "create")
                {
                    string text34 = (string)commandData.GetValue("f");
                    string text35 = Storage.CombinePaths(DataHandle.GetCommandPath(), text34);
                    if (Storage.DirectoryExists(text35))
                    {
                        ShowSubmitTips("Command目录已存在该世界");
                        return SubmitResult.Fail;
                    }

                    Storage.CreateDirectory(text35);
                    WorldSettings worldSettings = GameManager.m_worldInfo.WorldSettings;
                    worldSettings.Seed = text34.Replace("/", "$");
                    int num24 = 0;
                    int num25 = 1;
                    string seed = worldSettings.Seed;
                    foreach (char c in seed)
                    {
                        num24 += c * num25;
                        num25 += 29;
                    }

                    ValuesDictionary valuesDictionary = new ValuesDictionary();
                    worldSettings.Save(valuesDictionary, liveModifiableParametersOnly: false);
                    valuesDictionary.SetValue("WorldDirectoryName", text35);
                    valuesDictionary.SetValue("WorldSeed", num24);
                    ValuesDictionary valuesDictionary2 = new ValuesDictionary();
                    valuesDictionary2.SetValue("Players", new ValuesDictionary());
                    DatabaseObject databaseObject = DatabaseManager.GameDatabase.Database.FindDatabaseObject("GameProject", DatabaseManager.GameDatabase.ProjectTemplateType, throwIfNotFound: true);
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
                    using (Stream stream3 = Storage.OpenFile(Storage.CombinePaths(text35, "Project.xml"), OpenFileMode.Create))
                    {
                        XmlUtils.SaveXmlToStream(xElement, stream3, null, throwOnError: true);
                    }

                    ShowSubmitTips("已在Command目录创建世界:" + text34);
                }
                else if (commandData.Type == "remove")
                {
                    string text36 = (string)commandData.GetValue("f");
                    string text37 = Storage.CombinePaths(DataHandle.GetCommandPath(), text36);
                    if (!Storage.DirectoryExists(text37))
                    {
                        ShowSubmitTips("找不到文件目录" + text37);
                        return SubmitResult.Fail;
                    }

                    WorldsManager.DeleteWorld(text37);
                    ShowSubmitTips("已删除Command目录中的世界:" + text36);
                }
                else if (commandData.Type == "unzip")
                {
                    string text38 = (string)commandData.GetValue("f");
                    string path6 = Storage.CombinePaths(GameManager.m_worldInfo.DirectoryName, text38);
                    if (!Storage.FileExists(path6))
                    {
                        ShowSubmitTips("当前存档中找不到子存档:" + text38);
                        return SubmitResult.Fail;
                    }

                    Stream stream4 = Storage.OpenFile(path6, OpenFileMode.CreateOrOpen);
                    string text39 = text38.Replace(Storage.GetExtension(text38), "");
                    string text40 = Storage.CombinePaths(DataHandle.GetCommandPath(), text39);
                    if (Storage.DirectoryExists(text40))
                    {
                        ShowSubmitTips("Command目录已经存在世界:" + text39);
                        return SubmitResult.Fail;
                    }

                    Storage.CreateDirectory(text40);
                    WorldsManager.UnpackWorld(text40, stream4, importEmbeddedExternalContent: true);
                    stream4.Dispose();
                    ShowSubmitTips("已成功在Command目录中创建世界:" + text39);
                }
                else if (commandData.Type == "delcurrent")
                {
                    string directoryName = GameManager.m_worldInfo.DirectoryName;
                    GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
                    GameManager.DisposeProject();
                    WorldsManager.DeleteWorld(directoryName);
                    ScreensManager.SwitchScreen("MainMenu");
                }
                else if (commandData.Type == "decipher")
                {
                    string text41 = (string)commandData.GetValue("f");
                    string path7 = Storage.CombinePaths(GameManager.m_worldInfo.DirectoryName, text41);
                    if (!Storage.FileExists(path7))
                    {
                        ShowSubmitTips("当前存档中找不到:" + text41);
                        return SubmitResult.Fail;
                    }

                    Submit("world", new CommandData(commandData.Position, commandData.Line)
                    {
                        Type = "unzip",
                        Data = { ["f"] = text41 }
                    }, Judge: false);
                }

                return SubmitResult.Success;
            });
            AddFunction("texture", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    base.Project.FindSubsystem<SubsystemBlocksTexture>().BlocksTexture = BlocksTexturesManager.DefaultBlocksTexture;
                    UpdateAllChunks(0f, TerrainChunkState.InvalidLight);
                }
                else if (commandData.Type == "block")
                {
                    string f3 = (string)commandData.GetValue("f");
                    Stream commandFileStream9 = GetCommandFileStream(f3, OpenFileMode.ReadWrite);
                    if (commandFileStream9 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    if (!Png.IsPngStream(commandFileStream9))
                    {
                        ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                    }

                    Texture2D texture2D4 = Texture2D.Load(commandFileStream9);
                    if (!MathUtils.IsPowerOf2(texture2D4.Width) || !MathUtils.IsPowerOf2(texture2D4.Height))
                    {
                        ShowSubmitTips("材质图长和宽需为2的指数倍");
                        return SubmitResult.Fail;
                    }

                    base.Project.FindSubsystem<SubsystemBlocksTexture>().BlocksTexture = texture2D4;
                    UpdateAllChunks(0f, TerrainChunkState.InvalidLight);
                    commandFileStream9.Dispose();
                }
                else if (commandData.Type == "cmdcreature")
                {
                    string text26 = (string)commandData.GetValue("obj");
                    string text27 = (string)commandData.GetValue("f");
                    Stream commandFileStream10 = GetCommandFileStream(text27, OpenFileMode.ReadWrite);
                    if (commandFileStream10 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    if (!Png.IsPngStream(commandFileStream10))
                    {
                        ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                    }

                    Texture2D texture3 = Texture2D.Load(commandFileStream10);
                    ErgodicBody(text26, delegate (ComponentBody body)
                    {
                        ComponentModel componentModel6 = body.Entity.FindComponent<ComponentModel>();
                        if (componentModel6 != null)
                        {
                            componentModel6.TextureOverride = texture3;
                        }

                        return false;
                    });
                    commandFileStream10.Dispose();
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

                    ErgodicBody(text28, delegate (ComponentBody body)
                    {
                        ComponentModel componentModel5 = body.Entity.FindComponent<ComponentModel>();
                        if (componentModel5 != null)
                        {
                            componentModel5.TextureOverride = texture2;
                        }

                        return false;
                    });
                    CreatureTextures[text28] = text30;
                }
                else if (commandData.Type == "resetcreature")
                {
                    string text31 = (string)commandData.GetValue("obj");
                    Texture2D texture = null;
                    try
                    {
                        texture = ContentManager.Get<Texture2D>(EntityInfoManager.GetEntityInfo(text31).Texture);
                    }
                    catch
                    {
                        ShowSubmitTips(text31 + "材质恢复失败，发生未知错误");
                        return SubmitResult.Fail;
                    }

                    ErgodicBody(text31, delegate (ComponentBody body)
                    {
                        ComponentModel componentModel4 = body.Entity.FindComponent<ComponentModel>();
                        if (componentModel4 != null)
                        {
                            componentModel4.TextureOverride = texture;
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
            AddFunction("model", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text21 = (string)commandData.GetValue("obj");
                    bool flag13 = (bool)commandData.GetValue("con");
                    string text22 = (string)commandData.GetValue("f");
                    Stream commandFileStream8 = GetCommandFileStream(text22, OpenFileMode.ReadWrite);
                    if (commandFileStream8 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    Model model2 = Model.Load(commandFileStream8, keepSourceVertexDataInTags: true);
                    commandFileStream8.Dispose();
                    string modelType = EntityInfoManager.GetModelType(model2);
                    string modelType2 = EntityInfoManager.GetModelType(text21);
                    if (modelType != modelType2 && !flag13 && text21 != "boat")
                    {
                        string modelTypeDisplayName = EntityInfoManager.GetModelTypeDisplayName(modelType);
                        string modelTypeDisplayName2 = EntityInfoManager.GetModelTypeDisplayName(modelType2);
                        ShowSubmitTips($"导入模型为{modelTypeDisplayName},当前生物为{modelTypeDisplayName2},\n模型不匹配,请选择其他生物对象");
                        return SubmitResult.Fail;
                    }

                    ErgodicBody(text21, delegate (ComponentBody body)
                    {
                        ComponentModel componentModel3 = body.Entity.FindComponent<ComponentModel>();
                        if (componentModel3 != null)
                        {
                            try
                            {
                                componentModel3.Model = model2;
                            }
                            catch
                            {
                            }
                        }

                        return false;
                    });
                    CreatureModels[text21] = "$" + text22;
                }
                else if (commandData.Type == "pakmodel")
                {
                    string text23 = (string)commandData.GetValue("obj");
                    bool flag14 = (bool)commandData.GetValue("con");
                    string text24 = (string)commandData.GetValue("opt");
                    string text25 = Storage.CombinePaths("Models", text24);
                    Model model = ContentManager.Get<Model>(text25);
                    string modelType3 = EntityInfoManager.GetModelType(model);
                    string modelType4 = EntityInfoManager.GetModelType(text23);
                    if (modelType3 != modelType4 && !flag14 && text23 != "boat")
                    {
                        string modelTypeDisplayName3 = EntityInfoManager.GetModelTypeDisplayName(modelType3);
                        string modelTypeDisplayName4 = EntityInfoManager.GetModelTypeDisplayName(modelType4);
                        ShowSubmitTips($"导入模型为{modelTypeDisplayName3},当前生物为{modelTypeDisplayName4},\n模型不匹配,请选择其他生物对象");
                        return SubmitResult.Fail;
                    }

                    ErgodicBody(text23, delegate (ComponentBody body)
                    {
                        ComponentModel componentModel2 = body.Entity.FindComponent<ComponentModel>();
                        if (componentModel2 != null)
                        {
                            try
                            {
                                componentModel2.Model = model;
                            }
                            catch
                            {
                            }
                        }

                        return false;
                    });
                    CreatureModels[text23] = text25;
                }
                else if (commandData.Type == "resetall")
                {
                    CreatureModels.Clear();
                    ErgodicBody("all", delegate (ComponentBody body)
                    {
                        string name = body.Entity.ValuesDictionary.DatabaseObject.Name.ToLower();
                        EntityInfo entityInfo = EntityInfoManager.GetEntityInfo(name);
                        if (entityInfo != null)
                        {
                            ComponentModel componentModel = body.Entity.FindComponent<ComponentModel>();
                            if (componentModel != null)
                            {
                                componentModel.Model = ContentManager.Get<Model>(entityInfo.Model);
                            }
                        }

                        return false;
                    });
                }

                return SubmitResult.Success;
            });
            AddFunction("image", delegate (CommandData commandData)
            {
                WithdrawBlockManager wbManager2 = null;
                if (WithdrawBlockManager.WithdrawMode)
                {
                    wbManager2 = new WithdrawBlockManager();
                }

                Point3 onePoint5 = GetOnePoint("pos", commandData);
                bool flag9 = (bool)commandData.GetValue("con");
                string f2 = (string)commandData.GetValue("f");
                Stream commandFileStream7 = GetCommandFileStream(f2, OpenFileMode.ReadWrite);
                if (commandFileStream7 == null)
                {
                    return SubmitResult.Fail;
                }

                if (!Png.IsPngStream(commandFileStream7))
                {
                    ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                }

                Image image = Image.Load(commandFileStream7);
                commandFileStream7.Dispose();
                bool flag10 = commandData.Type == "default";
                bool flag11 = commandData.Type == "tile";
                bool flag12 = commandData.Type == "rotate";
                for (int m = 0; m < image.Height; m++)
                {
                    for (int n = 0; n < image.Width; n++)
                    {
                        Color pixel = image.GetPixel(n, m);
                        Point3 point5 = Point3.Zero;
                        int value6 = 0;
                        if (flag10)
                        {
                            point5 = new Point3(onePoint5.X - n, onePoint5.Y - m, onePoint5.Z);
                        }
                        else if (flag11)
                        {
                            point5 = new Point3(onePoint5.X - n, onePoint5.Y, onePoint5.Z - m);
                        }
                        else if (flag12)
                        {
                            point5 = new Point3(onePoint5.X, onePoint5.Y - m, onePoint5.Z - n);
                        }

                        if (point5.Y > 0 && point5.Y < 255)
                        {
                            if (pixel.A >= 20)
                            {
                                if (flag9)
                                {
                                    value6 = Mlfk.ClayBlock.SetCommandColor(72, pixel);
                                }
                                else if (!ColorIndexCaches.TryGetValue(pixel, out value6))
                                {
                                    value6 = DataHandle.GetColorIndex(pixel) * 32768 + 16456;
                                    if (ColorIndexCaches.Count < 1000)
                                    {
                                        ColorIndexCaches[pixel] = value6;
                                    }
                                }
                            }

                            ChangeBlockValue(wbManager2, point5.X, point5.Y, point5.Z, value6);
                        }
                    }
                }

                Point3 minPoint = onePoint5;
                if (flag10)
                {
                    minPoint = onePoint5 - new Point3(image.Width, image.Height, 0);
                }
                else if (flag11)
                {
                    minPoint = onePoint5 - new Point3(image.Width, 0, image.Height);
                }
                else if (flag12)
                {
                    minPoint = onePoint5 - new Point3(0, image.Height, image.Width);
                }

                PlaceReprocess(wbManager2, commandData, updateChunk: true, minPoint, onePoint5);
                return SubmitResult.Success;
            });
            AddFunction("pattern", delegate (CommandData commandData)
            {
                if (commandData.Type == "default" || commandData.Type == "online")
                {
                    Point3 pos2 = GetOnePoint("pos", commandData);
                    Color color3 = (Color)commandData.GetValue("color");
                    Vector3 vector3 = (Vector3)commandData.GetValue("vec3");
                    int num20 = (int)commandData.GetValue("v");
                    string text14 = (string)commandData.GetValue("opt");
                    bool flag3 = commandData.Type != "default";
                    bool con = flag3 && (bool)commandData.GetValue("con");
                    string text15 = (flag3 ? string.Empty : ((string)commandData.GetValue("f")));
                    string fix = ((!flag3) ? string.Empty : ((string)commandData.GetValue("fix")));
                    bool flag4 = text14 == "tile";
                    bool flag5 = text14 == "rotate";
                    Vector3 vector4 = vector3 / 10f;
                    Pattern pattern = new Pattern();
                    pattern.Point = pos2;
                    pattern.Color = color3;
                    pattern.Size = (float)num20 / 20.418f;
                    pattern.TexName = text15;
                    pattern.Position = vector4 + new Vector3(pos2) + new Vector3(0.5f, 0.5f, 0f);
                    pattern.Up = new Vector3(0f, -1f, 0f);
                    pattern.Right = new Vector3(-1f, 0f, 0f);
                    if (num20 <= 0 && PatternPoints.ContainsKey(pos2))
                    {
                        PatternPoints.Remove(pos2);
                    }

                    if (flag4)
                    {
                        pattern.Up = new Vector3(0f, 0f, -1f);
                        pattern.Position = vector4 + new Vector3(pos2) + new Vector3(0.5f, 1f, 0.5f);
                    }
                    else if (flag5)
                    {
                        pattern.Right = new Vector3(0f, 0f, -1f);
                        pattern.Position = vector4 + new Vector3(pos2) + new Vector3(1f, 0.5f, 0.5f);
                    }

                    if (!flag3)
                    {
                        Stream commandFileStream5 = GetCommandFileStream(text15, OpenFileMode.ReadWrite);
                        if (commandFileStream5 == null)
                        {
                            return SubmitResult.Fail;
                        }

                        if (!Png.IsPngStream(commandFileStream5))
                        {
                            ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                        }

                        Texture2D texture2D = Texture2D.Load(commandFileStream5);
                        pattern.LWratio = (float)texture2D.Height / (float)texture2D.Width;
                        pattern.Texture = texture2D;
                        PatternPoints[pos2] = pattern;
                        commandFileStream5.Dispose();
                    }
                    else
                    {
                        ShowSubmitTips("图片正在生成,请保证网络良好");
                        Task.Run(delegate
                        {
                            CancellableProgress progress = new CancellableProgress();
                            WebManager.Get(fix, null, null, progress, delegate (byte[] result)
                            {
                                Stream stream = new MemoryStream(result);
                                if (stream != null)
                                {
                                    StreamReader streamReader2 = new StreamReader(stream);
                                    string pic = GetPictureURL(streamReader2.ReadToEnd());
                                    if (!string.IsNullOrEmpty(pic))
                                    {
                                        WebManager.Get(pic, null, null, progress, delegate (byte[] result2)
                                        {
                                            Stream stream2 = new MemoryStream(result2);
                                            if (stream2 != null)
                                            {
                                                if (con)
                                                {
                                                    string systemPath4 = Storage.GetSystemPath(DataHandle.GetCommandPath());
                                                    pattern.TexName = pic.Substring(pic.LastIndexOf('/') + 1);
                                                    FileStream fileStream5 = new FileStream(Storage.CombinePaths(systemPath4, pattern.TexName), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                                    fileStream5.Write(result2, 0, result2.Length);
                                                    fileStream5.Flush();
                                                    fileStream5.Dispose();
                                                }

                                                Texture2D texture2D3 = Texture2D.Load(stream2);
                                                pattern.LWratio = (float)texture2D3.Height / (float)texture2D3.Width;
                                                pattern.Texture = texture2D3;
                                                PatternPoints[pos2] = pattern;
                                                stream2.Dispose();
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
                    string text16 = (string)commandData.GetValue("text");
                    Point3 onePoint4 = GetOnePoint("pos", commandData);
                    Color color4 = (Color)commandData.GetValue("color");
                    Vector3 vector5 = (Vector3)commandData.GetValue("vec3");
                    int num21 = (int)commandData.GetValue("v");
                    string text17 = (string)commandData.GetValue("opt");
                    PatternFont patternFont = new PatternFont
                    {
                        Point = onePoint4,
                        Position = new Vector3(onePoint4) + vector5 / 100f,
                        Text = text16,
                        Size = (float)num21 / 1000f,
                        Color = color4
                    };
                    switch (text17)
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

                    PatternFonts[onePoint4] = patternFont;
                }
                else if (commandData.Type == "remove")
                {
                    Point3[] twoPoint2 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea = new CubeArea(twoPoint2[0], twoPoint2[1]);
                    List<Point3> list2 = new List<Point3>();
                    List<Point3> list3 = new List<Point3>();
                    bool flag6 = false;
                    foreach (Point3 key6 in PatternPoints.Keys)
                    {
                        if (cubeArea.Exist(PatternPoints[key6].Position))
                        {
                            list2.Add(key6);
                            flag6 = true;
                        }
                    }

                    foreach (Point3 item12 in list2)
                    {
                        PatternPoints.Remove(item12);
                    }

                    foreach (Point3 key7 in PatternFonts.Keys)
                    {
                        if (cubeArea.Exist(PatternFonts[key7].Position))
                        {
                            list3.Add(key7);
                            flag6 = true;
                        }
                    }

                    foreach (Point3 item13 in list3)
                    {
                        PatternFonts.Remove(item13);
                    }

                    if (!flag6)
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
                    Vector2 vector6 = (Vector2)commandData.GetValue("vec2");
                    int layer = (int)commandData.GetValue("v");
                    string str2 = (string)commandData.GetValue("size");
                    bool flag7 = (bool)commandData.GetValue("con1");
                    bool flag8 = (bool)commandData.GetValue("con2");
                    string text18 = (string)commandData.GetValue("text");
                    string text19 = (string)commandData.GetValue("f");
                    Stream commandFileStream6 = GetCommandFileStream(text19, OpenFileMode.ReadWrite);
                    if (commandFileStream6 == null)
                    {
                        return SubmitResult.Fail;
                    }

                    if (!Png.IsPngStream(commandFileStream6))
                    {
                        ShowSubmitTips("建议将图片转换为png格式，以免颜色错乱");
                    }

                    Texture2D texture2D2 = Texture2D.Load(commandFileStream6);
                    commandFileStream6.Close();
                    if (ScreenPatterns.ContainsKey(text18))
                    {
                        ScreenPatterns.Remove(text18);
                    }

                    m_screenPatternsWidget.IsVisible = true;
                    Vector2 size = DataHandle.GetVector2Value(str2);
                    if (flag7)
                    {
                        size = new Vector2(size.X, size.X * (float)texture2D2.Height / (float)texture2D2.Width);
                    }

                    int num22 = (int)(m_componentPlayer.GameWidget.ActualSize.X / 2f) - (int)(size.X / 2f);
                    int num23 = (int)(m_componentPlayer.GameWidget.ActualSize.Y / 2f) - (int)(size.Y / 2f);
                    if (flag8)
                    {
                        BitmapButtonWidget widget = new BitmapButtonWidget
                        {
                            NormalSubtexture = new Subtexture(texture2D2, Vector2.Zero, Vector2.One),
                            ClickedSubtexture = new Subtexture(texture2D2, Vector2.Zero, Vector2.One),
                            Color = Color.White,
                            Size = size,
                            Margin = new Vector2(num22, num23) + vector6
                        };
                        ScreenPattern value3 = new ScreenPattern
                        {
                            Name = text18,
                            Texture = text19,
                            Layer = layer,
                            OutTime = 0f,
                            Widget = widget
                        };
                        ScreenPatterns.Add(text18, value3);
                    }
                    else
                    {
                        RectangleWidget widget2 = new RectangleWidget
                        {
                            Subtexture = new Subtexture(texture2D2, Vector2.Zero, Vector2.One),
                            FillColor = Color.White,
                            OutlineColor = new Color(0, 0, 0, 0),
                            Size = size,
                            Margin = new Vector2(num22, num23) + vector6
                        };
                        ScreenPattern value4 = new ScreenPattern
                        {
                            Name = text18,
                            Texture = text19,
                            Layer = layer,
                            OutTime = 0f,
                            Widget = widget2
                        };
                        ScreenPatterns.Add(text18, value4);
                    }

                    if (m_screenPatternsWidget.Children.Count > 0)
                    {
                        m_screenPatternsWidget.Children.Clear();
                    }

                    ScreenPattern[] array2 = ScreenPatterns.Values.ToArray();
                    for (int j = 0; j < array2.Length - 1; j++)
                    {
                        for (int k = 0; k < array2.Length - 1 - j; k++)
                        {
                            if (array2[k].Layer > array2[k + 1].Layer)
                            {
                                ScreenPattern screenPattern = array2[k + 1];
                                array2[k + 1] = array2[k];
                                array2[k] = screenPattern;
                            }
                        }
                    }

                    ScreenPattern[] array3 = array2;
                    foreach (ScreenPattern screenPattern2 in array3)
                    {
                        m_screenPatternsWidget.Children.Add(screenPattern2.Widget);
                    }
                }
                else if (commandData.Type == "screenremove")
                {
                    string text20 = (string)commandData.GetValue("text");
                    if (ScreenPatterns.TryGetValue(text20, out var value5))
                    {
                        m_screenPatternsWidget.IsVisible = true;
                        m_screenPatternsWidget.Children.Remove(value5.Widget);
                        ScreenPatterns.Remove(text20);
                    }
                    else
                    {
                        ShowSubmitTips($"屏幕中没有标识名为{text20}的贴图");
                    }
                }
                else if (commandData.Type == "screenclear")
                {
                    m_screenPatternsWidget.Children.Clear();
                    ScreenPatterns.Clear();
                }

                return SubmitResult.Success;
            });
            AddFunction("music", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text12 = (string)commandData.GetValue("f");
                    string text13 = Storage.GetExtension(text12).ToLower();
                    if (text13 != ".wav")
                    {
                        ShowSubmitTips("目前仅支持wav格式的音频");
                        return SubmitResult.Fail;
                    }

                    if (m_commandMusic.Sound == null || (m_commandMusic.Sound != null && m_commandMusic.Name != text12))
                    {
                        Stream commandFileStream4 = GetCommandFileStream(text12, OpenFileMode.ReadWrite);
                        if (commandFileStream4 == null)
                        {
                            return SubmitResult.Fail;
                        }

                        SoundBuffer soundBuffer2 = SoundBuffer.Load(commandFileStream4);
                        m_commandMusic = new CommandMusic(text12, new Sound(soundBuffer2));
                        m_commandMusic.Sound.Play();
                        ShowSubmitTips("开始播放歌曲：" + m_commandMusic.Name);
                        commandFileStream4.Dispose();
                    }
                    else if (m_commandMusic.Sound != null && m_commandMusic.Name == text12)
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
                    int num19 = (int)commandData.GetValue("v");
                    if (m_commandMusic.Sound == null)
                    {
                        ShowSubmitTips("当前后台无歌曲，请先播放歌曲");
                        return SubmitResult.Fail;
                    }

                    m_commandMusic.Sound.Volume = (float)num19 / 10f;
                }

                return SubmitResult.Success;
            });
            AddFunction("audio", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text9 = (string)commandData.GetValue("f");
                    int num10 = (int)commandData.GetValue("v");
                    int num11 = (int)commandData.GetValue("p");
                    string text10 = Storage.GetExtension(text9).ToLower();
                    if (text10 != ".wav")
                    {
                        ShowSubmitTips("目前仅支持wav格式的音频");
                        return SubmitResult.Fail;
                    }

                    if (num11 != 15 && num10 != 0)
                    {
                        Vector3 p = new Vector3(commandData.Position);
                        float pitch = CommandMusic.GetPitch(num11);
                        float num12 = (float)num10 / 15f;
                        Stream commandFileStream3 = GetCommandFileStream(text9, OpenFileMode.ReadWrite);
                        if (commandFileStream3 == null)
                        {
                            return SubmitResult.Fail;
                        }

                        SoundBuffer soundBuffer = SoundBuffer.Load(commandFileStream3);
                        float num13 = m_subsystemAudio.CalculateVolume(m_subsystemAudio.CalculateListenerDistance(p), 0.5f + 5f * num12);
                        new Sound(soundBuffer, num12 * num13, pitch, 0f, isLooped: false, disposeOnStop: true).Play();
                        commandFileStream3.Dispose();
                    }
                }
                else if (commandData.Type == "contentpak")
                {
                    string text11 = (string)commandData.GetValue("opt");
                    int num14 = (int)commandData.GetValue("v");
                    int num15 = (int)commandData.GetValue("p");
                    if (num15 != 15 && num14 != 0)
                    {
                        Vector3 position = new Vector3(commandData.Position);
                        float pitch2 = CommandMusic.GetPitch(num15);
                        float num16 = (float)num14 / 15f;
                        m_subsystemAudio.PlaySound("Audio/" + text11, num16, pitch2, position, 0.5f + 5f * num16, autoDelay: true);
                    }
                }
                else if (commandData.Type == "piano")
                {
                    int num17 = (int)commandData.GetValue("p");
                    int o2 = (int)commandData.GetValue("o");
                    int num18 = (int)commandData.GetValue("v");
                    if (num17 != 15 && num18 != 0)
                    {
                        Vector3 vector2 = new Vector3(commandData.Position) + new Vector3(0.5f);
                        float volume = (float)num18 / 15f;
                        float pitch3 = CommandMusic.GetRealPitch(num17, ref o2);
                        if (o2 > 7)
                        {
                            o2 = 7;
                            pitch3 = 1f;
                        }

                        float minDistance = 0.5f + (float)num18 / 3f;
                        m_subsystemAudio.PlaySound($"CommandPiano/PianoC{o2}", volume, pitch3, vector2, minDistance, autoDelay: true);
                        SoundParticleSystem soundParticleSystem = new SoundParticleSystem(m_subsystemTerrain, vector2 + new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1f, 0f));
                        if (soundParticleSystem.SubsystemParticles == null)
                        {
                            m_subsystemParticles.AddParticleSystem(soundParticleSystem);
                        }

                        Vector3 hsv = new Vector3(22.5f * (float)num17 + new Game.Random().Float(0f, 22f), 0.5f + (float)num18 / 30f, 1f);
                        soundParticleSystem.AddNote(new Color(Color.HsvToRgb(hsv)));
                    }
                }

                return SubmitResult.Success;
            });
            AddFunction("build", delegate (CommandData commandData)
            {
                WithdrawBlockManager wbManager = null;
                if (WithdrawBlockManager.WithdrawMode)
                {
                    wbManager = new WithdrawBlockManager();
                }

                if (commandData.Type == "default")
                {
                    Point3 pos = GetOnePoint("pos", commandData);
                    string f = (string)commandData.GetValue("f");
                    string opt = (string)commandData.GetValue("opt");
                    Task.Run(delegate
                    {
                        Stream commandFileStream2 = GetCommandFileStream(f, OpenFileMode.ReadWrite);
                        StreamReader streamReader = new StreamReader(commandFileStream2);
                        int num8 = 1;
                        string empty = string.Empty;
                        Point3 zero = Point3.Zero;
                        int num9 = 0;
                        try
                        {
                            while ((empty = streamReader.ReadLine()) != null)
                            {
                                string[] array = empty.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                if (array.Length == 4 || array.Length == 6)
                                {
                                    try
                                    {
                                        Point3 point4 = Point3.Zero;
                                        switch (opt)
                                        {
                                            case "x-y-z":
                                                point4 = new Point3(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
                                                break;
                                            case "x-z-y":
                                                point4 = new Point3(int.Parse(array[0]), int.Parse(array[2]), int.Parse(array[1]));
                                                break;
                                            case "y-x-z":
                                                point4 = new Point3(int.Parse(array[1]), int.Parse(array[0]), int.Parse(array[2]));
                                                break;
                                            case "y-z-x":
                                                point4 = new Point3(int.Parse(array[1]), int.Parse(array[2]), int.Parse(array[0]));
                                                break;
                                            case "z-x-y":
                                                point4 = new Point3(int.Parse(array[2]), int.Parse(array[0]), int.Parse(array[1]));
                                                break;
                                            case "z-y-x":
                                                point4 = new Point3(int.Parse(array[2]), int.Parse(array[1]), int.Parse(array[0]));
                                                break;
                                        }

                                        point4 += pos;
                                        if (point4.X > zero.X)
                                        {
                                            zero.X = point4.X;
                                        }

                                        if (point4.Y > zero.Y)
                                        {
                                            zero.Y = point4.Y;
                                        }

                                        if (point4.Z > zero.Z)
                                        {
                                            zero.Z = point4.Z;
                                        }

                                        num9 = ((array.Length != 4) ? Mlfk.ClayBlock.SetCommandColor(72, new Color(int.Parse(array[3]), int.Parse(array[4]), int.Parse(array[5]))) : int.Parse(array[3]));
                                        ChangeBlockValue(wbManager, point4.X, point4.Y, point4.Z, num9);
                                    }
                                    catch
                                    {
                                        ShowSubmitTips($"方块生成发生错误，错误发生在第{num8}行");
                                    }
                                }

                                num8++;
                            }
                        }
                        catch
                        {
                        }

                        PlaceReprocess(wbManager, commandData, updateChunk: true, pos, zero);
                        commandFileStream2.Dispose();
                    });
                }
                else if (commandData.Type == "copycache")
                {
                    Point3 onePoint3 = GetOnePoint("pos", commandData);
                    if (CopyBlockManager == null)
                    {
                        ShowSubmitTips("缓存区不存在复制的建筑，\n请先复制区域，复制指令为copyblock$copycache");
                        return SubmitResult.Fail;
                    }

                    CopyBlockManager.SubsystemCommandDef = this;
                    CopyBlockManager.WBManager = wbManager;
                    CopyBlockManager.CopyFromCache(onePoint3);
                    PlaceReprocess(wbManager, commandData, updateChunk: true, onePoint3, onePoint3 + CopyBlockManager.CubeArea.MaxPoint - CopyBlockManager.CubeArea.MinPoint);
                    ShowSubmitTips("已全部生成！\n如果复制工作已完成，可以重启游戏以清除后台方块缓存");
                }

                return SubmitResult.Success;
            });
            AddFunction("copyfile", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text7 = (string)commandData.GetValue("fix");
                    string path = Storage.CombinePaths(DataHandle.GetCommandPath(), text7);
                    if (!Storage.FileExists(path))
                    {
                        ShowSubmitTips("Command目录不存在文件:" + text7);
                        return SubmitResult.Fail;
                    }

                    string systemPath = Storage.GetSystemPath(path);
                    string path2 = Storage.CombinePaths(Storage.GetSystemPath(GameManager.m_worldInfo.DirectoryName), text7);
                    FileStream fileStream = new FileStream(systemPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    FileStream fileStream2 = new FileStream(path2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    fileStream.CopyTo(fileStream2);
                    fileStream.Dispose();
                    fileStream2.Dispose();
                    ShowSubmitTips("已将指定文件复制到存档工作目录");
                }
                else if (commandData.Type == "folder")
                {
                    string text8 = (string)commandData.GetValue("fix");
                    string path3 = Storage.CombinePaths(Storage.GetDirectoryName(DataHandle.GetCommandPath()), text8);
                    if (!Storage.DirectoryExists(path3))
                    {
                        ShowSubmitTips("Command目录不存在文件夹:" + text8);
                        return SubmitResult.Fail;
                    }

                    string systemPath2 = Storage.GetSystemPath(path3);
                    string systemPath3 = Storage.GetSystemPath(GameManager.m_worldInfo.DirectoryName);
                    foreach (string item14 in Storage.ListFileNames(path3))
                    {
                        string path4 = Storage.CombinePaths(systemPath2, item14);
                        string path5 = Storage.CombinePaths(systemPath3, item14);
                        FileStream fileStream3 = new FileStream(path4, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        FileStream fileStream4 = new FileStream(path5, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        fileStream3.CopyTo(fileStream4);
                        fileStream3.Dispose();
                        fileStream4.Dispose();
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
            AddFunction("website", delegate (CommandData commandData)
            {
                string url = (string)commandData.GetValue("text");
                WebBrowserManager.LaunchBrowser(url);
                return SubmitResult.Success;
            });
            AddFunction("note", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    m_componentPlayer.ComponentGui.ModalPanelWidget = new NotesWidget(m_componentPlayer, Notes);
                }
                else if (commandData.Type == "onlyread")
                {
                    string text6 = (string)commandData.GetValue("text");
                    string str = (string)commandData.GetValue("size");
                    Vector2 vector = (Vector2)commandData.GetValue("vec2");
                    int num3 = (int)commandData.GetValue("v1");
                    Color color2 = (Color)commandData.GetValue("color1");
                    int num4 = (int)commandData.GetValue("v2");
                    Color fillColor = (Color)commandData.GetValue("color2");
                    Color outlineColor = (Color)commandData.GetValue("color3");
                    int num5 = (int)commandData.GetValue("v3");
                    if (Notes.TryGetValue(text6, out var value2))
                    {
                        m_screenLabelCanvasWidget.IsVisible = true;
                        m_screenLabelCloseTime = num3;
                        Vector2 vector2Value = DataHandle.GetVector2Value(str);
                        CanvasWidget canvasWidget = (CanvasWidget)m_screenLabelCanvasWidget;
                        canvasWidget.Size = vector2Value;
                        int num6 = (int)(m_componentPlayer.GameWidget.ActualSize.X / 2f) - (int)(vector2Value.X / 2f);
                        int num7 = (int)(m_componentPlayer.GameWidget.ActualSize.Y / 2f) - (int)(vector2Value.Y / 2f);
                        m_componentPlayer.GameWidget.SetWidgetPosition(m_screenLabelCanvasWidget, new Vector2(num6, num7) + vector);
                        LabelWidget labelWidget = m_screenLabelCanvasWidget.Children.Find<LabelWidget>("Content");
                        labelWidget.Text = value2.Replace("[n]", "\n").Replace("\t", "");
                        labelWidget.Color = color2;
                        labelWidget.FontScale = (float)num4 / 50f;
                        RectangleWidget rectangleWidget = m_screenLabelCanvasWidget.Children.Find<RectangleWidget>("ScreenLabelRectangle");
                        rectangleWidget.FillColor = fillColor;
                        rectangleWidget.OutlineColor = outlineColor;
                        rectangleWidget.OutlineThickness = (float)num5 / 10f;
                    }
                    else
                    {
                        ShowSubmitTips($"没有标题名为{text6}的笔记");
                    }
                }
                else if (commandData.Type == "close")
                {
                    m_screenLabelCanvasWidget.IsVisible = false;
                }

                return SubmitResult.Success;
            });
            AddFunction("colorblock", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    int value = (int)commandData.GetValue("id");
                    Color color = (Color)commandData.GetValue("color");
                    bool flag2 = (bool)commandData.GetValue("con");
                    value = Terrain.ExtractContents(value);
                    int num2 = 0;
                    if (value != 72 && value != 12 && value != 13 && value != 14 && value != 225 && value != 256 && value != 15)
                    {
                        ShowSubmitTips("该功能目前只支持黏土、玻璃以及树叶");
                        return SubmitResult.Fail;
                    }

                    switch (value)
                    {
                        case 72:
                            num2 = Mlfk.ClayBlock.SetCommandColor(value, color);
                            break;
                        case 12:
                            num2 = Mlfk.OakLeavesBlock.SetCommandColor(value, color);
                            break;
                        case 13:
                            num2 = Mlfk.BirchLeavesBlock.SetCommandColor(value, color);
                            break;
                        case 14:
                            num2 = Mlfk.SpruceLeavesBlock.SetCommandColor(value, color);
                            break;
                        case 225:
                            num2 = Mlfk.TallSpruceLeavesBlock.SetCommandColor(value, color);
                            break;
                        case 256:
                            num2 = Mlfk.MimosaLeavesBlock.SetCommandColor(value, color);
                            break;
                        case 15:
                            num2 = Mlfk.GlassBlock.SetCommandColor(value, color);
                            break;
                    }

                    if (value == 15)
                    {
                        num2 = Mlfk.GlassBlock.SetCommandColorAlpha(num2, (int)((float)(int)color.A / 16f));
                    }

                    if (flag2)
                    {
                        int activeSlotIndex = m_componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex;
                        int slotsCount = m_componentPlayer.ComponentMiner.Inventory.SlotsCount;
                        m_componentPlayer.ComponentMiner.Inventory.RemoveSlotItems(activeSlotIndex, slotsCount);
                        m_componentPlayer.ComponentMiner.Inventory.AddSlotItems(activeSlotIndex, num2, slotsCount);
                    }

                    ShowSubmitTips($"方块id为{value},颜色为{color.ToString()}的方块值为{num2},\n已将该方块值复制到剪切板以及命令辅助棒记录方块值");
                    ClipboardManager.ClipboardString = num2.ToString();
                    base.Project.FindSubsystem<SubsystemCmdRodBlockBehavior>().m_recordBlockValue = num2;
                }
                else if (commandData.Type == "display")
                {
                    bool displayColorBlock = (bool)commandData.GetValue("con");
                    DisplayColorBlock = displayColorBlock;
                    if (DisplayColorBlock)
                    {
                        BlocksManager.AddCategory(Mlfk.ClayBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.BirchLeavesBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.OakLeavesBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.SpruceLeavesBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.TallSpruceLeavesBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.MimosaLeavesBlock.CommandCategory);
                        BlocksManager.AddCategory(Mlfk.GlassBlock.CommandCategory);
                    }
                    else
                    {
                        if (BlocksManager.m_categories.Contains(Mlfk.ClayBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.ClayBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.BirchLeavesBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.BirchLeavesBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.OakLeavesBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.OakLeavesBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.SpruceLeavesBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.SpruceLeavesBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.TallSpruceLeavesBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.TallSpruceLeavesBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.MimosaLeavesBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.MimosaLeavesBlock.CommandCategory);
                        }

                        if (BlocksManager.m_categories.Contains(Mlfk.GlassBlock.CommandCategory))
                        {
                            BlocksManager.m_categories.Remove(Mlfk.GlassBlock.CommandCategory);
                        }
                    }

                    ComponentCreativeInventory componentCreativeInventory = m_componentPlayer.Entity.FindComponent<ComponentCreativeInventory>();
                    List<Order> list = new List<Order>();
                    Block[] blocks = BlocksManager.Blocks;
                    foreach (Block block in blocks)
                    {
                        foreach (int creativeValue in block.GetCreativeValues())
                        {
                            list.Add(new Order(block, block.GetDisplayOrder(creativeValue), creativeValue));
                        }
                    }

                    IOrderedEnumerable<Order> orderedEnumerable = list.OrderBy((Order o) => o.order);
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
            AddFunction("withdraw", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    int num = (int)commandData.GetValue("v");
                    bool withdrawMode = (bool)commandData.GetValue("con");
                    WithdrawBlockManager.WithdrawMode = withdrawMode;
                    if (num < WithdrawBlockManager.MaxSteps)
                    {
                        WithdrawBlockManager.Clear();
                    }

                    WithdrawBlockManager.MaxSteps = num;
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
            AddFunction("chunkwork", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint = GetOnePoint("pos", commandData);
                    Point2 point = Terrain.ToChunk(onePoint.X, onePoint.Z);
                    bool flag = AddChunks007(point.X, point.Y);
                    ShowSubmitTips($"点({onePoint.ToString()})对应的区块" + (flag ? "已安排到007岗位" : "已在拼命工作中，无需重复安排"));
                }
                else if (commandData.Type == "areawork")
                {
                    Point3[] twoPoint = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cube = new CubeArea(twoPoint[0], twoPoint[1]);
                    cube.Ergodic(delegate
                    {
                        Point2 point3 = Terrain.ToChunk(cube.Current.X, cube.Current.Z);
                        AddChunks007(point3.X, point3.Y);
                        return false;
                    });
                    ShowSubmitTips($"区域[({twoPoint[0]})-({twoPoint[1]})]对应的所有区块已全部安排到007岗位");
                }
                else if (commandData.Type == "reset")
                {
                    Point3 onePoint2 = GetOnePoint("pos", commandData);
                    Point2 point2 = Terrain.ToChunk(onePoint2.X, onePoint2.Z);
                    RemoveChunks007(point2.X, point2.Y);
                    ShowSubmitTips($"点({onePoint2.ToString()})对应的区块给予享受8小时工作制福利");
                }
                else if (commandData.Type == "resetall")
                {
                    m_terrainChunks007.Clear();
                    ShowSubmitTips("所有007区块已正常放假");
                }
                else if (commandData.Type == "show")
                {
                    string text5 = ((m_terrainChunks007.Count == 0) ? "当前没有007区块" : "以下点对应区块正在拼命工作：\n");
                    foreach (Point2 item16 in m_terrainChunks007)
                    {
                        text5 = text5 + "(" + new Point3(item16.X * 16 + 7, 64, item16.Y * 16 + 7).ToString() + "); ";
                    }

                    ShowSubmitTips(text5);
                }

                return SubmitResult.Success;
            });
            AddFunction("convertfile", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    string text = (string)commandData.GetValue("f");
                    Stream commandFileStream = GetCommandFileStream(text, OpenFileMode.ReadWrite);
                    if (commandFileStream == null)
                    {
                        return SubmitResult.Fail;
                    }

                    try
                    {
                        string text2 = Storage.CombinePaths(DataHandle.GetCommandPath(), text.Replace(".scworld", ""));
                        if (!Storage.DirectoryExists(text2))
                        {
                            Storage.CreateDirectory(text2);
                        }

                        WorldsManager.UnpackWorld(text2, commandFileStream, importEmbeddedExternalContent: true);
                        ConvertWorld(Storage.CombinePaths(text2, "Project.xml"), removeAll: true);
                        using (Stream targetStream = Storage.OpenFile(text2 + "(转换).scworld", OpenFileMode.Create))
                        {
                            WorldsManager.PackWorld(text2, targetStream, null, embedExternalContent: true);
                        }

                        if (Storage.DirectoryExists(text2))
                        {
                            DataHandle.DeleteAllDirectoryAndFile(text2);
                        }

                        ShowSubmitTips("存档转换完成！新存档路径名：" + text2 + "(转换).scworld");
                    }
                    catch (Exception ex)
                    {
                        ShowSubmitTips("存档转换失败！错误信息：" + ex.Message);
                    }
                }
                else if (commandData.Type == "decrypt")
                {
                    string text3 = (string)commandData.GetValue("text");
                    string text4 = (string)commandData.GetValue("f");
                    ShowSubmitTips("已停用");
                }

                return SubmitResult.Success;
            });
            AddFunction("moremode", delegate (CommandData commandData)
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
            AddFunction("exit", delegate (CommandData commandData)
            {
                GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
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
            AddCondition("blockexist", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint10 = GetOnePoint("pos", commandData);
                    int num44 = (int)commandData.GetValue("id");
                    int limitValue2 = GetLimitValue(onePoint10.X, onePoint10.Y, onePoint10.Z);
                    if (limitValue2 == num44)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint10 = GetTwoPoint("pos1", "pos2", commandData);
                    int id2 = (int)commandData.GetValue("id");
                    CubeArea cube4 = new CubeArea(twoPoint10[0], twoPoint10[1]);
                    if (cube4.Ergodic(delegate
                    {
                        int limitValue4 = GetLimitValue(cube4.Current.X, cube4.Current.Y, cube4.Current.Z);
                        return limitValue4 == id2;
                    }))
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "global")
                {
                    Point3[] twoPoint11 = GetTwoPoint("pos1", "pos2", commandData);
                    int id = (int)commandData.GetValue("id");
                    CubeArea cube3 = new CubeArea(twoPoint11[0], twoPoint11[1]);
                    if (!cube3.Ergodic(delegate
                    {
                        int limitValue3 = GetLimitValue(cube3.Current.X, cube3.Current.Y, cube3.Current.Z);
                        return limitValue3 != id;
                    }))
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("blockchange", delegate (CommandData commandData)
            {
                Point3 onePoint9 = GetOnePoint("pos", commandData);
                int limitValue = GetLimitValue(onePoint9.X, onePoint9.Y, onePoint9.Z);
                if (commandData.DIYPara.TryGetValue("lastLimitValue", out var value9))
                {
                    if (limitValue != (int)value9)
                    {
                        commandData.DIYPara["lastLimitValue"] = limitValue;
                        return SubmitResult.Success;
                    }
                }
                else
                {
                    commandData.DIYPara["lastLimitValue"] = limitValue;
                }

                return SubmitResult.Fail;
            });
            AddCondition("blocklight", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint8 = GetOnePoint("pos", commandData);
                    Vector2 vector13 = (Vector2)commandData.GetValue("vec2");
                    int cellLight = m_subsystemTerrain.Terrain.GetCellLight(onePoint8.X, onePoint8.Y, onePoint8.Z);
                    if ((float)cellLight >= vector13.X && (float)cellLight <= vector13.Y)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "area")
                {
                    Point3[] twoPoint9 = GetTwoPoint("pos1", "pos2", commandData);
                    Vector2 vector14 = (Vector2)commandData.GetValue("vec2");
                    int maxLight = -1;
                    CubeArea cube2 = new CubeArea(twoPoint9[0], twoPoint9[1]);
                    cube2.Ergodic(delegate
                    {
                        int cellLight2 = m_subsystemTerrain.Terrain.GetCellLight(cube2.Current.X, cube2.Current.Y, cube2.Current.Z);
                        if (cellLight2 > maxLight)
                        {
                            maxLight = cellLight2;
                        }

                        return false;
                    });
                    if ((float)maxLight >= vector14.X && (float)maxLight <= vector14.Y)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("entityexist", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3[] twoPoint6 = GetTwoPoint("pos1", "pos2", commandData);
                    string target = (string)commandData.GetValue("obj");
                    CubeArea cube = new CubeArea(twoPoint6[0], twoPoint6[1]);
                    if (ErgodicBody(target, (ComponentBody body) => cube.Exist(body.Position)))
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "limit")
                {
                    Point3[] twoPoint7 = GetTwoPoint("pos1", "pos2", commandData);
                    string text5 = (string)commandData.GetValue("obj");
                    Vector2 vector9 = (Vector2)commandData.GetValue("vec2");
                    CubeArea cubeArea6 = new CubeArea(twoPoint7[0], twoPoint7[1]);
                    int num42 = 0;
                    Vector2 vector10 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
                    DynamicArray<ComponentBody> dynamicArray3 = m_subsystemBodies.Bodies.ToDynamicArray();
                    foreach (ComponentBody item in dynamicArray3)
                    {
                        if (text5 == item.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() && cubeArea6.Exist(item.Position))
                        {
                            num42++;
                        }
                    }

                    if ((float)num42 >= vector9.X && (float)num42 <= vector9.Y)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "count")
                {
                    Point3[] twoPoint8 = GetTwoPoint("pos1", "pos2", commandData);
                    string text6 = (string)commandData.GetValue("obj");
                    Vector2 vector11 = (Vector2)commandData.GetValue("vec2");
                    CubeArea cubeArea7 = new CubeArea(twoPoint8[0], twoPoint8[1]);
                    int num43 = 0;
                    Vector2 vector12 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
                    DynamicArray<ComponentBody> dynamicArray4 = m_subsystemBodies.Bodies.ToDynamicArray();
                    foreach (ComponentBody item2 in dynamicArray4)
                    {
                        if (cubeArea7.Exist(item2.Position))
                        {
                            num43++;
                        }
                    }

                    if ((float)num43 >= vector11.X && (float)num43 <= vector11.Y)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("creaturedie", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3[] twoPoint4 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea4 = new CubeArea(twoPoint4[0], twoPoint4[1]);
                    Vector2 vector7 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
                    DynamicArray<ComponentBody> dynamicArray = m_subsystemBodies.Bodies.ToDynamicArray();
                    foreach (ComponentBody item3 in dynamicArray)
                    {
                        if (cubeArea4.Exist(item3.Position))
                        {
                            ComponentCreature componentCreature2 = item3.Entity.FindComponent<ComponentCreature>();
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
                else if (commandData.Type == "limit")
                {
                    Point3[] twoPoint5 = GetTwoPoint("pos1", "pos2", commandData);
                    string text4 = (string)commandData.GetValue("obj");
                    CubeArea cubeArea5 = new CubeArea(twoPoint5[0], twoPoint5[1]);
                    Vector2 vector8 = new Vector2(m_componentPlayer.ComponentBody.Position.X, m_componentPlayer.ComponentBody.Position.Z);
                    DynamicArray<ComponentBody> dynamicArray2 = m_subsystemBodies.Bodies.ToDynamicArray();
                    foreach (ComponentBody item4 in dynamicArray2)
                    {
                        if (text4 == item4.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() && cubeArea5.Exist(item4.Position))
                        {
                            ComponentCreature componentCreature = item4.Entity.FindComponent<ComponentCreature>();
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

                return SubmitResult.Fail;
            });
            AddCondition("dropexist", delegate (CommandData commandData)
            {
                Point3[] twoPoint3 = GetTwoPoint("pos1", "pos2", commandData);
                int num41 = (int)commandData.GetValue("id");
                CubeArea cubeArea3 = new CubeArea(twoPoint3[0], twoPoint3[1]);
                foreach (Pickable pickable in m_subsystemPickables.Pickables)
                {
                    if (pickable.Value == num41 && cubeArea3.Exist(pickable.Position))
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("itemexist", delegate (CommandData commandData)
            {
                if (commandData.Type == "default" || commandData.Type == "onlyid")
                {
                    int num18 = (int)commandData.GetValue("id");
                    bool flag5 = commandData.Type == "onlyid";
                    ComponentInventoryBase componentInventoryBase = m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>(throwOnError: true);
                    ComponentCraftingTable componentCraftingTable = m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true);
                    foreach (ComponentInventoryBase.Slot slot in componentInventoryBase.m_slots)
                    {
                        int num19 = (flag5 ? Terrain.ExtractContents(slot.Value) : slot.Value);
                        if (num19 == num18 && slot.Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }

                    foreach (ComponentInventoryBase.Slot slot2 in componentCraftingTable.m_slots)
                    {
                        int num20 = (flag5 ? Terrain.ExtractContents(slot2.Value) : slot2.Value);
                        if (num20 == num18 && slot2.Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type.Contains("main") || commandData.Type.Contains("inventory") || commandData.Type.Contains("craft"))
                {
                    int num21 = (int)commandData.GetValue("id");
                    int num22 = (int)commandData.GetValue("s");
                    int num23 = -1;
                    int num24 = -1;
                    List<ComponentInventoryBase.Slot> list = null;
                    switch (commandData.Type)
                    {
                        case "main":
                            num23 = num22 - 1;
                            break;
                        case "limmain":
                            num23 = num22 - 1;
                            num24 = (int)commandData.GetValue("v");
                            break;
                        case "inventory":
                            num23 = num22 + 9;
                            break;
                        case "liminventory":
                            num23 = num22 + 9;
                            num24 = (int)commandData.GetValue("v");
                            break;
                        case "craft":
                            num23 = num22 - 1;
                            break;
                        case "limcraft":
                            num23 = num22 - 1;
                            num24 = (int)commandData.GetValue("v");
                            break;
                        default:
                            return SubmitResult.Fail;
                    }

                    list = (commandData.Type.Contains("craft") ? m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true).m_slots : m_componentPlayer.Entity.FindComponent<ComponentInventoryBase>(throwOnError: true).m_slots);
                    if (list[num23].Value == num21)
                    {
                        if (num24 == -1)
                        {
                            if (list[num23].Count >= 1)
                            {
                                return SubmitResult.Success;
                            }
                        }
                        else if (list[num23].Count == num24)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type.Contains("chest"))
                {
                    int num25 = (int)commandData.GetValue("id");
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
                            if (slot3.Value == num25 && slot3.Count >= 1)
                            {
                                return SubmitResult.Success;
                            }
                        }
                    }
                    else if (commandData.Type == "slotchest")
                    {
                        int num26 = (int)commandData.GetValue("s");
                        int index = num26 - 1;
                        if (componentChest.m_slots[index].Value == num25 && componentChest.m_slots[index].Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }
                    else if (commandData.Type == "limchest")
                    {
                        int num27 = (int)commandData.GetValue("s");
                        int num28 = (int)commandData.GetValue("v");
                        int index2 = num27 - 1;
                        if (componentChest.m_slots[index2].Value == num25 && componentChest.m_slots[index2].Count == num28)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type.Contains("table"))
                {
                    int num29 = (int)commandData.GetValue("id");
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
                            if (slot4.Value == num29 && slot4.Count >= 1)
                            {
                                return SubmitResult.Success;
                            }
                        }
                    }
                    else if (commandData.Type == "slottable")
                    {
                        int num30 = (int)commandData.GetValue("s");
                        int index3 = num30 - 1;
                        if (componentCraftingTable2.m_slots[index3].Value == num29 && componentCraftingTable2.m_slots[index3].Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }
                    else if (commandData.Type == "limtable")
                    {
                        int num31 = (int)commandData.GetValue("s");
                        int num32 = (int)commandData.GetValue("v");
                        int index4 = num31 - 1;
                        if (componentCraftingTable2.m_slots[index4].Value == num29 && componentCraftingTable2.m_slots[index4].Count == num32)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type.Contains("dispenser"))
                {
                    int num33 = (int)commandData.GetValue("id");
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
                            if (slot5.Value == num33 && slot5.Count >= 1)
                            {
                                return SubmitResult.Success;
                            }
                        }
                    }
                    else if (commandData.Type == "slotdispenser")
                    {
                        int num34 = (int)commandData.GetValue("s");
                        int index5 = num34 - 1;
                        if (componentDispenser.m_slots[index5].Value == num33 && componentDispenser.m_slots[index5].Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }
                    else if (commandData.Type == "limdispenser")
                    {
                        int num35 = (int)commandData.GetValue("s");
                        int num36 = (int)commandData.GetValue("v");
                        int index6 = num35 - 1;
                        if (componentDispenser.m_slots[index6].Value == num33 && componentDispenser.m_slots[index6].Count == num36)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type.Contains("furnace"))
                {
                    int num37 = (int)commandData.GetValue("id");
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
                            if (slot6.Value == num37 && slot6.Count >= 1)
                            {
                                return SubmitResult.Success;
                            }
                        }
                    }
                    else if (commandData.Type == "slotfurnace")
                    {
                        int num38 = (int)commandData.GetValue("s");
                        int index7 = num38 - 1;
                        if (componentFurnace.m_slots[index7].Value == num37 && componentFurnace.m_slots[index7].Count >= 1)
                        {
                            return SubmitResult.Success;
                        }
                    }
                    else if (commandData.Type == "limfurnace")
                    {
                        int num39 = (int)commandData.GetValue("s");
                        int num40 = (int)commandData.GetValue("v");
                        int index8 = num39 - 1;
                        if (componentFurnace.m_slots[index8].Value == num37 && componentFurnace.m_slots[index8].Count == num40)
                        {
                            return SubmitResult.Success;
                        }
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("handitem", delegate (CommandData commandData)
            {
                int activeSlotIndex = m_componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex;
                int slotValue = m_componentPlayer.ComponentMiner.Inventory.GetSlotValue(activeSlotIndex);
                int slotCount = m_componentPlayer.ComponentMiner.Inventory.GetSlotCount(activeSlotIndex);
                if (commandData.Type == "default")
                {
                    int num15 = (int)commandData.GetValue("id");
                    if (slotValue == num15 && slotCount >= 1)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "limit")
                {
                    int num16 = (int)commandData.GetValue("id");
                    int num17 = (int)commandData.GetValue("v");
                    if (slotValue == num16 && slotCount == num17)
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
            AddCondition("levelrange", delegate (CommandData commandData)
            {
                Vector2 vector6 = (Vector2)commandData.GetValue("vec2");
                int num14 = (int)m_componentPlayer.PlayerData.Level;
                return (!((float)num14 >= vector6.X) || !((float)num14 <= vector6.Y)) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("heightrange", delegate (CommandData commandData)
            {
                Vector2 vector5 = (Vector2)commandData.GetValue("vec2");
                float y = m_componentPlayer.ComponentBody.Position.Y;
                return (!(y >= vector5.X) || !(y <= vector5.Y)) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("eyesangle", delegate (CommandData commandData)
            {
                Point2 point4 = (Point2)commandData.GetValue("eyes1");
                Point2 point5 = (Point2)commandData.GetValue("eyes2");
                Point2 playerEyesAngle = DataHandle.GetPlayerEyesAngle(m_componentPlayer);
                return (playerEyesAngle.X < point4.X || playerEyesAngle.X > point4.Y || playerEyesAngle.Y < point5.X || playerEyesAngle.Y > point5.Y) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("statsrange", delegate (CommandData commandData)
            {
                if (commandData.Type != "default")
                {
                    Vector2 vector4 = (Vector2)commandData.GetValue("vec2");
                    float num13 = -1f;
                    switch (commandData.Type)
                    {
                        case "health":
                            num13 = m_componentPlayer.ComponentHealth.Health * 100f;
                            break;
                        case "food":
                            num13 = m_componentPlayer.ComponentVitalStats.Food * 100f;
                            break;
                        case "stamina":
                            num13 = m_componentPlayer.ComponentVitalStats.Stamina * 100f;
                            break;
                        case "sleep":
                            num13 = m_componentPlayer.ComponentVitalStats.Sleep * 100f;
                            break;
                        case "attack":
                            num13 = m_componentPlayer.ComponentMiner.AttackPower;
                            break;
                        case "defense":
                            num13 = m_componentPlayer.ComponentHealth.AttackResilience;
                            break;
                        case "speed":
                            num13 = m_componentPlayer.ComponentLocomotion.WalkSpeed * 10f;
                            break;
                        case "temperature":
                            num13 = m_componentPlayer.ComponentVitalStats.Temperature;
                            break;
                        case "wetness":
                            num13 = m_componentPlayer.ComponentVitalStats.Wetness * 100f;
                            break;
                    }

                    if (num13 >= vector4.X && num13 <= vector4.Y)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("actionmake", delegate (CommandData commandData)
            {
                bool flag4 = false;
                switch (commandData.Type)
                {
                    case "default":
                        flag4 = m_componentPlayer.ComponentBody.Velocity.LengthSquared() > 0.0625f;
                        break;
                    case "sneak":
                        flag4 = m_componentPlayer.ComponentBody.IsSneaking;
                        break;
                    case "rider":
                        flag4 = m_componentPlayer.ComponentRider.Mount != null;
                        break;
                    case "sleep":
                        flag4 = m_componentPlayer.ComponentSleep.IsSleeping;
                        break;
                    case "jump":
                        flag4 = m_componentPlayer.ComponentInput.PlayerInput.Jump;
                        break;
                    case "hasflu":
                        flag4 = m_componentPlayer.ComponentFlu.HasFlu;
                        break;
                    case "sick":
                        flag4 = m_componentPlayer.ComponentSickness.IsSick;
                        break;
                    case "moveup":
                        flag4 = m_componentPlayer.ComponentInput.PlayerInput.Move.Z > 0f;
                        break;
                    case "movedown":
                        flag4 = m_componentPlayer.ComponentInput.PlayerInput.Move.Z < 0f;
                        break;
                    case "moveleft":
                        flag4 = m_componentPlayer.ComponentInput.PlayerInput.Move.X < 0f;
                        break;
                    case "moveright":
                        flag4 = m_componentPlayer.ComponentInput.PlayerInput.Move.X > 0f;
                        break;
                    default:
                        if (commandData.Type.StartsWith("look"))
                        {
                            float num10 = MathUtils.Abs(m_componentPlayer.ComponentInput.PlayerInput.Look.X);
                            float num11 = MathUtils.Abs(m_componentPlayer.ComponentInput.PlayerInput.Look.Y);
                            int num12 = (int)((double)MathUtils.Atan(num11 / num10) / 3.14 * 180.0);
                            if (commandData.Type == "lookup")
                            {
                                flag4 = m_componentPlayer.ComponentInput.PlayerInput.Look.Y > 0f && num12 > 30;
                            }
                            else if (commandData.Type == "lookdown")
                            {
                                flag4 = m_componentPlayer.ComponentInput.PlayerInput.Look.Y < 0f && num12 > 30;
                            }
                            else if (commandData.Type == "lookleft")
                            {
                                flag4 = m_componentPlayer.ComponentInput.PlayerInput.Look.X < 0f && num12 < 60;
                            }
                            else if (commandData.Type == "lookright")
                            {
                                flag4 = m_componentPlayer.ComponentInput.PlayerInput.Look.X > 0f && num12 < 60;
                            }
                        }

                        break;
                }

                return (!flag4) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("openwidget", delegate (CommandData commandData)
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
            AddCondition("gamemode", delegate (CommandData commandData)
            {
                if (commandData.Type != "default")
                {
                    bool flag2 = false;
                    switch (commandData.Type)
                    {
                        case "creative":
                            flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
                            break;
                        case "harmless":
                            flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless;
                            break;
                        case "challenge":
                            flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Challenging;
                            break;
                        case "cruel":
                            flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel;
                            break;
                        case "adventure":
                            flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure;
                            break;
                    }

                    if (flag2)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("camerapos", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3[] twoPoint2 = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea2 = new CubeArea(twoPoint2[0], twoPoint2[1]);
                    if (cubeArea2.Exist(m_componentPlayer.GameWidget.ActiveCamera.ViewPosition))
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "direction")
                {
                    Point2 point = (Point2)commandData.GetValue("eyes1");
                    Point2 point2 = (Point2)commandData.GetValue("eyes2");
                    Point2 point3 = DataHandle.DirectionToEyes(m_componentPlayer.PlayerData.GameWidget.ActiveCamera.ViewDirection);
                    if (point3.X >= point.X && point3.X <= point.Y && point3.Y >= point2.X && point3.Y <= point2.Y)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("signtext", delegate (CommandData commandData)
            {
                Point3 onePoint3 = GetOnePoint("pos", commandData);
                string text2 = (string)commandData.GetValue("text");
                SubsystemSignBlockBehavior subsystemSignBlockBehavior = base.Project.FindSubsystem<SubsystemSignBlockBehavior>();
                SignData signData = subsystemSignBlockBehavior.GetSignData(onePoint3);
                if (signData == null)
                {
                    return SubmitResult.Fail;
                }

                if (commandData.Type == "default")
                {
                    string[] lines = signData.Lines;
                    foreach (string text3 in lines)
                    {
                        if (text3.Contains(text2))
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else
                {
                    bool flag = false;
                    switch (commandData.Type)
                    {
                        case "lineone":
                            flag = signData.Lines[0] == text2;
                            break;
                        case "linetwo":
                            flag = signData.Lines[1] == text2;
                            break;
                        case "linethree":
                            flag = signData.Lines[2] == text2;
                            break;
                        case "linefour":
                            flag = signData.Lines[3] == text2;
                            break;
                        case "lineurl":
                            flag = signData.Url == text2;
                            break;
                    }

                    if (flag)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("clickinteract", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Point3 onePoint = GetOnePoint("pos", commandData);
                    int num6 = (int)commandData.GetValue("v");
                    if (m_interactResult != null && m_interactResult is TerrainRaycastResult && ((TerrainRaycastResult)m_interactResult).Distance <= (float)num6 && ((TerrainRaycastResult)m_interactResult).CellFace.Point == onePoint)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "limblock")
                {
                    Point3 onePoint2 = GetOnePoint("pos", commandData);
                    int num7 = (int)commandData.GetValue("id");
                    int num8 = (int)commandData.GetValue("v");
                    if (m_interactResult != null && m_interactResult is TerrainRaycastResult && ((TerrainRaycastResult)m_interactResult).Distance <= (float)num8 && ((TerrainRaycastResult)m_interactResult).CellFace.Point == onePoint2 && GetLimitValue(onePoint2.X, onePoint2.Y, onePoint2.Z) == num7)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "creature")
                {
                    string text = (string)commandData.GetValue("obj");
                    int num9 = (int)commandData.GetValue("v");
                    if (m_interactResult != null && m_interactResult is BodyRaycastResult && ((BodyRaycastResult)m_interactResult).Distance <= (float)num9)
                    {
                        Entity entity = ((BodyRaycastResult)m_interactResult).ComponentBody.Entity;
                        if (text.ToLower() == entity.ValuesDictionary.DatabaseObject.Name.ToLower())
                        {
                            return SubmitResult.Success;
                        }
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("longpress", delegate (CommandData commandData)
            {
                int num5 = (int)commandData.GetValue("v");
                return (!(m_aimDurationTime >= (float)num5)) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("timerange", delegate (CommandData commandData)
            {
                if (commandData.Type == "default")
                {
                    Vector2 vector2 = (Vector2)commandData.GetValue("vec2");
                    SubsystemTimeOfDay subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>();
                    int num4 = (int)(subsystemTimeOfDay.TimeOfDay * 4096f);
                    if ((float)num4 >= vector2.X && (float)num4 <= vector2.Y)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "system")
                {
                    DateTime value7 = (DateTime)commandData.GetValue("time1");
                    DateTime value8 = (DateTime)commandData.GetValue("time2");
                    if (DateTime.Now.CompareTo(value7) >= 0 && DateTime.Now.CompareTo(value8) <= 0)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "worldrun")
                {
                    Vector2 vector3 = (Vector2)commandData.GetValue("vec2");
                    if (m_worldRunTime >= vector3.X && m_worldRunTime <= vector3.Y)
                    {
                        return SubmitResult.Success;
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("fileexist", delegate (CommandData commandData)
            {
                string pathName = (string)commandData.GetValue("f");
                return (!Storage.FileExists(DataHandle.GetCommandResPathName(pathName))) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("modcount", delegate (CommandData commandData)
            {
                Vector2 vector = (Vector2)commandData.GetValue("vec2");
                return (!((float)ModsManager.ModList.Count >= vector.X) || !((float)ModsManager.ModList.Count <= vector.Y)) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("oncapture", (CommandData commandData) => (!m_onCapture) ? SubmitResult.Fail : SubmitResult.Success);
            AddCondition("eatorwear", delegate (CommandData commandData)
            {
                int num3 = (int)commandData.GetValue("id");
                return (!m_eatItem.HasValue || m_eatItem.Value.X != num3) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("clothes", delegate (CommandData commandData)
            {
                int num2 = (int)commandData.GetValue("id");
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(num2)];
                if (block is ClothingBlock)
                {
                    ClothingData clothingData = block.GetClothingData(num2);
                    if (clothingData != null)
                    {
                        foreach (int clothe in m_componentPlayer.ComponentClothing.GetClothes(clothingData.Slot))
                        {
                            if (clothe == num2)
                            {
                                return SubmitResult.Success;
                            }
                        }
                    }
                }

                return SubmitResult.Fail;
            });
            AddCondition("patternbutton", delegate (CommandData commandData)
            {
                string key = (string)commandData.GetValue("f");
                ScreenPattern value6;
                return (!ScreenPatterns.TryGetValue(key, out value6) || !(value6.OutTime > 0f)) ? SubmitResult.Fail : SubmitResult.Success;
            });
            AddCondition("moveset", delegate (CommandData commandData)
            {
                string n = (string)commandData.GetValue("n");
                string tag = GetMovingBlockTagLine(n);
                if (commandData.Type == "default")
                {
                    Point3[] twoPoint = GetTwoPoint("pos1", "pos2", commandData);
                    CubeArea cubeArea = new CubeArea(twoPoint[0], twoPoint[1]);
                    if (tag == null)
                    {
                        if (FindWaitMoveSet(n, out tag, out var value) && value.Count > 0 && cubeArea.Exist(new Vector3(value[0])) && cubeArea.Exist(new Vector3(value[value.Count - 1]) + Vector3.One))
                        {
                            return SubmitResult.Success;
                        }
                    }
                    else
                    {
                        IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.FindMovingBlocks("moveset", tag);
                        SubsystemMovingBlocks.MovingBlockSet movingBlockSet2 = (SubsystemMovingBlocks.MovingBlockSet)movingBlockSet;
                        if (cubeArea.Exist(movingBlockSet2.Position) && cubeArea.Exist(movingBlockSet2.Position + new Vector3(movingBlockSet2.Box.Size)))
                        {
                            return SubmitResult.Success;
                        }
                    }
                }
                else if (commandData.Type == "pausestop")
                {
                    if (tag == null)
                    {
                        if (FindWaitMoveSet(n, out tag, out var _))
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
                    if (tag != null && m_movingCollisions.TryGetValue(tag, out var value3) && value3.Block != 0)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "collidelimblock")
                {
                    int num = (int)commandData.GetValue("id");
                    if (tag != null && m_movingCollisions.TryGetValue(tag, out var value4) && value4.Block == num)
                    {
                        return SubmitResult.Success;
                    }
                }
                else if (commandData.Type == "collideentity")
                {
                    string obj = (string)commandData.GetValue("obj");
                    string entityName = EntityInfoManager.GetEntityName(obj);
                    if (tag != null && m_movingCollisions.TryGetValue(tag, out var value5) && value5.Creature == entityName)
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
            m_subsystemTerrain.ChangeCell(x, y, z, value);
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
                ShowSubmitTips($"在{DataHandle.GetCommandPath()}目录中找不到文件:{f}");
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
                m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(chunkState, forceGeometryRegeneration: true);
            }

            Time.QueueTimeDelayedExecution(Time.RealTime + (double)time, delegate
            {
                m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(chunkState, forceGeometryRegeneration: true);
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
            m_subsystemMovingBlocks.CollidedWithTerrain += delegate (IMovingBlockSet movingBlockSet, Point3 pos)
            {
                if (movingBlockSet.Id != null)
                {
                    if (movingBlockSet.Id == "moveblock$dig")
                    {
                        m_subsystemTerrain.DestroyCell(1, pos.X, pos.Y, pos.Z, 0, noDrop: false, noParticleSystem: false);
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
            m_subsystemMovingBlocks.Stopped += delegate (IMovingBlockSet movingBlockSet)
            {
                if (movingBlockSet.Id != null && movingBlockSet.Id.StartsWith("moveblock"))
                {
                    foreach (MovingBlock block in movingBlockSet.Blocks)
                    {
                        Point3 point = new Point3((int)MathUtils.Round(movingBlockSet.Position.X), (int)MathUtils.Round(movingBlockSet.Position.Y), (int)MathUtils.Round(movingBlockSet.Position.Z));
                        m_subsystemTerrain.ChangeCell(point.X + block.Offset.X, point.Y + block.Offset.Y, point.Z + block.Offset.Z, block.Value);
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
            IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.AddMovingBlockSet(vector, vector, 0f, 0f, 0f, new Vector2(1f, 1f), list2, "moveset", tag, testCollision: true);
            if (movingBlockSet != null)
            {
                foreach (Point3 item3 in list)
                {
                    m_subsystemTerrain.ChangeCell(item3.X, item3.Y, item3.Z, 0);
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
                        m_batches[0] = m_primitivesRenderer.TexturedBatch(pattern.Texture, useAlphaTest: true, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
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
                base.Project.RemoveEntity(entity, disposeEntity: true);
                Entity entity2 = DatabaseManager.CreateEntity(base.Project, "Werewolf", throwIfNotFound: true);
                ComponentFrame componentFrame = entity2.FindComponent<ComponentFrame>();
                ComponentSpawn componentSpawn = entity2.FindComponent<ComponentSpawn>();
                if (componentFrame != null && componentSpawn != null)
                {
                    componentFrame.Position = componentBody.Position;
                    componentFrame.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, new Game.Random().Float(0f, (float)Math.PI * 2f));
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
                if (CreatureTextures.TryGetValue(key, out var value))
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

                if (!CreatureModels.TryGetValue(key, out var value2))
                {
                    return;
                }

                if (value2.StartsWith("$"))
                {
                    Stream commandFileStream2 = GetCommandFileStream(value2.Substring(1), OpenFileMode.ReadWrite);
                    if (commandFileStream2 != null)
                    {
                        Model model = Model.Load(commandFileStream2, keepSourceVertexDataInTags: true);
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
                XElement xElement = XmlUtils.LoadXmlFromStream(stream, Encoding.UTF8, throwOnError: true);
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

            XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, throwOnError: true);
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
                    xElement = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
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
                    XmlUtils.SaveXmlToStream(xElement, stream2, null, throwOnError: true);
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