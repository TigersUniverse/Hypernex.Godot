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
        IEntity[] GetChildEnts();
        IEntity ParentEnt { get; }
        bool Enabled { get; set; }
        string Name { get; set; }
        Node AsNode => this as Node;
    }
}