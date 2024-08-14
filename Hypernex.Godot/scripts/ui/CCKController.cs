using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class CCKController : Node
    {
        [Export]
        public AcceptDialog dialog;

        public override void _EnterTree()
        {
            Reset();
        }

        public void Popup(string title, string text)
        {
            dialog.Title = title;
            dialog.DialogText = text;
            dialog.Show();
        }

        public void Reset()
        {
            dialog.Hide();
        }
    }
}