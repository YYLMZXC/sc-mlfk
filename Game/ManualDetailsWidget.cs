using System.Xml.Linq;

namespace Game
{
    public class ManualDetailsWidget : CanvasWidget
    {
        public LabelWidget m_titleLabel;

        public LabelWidget m_detailsLabel;

        public ButtonWidget m_lastButton;

        public ButtonWidget m_nextButton;

        public ButtonWidget m_backButton;

        public ComponentPlayer m_componentPlayer;

        public int m_topicIndex;

        public float parentScroll;

        public ManualDetailsWidget(ComponentPlayer componentPlayer, ManualTopicWidget.InformationTopic informationTopic, float scroll)
        {
            m_componentPlayer = componentPlayer;
            m_topicIndex = informationTopic.Index;
            parentScroll = scroll;
            XElement node = ContentManager.Get<XElement>("Widgets/ManualDetailsWidget");
            LoadContents(this, node);
            m_backButton = Children.Find<ButtonWidget>("BackButton");
            m_lastButton = Children.Find<ButtonWidget>("LastButton");
            m_nextButton = Children.Find<ButtonWidget>("NextButton");
            m_titleLabel = Children.Find<LabelWidget>("InformationTopic.Title");
            m_detailsLabel = Children.Find<LabelWidget>("InformationTopic.Details");
            m_titleLabel.Text = informationTopic.Title;
            m_detailsLabel.Text = informationTopic.Details;
        }

        public override void Update()
        {
            if (m_lastButton.IsClicked)
            {
                ManualTopicWidget.InformationTopic informationTopicByIndex = ManualTopicWidget.GetInformationTopicByIndex(m_topicIndex - 1);
                if (informationTopicByIndex != null)
                {
                    m_titleLabel.Text = informationTopicByIndex.Title;
                    m_detailsLabel.Text = informationTopicByIndex.Details;
                    m_topicIndex--;
                }
            }

            if (m_nextButton.IsClicked)
            {
                ManualTopicWidget.InformationTopic informationTopicByIndex2 = ManualTopicWidget.GetInformationTopicByIndex(m_topicIndex + 1);
                if (informationTopicByIndex2 != null)
                {
                    m_titleLabel.Text = informationTopicByIndex2.Title;
                    m_detailsLabel.Text = informationTopicByIndex2.Details;
                    m_topicIndex++;
                }
            }

            if (m_backButton.IsClicked)
            {
                m_componentPlayer.ComponentGui.ModalPanelWidget = new ManualTopicWidget(m_componentPlayer, parentScroll);
            }
        }
    }
}