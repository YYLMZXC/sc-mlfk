using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using GameEntitySystem;

using Game;

namespace Mlfk
{
    public class CommandEditWidget : CanvasWidget
    {
        public ButtonWidget m_commandNameButton;

        public ButtonWidget m_commandTypeButton;

        public ButtonWidget m_coordinateButton;

        public ButtonWidget m_collectInstructionButton;

        public ButtonWidget m_moreInstructionButton;

        public ButtonWidget m_workingModeButton;

        public ButtonWidget m_quickModeButton;

        public ButtonWidget m_submitButton;

        public ButtonWidget m_saveButton;

        public ButtonWidget m_cancelButton;

        public LabelWidget m_commandNameTitleLabel;

        public LabelWidget m_commandNameLabel;

        public LabelWidget m_commandTypeLabel;

        public LabelWidget m_coordinateLabel;

        public LabelWidget m_workingModeLabel;

        public LabelWidget m_quickModeLabel;

        public LabelWidget m_saveLabel;

        public LabelWidget m_instructionLabel;

        public RectangleWidget m_workingModeRectangle;

        public RectangleWidget m_quickModeRectangle;

        public RectangleWidget m_commandTypeRectangle;

        public RectangleWidget m_coordinateRectangle;

        public LabelWidget m_instructionScrollLabel;

        public CommandScrollPanelWidget m_instructionScrollPanel;

        public ScrollPanelWidget m_commandDataScrollPanelWidget;

        public StackPanelWidget m_commandDataWidget;

        public StackPanelWidget m_commandDataStaticWidget;

        public StackPanelWidget m_commandDataScrollWidget;

        public CanvasWidget m_submitCanvas;

        public Dictionary<string, Widget> m_commandWidgetDatas = new Dictionary<string, Widget>();

        public ComponentPlayer m_componentPlayer;

        public string m_name;

        public string m_type;

        public string m_coordinate;

        public Point3 m_position;

        public SubsystemCommandBlockBehavior m_subsystemCmdBlockBehavior;

        public SubsystemCmdRodBlockBehavior m_subsystemCmdRodBehavior;

        public SubsystemTerrain m_subsystemTerrain;

        public bool m_conditionMode = false;

        public bool m_fastMode;

        public static Dictionary<Point3, Vector2> ScrollPosition = new Dictionary<Point3, Vector2>();

        public CommandEditWidget(Project project, ComponentPlayer componentPlayer, Point3 position, bool fastMode = false)
        {
            m_componentPlayer = componentPlayer;
            m_position = position;
            m_fastMode = fastMode;
            m_subsystemCmdBlockBehavior = project.FindSubsystem<SubsystemCommandBlockBehavior>();
            m_subsystemCmdRodBehavior = project.FindSubsystem<SubsystemCmdRodBlockBehavior>();
            m_subsystemTerrain = project.FindSubsystem<SubsystemTerrain>();
            XElement node = ContentManager.Get<XElement>("Widgets/CommandEditWidget");
            LoadContents(this, node);
            m_commandNameButton = Children.Find<ButtonWidget>("CommandNameButton");
            m_commandTypeButton = Children.Find<ButtonWidget>("CommandTypeButton");
            m_coordinateButton = Children.Find<ButtonWidget>("CoordinateButton");
            m_collectInstructionButton = Children.Find<ButtonWidget>("CollectInstructionButton");
            m_moreInstructionButton = Children.Find<ButtonWidget>("MoreInstructionButton");
            m_workingModeButton = Children.Find<ButtonWidget>("WorkingModeButton");
            m_quickModeButton = Children.Find<ButtonWidget>("QuickModeButton");
            m_submitButton = Children.Find<ButtonWidget>("SubmitButton");
            m_saveButton = Children.Find<ButtonWidget>("SaveButton");
            m_cancelButton = Children.Find<ButtonWidget>("CancelButton");
            m_commandNameTitleLabel = Children.Find<LabelWidget>("CommandNameTitleLabel");
            m_commandNameLabel = Children.Find<LabelWidget>("CommandNameLabel");
            m_commandTypeLabel = Children.Find<LabelWidget>("CommandTypeLabel");
            m_coordinateLabel = Children.Find<LabelWidget>("CoordinateLabel");
            m_instructionLabel = Children.Find<LabelWidget>("InstructionLabel");
            m_workingModeLabel = Children.Find<LabelWidget>("WorkingModeLabel");
            m_quickModeLabel = Children.Find<LabelWidget>("QuickModeLabel");
            m_saveLabel = Children.Find<LabelWidget>("SaveLabel");
            m_workingModeRectangle = Children.Find<RectangleWidget>("WorkingModeRectangle");
            m_quickModeRectangle = Children.Find<RectangleWidget>("QuickModeRectangle");
            m_commandTypeRectangle = Children.Find<RectangleWidget>("CommandTypeRectangle");
            m_coordinateRectangle = Children.Find<RectangleWidget>("CoordinateRectangle");
            m_instructionScrollLabel = Children.Find<LabelWidget>("InstructionScrollLabel");
            m_instructionScrollPanel = Children.Find<CommandScrollPanelWidget>("InstructionScrollPanel");
            m_commandDataScrollPanelWidget = Children.Find<ScrollPanelWidget>("CommandDataScrollPanelWidget");
            m_commandDataStaticWidget = Children.Find<StackPanelWidget>("CommandDataStaticWidget");
            m_commandDataScrollWidget = Children.Find<StackPanelWidget>("CommandDataScrollWidget");
            m_submitCanvas = Children.Find<CanvasWidget>("SubmitCanvas");
            m_instructionScrollPanel.IsVisible = false;
            m_quickModeButton.IsVisible = false;
            CommandData commandData;
            if (m_fastMode)
            {
                commandData = new CommandData(Point3.Zero, m_subsystemCmdRodBehavior.m_commandLine);
                commandData.TrySetValue();
            }
            else
            {
                commandData = m_subsystemCmdBlockBehavior.GetCommandData(m_position);
            }

            Initialize();
            SetCommandDataValue(commandData);
            ChangeExplains();
            GetDynamicCommanndWidget();
            SetCommandWidgetValue(commandData);
            GuiWidgetControl(m_componentPlayer, button: false);
        }

