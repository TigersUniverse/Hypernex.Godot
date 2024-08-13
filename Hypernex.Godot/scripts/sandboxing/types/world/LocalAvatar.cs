using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using HypernexSharp.APIObjects;

namespace Hypernex.Sandboxing.SandboxedTypes.World
{
    public static class LocalAvatar
    {
        public static bool IsLocalClient() => false;
        public static bool IsLocalPlayerId(string userid) => APITools.CurrentUser.Id == userid;
        public static bool IsHost() => GameInstance.FocusedInstance?.isHost ?? false;

        /*
        public static ReadonlyItem GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }
        */

        /*
        public static ReadonlyItem GetAvatarObjectByPath(string path)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }
        */

        public static Item GetPlayerRoot()
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local))
                return null;
            return new Item(PlayerRoot.Local.Controller);
        }

        /*
        public static bool IsAvatarItem(Item item) =>
            AnimationUtility.GetRootOfChild(item.t).gameObject.GetComponent<LocalPlayer>() != null;
        
        public static bool IsAvatarItem(ReadonlyItem item) =>
            AnimationUtility.GetRootOfChild(item.item.t).gameObject.GetComponent<LocalPlayer>() != null;
        */

        public static void TeleportTo(float3 position)
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local))
                return;
            // if (LocalPlayer.Instance.Dashboard.IsVisible)
            //     LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
            PlayerRoot.Local.Pos = position.ToGodot3();
        }

        public static void Rotate(float4 rotation)
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local))
                return;
            // if (LocalPlayer.Instance.Dashboard.IsVisible)
            //     LocalPlayer.Instance.Dashboard.PositionDashboard(LocalPlayer.Instance);
            PlayerRoot.Local.Rot = rotation.ToGodotQuat();
        }


        /*
        public static void Scale(float scale)
        {
            if (LocalPlayer.Instance == null || CurrentAvatar.Instance == null)
                return;
            if(LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
            CurrentAvatar.Instance.SizeAvatar(scale);
            if(!LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
        }

        public static AvatarParameter[] GetAvatarParameters()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return Array.Empty<AvatarParameter>();
            List<AvatarParameter> parameterNames = new();
            foreach (AnimatorPlayable avatarAnimatorPlayable in LocalPlayer.Instance.avatar.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in avatarAnimatorPlayable.AnimatorControllerParameters)
                {
                    if (parameterNames.Count(x => x.Name == parameter.name) <= 0)
                        parameterNames.Add(new AvatarParameter(LocalPlayer.Instance.avatar, avatarAnimatorPlayable,
                            parameter, false));
                }
            }
            return parameterNames.ToArray();
        }
        
        public static AvatarParameter GetAvatarParameter(string parameterName)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            foreach (AnimatorPlayable animatorPlayable in LocalPlayer.Instance.avatar.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in animatorPlayable.AnimatorControllerParameters)
                {
                    if (parameter.name == parameterName)
                        return new AvatarParameter(LocalPlayer.Instance.avatar, animatorPlayable, parameter, false);
                }
            }
            return null;
        }

        public static bool IsExtraneousObjectPresent(string key)
        {
            if (LocalPlayer.Instance == null)
                return false;
            return LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key);
        }

        public static string[] GetExtraneousObjectKeys()
        {
            List<string> keys = new();
            foreach (string key in LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects.Keys)
                keys.Add(key);
            return keys.ToArray();
        }

        public static object GetExtraneousObject(string key)
        {
            if (LocalPlayer.Instance == null)
                return null;
            if (!LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key))
                return null;
            return LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects[key];
        }

        public static string[] GetPlayerAssignedTags()
        {
            if (LocalPlayer.Instance == null)
                return null;
            return LocalPlayer.Instance.LocalPlayerSyncController.LastPlayerAssignedTags.ToArray();
        }
        */

        public static void Respawn()
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local))
                return;
            GameInstance.FocusedInstance.World.RespawnPlayer(PlayerRoot.Local);
        }

        public static Pronouns GetPronouns()
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local))
                return null;
            if (APITools.CurrentUser == null)
                return null;
            return APITools.CurrentUser.Bio.Pronouns;
        }

        /*
        public static void SetAvatar(string avatarId)
        {
            if (!GodotObject.IsInstanceValid(PlayerRoot.Local) || ConfigManager.SelectedConfigUser == null)
                return;
            ConfigManager.SelectedConfigUser.CurrentAvatar = avatarId;
            if(LocalPlayer.Instance != null)
                LocalPlayer.Instance.LoadAvatar();
            ConfigManager.SaveConfigToFile();
        }
        */
    }
}