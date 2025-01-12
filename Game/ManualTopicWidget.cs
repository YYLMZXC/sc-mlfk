using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
    public class ManualTopicWidget : CanvasWidget
    {
        public class InformationTopic
        {
            public string Title;

            public string Details;

            public int Index;
        }

        public static List<InformationTopic> InformationTopicList = new List<InformationTopic>();

        public ListPanelWidget m_informationsWidget;

        public ButtonWidget m_okButton;

        public ComponentPlayer m_componentPlayer;

        public ManualTopicWidget(ComponentPlayer componentPlayer, float scroll)
        {
            m_componentPlayer = componentPlayer;
            XElement node = ContentManager.Get<XElement>("Widgets/ManualTopicWidget");
            LoadContents(this, node);
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_informationsWidget = Children.Find<ListPanelWidget>("Information.List");
            m_informationsWidget.ItemWidgetFactory = delegate (object item)
            {
                InformationTopic informationTopic2 = (InformationTopic)item;
                XElement node2 = ContentManager.Get<XElement>("Widgets/ManualItem");
                ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
                containerWidget.Children.Find<LabelWidget>("InformationTopic.Title").Text = informationTopic2.Title;
                return containerWidget;
            };
            m_informationsWidget.ScrollPosition = scroll;
            ListPanelWidget informationsWidget = m_informationsWidget;
            informationsWidget.ItemClicked = (Action<object>)Delegate.Combine(informationsWidget.ItemClicked, (Action<object>)delegate (object item)
            {
                InformationTopic informationTopic = (InformationTopic)item;
                m_componentPlayer.ComponentGui.ModalPanelWidget = new ManualDetailsWidget(m_componentPlayer, informationTopic, m_informationsWidget.ScrollPosition);
            });
            foreach (InformationTopic informationTopic3 in InformationTopicList)
            {
                m_informationsWidget.AddItem(informationTopic3);
            }
        }

        public override void Update()
        {
            if (m_okButton.IsClicked)
            {
                m_componentPlayer.ComponentGui.ModalPanelWidget = null;
            }
        }

        public static void LoadInformationTopics()
        {
            XElement xElement = ContentManager.Get<XElement>("Information");
            List<XElement> list = xElement.Elements("Topic").ToList();
            int num = 0;
            foreach (XElement item in list)
            {
                InformationTopic informationTopic = new InformationTopic();
                informationTopic.Title = item.Attribute("Title").Value;
                informationTopic.Details = item.Value.Trim('\n').Replace("\t", "");
                informationTopic.Index = num++;
                InformationTopicList.Add(informationTopic);
            }
        }

        public static InformationTopic GetInformationTopicByIndex(int index)
        {
            foreach (InformationTopic informationTopic in InformationTopicList)
            {
                if (informationTopic.Index == index)
                {
                    return informationTopic;
                }
            }

            return null;
        }
    }
}