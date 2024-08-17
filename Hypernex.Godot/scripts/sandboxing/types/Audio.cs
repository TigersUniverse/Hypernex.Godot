using System;
using System.Collections;
using System.IO;
using Godot;
using Hypernex.Tools;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Audio
    {
        private static AudioStreamPlayer3D GetAudio3D(Item item)
        {
            if (GodotObject.IsInstanceValid(item.t))
                if (item.t is AudioStreamPlayer3D plr)
                    return plr;
            return null;
        }


        public static bool IsValid(Item item) => GetAudio3D(item) != null;

        public static bool IsPlaying(Item item) => GetAudio3D(item).Playing;
        public static bool IsMuted(Item item) => GetAudio3D(item).VolumeDb > -80f;
        public static bool IsLooping(Item item) => throw new NotImplementedException();
        public static void Play(Item item) => GetAudio3D(item).Play();
        public static void Pause(Item item) => GetAudio3D(item).StreamPaused = true;
        public static void Resume(Item item) => GetAudio3D(item).StreamPaused = false;
        public static void Stop(Item item) => GetAudio3D(item).Stop();

        public static void SetAudioClip(Item item, string asset)
        {
            throw new NotImplementedException();
        }

        public static void SetMute(Item item, bool value)
        {
            throw new NotImplementedException();
        }
        
        public static void SetLoop(Item item, bool value)
        {
            throw new NotImplementedException();
        }
        
        public static float GetPitch(Item item) => GetAudio3D(item).PitchScale;
        public static void SetPitch(Item item, float value)
        {
            GetAudio3D(item).PitchScale = value;
        }
        
        public static float GetVolume(Item item) => Mathf.DbToLinear(GetAudio3D(item).VolumeDb);
        public static void SetVolume(Item item, float value) => GetAudio3D(item).VolumeDb = Mathf.LinearToDb(Mathf.Clamp(value, 0f, 1f));
        
        public static float GetPosition(Item item) => GetAudio3D(item).GetPlaybackPosition();
        public static void SetPosition(Item item, float value) => GetAudio3D(item).Play(value);

        public static float GetLength(Item item) => (float)GetAudio3D(item).Stream.GetLength();

        /*
        private static IEnumerator WaitForAudio(string pathToFile, AudioSource audioSource, object onLoad)
        {
            using (UnityWebRequest r = UnityWebRequestMultimedia.GetAudioClip(pathToFile, AudioType.MPEG))
            {
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success)
                {
                    audioSource.clip = DownloadHandlerAudioClip.GetContent(r);
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onLoad));
                }
            }
        }

        public static void LoadFromCobalt(Item item, CobaltDownload cobaltDownload, object onLoad)
        {
            AudioSource audioSource = GetAudioSource(item);
            if(audioSource == null)
                return;
            if (!File.Exists(cobaltDownload.PathToFile))
                return;
            CoroutineRunner.Instance.StartCoroutine(WaitForAudio("file://" + cobaltDownload.PathToFile, audioSource,
                onLoad));
        }
        */
    }
}