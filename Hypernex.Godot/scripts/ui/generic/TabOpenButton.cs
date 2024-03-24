using Godot;

namespace Hypernex.UI.Generic
{
    [GlobalClass]
    public partial class TabOpenButton : Button
    {
        [Export]
        public Control root;

        public override Variant _GetDragData(Vector2 atPosition)
        {
            return this;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            return root.GetParentControl()._CanDropData(atPosition, data);
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            root.GetParentControl()._DropData(atPosition, data);
        }
    }
}