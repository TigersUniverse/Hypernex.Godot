using System;
using System.IO;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.Configuration;
using Nexbox;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Streaming
    {
        internal static YoutubeDL ytdl = new();

        internal static bool IsStream(Uri uri)
        {
            bool isStream = false;
            switch (uri.Scheme.ToLower())
            {
                case "rtmp":
                case "rtsp":
                case "srt":
                case "udp":
                case "tcp":
                    isStream = true;
                    break;
            }
            if (isStream) return true;
            string fileName = Path.GetFileName(uri.LocalPath);
            string ext = Path.GetExtension(fileName);
            switch (ext)
            {
                case ".m3u8":
                case ".mpd":
                case ".flv":
                    isStream = true;
                    break;
            }
            return isStream;
        }

        public static async void Download(string url, object onDone, StreamDownloadOptions options)
        {
            try
            {
                Uri uri = new Uri(url);
                bool trusted = !ConfigManager.LoadedConfig.UseTrustedURLs;
                if (!trusted)
                {
                    foreach (Uri trustedUri in ConfigManager.LoadedConfig.TrustedURLs
                                 .Select(x => new Uri(x)))
                    {
                        if (uri.Host != trustedUri.Host) continue;
                        trusted = true;
                        break;
                    }
                }
                if (!trusted)
                {
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
                    return;
                }
                RunResult<VideoData> metaResult = await ytdl.RunVideoDataFetch(url);
                string liveUrl = String.Empty;
                if (metaResult.Success)
                {
                    switch (metaResult.Data.LiveStatus)
                    {
                        case LiveStatus.IsLive:
                            liveUrl = metaResult.Data.Url;
                            break;
                        case LiveStatus.IsUpcoming:
                            throw new Exception("Invalid LiveStream!");
                    }
                }
                if (!string.IsNullOrEmpty(liveUrl))
                {
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                        new StreamDownload(liveUrl, true));
                    return;
                }
                if (IsStream(uri))
                {
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                        new StreamDownload(url, true));
                    return;
                }
                OptionSet optionSet = new OptionSet
                {
                    Format = "bestvideo[vcodec=vp8]/bestvideo[vcodec=h264]+bestaudio/best"
                };
                RunResult<string> runResult;
                runResult = options.AudioOnly
                    ? await ytdl.RunAudioDownload(url, overrideOptions: optionSet)
                    : await ytdl.RunVideoDownload(url, overrideOptions: optionSet);
                if (!runResult.Success)
                {
                    foreach (string s in runResult.ErrorOutput)
                        Logger.CurrentLogger.Error(s);
                    throw new Exception("Failed to get data!");
                }
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone),
                    new StreamDownload(runResult.Data, false));
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
            }
        }

        internal static async void DownloadInternal(string url, Action<StreamDownload> onDone, StreamDownloadOptions options)
        {
            try
            {
                Uri uri = new Uri(url);
                bool trusted = !ConfigManager.LoadedConfig.UseTrustedURLs;
                if (!trusted)
                {
                    foreach (Uri trustedUri in ConfigManager.LoadedConfig.TrustedURLs
                                 .Select(x => new Uri(x)))
                    {
                        if (uri.Host != trustedUri.Host) continue;
                        trusted = true;
                        break;
                    }
                }
                if (!trusted)
                {
                    onDone(null);
                    return;
                }
                RunResult<VideoData> metaResult = await ytdl.RunVideoDataFetch(url);
                string liveUrl = String.Empty;
                if (metaResult.Success)
                {
                    switch (metaResult.Data.LiveStatus)
                    {
                        case LiveStatus.IsLive:
                            liveUrl = metaResult.Data.Url;
                            break;
                        case LiveStatus.IsUpcoming:
                            throw new Exception("Invalid LiveStream!");
                    }
                }
                if (!string.IsNullOrEmpty(liveUrl))
                {
                    onDone(new StreamDownload(liveUrl, true));
                    return;
                }
                if (IsStream(uri))
                {
                    onDone(new StreamDownload(url, true));
                    return;
                }
                OptionSet optionSet = new OptionSet
                {
                    Format = "bestvideo[vcodec=vp8]/bestvideo[vcodec=h264]+bestaudio/best"
                };
                RunResult<string> runResult;
                runResult = options.AudioOnly
                    ? await ytdl.RunAudioDownload(url, overrideOptions: optionSet)
                    : await ytdl.RunVideoDownload(url, overrideOptions: optionSet);
                if (!runResult.Success)
                {
                    foreach (string s in runResult.ErrorOutput)
                        Logger.CurrentLogger.Error(s);
                    throw new Exception("Failed to get data!");
                }
                onDone(new StreamDownload(runResult.Data, false));
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                onDone(null);
            }
        }

        public static void Download(string url, object onDone) => Download(url, onDone, default);
        
        public struct StreamDownloadOptions
        {
            public bool AudioOnly;

            public StreamDownloadOptions(bool audioOnly = false)
            {
                AudioOnly = audioOnly;
            }
        }

        public class StreamDownload
        {
            internal string pathToFile;
            internal bool isStream;

            public StreamDownload() { throw new Exception("Cannot Instantiate StreamDownload"); }

            internal StreamDownload(string pathToFile, bool isStream)
            {
                this.pathToFile = pathToFile;
                this.isStream = isStream;
            }
        }
    }
}