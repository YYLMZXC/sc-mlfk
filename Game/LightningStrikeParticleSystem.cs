using Engine;
using Engine.Graphics;
using Game;
namespace Mlfk
{
	public class LightningStrikeParticleSystem : ParticleSystem<LightningStrikeParticleSystem.Particle>
	{
		public class Particle : Game.Particle
		{
			public float TimeToLive;

			public Color ShowColor;
		}

		public PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

		public Game.Random m_random = new Game.Random();

		public LightningStrikeParticleSystem(Vector3 position, Color color)
			: base(1)
		{
			Particle particle = base.Particles[0];
			particle.IsActive = true;
			particle.Position = position;
			particle.TimeToLive = 1f;
			particle.ShowColor = color;
		}

		public override bool Simulate(float dt)
		{
			dt = MathUtils.Clamp(dt, 0f, 0.1f);
			bool flag = false;
			for (int i = 0; i < base.Particles.Length; i++)
			{
				Particle particle = base.Particles[i];
				if (particle.IsActive)
				{
					flag = true;
					particle.TimeToLive -= dt;
					if (particle.TimeToLive <= 0f)
					{
						particle.IsActive = false;
					}
				}
			}
			return !flag;
		}

		public override void Draw(Camera camera)
		{
			FlatBatch3D flatBatch3D = m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.DepthRead, null, BlendState.Additive);
			Vector3 unitY = Vector3.UnitY;
			Vector3 vector = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, unitY));
			Viewport viewport = Display.Viewport;
			for (int i = 0; i < base.Particles.Length; i++)
			{
				Particle particle = base.Particles[i];
				if (!particle.IsActive)
				{
					continue;
				}
				Vector3 position = particle.Position;
				float num = Vector4.Transform(new Vector4(position, 1f), camera.ViewProjectionMatrix).W * 2f / ((float)viewport.Width * camera.ProjectionMatrix.M11);
				for (int j = 0; j < (int)(particle.TimeToLive * 30f); j++)
				{
					float num2 = m_random.NormalFloat(0f, 1f * num);
					float num3 = m_random.NormalFloat(0f, 1f * num);
					Vector3 vector2 = num2 * vector + num3 * unitY;
					float num4 = 260f;
					while (num4 > position.Y)
					{
						uint num5 = MathUtils.Hash((uint)(particle.Position.X + 100f * particle.Position.Z + 200f * num4));
						float num6 = MathUtils.Lerp(4f, 10f, (float)(double)(num5 & 0xFF) / 255f);
						float num7 = (((num5 & 1) == 0) ? 1 : (-1));
						float num8 = MathUtils.Lerp(0.05f, 0.2f, (float)(double)((num5 >> 8) & 0xFF) / 255f);
						float num9 = num4;
						float y = num9 - num6 * MathUtils.Lerp(0.45f, 0.55f, (float)(double)((num5 >> 16) & 0xFF) / 255f);
						float y2 = num9 - num6 * MathUtils.Lerp(0.45f, 0.55f, (float)(double)((num5 >> 24) & 0xFF) / 255f);
						float y3 = num9 - num6;
						Vector3 p = new Vector3(position.X, num9, position.Z) + vector2;
						Vector3 vector3 = new Vector3(position.X, y, position.Z) + vector2 - num6 * vector * num7 * num8;
						Vector3 vector4 = new Vector3(position.X, y2, position.Z) + vector2 + num6 * vector * num7 * num8;
						Vector3 p2 = new Vector3(position.X, y3, position.Z) + vector2;
						Color showColor = particle.ShowColor;
						flatBatch3D.QueueLine(p, vector3, showColor, showColor);
						flatBatch3D.QueueLine(vector3, vector4, showColor, showColor);
						flatBatch3D.QueueLine(vector4, p2, showColor, showColor);
						num4 -= num6;
					}
				}
			}
			m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
		}
	}
}
