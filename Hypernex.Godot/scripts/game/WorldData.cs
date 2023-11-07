using System.Collections.Generic;

namespace Hypernex.Game
{
    public class WorldData
    {
        public List<WorldObject> RootObjects { get; set; } = new List<WorldObject>();
        public List<WorldClass> Classes { get; set; } = new List<WorldClass>();
    }

    public class WorldObject
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ClassData { get; set; } = string.Empty;
        public List<WorldComponent> Components { get; set; } = new List<WorldComponent>();
        public List<WorldObject> ChildObjects { get; set; } = new List<WorldObject>();
    }

    public class WorldComponent
    {
        public string ClassName { get; set; } = string.Empty;
        public string ClassData { get; set; } = string.Empty;
    }

    public class WorldClass
    {
        public string Name { get; set; } = string.Empty;
        public string Lang { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
