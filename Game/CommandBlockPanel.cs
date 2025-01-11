using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;

namespace Game
{
	public class CommandBlockPanel : CanvasWidget
	{
		public CommandBlockSelectionWidget m_creativeInventoryWidget;

		public ComponentCreativeInventory m_componentCreativeInventory;

		public List<int> m_slotIndices = new List<int>();

		public GridPanelWidget m_inventoryGrid;

		public int m_pagesCount;

		public int m_assignedCategoryIndex = -1;

		public int m_assignedPageIndex = -1;

		public CommandBlockPanel(CommandBlockSelectionWidget creativeInventoryWidget)
		{
			m_creativeInventoryWidget = creativeInventoryWidget;
			m_componentCreativeInventory = creativeInventoryWidget.Entity.FindComponent<ComponentCreativeInventory>(true);
			XElement node = ContentManager.Get<XElement>("Widgets/CreativeInventoryPanel");
			LoadContents(this, node);
			m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
			for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
			{
				for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
				{
					XElement node2 = ContentManager.Get<XElement>("Widgets/CommandBlockSlot");
					ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
					BlockIconWidget blockIconWidget = containerWidget.Children.Find<BlockIconWidget>("BlockSlot.Icon");
					blockIconWidget.Value = 0;
					m_inventoryGrid.Children.Add(containerWidget);
					m_inventoryGrid.SetWidgetCell(containerWidget, new Point2(j, i));
				}
			}
		}

		public override void Update()
		{
			if (m_assignedCategoryIndex >= 0)
			{
				if (base.Input.Scroll.HasValue)
				{
					Widget widget = HitTestGlobal(base.Input.Scroll.Value.XY);
					if (widget != null && widget.IsChildWidgetOf(m_inventoryGrid))
					{
						m_componentCreativeInventory.PageIndex -= (int)base.Input.Scroll.Value.Z;
					}
				}
				if (m_creativeInventoryWidget.PageDownButton.IsClicked)
				{
					ComponentCreativeInventory componentCreativeInventory = m_componentCreativeInventory;
					int pageIndex = componentCreativeInventory.PageIndex + 1;
					componentCreativeInventory.PageIndex = pageIndex;
				}
				if (m_creativeInventoryWidget.PageUpButton.IsClicked)
				{
					ComponentCreativeInventory componentCreativeInventory2 = m_componentCreativeInventory;
					int pageIndex = componentCreativeInventory2.PageIndex - 1;
					componentCreativeInventory2.PageIndex = pageIndex;
				}
				m_componentCreativeInventory.PageIndex = ((m_pagesCount > 0) ? MathUtils.Clamp(m_componentCreativeInventory.PageIndex, 0, m_pagesCount - 1) : 0);
			}
			if (m_componentCreativeInventory.CategoryIndex != m_assignedCategoryIndex)
			{
				if (m_creativeInventoryWidget.GetCategoryName(m_componentCreativeInventory.CategoryIndex) == LanguageControl.Get("CreativeInventoryWidget", 2))
				{
					m_slotIndices = new List<int>(Enumerable.Range(10, m_componentCreativeInventory.OpenSlotsCount - 10));
				}
				else
				{
					m_slotIndices.Clear();
					for (int i = m_componentCreativeInventory.OpenSlotsCount; i < m_componentCreativeInventory.SlotsCount; i++)
					{
						int slotValue = m_componentCreativeInventory.GetSlotValue(i);
						int num = Terrain.ExtractContents(slotValue);
						if (BlocksManager.Blocks[num].GetCategory(slotValue) == m_creativeInventoryWidget.GetCategoryName(m_componentCreativeInventory.CategoryIndex))
						{
							m_slotIndices.Add(i);
						}
					}
				}
				int num2 = m_inventoryGrid.ColumnsCount * m_inventoryGrid.RowsCount;
				m_pagesCount = (m_slotIndices.Count + num2 - 1) / num2;
				m_assignedCategoryIndex = m_componentCreativeInventory.CategoryIndex;
				m_assignedPageIndex = -1;
				m_componentCreativeInventory.PageIndex = 0;
			}
			if (m_componentCreativeInventory.PageIndex != m_assignedPageIndex)
			{
				int num3 = m_inventoryGrid.ColumnsCount * m_inventoryGrid.RowsCount;
				int num4 = m_componentCreativeInventory.PageIndex * num3;
				foreach (Widget child in m_inventoryGrid.Children)
				{
					ContainerWidget containerWidget = child as ContainerWidget;
					BlockIconWidget blockIconWidget = containerWidget.Children.Find<BlockIconWidget>("BlockSlot.Icon");
					if (containerWidget != null)
					{
						if (num4 < m_slotIndices.Count)
						{
							blockIconWidget.Value = m_componentCreativeInventory.GetSlotValue(m_slotIndices[num4++]);
						}
						else
						{
							blockIconWidget.Value = 0;
						}
					}
				}
				m_assignedPageIndex = m_componentCreativeInventory.PageIndex;
			}
			foreach (Widget child2 in m_inventoryGrid.Children)
			{
				ContainerWidget containerWidget2 = child2 as ContainerWidget;
				if (containerWidget2 != null)
				{
					ButtonWidget buttonWidget = containerWidget2.Children.Find<ButtonWidget>("BlockSlot.Button");
					if (buttonWidget.IsClicked)
					{
						BlockIconWidget blockIconWidget2 = containerWidget2.Children.Find<BlockIconWidget>("BlockSlot.Icon");
						TextBoxWidget textBoxWidget = (TextBoxWidget)m_creativeInventoryWidget.m_commandEditWidget.m_commandWidgetDatas["TextBox$" + m_creativeInventoryWidget.m_para];
						BlockIconWidget blockIconWidget3 = (BlockIconWidget)m_creativeInventoryWidget.m_commandEditWidget.m_commandWidgetDatas["BlockIcon$" + m_creativeInventoryWidget.m_para];
						textBoxWidget.Text = blockIconWidget2.Value.ToString() ?? "";
						blockIconWidget3.Value = blockIconWidget2.Value;
						m_creativeInventoryWidget.CloseCurrentWidget();
					}
				}
			}
			if (m_componentCreativeInventory.PageIndex < m_pagesCount - 1)
			{
				m_creativeInventoryWidget.PageDownButton.IsEnabled = true;
				m_creativeInventoryWidget.m_pageDownRectangle.FillColor = new Color(100, 100, 100);
			}
			else
			{
				m_creativeInventoryWidget.PageDownButton.IsEnabled = false;
				m_creativeInventoryWidget.m_pageDownRectangle.FillColor = new Color(80, 80, 80);
			}
			if (m_componentCreativeInventory.PageIndex > 0)
			{
				m_creativeInventoryWidget.PageUpButton.IsEnabled = true;
				m_creativeInventoryWidget.m_pageUpRectangle.FillColor = new Color(100, 100, 100);
			}
			else
			{
				m_creativeInventoryWidget.PageUpButton.IsEnabled = false;
				m_creativeInventoryWidget.m_pageUpRectangle.FillColor = new Color(80, 80, 80);
			}
		}
	}
}
