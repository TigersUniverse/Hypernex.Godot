using System;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes;
using Nexbox;
using Nexbox.Interpreters;

namespace Hypernex.Sandboxing
{
    public partial class ScriptRunner : Node
    {
        public WorldScript scriptRef;
        public IInterpreter interpreter;
        public NexboxScript script;
        public Runtime runtime;

        public void OnLog(object o)
        {
            Logger.CurrentLogger.Log($"[WORLD] [{script.Name}{script.GetExtensionFromLanguage()}] {o}");
        }

        public void OnError(Exception e)
        {
            Logger.CurrentLogger.Error($"[WORLD] [{script.Name}{script.GetExtensionFromLanguage()}] {e}");
        }

        public override void _EnterTree()
        {
            Start();
        }

        public override void _ExitTree()
        {
            Stop();
        }

        public void Start()
        {
            Stop();
            if (IsInstanceValid(scriptRef))
            {
                script = new NexboxScript(scriptRef.Language, scriptRef.Contents);
                script.Name = scriptRef.Name;
                switch (scriptRef.Language)
                {
                    case NexboxLanguage.JavaScript:
                        interpreter = new JavaScriptInterpreter();
                        break;
                    case NexboxLanguage.Lua:
                        interpreter = new LuaInterpreter();
                        break;
                    default:
                        throw new Exception("Unknown NexboxScript language");
                }
                interpreter.StartSandbox(o => OnLog(o));
                ForwardTypes();
                runtime = new Runtime(this);
                interpreter.CreateGlobal("item", new Item(GetParent()));
                interpreter.CreateGlobal("Runtime", runtime);
                interpreter.RunScript(script.Script, e => OnError(e));
            }
        }

        public void Stop()
        {
            runtime?.Dispose();
            interpreter?.Stop();
            runtime = null;
            interpreter = null;
        }

        public void ForwardTypes()
        {
            interpreter.ForwardType("float2", typeof(float2));
            interpreter.ForwardType("float3", typeof(float3));
            interpreter.ForwardType("float4", typeof(float4));
            interpreter.ForwardType("Item", typeof(Item));
            interpreter.ForwardType("Runtime", typeof(Runtime));
            interpreter.ForwardType("UI", typeof(SandboxedTypes.UI));
            interpreter.ForwardType("Time", typeof(SandboxedTypes.Time));
            interpreter.ForwardType("UtcTime", typeof(UtcTime));
        }
    }
}