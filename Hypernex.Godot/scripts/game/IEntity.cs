using System;
using Godot;

namespace Hypernex.Game
{
    public interface IEntity
    {
        public const string TypeName = "IEntity";
        Node GetComponent(Type type);
        Node[] GetComponents(Type type);
        Node AddComponent(Node value);
        bool Enabled { get; set; }
    }

    public static class IEntityExt
    {
        public static T GetComponent<T>(this IEntity ent) where T : Node
        {
            return ent.GetComponent(typeof(T)) as T;
        }

        public static T AddNewComponent<T>(this IEntity ent) where T : Node, new()
        {
            T v = ent.AddComponent(new T()) as T;
            ((Node)ent).AddChild(v, true);
            return v;
        }

        public static bool TryGetComponent<T>(this IEntity ent, out T value) where T : Node
        {
            value = ent.GetComponent(typeof(T)) as T;
            return GodotObject.IsInstanceValid(value);
        }

        public static T[] GetComponents<T>(this IEntity ent) where T : Node
        {
            return ent.GetComponents(typeof(T)) as T[];
        }
    }
}