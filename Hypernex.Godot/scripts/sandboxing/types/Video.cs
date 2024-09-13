using System;
using System.IO;
using CobaltSharp;
using FFmpeg.Godot;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes.World;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Video
    {
        private static VideoPlayer GetVideoPlayer(Item item)
        {
            if (item.t.TryFindComponent(out VideoPlayer player))
                return player;
            return null;
        }

        private static FFGodot GetFFGodot(Item item)
        {
            VideoPlayer v = GetVideoPlayer(item);
            if (!GodotObject.IsInstanceValid(v))
                return null;
            return v.video;
        }

        public static bool IsValid(Item item) => GetFFGodot(item) != null;
        
        public static bool IsPlaying(Item item) => !GetFFGodot(item).IsPaused && !GetFFGodot(item).IsFinished;
        public static bool IsMuted(Item item) => throw new NotImplementedException();
        public static bool IsLooping(Item item) => GetVideoPlayer(item).loop;
        public static void Play(Item item) => GetFFGodot(item).Resume();
        public static void Pause(Item item) => GetFFGodot(item)?.Pause();
        public static void Stop(Item item) => GetFFGodot(item)?.Pause();
        
        public static void SetMute(Item item, bool value)
        {
            throw new NotImplementedException();
        }
        
        public static void SetLoop(Item item, bool value)
        {
            GetVideoPlayer(item).loop = value;
        }
        
        public static float GetPitch(Item item) => throw new NotImplementedException();
        public static void SetPitch(Item item, float value)
        {
            throw new NotImplementedException();
        }
        
        public static float GetVolume(Item item) => Mathf.DbToLinear(GetFFGodot(item).source.VolumeDb); //Mathf.Remap(GetFFGodot(item).source.VolumeDb, -80f, 0f, 0f, 1f);

        public static void SetVolume(Item item, float value) => GetFFGodot(item).source.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(value, 0f, 1f)); //Mathf.Remap(Mathf.Clamp(value, 0f, 1f), 0f, 1f, -80f, 0f);
        
        public static float GetPosition(Item item) => (float)GetFFGodot(item).PlaybackTime;
        public static void SetPosition(Item item, float value) => GetFFGodot(item).Seek(value);

        public static float GetLength(Item item)
        {
            return (float)GetFFGodot(item).Length;
        }

        public static void LoadUrl(Item item, string url)
        {
            GetMedia getMedia = new GetMedia();
            getMedia.url = url;
            getMedia.vQuality = VideoQuality.q720;
            World.Cobalt.GetOptions(getMedia, (opts) =>
            {
                if (opts == null || opts.Options.Length == 0)
                {
                    Logger.CurrentLogger.Error("Could not find a video for URL!");
                    return;
                }
                Logger.CurrentLogger.Log($"Downloading video {url}...");
                opts.Options[0].Download((file) =>
                {
                    if (file == null)
                    {
                        Logger.CurrentLogger.Error("Failed to download video!");
                        return;
                    }
                    Logger.CurrentLogger.Log($"Video downloaded {url}...");
                    LoadFromCobalt(item, file);
                });
            });
        }

        public static void LoadFromCobalt(Item item, CobaltDownload cobaltDownload)
        {
            VideoPlayer videoPlayer = GetVideoPlayer(item);
            FFGodot ff = GetFFGodot(item);
            if (!GodotObject.IsInstanceValid(videoPlayer))
                return;
            if (cobaltDownload.isStream)
            {
                ff.CanSeek = false;
                ff.Play(cobaltDownload.PathToFile, cobaltDownload.PathToFile);
            }
            else
            {
                if (!File.Exists(cobaltDownload.PathToFile))
                    return;
                string filePath = cobaltDownload.PathToFile;
                ff.CanSeek = true;
                ff.Play(filePath, filePath);
            }
        }
    }
}