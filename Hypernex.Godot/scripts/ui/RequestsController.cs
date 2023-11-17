using System;
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

        public override void _Ready()
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
            var friends = APITools.CurrentUser.FriendRequests;
            label.Text = string.Format(labelFormat, friends.Count);
            foreach (var node in container.GetChildren())
            {
                node.QueueFree();
            }
            if (!label.Visible)
                return;
            foreach (var friend in friends)
            {
                CardTemplate node = friendUI.Instantiate<CardTemplate>();
                container.AddChild(node);
                node.SetUserId(friend, CardTemplate.CardUserType.FriendRequest);
            }
        }
    }
}
