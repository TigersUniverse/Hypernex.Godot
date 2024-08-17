using System.Linq;
using Godot;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class UI
    {
        public static string GetText(Item item)
        {
            if (item.t is Label3D l3d)
                return l3d.Text;
            if (item.t is Label l)
                return l.Text;
            if (item.t is RichTextLabel rl)
                return rl.Text;
            if (item.t is Button btn)
                return btn.Text;
            return string.Empty;
        }

        public static void SetText(Item item, string text)
        {
            if (item.t is Label3D l3d)
                l3d.Text = text;
            if (item.t is Label l)
                l.Text = text;
            if (item.t is RichTextLabel rl)
                rl.Text = text;
            if (item.t is Button btn)
                btn.Text = text;
        }

        public static void RegisterButtonClick(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
                b.Pressed += () => SandboxFuncTools.InvokeSandboxFunc(s);
        }

        public static void RemoveAllButtonClicks(Item item)
        {
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                foreach (var conn in b.GetSignalConnectionList(BaseButton.SignalName.Pressed).ToArray())
                {
                    b.Disconnect(BaseButton.SignalName.Pressed, conn["callable"].AsCallable());
                }
            }
        }
    }
}