using Godot;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class UI
    {
        public static string GetText(Item item)
        {
            if (item.t is Label3D l3d)
                return l3d.Text;
            return string.Empty;
        }

        public static void SetText(Item item, string text)
        {
            if (item.t is Label3D l3d)
                l3d.Text = text;
        }
    }
}