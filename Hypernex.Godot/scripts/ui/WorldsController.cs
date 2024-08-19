using System;
using System.Collections.Generic;
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
        public bool myWorlds = true;
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

        public async void UpdateWith(string[] worlds)
        {
            var oldNodes = container.GetChildren();
            List<CardTemplate> templates = new List<CardTemplate>();
            foreach (var world in worlds)
            {
                CardTemplate node = worldUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetWorldId(world);
                templates.Add(node);
            }
            while (templates.Any(x => !x.isLoaded))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            label.Text = string.Format(labelFormat, worlds.Length);
            foreach (var node in templates)
            {
                if (IsInstanceValid(node))
                    node.Visible = node.shouldShow;
            }
            foreach (var node in oldNodes)
            {
                if (IsInstanceValid(node))
                    node.QueueFree();
            }
        }

        private void OnVisible()
        {
            if (!label.IsVisibleInTree())
                return;
            if (myWorlds)
            {
                var worlds = APITools.CurrentUser.Worlds;
                UpdateWith(worlds.ToArray());
            }
            else
            {
                APITools.APIObject.GetWorldPopularity(result =>
                {
                    if (result.success)
                    {
                        QuickInvoke.InvokeActionOnMainThread(() =>
                        {
                            UpdateWith(result.result.Popularity.OrderByDescending(x => x.Weekly.Usages).Select(x => x.Id).ToArray());
                        });
                    }
                }, HypernexSharp.APIObjects.PopularityType.Weekly);
            }
        }
    }
}
