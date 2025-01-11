using Engine;
using Engine.Graphics;
using Game;
namespace Mlfk
{
    public class CmdRodBlock : Block
    {
        public const int Index = 334;

        private Texture2D m_texture;

        private BlockMesh m_standaloneBlockMesh = new BlockMesh();

        public override void Initialize()
        {
            Model model = ContentManager.Get<Model>("Models/Stick");
            m_texture = ContentManager.Get<Texture2D>("Textures/Lightstick");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Stick").ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Stick").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
        {
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
        {
            BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, m_texture, color, 2f * size, ref matrix, environmentData);
        }
    }
}