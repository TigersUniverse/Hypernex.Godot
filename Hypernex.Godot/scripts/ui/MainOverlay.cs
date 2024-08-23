using System;
using System.Collections.Generic;
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
        public TabBar bar;
        [Export]
        public TabContainer tabs;
        [Export]
        public Button cckButton;
        [Export]
        public Window cckWindow;
        [Export]
        public Button logoutButton;
        [Export]
        public Button exitButton;
        [Export]
        public int currentInstanceIdx;
        [Export]
        public PackedScene bigCardUI;
        public BigCardTemplate currentBigCard;

        public override void _EnterTree()
        {
            APITools.OnUserLogin += Login;
            cckButton.Pressed += LaunchCCK;
            logoutButton.Pressed += APITools.Logout;
            exitButton.Pressed += Exit;
            GameInstance.OnGameInstanceLoaded += GameInstanceLoaded;
            bar.TabChanged += ChangedTabs2;
            tabs.TabChanged += ChangedTabs;
            tabs.SetTabHidden(currentInstanceIdx, true);
        }

        public override void _ExitTree()
        {
            APITools.OnUserLogin -= Login;
            cckButton.Pressed -= LaunchCCK;
            logoutButton.Pressed -= APITools.Logout;
            exitButton.Pressed -= Exit;
            GameInstance.OnGameInstanceLoaded -= GameInstanceLoaded;
            bar.TabChanged -= ChangedTabs2;
            tabs.TabChanged -= ChangedTabs;
        }

        public override void _Process(double delta)
        {
            // bar.ClearTabs();
            for (int i = 0; i < tabs.GetTabCount(); i++)
            {
                string title = tabs.GetTabTitle(i);
                if (bar.TabCount >= i)
                    bar.AddTab(title);
                else
                    bar.SetTabTitle(i, title);
                bar.SetTabHidden(i, tabs.IsTabHidden(i));
            }
            bar.TabCount = tabs.GetTabCount();
            bar.CurrentTab = tabs.CurrentTab;
        }

        private void Login(User user)
        {
            currentBigCard?.Free();
            currentBigCard = null;
        }

        private void ChangedTabs2(long tab)
        {
            tabs.CurrentTab = (int)tab;
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
            tabs.CurrentTab = idx;
        }

        public void AddCard(User user, CardTemplate.CardUserType utype)
        {
            currentBigCard?.Free();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetUser(user, utype);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        public void AddCard(WorldMeta worldMeta)
        {
            currentBigCard?.Free();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetWorldMeta(worldMeta);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        public void AddCard(SafeInstance instance)
        {
            currentBigCard?.Free();
            var card = bigCardUI.Instantiate<BigCardTemplate>();
            currentBigCard = card;
            tabs.AddChild(card);
            card.SetSafeInstance(instance);
            int idx = tabs.GetTabIdxFromControl(card);
            CallDeferred(nameof(SetTab), idx);
        }

        public void LaunchCCK()
        {
            cckWindow.Show();
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