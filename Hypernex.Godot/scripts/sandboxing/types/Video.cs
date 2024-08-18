using System;
using System.IO;
using FFmpeg.Godot;
using Godot;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Video
    {
        private static VideoPlayer GetVideoPlayer(Item item)
        {
            VideoPlayer v = item.t as VideoPlayer;
            if (!GodotObject.IsInstanceValid(v)) return null;
            return v;
        }

        private static FFGodot GetFFGodot(Item item)
        {
            VideoPlayer v = item.t as VideoPlayer;
            if (!GodotObject.IsInstanceValid(v)) return null;
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
        
        public static float GetVolume(Item item) => Mathf.DbToLinear(GetFFGodot(item).source.VolumeDb);

        public static void SetVolume(Item item, float value) => GetFFGodot(item).source.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(value, 0f, 1f));
        
        public static double GetPosition(Item item) => GetFFGodot(item).PlaybackTime;
        public static void SetPosition(Item item, float value) => GetFFGodot(item).Seek(value);

        public static double GetLength(Item item)
        {
            throw new NotImplementedException();
        }

        public static void LoadUrl(Item item, string url)
        {
            var ff = GetFFGodot(item);
            DownloadTools.DownloadBytes(url, data =>
            {
                if (GodotObject.IsInstanceValid(ff))
                {
                    ImageTools.LoadFFmpeg(ff, data);
                }
            });
        }

        /*
        public static void LoadFromCobalt(Item item, CobaltDownload cobaltDownload)
        {
            VideoPlayerDescriptor videoPlayerDescriptor = GetVideoPlayerDescriptor(item);
            if(videoPlayerDescriptor == null)
                return;
            if (cobaltDownload.isStream)
            {
                IVideoPlayer videoPlayer = videoPlayerDescriptor.Replace(
                    VideoPlayerManager.GetVideoPlayerType(new Uri(cobaltDownload.PathToFile)) ??
                    VideoPlayerManager.DefaultVideoPlayerType);
                if (videoPlayer == null)
                    return;
                videoPlayer.Source = cobaltDownload.PathToFile;
            }
            else
            {
                if (!File.Exists(cobaltDownload.PathToFile))
                    return;
                string filePath = "file:///" + cobaltDownload.PathToFile;
                IVideoPlayer videoPlayer = videoPlayerDescriptor.Replace(
                    VideoPlayerManager.GetVideoPlayerType(new Uri(filePath)) ??
                    VideoPlayerManager.DefaultVideoPlayerType);
                if (videoPlayer == null)
                    return;
                videoPlayer.Source = filePath;
            }
        }
        */
    }
}