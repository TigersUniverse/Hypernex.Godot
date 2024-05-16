using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Runtime : IDisposable
    {
        internal ScriptRunner runner;

        internal Dictionary<object, SandboxFunc> OnFixedUpdates => new (onFixedUpdates);
        private Dictionary<object, SandboxFunc> onFixedUpdates = new ();

        internal Dictionary<object, SandboxFunc> OnUpdates => new (onUpdates);
        private Dictionary<object, SandboxFunc> onUpdates = new ();

        internal Dictionary<object, SandboxFunc> OnLateUpdates => new(onLateUpdates);
        private Dictionary<object, SandboxFunc> onLateUpdates = new();

        internal Runtime(ScriptRunner r)
        {
            runner = r;
        }

        public void OnFixedUpdate(object s) => onFixedUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnFixedUpdate(object s) => onFixedUpdates.Remove(s);
        public void OnUpdate(object s) => onUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnUpdate(object s) => onUpdates.Remove(s);
        public void OnLateUpdate(object s) => onLateUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnLateUpdate(object s) => onLateUpdates.Remove(s);

        public void RepeatSeconds(object s, float waitTime)
        {
            SandboxFunc sandboxFunc = SandboxFuncTools.TryConvert(s);
            Timer timer = new Timer();
            timer.WaitTime = waitTime;
            timer.Autostart = true;
            timer.OneShot = false;
            timer.Timeout += () =>
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(sandboxFunc);
                }
                catch (Exception e)
                {
                    runner.OnError(e);
                }
            };
            runner.AddChild(timer);
        }

        public async void RunAfterSeconds(object s, float time)
        {
            await runner.ToSignal(runner.GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
            if (!GodotObject.IsInstanceValid(runner))
                return;
            SandboxFunc sandboxFunc = SandboxFuncTools.TryConvert(s);
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(sandboxFunc);
            }
            catch (Exception e)
            {
                runner.OnError(e);
            }
        }

        public void FixedUpdate()
        {
            foreach (var kvp in onFixedUpdates)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(kvp.Value);
                }
                catch (Exception e)
                {
                    runner.OnError(e);
                }
            }
        }

        public void Update()
        {
            foreach (var kvp in onUpdates)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(kvp.Value);
                }
                catch (Exception e)
                {
                    runner.OnError(e);
                }
            }
        }

        public void LateUpdate()
        {
            foreach (var kvp in onLateUpdates)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(kvp.Value);
                }
                catch (Exception e)
                {
                    runner.OnError(e);
                }
            }
        }

        public void Dispose()
        {
            onFixedUpdates.Clear();
            onUpdates.Clear();
            onLateUpdates.Clear();
        }
    }
}