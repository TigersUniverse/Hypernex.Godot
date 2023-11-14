using System.Collections.Generic;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game
{
    public static class PlayerManagement
    {
        private static Dictionary<GameInstance, List<PlayerRoot>> players = new();
        public static Dictionary<GameInstance, List<PlayerRoot>> Players => new(players);

        public static PlayerRoot GetNetPlayer(GameInstance instance, string userid)
        {
            foreach (KeyValuePair<GameInstance,List<PlayerRoot>> keyValuePair in Players)
            {
                if (keyValuePair.Key.gameServerId == instance.gameServerId && keyValuePair.Key.instanceId == instance.instanceId)
                    foreach (PlayerRoot netPlayer in keyValuePair.Value)
                        if (netPlayer.UserId == userid)
                            return netPlayer;
            }
            return null;
        }

        private static PlayerRoot GetOrCreateNetPlayer(GameInstance instance, string userid)
        {
            if (!Players.ContainsKey(instance))
                return null;
            PlayerRoot netPlayer = GetNetPlayer(instance, userid);
            if (netPlayer != null)
                return netPlayer;
            netPlayer = Init.NewPlayer(false);
            netPlayer.SetUser(userid, instance);
            instance.World.AddPlayer(netPlayer);
            players[instance].Add(netPlayer);
            return netPlayer;
        }

        public static void HandlePlayerUpdate(GameInstance gameInstance, PlayerUpdate playerUpdate)
        {
            if (playerUpdate.Auth.UserId == APITools.CurrentUser?.Id || string.IsNullOrEmpty(playerUpdate.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, playerUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.NetworkUpdate(playerUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerUpdate.Auth.UserId}");
        }

        public static void HandleWeightedObjectUpdate(GameInstance gameInstance, WeightedObjectUpdate weightedObjectUpdate)
        {
            if (weightedObjectUpdate.Auth.UserId == APITools.CurrentUser?.Id ||
                string.IsNullOrEmpty(weightedObjectUpdate.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, weightedObjectUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.WeightedObject(weightedObjectUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{weightedObjectUpdate.Auth.UserId}");
        }

        public static void HandleResetWeightedObject(GameInstance gameInstance,
            ResetWeightedObjects resetWeightedObjects)
        {
            if (resetWeightedObjects.Auth.UserId == APITools.CurrentUser?.Id ||
                string.IsNullOrEmpty(resetWeightedObjects.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, resetWeightedObjects.Auth.UserId);
            if (netPlayer != null)
                netPlayer.ResetWeightedObjects();
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{resetWeightedObjects.Auth.UserId}");
        }

        public static void HandlePlayerObjectUpdate(GameInstance gameInstance, PlayerObjectUpdate playerObjectUpdate)
        {
            if (playerObjectUpdate.Auth.UserId == APITools.CurrentUser?.Id || string.IsNullOrEmpty(playerObjectUpdate.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, playerObjectUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.NetworkObjectUpdate(playerObjectUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerObjectUpdate.Auth.UserId}");
        }

        public static void HandlePlayerVoice(GameInstance gameInstance, PlayerVoice playerVoice)
        {
            if (playerVoice.Auth.UserId == APITools.CurrentUser?.Id || string.IsNullOrEmpty(playerVoice.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, playerVoice.Auth.UserId);
            if (netPlayer != null)
                netPlayer.VoiceUpdate(playerVoice);
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerVoice.Auth.UserId}");
        }

        public static void HandlePlayerMessage(GameInstance gameInstance, PlayerMessage playerMessage)
        {
            if (playerMessage.Auth.UserId == APITools.CurrentUser?.Id || string.IsNullOrEmpty(playerMessage.Auth.UserId))
                return;
            PlayerRoot netPlayer = GetOrCreateNetPlayer(gameInstance, playerMessage.Auth.UserId);
            if (netPlayer != null)
                netPlayer.MessageUpdate(playerMessage);
            else
                Logger.CurrentLogger.Debug(
                    $"PlayerRoot not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerMessage.Auth.UserId}");
        }

        public static void PlayerLeave(GameInstance gameInstance, User user)
        {
            if (!Players.ContainsKey(gameInstance))
                return;
            PlayerRoot netPlayer = GetNetPlayer(gameInstance, user.Id);
            if (netPlayer != null)
            {
                players[gameInstance].Remove(netPlayer);
                netPlayer.QueueFree();
            }
            /*
            gameInstance.ScriptEvents?.OnUserLeave.Invoke(user.Id);
            if (!gameInstance.isHost) return;
            // Claim all NetworkSyncs that have Host Only
            foreach (GameObject rootGameObject in gameInstance.loadedScene.GetRootGameObjects())
            {
                Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in ts)
                {
                    NetworkSync networkSync = transform.gameObject.AddComponent<NetworkSync>();
                    if (networkSync == null) continue;
                    if(networkSync.InstanceHostOnly)
                        networkSync.Claim();
                }
            }
            */
        }

        internal static void CreateGameInstance(GameInstance gameInstance)
        {
            if(!Players.ContainsKey(gameInstance))
                players.Add(gameInstance, new List<PlayerRoot>());
        }

        internal static void DestroyGameInstance(GameInstance gameInstance)
        {
            if (Players.ContainsKey(gameInstance))
                players.Remove(gameInstance);
        }
    }
}