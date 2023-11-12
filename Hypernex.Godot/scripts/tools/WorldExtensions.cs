using System.Linq;
using Godot;
using Hypernex.Game;
using Hypernex.Game.Classes;

namespace Hypernex.Tools.Godot
{
    public static class WorldExtensions
    {
        public static WorldAsset LoadAssetFromPath(this WorldData dataObject, string path)
        {
            var assetData = dataObject.AllAssets.FirstOrDefault(x => x.Path == path);
            if (assetData == null)
                return null;
            return WorldManager.Instance.LoadAsset(dataObject, assetData);
        }

        public static T LoadAssetFromPath<T>(this WorldData dataObject, string path) where T : WorldAsset
        {
            return (T)LoadAssetFromPath(dataObject, path);
        }

        public static string SaveAssetFromResource(this WorldData dataObject, WorldAsset resource)
        {
            var assetData = WorldManager.Instance.SaveAsset(dataObject, resource);
            if (assetData == null)
                return null;
            dataObject.AllAssets.Add(assetData);
            return assetData?.Path;
        }
    }
}