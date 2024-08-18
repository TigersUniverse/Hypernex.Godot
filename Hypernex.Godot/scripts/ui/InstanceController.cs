using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class InstanceController : Node
    {
        [Export(PropertyHint.MultilineText)]
        public string labelFormat = "[center]Instances ({0})[/center][right][url]Refresh[/url][/right]";
        [Export]
        public RichTextLabel label;
        [Export]
        public Container container;
        [Export]
        public PackedScene worldUI;
        public List<SafeInstance> instances = new List<SafeInstance>();

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
            APITools.APIObject.GetInstances(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        instances.Clear();
                        instances.AddRange(r.result.SafeInstances);
                        OnVisible();
                    });
                }
            }, APITools.CurrentUser, APITools.CurrentToken);
        }

        private void OnVisible()
        {
            label.Text = string.Format(labelFormat, instances.Count);
            foreach (var node in container.GetChildren())
            {
                node.QueueFree();
            }
            if (!label.Visible)
                return;
            foreach (var inst in instances)
            {
                CardTemplate node = worldUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetSafeInstance(inst);
            }
        }
    }
}
