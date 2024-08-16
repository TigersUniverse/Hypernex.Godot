using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Hypernex.CCK;

namespace Hypernex.Tools
{
    public partial class QuickInvoke : Node
    {
        public static QuickInvoke Instance;
        public static System.Threading.Mutex mutex = new System.Threading.Mutex();
        public static Queue<(object, object[])> queue = new Queue<(object, object[])>();

        public static void InvokeActionOnMainThread(object action, params object[] args)
        {
            if (mutex.WaitOne())
            {
                queue.Enqueue((action, args));
                mutex.ReleaseMutex();
            }
        }

        public void CallMe(object action, object[] args)
        {
            action.GetType().GetMethod("Invoke").Invoke(action, args);
        }

        public override void _Ready()
        {
            Instance = this;
        }

        public override void _Process(double delta)
        {
            if (mutex.WaitOne(0))
            {
                while (queue.Count > 0)
                {
                    try
                    {
                        var val = queue.Dequeue();
                        CallMe(val.Item1, val.Item2);
                    }
                    catch (Exception e)
                    {
                        Logger.CurrentLogger.Critical(e);
                    }
                }
                mutex.ReleaseMutex();
            }
        }
    }
}
