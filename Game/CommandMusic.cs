using Engine;
using Engine.Audio;

namespace Game
{
	public class CommandMusic
	{
		public string Name;

		public Sound Sound;

		public CommandMusic(string name, Sound sound)
		{
			Name = name;
			Sound = sound;
		}

		public static float GetRealPitch(int p, ref int o)
		{
			if (p <= 11)
			{
				return GetPitch(p);
			}
			o++;
			return GetPitch(p - 11);
		}

		public static float GetPitch(int p)
		{
			if (p > 11)
			{
				return 1f;
			}
			float n = 1f;
			float num = 0f;
			float num2 = 130.8125f * MathUtils.Pow(1.0594631f, p);
			int num3 = 0;
			for (int i = 4; i <= 6; i++)
			{
				float num4 = num2 / (523.25f * MathUtils.Pow(2f, i - 5));
				if (num3 == 0 || (num4 >= 0.5f && num4 < num))
				{
					num3 = i;
					num = num4;
				}
			}
			if (num != 0f)
			{
				n = MathUtils.Clamp(MathUtils.Log(num) / MathUtils.Log(2f), -1f, 1f);
			}
			return MathUtils.Pow(2f, n);
		}
	}
}
