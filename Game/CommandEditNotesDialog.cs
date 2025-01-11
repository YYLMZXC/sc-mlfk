using System;
using System.Xml.Linq;

namespace Game
{
	public class CommandEditNotesDialog : Dialog
	{
		public Action<string, string> m_handler;

		public LabelWidget m_titleWidget;

		public TextBoxWidget m_textBoxWidget;

		public ButtonWidget m_okButtonWidget;

		public ButtonWidget m_cancelButtonWidget;

		public ButtonWidget m_titleButtonWidget;

		public ButtonWidget m_copyButtonWidget;

		public ButtonWidget m_pasteButtonWidget;

		public ButtonWidget m_clearButtonWidget;

		public NotesWidget m_notesWidget;

		public string m_title;

		public bool AutoHide { get; set; }

		public CommandEditNotesDialog(NotesWidget notesWidget, string title, Action<string, string> handler)
		{
			m_notesWidget = notesWidget;
			m_title = title;
			m_handler = handler;
			XElement node = ContentManager.Get<XElement>("Widgets/CommandEditNotes");
			LoadContents(this, node);
			m_titleWidget = Children.Find<LabelWidget>("TextBoxDialog.Title");
			m_textBoxWidget = Children.Find<TextBoxWidget>("TextBoxDialog.TextBox");
			m_okButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.OkButton");
			m_cancelButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.CancelButton");
			m_titleButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.TitleButton");
			m_copyButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.CopyButton");
			m_pasteButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.PasteButton");
			m_clearButtonWidget = Children.Find<ButtonWidget>("TextBoxDialog.ClearButton");
			m_titleWidget.IsVisible = !string.IsNullOrEmpty(title);
			m_titleWidget.Text = "编辑笔记内容:" + title;
			m_textBoxWidget.MaximumLength = 5000;
			m_textBoxWidget.Text = NotesWidget.m_notes[title] ?? string.Empty;
			m_textBoxWidget.HasFocus = true;
			m_textBoxWidget.Text = m_textBoxWidget.Text.Replace("\n", "[n]");
			m_textBoxWidget.Text = m_textBoxWidget.Text.Replace("\r", "");
			m_textBoxWidget.TextChanged += delegate(TextBoxWidget textBox)
			{
				textBox.Text = textBox.Text.Replace("\n", "[n]");
			};
			m_textBoxWidget.Enter += delegate
			{
				Dismiss(m_textBoxWidget.Text);
			};
			AutoHide = true;
		}

		public override void Update()
		{
			if (base.Input.Cancel)
			{
				Dismiss(null);
			}
			else if (base.Input.Ok)
			{
				Dismiss(m_textBoxWidget.Text);
			}
			else if (m_okButtonWidget.IsClicked)
			{
				Dismiss(m_textBoxWidget.Text);
			}
			else if (m_cancelButtonWidget.IsClicked)
			{
				Dismiss(null);
			}
			if (m_copyButtonWidget.IsClicked)
			{
				ClipboardManager.ClipboardString = m_textBoxWidget.Text;
			}
			if (m_pasteButtonWidget.IsClicked)
			{
				m_textBoxWidget.Text = ClipboardManager.ClipboardString;
			}
			if (m_clearButtonWidget.IsClicked)
			{
				m_textBoxWidget.Text = string.Empty;
			}
			if (!m_titleButtonWidget.IsClicked)
			{
				return;
			}
			DialogsManager.ShowDialog(m_notesWidget.m_componentPlayer.GuiWidget, new TextBoxDialog("请重新输入笔记标题", m_title, 256, delegate(string title)
			{
				if (title != null)
				{
					m_titleWidget.Text = "编辑笔记内容:" + title;
					m_title = title;
				}
			}));
		}

		public void Dismiss(string result)
		{
			if (!string.IsNullOrEmpty(result))
			{
				result = result.Replace("\r", "");
			}
			if (AutoHide)
			{
				DialogsManager.HideDialog(this);
			}
			Action<string, string> handler = m_handler;
			if (handler != null)
			{
				handler(m_title, result);
			}
		}
	}
}
