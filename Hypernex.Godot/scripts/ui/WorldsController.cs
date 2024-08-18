using System;
using System.Linq;
using Godot;
using Hypernex.Tools;

namespace Hypernex.UI
{
    public partial class WorldsController : Node
    {
        [Export(PropertyHint.MultilineText)]
        public string labelFormat = "[center]Worlds ({0})[/center][right][url]Refresh[/url][/right]";
        [Export]
        public RichTextLabel label;
        [Export]
        public Container container;
        [Export]
        public PackedScene worldUI;

        public override void _EnterTree()
        {
            label.VisibilityChanged += OnVisible;
            label.MetaClicked += OnClick;
        }

        public override void _ExitTree()
        {
            label.VisibilityChanged -= OnVisible;
            label.MetaClicked -= OnClick;
        }

        private void OnClick(Variant meta)
        {
            label.Text = string.Format(labelFormat, "...");
            APITools.APIObject.GetUser(APITools.CurrentToken, r =>
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    if (r.success)
                    {
                        APITools.CurrentUser = r.result.UserData;
                        OnVisible();
                    }
                });
            });
        }

        private void OnVisible()
        {
            var worlds = APITools.CurrentUser.Worlds;
            label.Text = string.Format(labelFormat, worlds.Count);
            foreach (var node in container.GetChildren())
            {
                node.QueueFree();
            }
            if (!label.Visible)
                return;
            foreach (var world in worlds)
            {
                CardTemplate node = worldUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetWorldId(world);
            }
        }
    }
}
