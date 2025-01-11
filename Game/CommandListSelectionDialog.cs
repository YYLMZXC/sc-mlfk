using System;
using System.Collections;
using System.Xml.Linq;
using Engine;

namespace Game
{
	public class CommandListSelectionDialog : Dialog
	{
		public static IEnumerable m_oldItems;

		public Action<object, float> m_selectionHandler;

		public TextBoxWidget m_textBoxWidget;

		public LabelWidget m_titleLabelWidget;

		public ListPanelWidget m_listWidget;

		public CanvasWidget m_contentWidget;

		public ButtonWidget m_selectionCancelButton;

		public double? m_dismissTime;

		public bool m_isDismissed;

		public Vector2 ContentSize
		{
			get
			{
				return m_contentWidget.Size;
			}
			set
			{
				m_contentWidget.Size = value;
			}
		}

		public CommandListSelectionDialog(bool search, IEnumerable items, float itemSize, float scrollPosition, Func<object, Widget> itemWidgetFactory, Action<object, float> selectionHandler, Action<TextBoxWidget, ListPanelWidget> textBoxChange)
		{
			CommandListSelectionDialog commandListSelectionDialog = this;
			m_oldItems = items;
			m_selectionHandler = selectionHandler;
			XElement node = ContentManager.Get<XElement>("Widgets/CommandListSelectionDialog");
			LoadContents(this, node);
			m_titleLabelWidget = Children.Find<LabelWidget>("ListSelectionDialog.Title");
			m_listWidget = Children.Find<ListPanelWidget>("ListSelectionDialog.List");
			m_contentWidget = Children.Find<CanvasWidget>("ListSelectionDialog.Content");
			m_textBoxWidget = Children.Find<TextBoxWidget>("ListSelectionDialog.TextBox");
			m_selectionCancelButton = Children.Find<ButtonWidget>("SelectionCancel");
			Children.Find<StackPanelWidget>("SearchTextBox").IsVisible = search;
			Children.Find<CanvasWidget>("SearchInterval").IsVisible = search;
			m_titleLabelWidget.IsVisible = false;
			m_listWidget.ItemSize = itemSize;
			if (itemWidgetFactory != null)
			{
				m_listWidget.ItemWidgetFactory = itemWidgetFactory;
			}
			foreach (object item in items)
			{
				m_listWidget.AddItem(item);
			}
			int num = m_listWidget.Items.Count;
			float num2;
			while (true)
			{
				if (num >= 0)
				{
					num2 = MathUtils.Min((float)num + 0.5f, m_listWidget.Items.Count);
					if (num2 * itemSize <= m_contentWidget.Size.Y)
					{
						break;
					}
					num--;
					continue;
				}
				return;
			}
			m_contentWidget.Size = new Vector2(m_contentWidget.Size.X, num2 * itemSize);
			m_textBoxWidget.TextChanged += delegate(TextBoxWidget textBox)
			{
				if (textBoxChange != null)
				{
					textBoxChange(textBox, commandListSelectionDialog.m_listWidget);
				}
			};
			m_listWidget.ScrollPosition = scrollPosition;
		}

		public CommandListSelectionDialog(bool search, IEnumerable items, float itemSize, float scrollPosition, Func<object, string> itemToStringConverter, Action<object, float> selectionHandler, Action<TextBoxWidget, ListPanelWidget> textBoxChange)
			: this(search, items, itemSize, scrollPosition, (object item) => new LabelWidget
			{
				Text = itemToStringConverter(item),
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			}, selectionHandler, textBoxChange)
		{
		}

		public override void Update()
		{
			if (base.Input.Back || base.Input.Cancel || m_selectionCancelButton.IsClicked)
			{
				m_dismissTime = 0.0;
			}
			else if (!m_dismissTime.HasValue && m_listWidget.SelectedItem != null)
			{
				m_dismissTime = Time.FrameStartTime + 0.05000000074505806;
			}
			if (m_dismissTime.HasValue && Time.FrameStartTime >= m_dismissTime.Value)
			{
				Dismiss(m_listWidget.SelectedItem);
			}
		}

		public void Dismiss(object result)
		{
			if (!m_isDismissed)
			{
				m_isDismissed = true;
				DialogsManager.HideDialog(this);
				if (m_selectionHandler != null && result != null)
				{
					m_selectionHandler(result, m_listWidget.ScrollPosition);
				}
			}
		}
	}
}
