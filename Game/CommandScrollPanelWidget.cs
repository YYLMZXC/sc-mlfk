using Engine;

namespace Game
{
    public class CommandScrollPanelWidget : ScrollPanelWidget
    {
        public override float CalculateScrollAreaLength()
        {
            float num = 0f;
            foreach (Widget child in Children)
            {
                if (child.IsVisible)
                {
                    num = MathUtils.Max(num, child.ParentDesiredSize.X + 2f * child.Margin.X - 600f);
                }
            }

            return num;
        }
    }
}