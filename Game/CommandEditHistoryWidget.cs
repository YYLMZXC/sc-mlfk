using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game
{
    public class CommandEditHistoryWidget : CanvasWidget
    {
        public ButtonWidget m_okButton;

        public ButtonWidget m_filterButton;

        public ButtonWidget m_clearButton;

        public LabelWidget m_filterLabel;

        public LabelWidget m_removeLabel;

        public RectangleWidget m_removeRectangle;

        public ListPanelWidget m_instructionlistWidget;

        public CommandEditWidget m_commandEditWidget;

        public bool m_filter;

        public bool m_fastMode;

        public CommandEditHistoryWidget(CommandEditWidget commandEditWidget, Point3 pos, bool fast)
        {
            CommandEditHistoryWidget commandEditHistoryWidget = this;
            m_commandEditWidget = commandEditWidget;
            m_commandEditWidget.IsEnabled = false;
            m_fastMode = fast;
            m_filter = true;
            XElement node = ContentManager.Get<XElement>("Widgets/CommandEditHistoryWidget");
            LoadContents(this, node);
            m_instructionlistWidget = Children.Find<ListPanelWidget>("EditHistory.List");
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_filterButton = Children.Find<ButtonWidget>("FilterButton");
            m_clearButton = Children.Find<ButtonWidget>("ClearButton");
            m_filterLabel = Children.Find<LabelWidget>("FilterLabel");
            m_removeLabel = Children.Find<LabelWidget>("RemoveLabel");
            m_removeRectangle = Children.Find<RectangleWidget>("RemoveRectangle");
            UpdateItems();
            m_instructionlistWidget.ItemWidgetFactory = delegate (object item)
            {
                HistoryEditItem historyEditItem2 = (HistoryEditItem)item;
                XElement node2 = ContentManager.Get<XElement>("Widgets/HistoryEditItem");
                ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(commandEditHistoryWidget, node2, null);
                string text = $"{historyEditItem2.About}   编辑位置:({historyEditItem2.Position.ToString()})";
                Color fillColor = (historyEditItem2.Pass ? new Color(32, 255, 32, 160) : new Color(255, 32, 32, 160));
                containerWidget.Children.Find<RectangleWidget>("PassRectangle").FillColor = fillColor;
                containerWidget.Children.Find<LabelWidget>("HistoryEditItem.About").Text = text;
                containerWidget.Children.Find<LabelWidget>("HistoryEditItem.Line").Text = historyEditItem2.Line;
                return containerWidget;
            };
            ListPanelWidget instructionlistWidget = m_instructionlistWidget;
            instructionlistWidget.ItemClicked = (Action<object>)Delegate.Combine(instructionlistWidget.ItemClicked, (Action<object>)delegate (object item)
            {
                if (commandEditHistoryWidget.m_instructionlistWidget.SelectedItem == item)
                {
                    HistoryEditItem historyEditItem = (HistoryEditItem)item;
                    if (historyEditItem != null)
                    {
                        CommandData commandData = new CommandData(commandEditHistoryWidget.m_fastMode ? Point3.Zero : pos, historyEditItem.Line);
                        commandData.TrySetValue();
                        commandEditHistoryWidget.m_commandEditWidget.Initialize();
                        commandEditHistoryWidget.m_commandEditWidget.SetCommandDataValue(commandData);
                        commandEditHistoryWidget.m_commandEditWidget.ChangeExplains();
                        commandEditHistoryWidget.m_commandEditWidget.GetDynamicCommanndWidget();
                        commandEditHistoryWidget.m_commandEditWidget.SetCommandWidgetValue(commandData);
                        commandEditHistoryWidget.CloseCurrentWidget();
                    }
                }
            });
        }

        public override void Update()
        {
            m_filterLabel.Text = (m_filter ? "收藏" : "全部");
            m_removeLabel.Text = (m_filter ? "移除" : "清空");
            if (m_filter)
            {
                if (m_instructionlistWidget.SelectedItem == null)
                {
                    m_removeRectangle.FillColor = new Color(64, 64, 64);
                    m_clearButton.IsEnabled = false;
                }
                else
                {
                    m_removeRectangle.FillColor = new Color(100, 100, 100);
                    m_clearButton.IsEnabled = true;
                }
            }
            else
            {
                m_removeRectangle.FillColor = new Color(100, 100, 100);
                m_clearButton.IsEnabled = true;
            }

            if (m_okButton.IsClicked || base.Input.Cancel || base.Input.Back)
            {
                CloseCurrentWidget();
            }

            if (m_filterButton.IsClicked)
            {
                m_filter = !m_filter;
                UpdateItems();
            }

            if (!m_clearButton.IsClicked)
            {
                return;
            }

            if (m_filter)
            {
                HistoryEditItem historyEditItem = (HistoryEditItem)m_instructionlistWidget.SelectedItem;
                InstructionManager.RemoveHistoryItem(historyEditItem);
                UpdateItems();
                return;
            }

            DialogsManager.ShowDialog(m_commandEditWidget.m_componentPlayer.GuiWidget, new MessageDialog("是否清空所有指令历史记录", "注意：收藏的指令也会被清空", "确定", "取消", delegate (MessageDialogButton button)
            {
                if (button == MessageDialogButton.Button1)
                {
                    InstructionManager.HistoryEditInstructions.Clear();
                    InstructionManager.CollectionInstructions.Clear();
                    m_instructionlistWidget.ClearItems();
                }
            }));
        }

        public void UpdateItems()
        {
            m_instructionlistWidget.ClearItems();
            if (m_filter)
            {
                List<HistoryEditItem> collectionInstructions = InstructionManager.CollectionInstructions;
                for (int num = collectionInstructions.Count - 1; num >= 0; num--)
                {
                    if (!m_fastMode || !collectionInstructions[num].Condition)
                    {
                        m_instructionlistWidget.AddItem(collectionInstructions[num]);
                    }
                }

                return;
            }

            List<HistoryEditItem> historyEditInstructions = InstructionManager.HistoryEditInstructions;
            int num2 = 0;
            for (int num3 = historyEditInstructions.Count - 1; num3 >= 0; num3--)
            {
                if (!m_fastMode || !historyEditInstructions[num3].Condition)
                {
                    m_instructionlistWidget.AddItem(historyEditInstructions[num3]);
                    if (++num2 >= 50)
                    {
                        break;
                    }
                }
            }
        }

        public void CloseCurrentWidget()
        {
            base.ParentWidget.RemoveChildren(this);
            Time.QueueTimeDelayedExecution(Time.RealTime + 0.5, delegate
            {
                m_commandEditWidget.IsEnabled = true;
            });
        }
    }
}