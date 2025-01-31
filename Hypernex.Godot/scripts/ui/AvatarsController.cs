using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Tools;

namespace Hypernex.UI
{
    public partial class AvatarsController : Node
    {
        [Export(PropertyHint.MultilineText)]
        public string labelFormat = "[center]Avatars ({0})[/center][right][url]Refresh[/url][/right]";
        [Export]
        public bool myAvatars = true;
        [Export]
        public RichTextLabel label;
        [Export]
        public Container container;
        [Export]
        public PackedScene avatarUI;

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

        public async void UpdateWith(string[] avatars)
        {
            var oldNodes = container.GetChildren();
            List<CardTemplate> templates = new List<CardTemplate>();
            foreach (var item in avatars)
            {
                CardTemplate node = avatarUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetAvatarId(item);
                templates.Add(node);
            }
            while (templates.Any(x => !x.isLoaded))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            label.Text = string.Format(labelFormat, avatars.Length);
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
            if (myAvatars)
            {
                var avatars = APITools.CurrentUser.Avatars;
                UpdateWith(avatars.ToArray());
            }
            else
            {
                APITools.APIObject.GetAvatarPopularity(result =>
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
