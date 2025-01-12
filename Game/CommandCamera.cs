using Engine;

using Game;

namespace Mlfk
{
    public class CommandCamera : BasePerspectiveCamera
    {
        public enum CameraType
        {
            Lock,
            Aero,
            MovePos,
            MoveDirect,
            MoveWithPlayer
        }

        public Vector3 m_position;

        public Vector3 m_direction;

        public Vector3 m_targetPosition;

        public Vector3 m_targetDirection;

        public Vector3 m_relativePosition;

        public Point2 m_relativeAngle;

        public CameraType m_type;

        public float m_speed;

        public bool m_skipToAero;

        private Vector3 m_velocity;

        private bool m_usesMovementControls;

        private bool m_isEntityControlEnabled;

        public override bool UsesMovementControls => m_usesMovementControls;

        public override bool IsEntityControlEnabled => m_isEntityControlEnabled;

        public CommandCamera(GameWidget gameWidget, CameraType type)
            : base(gameWidget)
        {
            m_type = type;
            m_velocity = Vector3.Zero;
            m_targetPosition = Vector3.Zero;
            m_targetDirection = Vector3.Zero;
            m_relativePosition = Vector3.Zero;
            m_relativeAngle = Point2.Zero;
            m_speed = 0f;
            m_skipToAero = false;
            switch (m_type)
            {
                case CameraType.Aero:
                    m_usesMovementControls = true;
                    m_isEntityControlEnabled = true;
                    break;
                case CameraType.Lock:
                    m_usesMovementControls = false;
                    m_isEntityControlEnabled = true;
                    break;
                case CameraType.MoveDirect:
                    m_usesMovementControls = false;
                    m_isEntityControlEnabled = false;
                    break;
                case CameraType.MovePos:
                    m_usesMovementControls = false;
                    m_isEntityControlEnabled = false;
                    break;
                case CameraType.MoveWithPlayer:
                    m_usesMovementControls = false;
                    m_isEntityControlEnabled = true;
                    break;
            }
        }

        public override void Activate(Camera previousCamera)
        {
            m_position = previousCamera.ViewPosition;
            m_direction = previousCamera.ViewDirection;
            SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
        }

        public override void Update(float dt)
        {
            if (m_type == CameraType.Aero)
            {
                Vector3 vector = Vector3.Zero;
                Vector2 vector2 = Vector2.Zero;
                ComponentInput componentInput = base.GameWidget.PlayerData.ComponentPlayer?.ComponentInput;
                if (componentInput != null)
                {
                    vector = componentInput.PlayerInput.CameraMove * new Vector3(1f, 0f, 1f);
                    vector2 = componentInput.PlayerInput.CameraLook;
                }

                Vector3 direction = m_direction;
                Vector3 unitY = Vector3.UnitY;
                Vector3 vector3 = Vector3.Normalize(Vector3.Cross(direction, unitY));
                float num = 10f;
                Vector3 zero = Vector3.Zero;
                zero += num * vector.X * vector3;
                zero += num * vector.Y * unitY;
                zero += num * vector.Z * direction;
                m_velocity += 1.5f * (zero - m_velocity) * dt;
                if (MathUtils.Abs(vector.X) < 0.01f && MathUtils.Abs(vector.Z) < 0.01f)
                {
                    m_velocity = new Vector3(0f, 0f, 0f);
                }

                m_position += m_velocity * dt;
                m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(unitY, -4f * vector2.X * dt));
                m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(vector3, 4f * vector2.Y * dt));
                Vector3 up = Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromAxisAngle(m_direction, 0f));
                SetupPerspectiveCamera(m_position, m_direction, up);
            }
            else if (m_type == CameraType.Lock)
            {
                SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            }
            else if (m_type == CameraType.MovePos)
            {
                float num2 = Vector3.Distance(m_targetPosition, m_position);
                if (num2 != 0f)
                {
                    m_position += MathUtils.Min(dt * m_speed, num2) * Vector3.Normalize(m_targetPosition - m_position);
                }
                else if (m_skipToAero)
                {
                    m_type = CameraType.Aero;
                    m_usesMovementControls = true;
                    m_isEntityControlEnabled = true;
                }

                SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            }
            else if (m_type == CameraType.MoveDirect)
            {
                float num3 = Vector3.Dot(m_targetDirection, m_direction) / m_targetDirection.Length() * m_direction.Length();
                if (num3 < 1f)
                {
                    m_direction += Vector3.Normalize(m_targetDirection - m_direction) * dt * m_speed * 0.1f;
                }
                else if (m_skipToAero)
                {
                    m_type = CameraType.Aero;
                    m_usesMovementControls = true;
                    m_isEntityControlEnabled = true;
                }

                SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            }
            else if (m_type == CameraType.MoveWithPlayer)
            {
                ComponentPlayer componentPlayer = base.GameWidget.PlayerData.ComponentPlayer;
                Vector3 playerBodyDirection = DataHandle.GetPlayerBodyDirection(componentPlayer);
                Vector3 vector4 = Vector3.Normalize(new Vector3(playerBodyDirection.X, 0f, playerBodyDirection.Z)) * m_relativePosition.X;
                Vector3 vector5 = new Vector3(0f, 1f, 0f) * m_relativePosition.Y;
                Vector3 vector6 = Vector3.Normalize(new Vector3(0f - playerBodyDirection.Z, 0f, playerBodyDirection.X)) * m_relativePosition.Z;
                m_position = componentPlayer.ComponentBody.Position + vector4 + vector5 + vector6;
                m_direction = DataHandle.EyesToDirection(DataHandle.EyesAdd(DataHandle.GetPlayerEyesAngle(componentPlayer), m_relativeAngle));
                SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            }
        }
    }
}