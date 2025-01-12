using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using GameEntitySystem;

namespace Game
{
    public class CommandBlockSelectionWidget : CanvasWidget
    {
        public class Category
        {
            public string Name;

            public Color Color = Color.White;

            public ContainerWidget Panel;
        }

        public List<Category> m_categories = new List<Category>();

        public int m_activeCategoryIndex = -1;

        public ComponentCreativeInventory m_componentCreativeInventory;

        public ButtonWidget m_pageUpButton;

        public ButtonWidget m_pageDownButton;

        public ButtonWidget m_categoryLeftButton;

        public ButtonWidget m_categoryRightButton;

        public ContainerWidget m_panelContainer;

        public LabelWidget m_categoryLabel;

        public RectangleWidget m_categoryLeftRectangle;

        public RectangleWidget m_categoryRightRectangle;

        public RectangleWidget m_pageUpRectangle;

        public RectangleWidget m_pageDownRectangle;

        public ButtonWidget m_blockSelectionCancel;

        public CommandEditWidget m_commandEditWidget;

        public string m_para;

        public static string fName = "CreativeInventoryWidget";

        public Entity Entity => m_componentCreativeInventory.Entity;

        public ButtonWidget PageDownButton => m_pageDownButton;

        public ButtonWidget PageUpButton => m_pageUpButton;

        public CommandBlockSelectionWidget(CommandEditWidget commandEditWidget, Entity entity, string para)
        {
            m_commandEditWidget = commandEditWidget;
            m_para = para;
            m_commandEditWidget.IsEnabled = false;
            m_componentCreativeInventory = entity.FindComponent<ComponentCreativeInventory>(throwOnError: true);
            XElement node = ContentManager.Get<XElement>("Widgets/CommandBlockSelectionWidget");
            LoadContents(this, node);
            m_categoryLeftButton = Children.Find<ButtonWidget>("CategoryLeftButton");
            m_categoryRightButton = Children.Find<ButtonWidget>("CategoryRightButton");
            m_pageUpButton = Children.Find<ButtonWidget>("PageUpButton");
            m_pageDownButton = Children.Find<ButtonWidget>("PageDownButton");
            m_panelContainer = Children.Find<ContainerWidget>("PanelContainer");
            m_categoryLabel = Children.Find<LabelWidget>("CategoryLabel");
            m_categoryLeftRectangle = Children.Find<RectangleWidget>("CategoryLeftRectangle");
            m_categoryRightRectangle = Children.Find<RectangleWidget>("CategoryRightRectangle");
            m_pageUpRectangle = Children.Find<RectangleWidget>("PageUpRectangle");
            m_pageDownRectangle = Children.Find<RectangleWidget>("PageDownRectangle");
            m_blockSelectionCancel = Children.Find<ButtonWidget>("BlockSelectionCancel");
            CommandBlockPanel commandBlockPanel = new CommandBlockPanel(this)
            {
                IsVisible = false
            };
            m_panelContainer.Children.Add(commandBlockPanel);
            foreach (string category in BlocksManager.Categories)
            {
                m_categories.Add(new Category
                {
                    Name = category,
                    Panel = commandBlockPanel
                });
            }
        }

        public string GetCategoryName(int index)
        {
            return m_categories[index].Name;
        }

        public override void Update()
        {
            if (m_categoryLeftButton.IsClicked || base.Input.Left)
            {
                ComponentCreativeInventory componentCreativeInventory = m_componentCreativeInventory;
                int categoryIndex = componentCreativeInventory.CategoryIndex - 1;
                componentCreativeInventory.CategoryIndex = categoryIndex;
            }

            if (m_categoryRightButton.IsClicked || base.Input.Right)
            {
                ComponentCreativeInventory componentCreativeInventory2 = m_componentCreativeInventory;
                int categoryIndex = componentCreativeInventory2.CategoryIndex + 1;
                componentCreativeInventory2.CategoryIndex = categoryIndex;
            }

            m_componentCreativeInventory.CategoryIndex = MathUtils.Clamp(m_componentCreativeInventory.CategoryIndex, 0, m_categories.Count - 1);
            m_categoryLabel.Text = LanguageControl.Get("BlocksManager", m_categories[m_componentCreativeInventory.CategoryIndex].Name);
            if (m_componentCreativeInventory.CategoryIndex > 0)
            {
                m_categoryLeftButton.IsEnabled = true;
                m_categoryLeftRectangle.FillColor = new Color(100, 100, 100);
            }
            else
            {
                m_categoryLeftButton.IsEnabled = false;
                m_categoryLeftRectangle.FillColor = new Color(80, 80, 80);
            }

            if (m_componentCreativeInventory.CategoryIndex < m_categories.Count - 1)
            {
                m_categoryRightButton.IsEnabled = true;
                m_categoryRightRectangle.FillColor = new Color(100, 100, 100);
            }
            else
            {
                m_categoryRightButton.IsEnabled = false;
                m_categoryRightRectangle.FillColor = new Color(80, 80, 80);
            }

            if (m_componentCreativeInventory.CategoryIndex != m_activeCategoryIndex)
            {
                foreach (Category category in m_categories)
                {
                    category.Panel.IsVisible = false;
                }

                m_categories[m_componentCreativeInventory.CategoryIndex].Panel.IsVisible = true;
                m_activeCategoryIndex = m_componentCreativeInventory.CategoryIndex;
            }

            if (m_blockSelectionCancel.IsClicked || base.Input.Cancel || base.Input.Back)
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