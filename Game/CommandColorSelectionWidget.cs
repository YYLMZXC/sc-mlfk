using System.Xml.Linq;
using Engine;
using Game;

namespace Mlfk
{
    public class CommandColorSelectionWidget : CanvasWidget
    {
        public ButtonWidget m_selectionOkButton;

        public ButtonWidget m_selectionCancelButton;

        public RectangleWidget m_colorDisplayRectangle;

        public CommandSliderWidget m_commandSliderR;

        public CommandSliderWidget m_commandSliderG;

        public CommandSliderWidget m_commandSliderB;

        public CommandEditWidget m_commandEditWidget;

        public string m_para;

        public CommandColorSelectionWidget(CommandEditWidget commandEditWidget, string para)
        {
            m_commandEditWidget = commandEditWidget;
            m_para = para;
            m_commandEditWidget.IsEnabled = false;
            XElement node = ContentManager.Get<XElement>("Widgets/CommandColorSelectionWidget");
            LoadContents(this, node);
            m_selectionOkButton = Children.Find<ButtonWidget>("SelectionOk");
            m_selectionCancelButton = Children.Find<ButtonWidget>("SelectionCancel");
            m_colorDisplayRectangle = Children.Find<RectangleWidget>("ColorDisplay");
            m_commandSliderR = Children.Find<CommandSliderWidget>("ColorSlider.R");
            m_commandSliderG = Children.Find<CommandSliderWidget>("ColorSlider.G");
            m_commandSliderB = Children.Find<CommandSliderWidget>("ColorSlider.B");
            m_commandSliderR.IsLabelVisible = false;
            m_commandSliderG.IsLabelVisible = false;
            m_commandSliderB.IsLabelVisible = false;
            m_commandSliderR.MinValue = 0f;
            m_commandSliderG.MinValue = 0f;
            m_commandSliderB.MinValue = 0f;
            m_commandSliderR.MaxValue = 255f;
            m_commandSliderG.MaxValue = 255f;
            m_commandSliderB.MaxValue = 255f;
            RectangleWidget rectangleWidget = (RectangleWidget)commandEditWidget.m_commandWidgetDatas["Rectangle$" + para];
            m_commandSliderR.Value = (int)rectangleWidget.FillColor.R;
            m_commandSliderG.Value = (int)rectangleWidget.FillColor.G;
            m_commandSliderB.Value = (int)rectangleWidget.FillColor.B;
            m_colorDisplayRectangle.FillColor = new Color(80, 80, 80);
        }

        public override void Update()
        {
            m_colorDisplayRectangle.FillColor = new Color((int)m_commandSliderR.Value, (int)m_commandSliderG.Value, (int)m_commandSliderB.Value);
            if (m_selectionOkButton.IsClicked)
            {
                RectangleWidget rectangleWidget = (RectangleWidget)m_commandEditWidget.m_commandWidgetDatas["Rectangle$" + m_para];
                TextBoxWidget textBoxWidget = (TextBoxWidget)m_commandEditWidget.m_commandWidgetDatas["TextBox$" + m_para];
                rectangleWidget.FillColor = m_colorDisplayRectangle.FillColor;
                textBoxWidget.Text = m_colorDisplayRectangle.FillColor.ToString();
                CloseCurrentWidget();
            }

            if (m_selectionCancelButton.IsClicked || base.Input.Cancel || base.Input.Back)
            {
                CloseCurrentWidget();
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