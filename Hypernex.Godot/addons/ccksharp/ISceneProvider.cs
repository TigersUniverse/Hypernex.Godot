using System;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public interface ISceneProvider : IDisposable
    {
        PackedScene LoadFromFile(string filePath);
    }
}