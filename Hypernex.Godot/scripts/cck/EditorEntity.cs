using System.Linq;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public partial class EditorEntity : Node3D
    {
        public T GetComponent<T>() where T : Node
        {
            return GetChildren().FirstOrDefault(x => x is T) as T;
        }

        public EditorEntity[] GetChildEnts() => GetChildren().Where(x => x is EditorEntity).Select(x => x as EditorEntity).ToArray();
    }
}