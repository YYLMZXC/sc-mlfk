using System.Xml.Linq;
using Engine;

using Game;

namespace Mlfk
{
    public class CommandSwitchWidget : CanvasWidget
    {
        public ButtonWidget m_falseButton;

        public ButtonWidget m_trueButton;

        public RectangleWidget m_falseRectangle;

        public RectangleWidget m_trueRectangle;

        public string m_checkLabelText;

        public CommandSwitchWidget()
        {
            XElement node = ContentManager.Get<XElement>("Widgets/CommandSwitchWidget");
            LoadContents(this, node);
            m_falseButton = Children.Find<ButtonWidget>("CommandData.FalseButton");
            m_trueButton = Children.Find<ButtonWidget>("CommandData.TrueButton");
            m_falseRectangle = Children.Find<RectangleWidget>("CommandData.FalseRectangle");
            m_trueRectangle = Children.Find<RectangleWidget>("CommandData.TrueRectangle");
            m_checkLabelText = "true";
        }

        public override void Update()
        {
            if (m_checkLabelText == "true")
            {
                m_trueRectangle.FillColor = new Color(100, 100, 100);
                m_falseRectangle.FillColor = new Color(80, 80, 80);
            }
            else
            {
                m_falseRectangle.FillColor = new Color(100, 100, 100);
                m_trueRectangle.FillColor = new Color(80, 80, 80);
            }

            if (m_falseButton.IsClicked)
            {
                m_checkLabelText = "false";
            }

            if (m_trueButton.IsClicked)
            {
                m_checkLabelText = "true";
            }
        }
    }
}