        public override void Update()
        {
            if (m_commandNameButton.IsClicked)
            {
                Dictionary<string, Instruction> instructions = ((!m_conditionMode) ? InstructionManager.FunInstructions : InstructionManager.ConInstructions);
                GameMode gameMode = m_componentPlayer.m_subsystemGameInfo.WorldSettings.GameMode;
                if (gameMode == GameMode.Cruel || gameMode == GameMode.Challenging)
                {
                    instructions = InstructionManager.GetSurvivalList(instructions);
                }

                if (!ScrollPosition.TryGetValue(m_position, out var value))
                {
                    value = Vector2.Zero;
                    ScrollPosition[m_position] = value;
                }

                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new CommandListSelectionDialog(search: true, instructions.Values, 50f, value.X, (object i) => new LabelWidget
                {
                    Text = InstructionManager.GetDisplayName(((Instruction)i).Name) + " : " + ((Instruction)i).About,
                    Color = ((m_name == InstructionManager.GetDisplayName(((Instruction)i).Name)) ? Color.Green : Color.White),
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center
                }, delegate (object i, float scroll)
                {
                    ScrollPosition[m_position] = new Vector2(scroll, ScrollPosition[m_position].Y);
                    if (i != null && m_name != InstructionManager.GetDisplayName(((Instruction)i).Name))
                    {
                        m_name = InstructionManager.GetDisplayName(((Instruction)i).Name);
                        m_type = ((Instruction)i).Types[0];
                        m_commandNameLabel.Text = GetAbridgeText(m_name);
                        m_commandTypeLabel.Text = GetAbridgeText(m_type);
                        if (((Instruction)i).Types.Count <= 1)
                        {
                            m_commandTypeRectangle.FillColor = new Color(90, 90, 90);
                        }
                        else
                        {
                            m_commandTypeRectangle.FillColor = new Color(120, 120, 120);
                        }

                        CommandData commandData4 = new CommandData(Point3.Zero, string.Empty);
                        SetCommandDataValue(commandData4);
                        ChangeExplains();
                        GetDynamicCommanndWidget();
                        SetCommandWidgetValue(commandData4);
                    }
                }, delegate (TextBoxWidget textBox, ListPanelWidget listWidget)
                {
                    listWidget.ClearItems();
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        foreach (Instruction value3 in instructions.Values)
                        {
                            listWidget.AddItem(value3);
                        }

                        return;
                    }

                    foreach (Instruction value4 in instructions.Values)
                    {
                        string text3 = InstructionManager.GetDisplayName(value4.Name) + value4.About;
                        if (text3.Contains(textBox.Text))
                        {
                            listWidget.AddItem(value4);
                        }
                    }
                }));
            }

