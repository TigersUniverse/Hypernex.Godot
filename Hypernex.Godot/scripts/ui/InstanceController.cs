using System;
using System.Collections.Generic;
using System.Linq;
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

        public async void UpdateWith(SafeInstance[] instances)
        {
            var oldNodes = container.GetChildren();
            List<CardTemplate> templates = new List<CardTemplate>();
            foreach (var item in instances)
            {
                CardTemplate node = worldUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetSafeInstance(item);
                templates.Add(node);
            }
            while (templates.Any(x => !x.isLoaded))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            label.Text = string.Format(labelFormat, instances.Length);
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
            UpdateWith(instances.ToArray());
        }
    }
}
