using System;
using System.Linq;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class MainOverlay : Node
    {
        [Export]
        public Control root;
        [Export]
        public TabContainer tabs;
        [Export]
        public Button logoutButton;
        [Export]
        public Button exitButton;
        [Export]
        public int currentInstanceIdx;
        [Export]
        public PackedScene bigCardUI;
        public BigCardTemplate currentBigCard;

        public override void _Ready()
        {
            logoutButton.Pressed += APITools.Logout;
            exitButton.Pressed += Exit;
            GameInstance.OnGameInstanceLoaded += GameInstanceLoaded;
            tabs.TabChanged += ChangedTabs;
            tabs.SetTabHidden(currentInstanceIdx, true);
        }

        public override void _ExitTree()
        {
            logoutButton.Pressed -= APITools.Logout;
            exitButton.Pressed -= Exit;
            GameInstance.OnGameInstanceLoaded -= GameInstanceLoaded;
            tabs.TabChanged -= ChangedTabs;
        }

        private void ChangedTabs(long tab)
        {
            if (IsInstanceValid(currentBigCard) && tabs.GetTabIdxFromControl(currentBigCard) != tab)
            {
                currentBigCard.QueueFree();
                currentBigCard = null;
            }
        }

        private void GameInstanceLoaded(GameInstance instance, WorldMeta meta)
        {
            tabs.SetTabHidden(currentInstanceIdx, (instance?.IsDisposed) ?? true);
            if (tabs.GetTabControl(currentInstanceIdx) is BigCardTemplate card)
            {
                card.SetGameInstance(instance);
            }
            if ((instance?.IsDisposed ?? true) && tabs.CurrentTab == currentInstanceIdx)
            {
                tabs.CurrentTab = 0;
            }
        }

        public void SetTab(int idx)
        {
            QuickInvoke.InvokeActionOnMainThread(() =>
            {
                tabs.CurrentTab = idx;
            });
        }

        public void AddCard(User user)
        {
            currentBigCard?.QueueFree();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetUser(user);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        public void AddCard(WorldMeta worldMeta)
        {
            currentBigCard?.QueueFree();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetWorldMeta(worldMeta);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        public void AddCard(SafeInstance instance)
        {
            currentBigCard?.QueueFree();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetSafeInstance(instance);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        private void Exit()
        {
            GetTree().Quit();
        }

        public void ShowHome()
        {
            tabs.CurrentTab = 0;
        }
    }
}