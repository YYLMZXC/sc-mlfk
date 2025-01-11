using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game
{
	public class CommandSliderWidget : CanvasWidget
	{
		public CanvasWidget m_canvasWidget;

		public CanvasWidget m_labelCanvasWidget;

		public Widget m_tabWidget;

		public TextBoxWidget m_textBoxWidget;

		public float m_minValue;

		public float m_maxValue = 1f;

		public float m_granularity = 1f;

		public float m_value;

		public float m_lastValue;

		public Vector2? m_dragStartPoint;

		public bool IsSliding { get; set; }

		public LayoutDirection LayoutDirection { get; set; }

		public float MinValue
		{
			get
			{
				return m_minValue;
			}
			set
			{
				if (value != m_minValue)
				{
					m_minValue = value;
					MaxValue = MathUtils.Max(MinValue, MaxValue);
					Value = MathUtils.Clamp(Value, MinValue, MaxValue);
				}
			}
		}

		public float MaxValue
		{
			get
			{
				return m_maxValue;
			}
			set
			{
				if (value != m_maxValue)
				{
					m_maxValue = value;
					MinValue = MathUtils.Min(MinValue, MaxValue);
					Value = MathUtils.Clamp(Value, MinValue, MaxValue);
				}
			}
		}

		public float Value
		{
			get
			{
				return m_value;
			}
			set
			{
				m_lastValue = m_value;
				m_value = ((m_granularity > 0f) ? (MathUtils.Round(MathUtils.Clamp(value, MinValue, MaxValue) / m_granularity) * m_granularity) : MathUtils.Clamp(value, MinValue, MaxValue));
			}
		}

		public float Granularity
		{
			get
			{
				return m_granularity;
			}
			set
			{
				m_granularity = MathUtils.Max(value, 0f);
			}
		}

		public string Text
		{
			get
			{
				return m_textBoxWidget.Text;
			}
			set
			{
				m_textBoxWidget.Text = value;
			}
		}

		public BitmapFont Font
		{
			get
			{
				return m_textBoxWidget.Font;
			}
			set
			{
				m_textBoxWidget.Font = value;
			}
		}

		public string SoundName { get; set; }

		public bool IsLabelVisible
		{
			get
			{
				return m_labelCanvasWidget.IsVisible;
			}
			set
			{
				m_labelCanvasWidget.IsVisible = value;
			}
		}

		public float LabelWidth
		{
			get
			{
				return m_labelCanvasWidget.Size.X;
			}
			set
			{
				m_labelCanvasWidget.Size = new Vector2(value, m_labelCanvasWidget.Size.Y);
			}
		}

		public CommandSliderWidget()
		{
			XElement node = ContentManager.Get<XElement>("Widgets/CommandSliderContents");
			LoadChildren(this, node);
			m_canvasWidget = Children.Find<CanvasWidget>("Slider.Canvas");
			m_labelCanvasWidget = Children.Find<CanvasWidget>("Slider.LabelCanvas");
			m_tabWidget = Children.Find<Widget>("Slider.Tab");
			m_textBoxWidget = Children.Find<TextBoxWidget>("Slider.TextBox");
			LoadProperties(this, node);
			m_lastValue = m_value;
			m_textBoxWidget.TextChanged += delegate(TextBoxWidget textBox)
			{
				int result;
				if (int.TryParse(textBox.Text, out result))
				{
					if ((float)result >= MinValue && (float)result <= MaxValue)
					{
						m_lastValue = result;
						m_value = result;
					}
					else if ((float)result < MinValue)
					{
						m_lastValue = MinValue;
						m_value = MinValue;
					}
					else if ((float)result > MaxValue)
					{
						m_lastValue = MaxValue;
						m_value = MaxValue;
					}
				}
				else
				{
					m_lastValue = MinValue;
					m_value = MinValue;
				}
			};
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.MeasureOverride(parentAvailableSize);
			base.IsDrawRequired = true;
		}

		public override void ArrangeOverride()
		{
			base.ArrangeOverride();
			float num = ((LayoutDirection == LayoutDirection.Horizontal) ? m_canvasWidget.ActualSize.X : m_canvasWidget.ActualSize.Y);
			float num2 = ((LayoutDirection == LayoutDirection.Horizontal) ? m_tabWidget.ActualSize.X : m_tabWidget.ActualSize.Y);
			float num3 = ((MaxValue > MinValue) ? ((Value - MinValue) / (MaxValue - MinValue)) : 0f);
			if (LayoutDirection == LayoutDirection.Horizontal)
			{
				Vector2 zero = Vector2.Zero;
				zero.X = num3 * (num - num2);
				zero.Y = MathUtils.Max((base.ActualSize.Y - m_tabWidget.ActualSize.Y) / 2f, 0f);
				m_canvasWidget.SetWidgetPosition(m_tabWidget, zero);
			}
			else
			{
				Vector2 zero2 = Vector2.Zero;
				zero2.X = MathUtils.Max(base.ActualSize.X - m_tabWidget.ActualSize.X, 0f) / 2f;
				zero2.Y = num3 * (num - num2);
				m_canvasWidget.SetWidgetPosition(m_tabWidget, zero2);
			}
			base.ArrangeOverride();
		}

		public override void Update()
		{
			if (m_value != m_lastValue)
			{
				m_lastValue = m_value;
				m_textBoxWidget.Text = m_value.ToString() ?? "";
			}
			float num = ((LayoutDirection == LayoutDirection.Horizontal) ? m_canvasWidget.ActualSize.X : m_canvasWidget.ActualSize.Y);
			float num2 = ((LayoutDirection == LayoutDirection.Horizontal) ? m_tabWidget.ActualSize.X : m_tabWidget.ActualSize.Y);
			if (base.Input.Tap.HasValue && HitTestGlobal(base.Input.Tap.Value) == m_tabWidget)
			{
				m_dragStartPoint = ScreenToWidget(base.Input.Press.Value);
			}
			if (base.Input.Press.HasValue)
			{
				if (m_dragStartPoint.HasValue)
				{
					Vector2 vector = ScreenToWidget(base.Input.Press.Value);
					float value = Value;
					if (LayoutDirection == LayoutDirection.Horizontal)
					{
						float f = (vector.X - num2 / 2f) / (num - num2);
						Value = MathUtils.Lerp(MinValue, MaxValue, f);
					}
					else
					{
						float f2 = (vector.Y - num2 / 2f) / (num - num2);
						Value = MathUtils.Lerp(MinValue, MaxValue, f2);
					}
					if (Value != value && m_granularity > 0f && !string.IsNullOrEmpty(SoundName))
					{
						AudioManager.PlaySound(SoundName, 1f, 0f, 0f);
					}
				}
			}
			else
			{
				m_dragStartPoint = null;
			}
			IsSliding = m_dragStartPoint.HasValue && base.IsEnabledGlobal && base.IsVisibleGlobal;
			if (m_dragStartPoint.HasValue)
			{
				base.Input.Clear();
			}
		}
	}
}
