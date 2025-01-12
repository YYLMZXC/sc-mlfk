using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;

using Game;

namespace Mlfk
{
    public class NotesWidget : CanvasWidget
    {
        public LabelWidget m_contentLabel;

        public ButtonWidget m_okButton;

        public ButtonWidget m_LButton;

        public ButtonWidget m_RButton;

        public ButtonWidget m_editButton;

        public ButtonWidget m_removeButton;

        public ButtonWidget m_addButton;

        public RectangleWidget m_reftRectangle;

        public RectangleWidget m_rightRectangle;

        public static Dictionary<string, string> m_notes;

        public ComponentPlayer m_componentPlayer;

        public int m_index;

        public NotesWidget(ComponentPlayer componentPlayer, Dictionary<string, string> notes)
        {
            m_componentPlayer = componentPlayer;
            m_notes = notes;
            XElement node = ContentManager.Get<XElement>("Widgets/NotesWidget");
            LoadContents(this, node);
            m_LButton = Children.Find<ButtonWidget>("LButton");
            m_RButton = Children.Find<ButtonWidget>("RButton");
            m_editButton = Children.Find<ButtonWidget>("EditButton");
            m_removeButton = Children.Find<ButtonWidget>("RemoveButton");
            m_addButton = Children.Find<ButtonWidget>("AddButton");
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_contentLabel = Children.Find<LabelWidget>("Content");
            m_reftRectangle = Children.Find<RectangleWidget>("Left");
            m_rightRectangle = Children.Find<RectangleWidget>("Right");
            m_index = 0;
            m_contentLabel.Text = ShowText(m_index);
        }

        public override void Update()
        {
            m_LButton.IsEnabled = m_index > 0;
            m_RButton.IsEnabled = m_index < m_notes.Count - 1;
            m_reftRectangle.FillColor = ((m_index > 0) ? new Color(100, 100, 100) : new Color(64, 64, 64));
            m_rightRectangle.FillColor = ((m_index < m_notes.Count - 1) ? new Color(100, 100, 100) : new Color(64, 64, 64));
            if (m_LButton.IsClicked)
            {
                m_index--;
                KeyValuePair<string, string> keyValuePair = m_notes.ElementAt(m_index);
                m_contentLabel.Text = ShowText(m_index);
            }

            if (m_RButton.IsClicked)
            {
                m_index++;
                KeyValuePair<string, string> keyValuePair2 = m_notes.ElementAt(m_index);
                m_contentLabel.Text = ShowText(m_index);
            }

            if (m_editButton.IsClicked)
            {
                if (m_notes.Count == 0)
                {
                    return;
                }

                KeyValuePair<string, string> j = m_notes.ElementAt(m_index);
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new CommandEditNotesDialog(this, j.Key, delegate (string t, string s)
                {
                    try
                    {
                        if (s != null)
                        {
                            if (t != j.Key)
                            {
                                m_notes.Remove(j.Key);
                            }

                            m_notes[t] = s;
                            m_index = m_notes.Keys.FirstIndex(t);
                            m_contentLabel.Text = ShowText(m_index);
                        }
                    }
                    catch
                    {
                    }
                }));
            }

            if (m_removeButton.IsClicked)
            {
                if (m_notes.Count == 0)
                {
                    return;
                }

                KeyValuePair<string, string> i = m_notes.ElementAt(m_index);
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog($"是否删除标题为{i.Key}的笔记", string.Empty, "确定", "取消", delegate (MessageDialogButton button)
                {
                    if (button == MessageDialogButton.Button1)
                    {
                        m_notes.Remove(i.Key);
                        m_index = 0;
                        m_contentLabel.Text = ShowText(m_index);
                    }
                }));
            }

            if (m_addButton.IsClicked)
            {
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new TextBoxDialog("请输入笔记标题", string.Empty, 256, delegate (string title)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(title))
                        {
                            if (m_notes.TryGetValue(title, out var value))
                            {
                                m_notes[title] = value;
                            }
                            else
                            {
                                m_notes[title] = string.Empty;
                            }

                            DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new CommandEditNotesDialog(this, title, delegate (string t, string s)
                            {
                                try
                                {
                                    if (s != null)
                                    {
                                        if (t != title)
                                        {
                                            m_notes.Remove(title);
                                        }

                                        m_notes[t] = s;
                                        m_index = m_notes.Keys.FirstIndex(t);
                                        m_contentLabel.Text = ShowText(m_index);
                                    }
                                }
                                catch
                                {
                                }
                            }));
                        }
                    }
                    catch
                    {
                    }
                }));
            }

            if (m_okButton.IsClicked)
            {
                base.ParentWidget.RemoveChildren(this);
            }
        }

        public string ShowText(int index)
        {
            if (m_notes.Count > 0)
            {
                KeyValuePair<string, string> keyValuePair = m_notes.ElementAt(index);
                return "\n标题：" + keyValuePair.Key + "\n\n" + keyValuePair.Value.Replace("[n]", "\n").Replace("\t", "");
            }

            return "\n\n未找到编写过的笔记，请添加笔记内容";
        }
    }
}