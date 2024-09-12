using System;
using Godot;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Colliders
    {
        private static Area3D GetArea3D(Item item)
        {
            if (GodotObject.IsInstanceValid(item.t))
            {
                if (item.t is IEntity ent)
                    return ent.GetComponent<Area3D>();
                if (item.t is Area3D a)
                    return a;
            }
            return null;
        }

        public static void OnTriggerEnter(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            GetArea3D(item).BodyEntered += c => SandboxFuncTools.InvokeSandboxFunc(s, new Item(c, item.world));
        }

        public static void OnTriggerEnter(ReadonlyItem item, object s) => OnTriggerEnter(item.item, s);

        public static void OnTriggerStay(Item item, object o)
        {
            throw new NotImplementedException();
        }

        public static void OnTriggerStay(ReadonlyItem item, object s) => OnTriggerStay(item.item, s);

        public static void OnTriggerExit(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            GetArea3D(item).BodyExited += c => SandboxFuncTools.InvokeSandboxFunc(s, new Item(c, item.world));
        }

        public static void OnTriggerExit(ReadonlyItem item, object s) => OnTriggerExit(item.item, s);

        /*
        public static void OnCollisionEnter(Item item, object o)
        {
            throw new NotImplementedException();
        }
        
        public static void OnCollisionEnter(ReadonlyItem item, object s) => OnCollisionEnter(item.item, s);

        public static void OnCollisionStay(Item item, object o)
        {
            throw new NotImplementedException();
        }

        public static void OnCollisionStay(ReadonlyItem item, object s) => OnCollisionStay(item.item, s);

        public static void OnCollisionExit(Item item, object o)
        {
            throw new NotImplementedException();
        }

        public static void OnCollisionExit(ReadonlyItem item, object s) => OnCollisionExit(item.item, s);
        */
    }
}