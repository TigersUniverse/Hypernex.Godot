using System;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Tools;
using HypernexSharp;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class MainOverlay : Node
    {
        [Export]
        public Control root;
        public event EventHandler OnLogout;
    }
}