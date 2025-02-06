using Godot;
using Hypernex.CCK.GodotVersion.AssetTypes;

namespace Hypernex.CCK.GodotVersion.ComponentTypes
{
    public class MeshComponent : ComponentData
    {
        public string MeshPath;

        public MeshComponent()
        {
        }

        public MeshComponent(MeshInstance3D mesh)
        {
        }

        public MeshInstance3D GetMesh()
        {
            MeshInstance3D mesh = new MeshInstance3D();
            return mesh;
        }
    }
}