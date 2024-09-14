using System;
using System.Linq;
using Godot;

namespace Hypernex.Game
{
    public static class EntityExtensions
    {
        public static bool TryFindComponent(this Node node, Type type, out Node value)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                if (type.IsAssignableFrom(node.GetType()))
                {
                    value = node;
                    return true;
                }
                if (TryFindEntity(node, out IEntity ent))
                {
                    value = ent.GetComponent(type);
                    return GodotObject.IsInstanceValid(value);
                }
            }
            value = null;
            return false;
        }

        public static bool TryFindComponent<T>(this Node node, out T value) where T : Node
        {
            bool ret = TryFindComponent(node, typeof(T), out Node val);
            value = val as T;
            return ret;
        }

        public static IEntity FindEntity(this Node node)
        {
            if (!GodotObject.IsInstanceValid(node))
                return null;
            if (node is IEntity ent)
                return ent;
            if (node.HasMeta(IEntity.TypeName) && node.GetNodeOrNull(node.GetMeta(IEntity.TypeName).AsNodePath()) is IEntity ent2)
                return ent2;
            return null;
        }

        public static bool TryFindEntity(this Node node, out IEntity ent)
        {
            ent = FindEntity(node);
            return ent != null;
        }

        public static Node FindAddComponent(this Node node, Node value)
        {
            if (TryFindEntity(node, out IEntity ent))
            {
                ent.AddComponent(value);
            }
            return value;
        }

        public static T GetComponent<T>(this IEntity ent) where T : Node
        {
            return ent.GetComponent(typeof(T)) as T;
        }

        public static T AddNewComponent<T>(this IEntity ent) where T : Node, new()
        {
            T v = new T();
            ((Node)ent).AddChild(v, true);
            ent.AddComponent(v);
            return v;
        }

        public static bool TryGetComponent<T>(this IEntity ent, out T value) where T : Node
        {
            value = ent.GetComponent(typeof(T)) as T;
            return GodotObject.IsInstanceValid(value);
        }

        public static T[] GetComponents<T>(this IEntity ent) where T : Node
        {
            return ent.GetComponents(typeof(T)).Select(x => x as T).ToArray();
        }
    }
}