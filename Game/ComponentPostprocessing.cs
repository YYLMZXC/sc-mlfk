using GameEntitySystem;
using TemplatesDatabase;
using Game;
namespace Mlfk
{
    public class ComponentPostprocessing : Component
    {
        public ComponentPlayer m_componentPlayer;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
        {
            m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>();
        }

        public void Update(float dt)
        {
        }
    }
}