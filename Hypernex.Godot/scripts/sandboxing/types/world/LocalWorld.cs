using System;
using Godot;
using Hypernex.Game;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes.World
{
    public static class LocalWorld
    {
        public static Item GetItemInRoot(string name)
        {
            if (string.IsNullOrEmpty(name) || GameInstance.FocusedInstance == null)
                return null;
            foreach (Node rootNode in GameInstance.FocusedInstance.World.GetChildren())
                if (rootNode.Name == name)
                    return new Item(rootNode, GameInstance.FocusedInstance.World);
            return null;
        }

        public static void SetSkyboxMaterial(string asset)
        {
            throw new NotImplementedException();
            // GameInstance.FocusedInstance.World.GetAsset();
        }

        public static void UpdateEnvironment() => throw new NotImplementedException();

        public static Item DuplicateItem(Item item, string name = "")
        {
            throw new NotImplementedException();
            /*
            Transform r = AnimationUtility.GetRootOfChild(item.t);
            if (r == null || r.GetComponent<LocalPlayer>() != null || r.GetComponent<NetPlayer>() != null)
                return null;
            Transform d = Object.Instantiate(item.t.gameObject).transform;
            if (!string.IsNullOrEmpty(name))
            {
                bool allow = true;
                if (d.parent == null)
                {
                    foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        if (rootGameObject.name == name)
                            allow = false;
                    }
                }
                else
                {
                    for (int i = 0; i < d.parent.childCount; i++)
                    {
                        Transform child = d.parent.GetChild(i);
                        if (child.name == name)
                            allow = false;
                    }
                }
                if (allow)
                    d.gameObject.name = name;
            }
            return new Item(d);
            */
        }
    }
}