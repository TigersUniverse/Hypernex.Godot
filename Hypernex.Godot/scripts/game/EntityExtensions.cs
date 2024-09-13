using System;
using Godot;

namespace Hypernex.Game
{
    public static class EntityExtensions
    {
        public static bool TryFindComponent(this Node node, Type type, out Node value)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                if (node.GetType().IsAssignableTo(type))
                {
                    value = node;
                    return true;
                }
                if (node is IEntity ent)
                {
                    value = ent.GetComponent(type);
                    return GodotObject.IsInstanceValid(value);
                }
                if (node.HasMeta(IEntity.TypeName) && node.GetMeta(IEntity.TypeName).AsGodotObject() is IEntity ent2)
                {
                    value = ent2.GetComponent(type);
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
            if (node.HasMeta(IEntity.TypeName) && node.GetMeta(IEntity.TypeName).AsGodotObject() is IEntity ent2)
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
            node.AddSibling(value, true);
            return value;
        }
    }
}