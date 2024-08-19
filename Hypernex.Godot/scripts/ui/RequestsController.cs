using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Tools;

namespace Hypernex.UI
{
    public partial class RequestsController : Node
    {
        [Export(PropertyHint.MultilineText)]
        public string labelFormat = "[center]Friend Requests ({0})[/center][right][url]Refresh[/url][/right]";
        [Export]
        public RichTextLabel label;
        [Export]
        public Container container;
        [Export]
        public PackedScene friendUI;

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

        public async void UpdateWith(string[] friends)
        {
            var oldNodes = container.GetChildren();
            List<CardTemplate> templates = new List<CardTemplate>();
            foreach (var item in friends)
            {
                CardTemplate node = friendUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetUserId(item, CardTemplate.CardUserType.FriendRequest);
                templates.Add(node);
            }
            while (templates.Any(x => !x.isLoaded))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            label.Text = string.Format(labelFormat, friends.Length);
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
            var friends = APITools.CurrentUser.FriendRequests;
            UpdateWith(friends.ToArray());
        }
    }
}
