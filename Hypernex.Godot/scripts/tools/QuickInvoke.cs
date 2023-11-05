using System;
using Godot;

namespace Hypernex.Tools
{
    // TODO: check if this class actually works
    public partial class QuickInvoke : Node
    {
        public static QuickInvoke Instance;

        public static void InvokeActionOnMainThread(object action, params object[] args) => Instance.CallDeferred(nameof(CallMe), Variant.From(action), Variant.From(args));

        public void CallMe(object action, object[] args)
        {
            action.GetType().GetMethod("Invoke").Invoke(action, args);
        }

        public override void _Ready()
        {
            Instance = this;
        }
    }
}
