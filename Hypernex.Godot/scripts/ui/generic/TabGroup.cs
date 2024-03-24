using Godot;

namespace Hypernex.UI.Generic
{
    public partial class TabGroup : HBoxContainer
    {
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.AsGodotObject() is TabOpenButton btn)
            {
                return true;
            }
            return false;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (data.AsGodotObject() is TabOpenButton btn)
            {
                btn.root.Reparent(this);
            }
        }
    }
}
