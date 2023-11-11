using Godot;
using Hypernex.Tools;

namespace Hypernex.Game
{
    public partial class WorldRoot : Node
    {
        public GameInstance gameInstance;

        public void AddPlayer(PlayerRoot player)
        {
            AddChild(player);
        }
    }
}
