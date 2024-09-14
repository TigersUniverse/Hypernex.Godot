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
}