            if (m_commandTypeButton.IsClicked)
            {
                Instruction instruction = InstructionManager.GetInstruction(m_name, m_conditionMode);
                if (instruction == null)
                {
                    return;
                }

                if (instruction.Types.Count < 2)
                {
                    ShowEditTips(InstructionManager.GetDisplayName(instruction.Name) + "指令无其他命令类型");
                    return;
                }

                if (!ScrollPosition.TryGetValue(m_position, out var value2))
                {
                    value2 = Vector2.Zero;
                    ScrollPosition[m_position] = value2;
                }

                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new CommandListSelectionDialog(search: true, instruction.Types, 50f, value2.Y, (object type) => new LabelWidget
                {
                    Text = (string)type,
                    Color = ((m_type == (string)type) ? Color.Green : Color.White),
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center
                }, delegate (object type, float scroll)
                {
                    ScrollPosition[m_position] = new Vector2(ScrollPosition[m_position].X, scroll);
                    if (type != null && m_type != (string)type)
                    {
                        m_type = (string)type;
                        m_commandTypeLabel.Text = GetAbridgeText(m_type);
                        CommandData commandData3 = new CommandData(Point3.Zero, string.Empty);
                        SetCommandDataValue(commandData3);
                        ChangeExplains();
                        GetDynamicCommanndWidget();
                        SetCommandWidgetValue(commandData3);
                    }
                }, delegate (TextBoxWidget textBox, ListPanelWidget listWidget)
                {
                    listWidget.ClearItems();
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        foreach (string type in instruction.Types)
                        {
                            listWidget.AddItem(type);
                        }

                        return;
                    }

                    foreach (string type2 in instruction.Types)
                    {
                        string text2 = type2 + instruction.Details[type2];
                        if (text2.Contains(textBox.Text))
                        {
                            listWidget.AddItem(type2);
                        }
                    }
                }));
            }

            if (m_coordinateButton.IsClicked)
            {
                if (m_fastMode)
                {
                    ShowEditTips("快速指令界面只允许使用标准坐标");
                    return;
                }

                string[] items = new string[3] { "default : 默认世界坐标", "command : 相对命令方块", "player : 相对当前玩家" };
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new CommandListSelectionDialog(search: false, items, 50f, 0f, (object coord) => new LabelWidget
                {
                    Text = (string)coord,
                    Color = (((string)coord).Contains(m_coordinate) ? Color.Green : Color.White),
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center
                }, delegate (object coord, float scroll)
                {
                    if (coord != null)
                    {
                        string text = ((string)coord).Replace(" ", "").Split(':')[0];
                        if (m_coordinate != text)
                        {
                            m_coordinate = text;
                            m_coordinateLabel.Text = m_coordinate;
                        }
                    }
                }, null));
            }

            if (m_moreInstructionButton.IsClicked)
            {
                CommandEditHistoryWidget widget = new CommandEditHistoryWidget(this, m_position, m_fastMode);
                base.ParentWidget.Children.Add(widget);
            }

            if (m_collectInstructionButton.IsClicked)
            {
                SaveCommandData(collection: true);
                ShowEditTips("已收藏当前指令");
            }

            if (m_workingModeButton.IsClicked)
            {
                if (m_fastMode)
                {
                    m_subsystemCmdRodBehavior.m_subsystemCommand.m_canWorking = !m_subsystemCmdRodBehavior.m_subsystemCommand.m_canWorking;
                    if (m_subsystemCmdRodBehavior.m_subsystemCommand.m_canWorking)
                    {
                        m_workingModeRectangle.FillColor = new Color(120, 120, 120);
                        ShowEditTips("已开启指令的功能运作");
                    }
                    else
                    {
                        m_workingModeRectangle.FillColor = new Color(80, 80, 80);
                        ShowEditTips("已关闭指令的功能运作");
                    }
                }
                else
                {
                    m_conditionMode = !m_conditionMode;
                    CommandData commandData = new CommandData(Point3.Zero, string.Empty);
                    Initialize();
                    SetCommandDataValue(commandData);
                    ChangeExplains();
                    GetDynamicCommanndWidget();
                    SetCommandWidgetValue(commandData);
                }
            }

            if (m_quickModeButton.IsClicked)
            {
                SubsystemCmdRodBlockBehavior.QuickMode = !SubsystemCmdRodBlockBehavior.QuickMode;
                if (SubsystemCmdRodBlockBehavior.QuickMode)
                {
                    WithdrawBlockManager.WithdrawMode = true;
                    m_quickModeRectangle.FillColor = new Color(120, 120, 120);
                    ShowEditTips("已开启快捷模式，可在命令手册查看使用说明", 3);
                }
                else
                {
                    m_quickModeRectangle.FillColor = new Color(80, 80, 80);
                    ShowEditTips("已关闭快捷模式");
                }
            }

            if (m_cancelButton.IsClicked || base.Input.Cancel || base.Input.Back)
            {
                GuiWidgetControl(m_componentPlayer, button: true);
                base.ParentWidget.Children.Remove(this);
            }

            if (m_saveButton.IsClicked)
            {
                SaveCommandData(collection: false);
                GuiWidgetControl(m_componentPlayer, button: true);
                base.ParentWidget.Children.Remove(this);
            }

            if (m_submitButton.IsClicked && m_fastMode)
            {
                CommandData commandData2 = SaveCommandData(collection: false);
                if (commandData2.Mode == WorkingMode.Default)
                {
                    SubmitResult submitResult = m_subsystemCmdRodBehavior.m_subsystemCommand.Submit(commandData2.Name, commandData2, Judge: false);
                    switch (submitResult)
                    {
                        case SubmitResult.Success:
                            ShowEditTips("指令提交成功");
                            break;
                        case SubmitResult.Fail:
                            ShowEditTips("指令提交失败");
                            break;
                        case SubmitResult.Exception:
                            ShowEditTips("指令在执行时发生了异常");
                            break;
                        case SubmitResult.Invalid:
                            ShowEditTips("编辑的指令不符合格式要求");
                            break;
                        case SubmitResult.NoFound:
                            ShowEditTips("当前指令不存在");
                            break;
                        case SubmitResult.OutRange:
                            ShowEditTips("指令存在超出范围的参数");
                            break;
                    }

                    if (submitResult == SubmitResult.Success)
                    {
                        Time.QueueTimeDelayedExecution(Time.RealTime + 0.20000000298023224, delegate
                        {
                            GuiWidgetControl(m_componentPlayer, button: true);
                            if (base.ParentWidget != null)
                            {
                                base.ParentWidget.Children.Remove(this);
                            }
                        });
                    }
                }
                else if (commandData2.Mode == WorkingMode.Condition)
                {
                    ShowEditTips("快速指令功能不支持条件指令");
                }
                else if (commandData2.Mode == WorkingMode.Variable)
                {
                    ShowEditTips("快速指令功能不支持指令变量");
                }
            }

            try
            {
                UpdateCommandWidget();
            }
            catch (Exception ex)
            {
                Log.Warning("UpdateCommandWidget:" + ex.Message);
            }
        }

        public void UpdateCommandWidget()
        {
            foreach (string key in m_commandWidgetDatas.Keys)
            {
                if (!key.Contains("Button") || !((ButtonWidget)m_commandWidgetDatas[key]).IsClicked)
                {
                    continue;
                }

                string[] widgetPara = key.Split('$');
                if (widgetPara[0] == "BlockButton")
                {
                    CommandBlockSelectionWidget widget = new CommandBlockSelectionWidget(this, m_componentPlayer.Entity, widgetPara[1]);
                    base.ParentWidget.Children.Add(widget);
                }
                else if (widgetPara[0] == "ModelButton")
                {
                    CommandEntitySelectionWidget widget2 = new CommandEntitySelectionWidget(this, widgetPara[1]);
                    base.ParentWidget.Children.Add(widget2);
                }
                else if (widgetPara[0] == "ColorButton")
                {
                    CommandColorSelectionWidget widget3 = new CommandColorSelectionWidget(this, widgetPara[1]);
                    base.ParentWidget.Children.Add(widget3);
                }
                else
                {
                    if (!(widgetPara[0] == "ActionButton"))
                    {
                        continue;
                    }

                    if (widgetPara[1].StartsWith("id"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordBlockValue.HasValue)
                        {
                            int value = m_subsystemCmdRodBehavior.m_recordBlockValue.Value;
                            TextBoxWidget textBoxWidget = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            BlockIconWidget blockIconWidget = (BlockIconWidget)m_commandWidgetDatas["BlockIcon$" + widgetPara[1]];
                            textBoxWidget.Text = value.ToString() ?? "";
                            blockIconWidget.Value = value;
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击方块获取信息", 5);
                        }
                    }
                    else if (widgetPara[1].StartsWith("obj"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordEntityName != null)
                        {
                            string recordEntityName = m_subsystemCmdRodBehavior.m_recordEntityName;
                            TextBoxWidget textBoxWidget2 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            ModelWidget modelWidget = (ModelWidget)m_commandWidgetDatas["Model$" + widgetPara[1]];
                            textBoxWidget2.Text = recordEntityName;
                            ChangeModelWidget(recordEntityName, modelWidget);
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击动物获取信息", 5);
                        }
                    }
                    else if (widgetPara[1].StartsWith("pos"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordPosition.HasValue)
                        {
                            Point3 value2 = m_subsystemCmdRodBehavior.m_recordPosition.Value;
                            TextBoxWidget textBoxWidget3 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            textBoxWidget3.Text = value2.X + "," + value2.Y + "," + value2.Z;
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击方块获取信息", 5);
                        }
                    }
                    else if (widgetPara[1].StartsWith("eyes"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordEyes.HasValue)
                        {
                            Point2 value3 = m_subsystemCmdRodBehavior.m_recordEyes.Value;
                            TextBoxWidget textBoxWidget4 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            textBoxWidget4.Text = value3.X + "," + value3.Y;
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击屏幕获取信息", 5);
                        }
                    }
                    else if (widgetPara[1].StartsWith("fid"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordfurnitureId.HasValue)
                        {
                            int value4 = m_subsystemCmdRodBehavior.m_recordfurnitureId.Value;
                            TextBoxWidget textBoxWidget5 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            textBoxWidget5.Text = value4.ToString() ?? "";
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击家具获取信息", 5);
                        }
                    }
                    else if (widgetPara[1].StartsWith("cid"))
                    {
                        if (m_subsystemCmdRodBehavior.m_recordclothesId.HasValue)
                        {
                            int value5 = m_subsystemCmdRodBehavior.m_recordclothesId.Value;
                            TextBoxWidget textBoxWidget6 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                            textBoxWidget6.Text = value5.ToString() ?? "";
                        }
                        else
                        {
                            ShowEditTips("请先用命令辅助棒点击衣物掉落物获取信息", 5);
                        }
                    }
                    else
                    {
                        if (!widgetPara[1].StartsWith("opt"))
                        {
                            continue;
                        }

                        string[] instructionOption = InstructionManager.GetInstructionOption(m_name, m_type, widgetPara[1], m_conditionMode);
                        if (instructionOption != null)
                        {
                            DialogsManager.ShowDialog(null, new CommandListSelectionDialog(search: false, instructionOption, 50f, 0f, (object option) => new LabelWidget
                            {
                                Text = (((string)option).Contains(":") ? (((string)option).Split(':')[0] + " : " + ((string)option).Split(':')[1]) : ((string)option)),
                                HorizontalAlignment = WidgetAlignment.Center,
                                VerticalAlignment = WidgetAlignment.Center
                            }, delegate (object option, float scroll)
                            {
                                if (option != null)
                                {
                                    string text = (string)option;
                                    if (text.Contains(":"))
                                    {
                                        text = text.Split(':')[0].Replace(" ", "");
                                    }

                                    TextBoxWidget textBoxWidget7 = (TextBoxWidget)m_commandWidgetDatas["TextBox$" + widgetPara[1]];
                                    textBoxWidget7.Text = text;
                                }
                            }, null));
                        }
                        else
                        {
                            ShowEditTips("暂无可供选择的值", 5);
                        }
                    }
                }
            }
        }

        public void Initialize()
        {
            Dictionary<string, Instruction> dictionary = ((!m_conditionMode) ? InstructionManager.FunInstructions : InstructionManager.ConInstructions);
            using (Dictionary<string, Instruction>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    Instruction current = enumerator.Current;
                    m_name = InstructionManager.GetDisplayName(current.Name);
                    m_type = current.Types[0];
                    m_coordinate = "default";
                    m_commandNameLabel.Text = GetAbridgeText(m_name);
                    m_commandTypeLabel.Text = GetAbridgeText(m_type);
                    m_coordinateLabel.Text = m_coordinate;
                }
            }

            m_commandNameTitleLabel.Text = "功能指令:";
            m_workingModeLabel.Text = "功能执行";
            m_submitCanvas.IsVisible = m_fastMode;
            if (m_fastMode)
            {
                m_workingModeLabel.Text = "指令运作";
                m_saveLabel.Text = "保存";
                m_coordinateRectangle.FillColor = new Color(90, 90, 90);
                if (m_subsystemCmdRodBehavior.m_subsystemCommand.m_canWorking)
                {
                    m_workingModeRectangle.FillColor = new Color(120, 120, 120);
                }
                else
                {
                    m_workingModeRectangle.FillColor = new Color(80, 80, 80);
                }

                m_quickModeButton.IsVisible = true;
                m_quickModeLabel.Text = "快捷模式";
                if (SubsystemCmdRodBlockBehavior.QuickMode)
                {
                    m_quickModeRectangle.FillColor = new Color(120, 120, 120);
                }
                else
                {
                    m_quickModeRectangle.FillColor = new Color(80, 80, 80);
                }
            }
        }

        public void SetCommandDataValue(CommandData commandData)
        {
            if (commandData != null && string.IsNullOrEmpty(commandData.Line))
            {
                commandData.Line = InstructionManager.GetInstructionDemo(m_name, m_type, m_conditionMode);
                commandData.TrySetValue();
            }
            else if (commandData != null && !string.IsNullOrEmpty(commandData.Line))
            {
                m_name = commandData.Name;
                m_type = commandData.Type;
                m_commandNameLabel.Text = GetAbridgeText(m_name);
                m_commandTypeLabel.Text = GetAbridgeText(m_type);
                if (commandData.Coordinate == CoordinateMode.Command)
                {
                    m_coordinate = "command";
                }
                else if (commandData.Coordinate == CoordinateMode.Player)
                {
                    m_coordinate = "player";
                }
                else
                {
                    m_coordinate = "default";
                }

                m_coordinateLabel.Text = m_coordinate;
                m_conditionMode = commandData.Mode == WorkingMode.Condition;
            }
        }

        public void ChangeExplains()
        {
            m_instructionLabel.Text = "说明：" + InstructionManager.GetInstructionDetail(m_name, m_type, m_conditionMode);
            m_instructionScrollLabel.Text = m_instructionLabel.Text;
            if ((float)m_instructionLabel.Text.Length > 25f)
            {
                m_instructionLabel.IsVisible = false;
                m_instructionScrollPanel.IsVisible = true;
            }
            else
            {
                m_instructionLabel.IsVisible = true;
                m_instructionScrollPanel.IsVisible = false;
            }

            if (!m_fastMode)
            {
                m_commandNameTitleLabel.Text = ((!m_conditionMode) ? "功能指令:" : "条件指令:");
                m_workingModeLabel.Text = ((!m_conditionMode) ? "功能执行" : "条件判断");
            }
        }

        public void GetDynamicCommanndWidget()
        {
            int num = 0;
            Instruction instruction = InstructionManager.GetInstruction(m_name, m_conditionMode);
            if (instruction == null)
            {
                instruction = InstructionManager.FunInstructions.Values.FirstOrDefault();
            }

            m_commandTypeRectangle.FillColor = ((instruction.Types.Count <= 1) ? new Color(90, 90, 90) : new Color(120, 120, 120));
            if (instruction != null)
            {
                int num2 = instruction.Paras[m_type].Length;
                if (num2 > 5)
                {
                    m_commandDataScrollPanelWidget.IsVisible = true;
                    m_commandDataStaticWidget.IsVisible = false;
                    m_commandDataWidget = m_commandDataScrollWidget;
                }
                else
                {
                    m_commandDataScrollPanelWidget.IsVisible = false;
                    m_commandDataStaticWidget.IsVisible = true;
                    m_commandDataWidget = m_commandDataStaticWidget;
                }

                m_commandWidgetDatas.Clear();
                m_commandDataWidget.ClearChildren();
                if (m_name == "book")
                {
                    XElement node = ContentManager.Get<XElement>("Widgets/CommandBookBox");
                    ContainerWidget widget = (ContainerWidget)Widget.LoadWidget(this, node, null);
                    m_commandDataWidget.AddChildren(widget);
                    return;
                }

                int num3 = ((num2 >= 5) ? 20 : 30);
                if (num2 > 5)
                {
                    CanvasWidget canvasWidget = new CanvasWidget();
                    canvasWidget.Size = new Vector2(0f, num3);
                    m_commandDataWidget.AddChildren(canvasWidget);
                }

                string[] array = instruction.Paras[m_type];
                foreach (string text in array)
                {
                    if (InstructionManager.IsFixedParameter(text))
                    {
                        continue;
                    }

                    if (num != 0)
                    {
                        CanvasWidget canvasWidget2 = new CanvasWidget();
                        canvasWidget2.Size = new Vector2(0f, num3);
                        m_commandDataWidget.AddChildren(canvasWidget2);
                    }

                    Point2 value9;
                    if (text.StartsWith("id"))
                    {
                        XElement node2 = ContentManager.Get<XElement>("Widgets/CommandBlockBox");
                        ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
                        LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        TextBoxWidget textBoxWidget = containerWidget.Children.Find<TextBoxWidget>("CommandData.TextBox");
                        BlockIconWidget blockIconWidget = containerWidget.Children.Find<BlockIconWidget>("CommandData.BlockIcon");
                        blockIconWidget.Value = 46;
                        ButtonWidget value = containerWidget.Children.Find<ButtonWidget>("CommandData.BlockButton");
                        ButtonWidget value2 = containerWidget.Children.Find<ButtonWidget>("CommandData.ActionButton");
                        m_commandDataWidget.AddChildren(containerWidget);
                        m_commandWidgetDatas.Add("TextBox$" + text, textBoxWidget);
                        m_commandWidgetDatas.Add("BlockIcon$" + text, blockIconWidget);
                        m_commandWidgetDatas.Add("BlockButton$" + text, value);
                        m_commandWidgetDatas.Add("ActionButton$" + text, value2);
                        textBoxWidget.TextChanged += delegate (TextBoxWidget boxWidget)
                        {
                            ChangeBlockIconWidget(boxWidget.Text, blockIconWidget);
                        };
                    }
                    else if (text.StartsWith("obj"))
                    {
                        XElement node3 = ContentManager.Get<XElement>("Widgets/CommandEntityBox");
                        ContainerWidget containerWidget2 = (ContainerWidget)Widget.LoadWidget(this, node3, null);
                        LabelWidget labelWidget2 = containerWidget2.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget2.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        TextBoxWidget textBoxWidget2 = containerWidget2.Children.Find<TextBoxWidget>("CommandData.TextBox");
                        ModelWidget modelWidget = containerWidget2.Children.Find<ModelWidget>("CommandData.ModelIcon");
                        EntityInfoManager.ChangeModelDisplay(ref modelWidget, "Models/Tiger", "Textures/Creatures/Tiger");
                        ButtonWidget value3 = containerWidget2.Children.Find<ButtonWidget>("CommandData.ModelButton");
                        ButtonWidget value4 = containerWidget2.Children.Find<ButtonWidget>("CommandData.ActionButton");
                        m_commandDataWidget.AddChildren(containerWidget2);
                        m_commandWidgetDatas.Add("TextBox$" + text, textBoxWidget2);
                        m_commandWidgetDatas.Add("Model$" + text, modelWidget);
                        m_commandWidgetDatas.Add("ModelButton$" + text, value3);
                        m_commandWidgetDatas.Add("ActionButton$" + text, value4);
                        textBoxWidget2.TextChanged += delegate (TextBoxWidget boxWidget)
                        {
                            ChangeModelWidget(boxWidget.Text, modelWidget);
                        };
                    }
                    else if (text.StartsWith("color"))
                    {
                        XElement node4 = ContentManager.Get<XElement>("Widgets/CommandColorBox");
                        ContainerWidget containerWidget3 = (ContainerWidget)Widget.LoadWidget(this, node4, null);
                        LabelWidget labelWidget3 = containerWidget3.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget3.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        TextBoxWidget textBoxWidget3 = containerWidget3.Children.Find<TextBoxWidget>("CommandData.TextBox");
                        RectangleWidget rectangleWidget = containerWidget3.Children.Find<RectangleWidget>("CommandData.ColorPanel");
                        rectangleWidget.FillColor = new Color(100, 100, 100, 255);
                        ButtonWidget value5 = containerWidget3.Children.Find<ButtonWidget>("CommandData.ColorButton");
                        m_commandDataWidget.AddChildren(containerWidget3);
                        m_commandWidgetDatas.Add("TextBox$" + text, textBoxWidget3);
                        m_commandWidgetDatas.Add("Rectangle$" + text, rectangleWidget);
                        m_commandWidgetDatas.Add("ColorButton$" + text, value5);
                        textBoxWidget3.TextChanged += delegate (TextBoxWidget boxWidget)
                        {
                            ChangeRectangleWidget(boxWidget.Text, rectangleWidget);
                        };
                    }
                    else if (text.StartsWith("pos") || text.StartsWith("eyes") || text.StartsWith("fid") || text.StartsWith("cid") || text.StartsWith("opt"))
                    {
                        XElement node5 = ContentManager.Get<XElement>("Widgets/CommandTextButtonBox");
                        ContainerWidget containerWidget4 = (ContainerWidget)Widget.LoadWidget(this, node5, null);
                        LabelWidget labelWidget4 = containerWidget4.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget4.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        TextBoxWidget value6 = containerWidget4.Children.Find<TextBoxWidget>("CommandData.TextBox");
                        ButtonWidget value7 = containerWidget4.Children.Find<ButtonWidget>("CommandData.ActionButton");
                        m_commandDataWidget.AddChildren(containerWidget4);
                        m_commandWidgetDatas.Add("TextBox$" + text, value6);
                        m_commandWidgetDatas.Add("ActionButton$" + text, value7);
                    }
                    else if (text.StartsWith("con"))
                    {
                        XElement node6 = ContentManager.Get<XElement>("Widgets/CommandSwitchBox");
                        ContainerWidget containerWidget5 = (ContainerWidget)Widget.LoadWidget(this, node6, null);
                        LabelWidget labelWidget5 = containerWidget5.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget5.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        CommandSwitchWidget value8 = containerWidget5.Children.Find<CommandSwitchWidget>("CommandData.SwitchWidget");
                        m_commandWidgetDatas.Add("Switch$" + text, value8);
                        m_commandDataWidget.AddChildren(containerWidget5);
                    }
                    else if (instruction.Ranges.TryGetValue(m_type + "$" + text, out value9))
                    {
                        XElement node7 = ContentManager.Get<XElement>("Widgets/CommandSliderBox");
                        ContainerWidget containerWidget6 = (ContainerWidget)Widget.LoadWidget(this, node7, null);
                        LabelWidget labelWidget6 = containerWidget6.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget6.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        CommandSliderWidget commandSliderWidget = containerWidget6.Children.Find<CommandSliderWidget>("CommandData.Slider");
                        TextBoxWidget textBoxWidget4 = commandSliderWidget.m_textBoxWidget;
                        commandSliderWidget.MinValue = value9.X;
                        commandSliderWidget.MaxValue = value9.Y;
                        m_commandDataWidget.AddChildren(containerWidget6);
                        m_commandWidgetDatas.Add("Slider$" + text, commandSliderWidget);
                        m_commandWidgetDatas.Add("TextBox$" + text, textBoxWidget4);
                    }
                    else
                    {
                        XElement node8 = ContentManager.Get<XElement>("Widgets/CommandTextBox");
                        ContainerWidget containerWidget7 = (ContainerWidget)Widget.LoadWidget(this, node8, null);
                        LabelWidget labelWidget7 = containerWidget7.Children.Find<LabelWidget>("CommandData.Label");
                        labelWidget7.Text = InstructionManager.GetParameterName(m_name, m_type, text, m_conditionMode) + ":";
                        TextBoxWidget value10 = containerWidget7.Children.Find<TextBoxWidget>("CommandData.TextBox");
                        m_commandDataWidget.AddChildren(containerWidget7);
                        m_commandWidgetDatas.Add("TextBox$" + text, value10);
                    }

                    num++;
                }

                if (num2 > 5)
                {
                    CanvasWidget canvasWidget3 = new CanvasWidget();
                    canvasWidget3.Size = new Vector2(0f, num3);
                    m_commandDataWidget.AddChildren(canvasWidget3);
                }
            }

            if (num == 0)
            {
                LabelWidget labelWidget8 = new LabelWidget();
                labelWidget8.Text = "暂无参数";
                labelWidget8.Color = Color.White;
                labelWidget8.HorizontalAlignment = WidgetAlignment.Center;
                labelWidget8.VerticalAlignment = WidgetAlignment.Center;
                m_commandDataWidget.AddChildren(labelWidget8);
            }
        }

        public void SetCommandWidgetValue(CommandData commandData)
        {
            foreach (string key in m_commandWidgetDatas.Keys)
            {
                string[] array = key.Split('$');
                if (commandData.DataText.TryGetValue(array[1], out var value))
                {
                    if (array[0] == "TextBox")
                    {
                        ((TextBoxWidget)m_commandWidgetDatas[key]).Text = value;
                    }
                    else if (array[0] == "BlockIcon")
                    {
                        ChangeBlockIconWidget(value, (BlockIconWidget)m_commandWidgetDatas[key]);
                    }
                    else if (array[0] == "Model")
                    {
                        ChangeModelWidget(value, (ModelWidget)m_commandWidgetDatas[key]);
                    }
                    else if (array[0] == "Rectangle")
                    {
                        ChangeRectangleWidget(value, (RectangleWidget)m_commandWidgetDatas[key]);
                    }
                    else if (array[0] == "Slider")
                    {
                        ChangeSliderWidget(value, (CommandSliderWidget)m_commandWidgetDatas[key]);
                    }
                    else if (array[0] == "Switch")
                    {
                        ((CommandSwitchWidget)m_commandWidgetDatas[key]).m_checkLabelText = value;
                    }
                }
            }
        }

        public void ChangeBlockIconWidget(string dataText, BlockIconWidget blockIconWidget)
        {
            if (int.TryParse(dataText, out var result))
            {
                blockIconWidget.IsVisible = true;
                blockIconWidget.Value = result;
            }
            else
            {
                blockIconWidget.IsVisible = false;
            }
        }

        public void ChangeModelWidget(string dataText, ModelWidget modelWidget)
        {
            dataText = dataText.ToLower();
            EntityInfo entityInfo = EntityInfoManager.GetEntityInfo(dataText);
            if (entityInfo != null)
            {
                modelWidget.IsVisible = true;
                EntityInfoManager.ChangeModelDisplay(ref modelWidget, entityInfo.Model, entityInfo.Texture);
            }
            else
            {
                modelWidget.IsVisible = false;
            }
        }

        public void ChangeRectangleWidget(string dataText, RectangleWidget rectangleWidget)
        {
            try
            {
                Color colorValue = DataHandle.GetColorValue(dataText);
                if (!(dataText != "255,255,255") || !(dataText != "255,255,255,255") || !(colorValue == Color.White))
                {
                    rectangleWidget.FillColor = colorValue;
                }
                else
                {
                    rectangleWidget.FillColor = new Color(100, 100, 100, 255);
                }
            }
            catch
            {
                rectangleWidget.FillColor = new Color(100, 100, 100, 255);
            }
        }

        public void ChangeSliderWidget(string dataText, CommandSliderWidget sliderWidget)
        {
            if (int.TryParse(dataText, out var result) && (float)result >= sliderWidget.MinValue && (float)result <= sliderWidget.MaxValue)
            {
                sliderWidget.Value = result;
            }
            else
            {
                sliderWidget.Value = sliderWidget.MinValue;
            }
        }

        public string GetCommandLines()
        {
            string text = ((!m_conditionMode) ? "cmd:" : "if:") + m_name + " type:" + m_type;
            string coordinate = m_coordinate;
            string text2 = coordinate;
            if (!(text2 == "command"))
            {
                if (text2 == "player")
                {
                    text += " cd:@pl";
                }
            }
            else
            {
                text += " cd:@c";
            }

            foreach (string key in m_commandWidgetDatas.Keys)
            {
                string[] array = key.Split('$');
                if (array[0] == "TextBox")
                {
                    text = text + " " + array[1] + ":" + ((TextBoxWidget)m_commandWidgetDatas[key]).Text;
                }
                else if (array[0] == "Switch")
                {
                    text = text + " " + array[1] + ":" + ((CommandSwitchWidget)m_commandWidgetDatas[key]).m_checkLabelText;
                }
            }

            return text;
        }

        public CommandData SaveCommandData(bool collection)
        {
            string commandLines = GetCommandLines();
            CommandData commandData = null;
            if (m_fastMode)
            {
                m_subsystemCmdRodBehavior.m_commandLine = commandLines;
                commandData = new CommandData(Point3.Zero, commandLines);
                commandData.TrySetValue();
                m_subsystemCmdRodBehavior.InitPointDataWidget();
            }
            else
            {
                commandData = m_subsystemCmdBlockBehavior.SetCommandData(m_position, commandLines);
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(m_position.X, m_position.Y, m_position.Z);
                m_subsystemTerrain.ChangeCell(m_position.X, m_position.Y, m_position.Z, CommandBlock.SetWorkingMode(cellValue, commandData.Mode));
            }

            HistoryEditItem historyEditItem = GetHistoryEditItem(commandData, collection);
            if (historyEditItem != null)
            {
                InstructionManager.AddHistoryItem(historyEditItem);
            }

            return commandData;
        }

        public HistoryEditItem GetHistoryEditItem(CommandData commandData, bool collection)
        {
            HistoryEditItem historyEditItem = new HistoryEditItem();
            Instruction instruction = InstructionManager.GetInstruction(m_name, m_conditionMode);
            if (instruction == null)
            {
                return null;
            }

            historyEditItem.About = instruction.About.Replace("\r", "");
            historyEditItem.Line = commandData.Line;
            historyEditItem.Position = (m_fastMode ? new Point3(m_componentPlayer.ComponentBody.Position) : commandData.Position);
            historyEditItem.Pass = commandData.Valid && !commandData.OutRange;
            historyEditItem.Condition = commandData.Mode == WorkingMode.Condition;
            historyEditItem.Collection = collection;
            return historyEditItem;
        }

        public void ShowEditTips(string tip, int time = 2)
        {
            m_instructionLabel.Text = "提示：" + tip;
            m_instructionScrollLabel.Text = m_instructionLabel.Text;
            m_instructionLabel.Color = Color.Yellow;
            m_instructionScrollLabel.Color = Color.Yellow;
            m_instructionLabel.IsVisible = true;
            m_instructionScrollPanel.IsVisible = false;
            Time.QueueTimeDelayedExecution(Time.RealTime + (double)time, delegate
            {
                ChangeExplains();
                m_instructionLabel.Color = Color.White;
                m_instructionScrollLabel.Color = Color.White;
            });
        }

        public string GetAbridgeText(string text)
        {
            if (text.Length > 12)
            {
                return text.Substring(0, 5) + "..." + text.Substring(text.Length - 5, 5);
            }

            return text;
        }

        public static void GuiWidgetControl(ComponentPlayer componentPlayer, bool button)
        {
            componentPlayer.ComponentGui.m_lookContainerWidget.IsVisible = button;
            componentPlayer.ComponentGui.m_moveContainerWidget.IsVisible = button;
            componentPlayer.ComponentGui.m_leftControlsContainerWidget.IsVisible = button;
            componentPlayer.ComponentGui.m_rightControlsContainerWidget.IsVisible = button;
            if (componentPlayer.ComponentGui.ShortInventoryWidget.m_inventory != null)
            {
                componentPlayer.ComponentGui.ShortInventoryWidget.IsVisible = button;
            }

            componentPlayer.ComponentInput.AllowHandleInput = button;
        }
    }
}