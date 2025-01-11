using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Game;
namespace Mlfk
{
    public class CommandEntitySelectionWidget : CanvasWidget
    {
        public ButtonWidget m_selectionCancelButton;

        public ButtonWidget m_orderButton;

        public ListPanelWidget m_entitylistWidget;

        public CommandEditWidget m_commandEditWidget;

        public static List<EntityInfo> EntityInfoDescList = new List<EntityInfo>();

        public bool m_direction;

        public string m_para;

        public CommandEntitySelectionWidget(CommandEditWidget commandEditWidget, string para)
        {
            m_commandEditWidget = commandEditWidget;
            m_para = para;
            m_commandEditWidget.IsEnabled = false;
            XElement node = ContentManager.Get<XElement>("Widgets/CommandEntitySelectionWidget");
            LoadContents(this, node);
            m_selectionCancelButton = Children.Find<ButtonWidget>("SelectionCancel");
            m_orderButton = Children.Find<ButtonWidget>("Order");
            m_entitylistWidget = Children.Find<ListPanelWidget>("CreaturesList");
            m_direction = true;
            if (EntityInfoDescList.Count == 0)
            {
                EntityInfo[] array = EntityInfoManager.EntityInfos.Values.ToArray();
                for (int num = array.Length - 1; num >= 0; num--)
                {
                    EntityInfoDescList.Add(array[num]);
                }
            }

            foreach (EntityInfo value in EntityInfoManager.EntityInfos.Values)
            {
                m_entitylistWidget.AddItem(value);
            }

            m_entitylistWidget.ItemWidgetFactory = delegate (object item)
            {
                EntityInfo entityInfo2 = (EntityInfo)item;
                XElement node2 = ContentManager.Get<XElement>("Widgets/CommandEntityItem");
                ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
                LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("ModelLabel");
                labelWidget.Text = entityInfo2.KeyName + " : " + entityInfo2.DisplayName;
                ModelWidget modelWidget2 = containerWidget.Children.Find<ModelWidget>("ModelIcon");
                EntityInfoManager.ChangeModelDisplay(ref modelWidget2, entityInfo2.Model, entityInfo2.Texture);
                return containerWidget;
            };
            ListPanelWidget entitylistWidget = m_entitylistWidget;
            entitylistWidget.ItemClicked = (Action<object>)Delegate.Combine(entitylistWidget.ItemClicked, (Action<object>)delegate (object item)
            {
                if (item != null)
                {
                    EntityInfo entityInfo = (EntityInfo)item;
                    TextBoxWidget textBoxWidget = (TextBoxWidget)m_commandEditWidget.m_commandWidgetDatas["TextBox$" + m_para];
                    ModelWidget modelWidget = (ModelWidget)m_commandEditWidget.m_commandWidgetDatas["Model$" + m_para];
                    textBoxWidget.Text = entityInfo.KeyName;
                    EntityInfoManager.ChangeModelDisplay(ref modelWidget, entityInfo.Model, entityInfo.Texture);
                    CloseCurrentWidget();
                }
            });
        }

        public override void Update()
        {
            if (m_orderButton.IsClicked)
            {
                m_direction = !m_direction;
                m_entitylistWidget.ClearItems();
                if (m_direction)
                {
                    foreach (EntityInfo value in EntityInfoManager.EntityInfos.Values)
                    {
                        m_entitylistWidget.AddItem(value);
                    }
                }
                else
                {
                    foreach (EntityInfo entityInfoDesc in EntityInfoDescList)
                    {
                        m_entitylistWidget.AddItem(entityInfoDesc);
                    }
                }
            }

            if (m_selectionCancelButton.IsClicked || base.Input.Cancel || base.Input.Back)
            {
                CloseCurrentWidget();
            }
        }

        public void CloseCurrentWidget()
        {
            Time.QueueTimeDelayedExecution(Time.RealTime + 0.05000000074505806, delegate
            {
                base.ParentWidget.RemoveChildren(this);
            });
            Time.QueueTimeDelayedExecution(Time.RealTime + 0.5, delegate
            {
                m_commandEditWidget.IsEnabled = true;
            });
        }
    }
}