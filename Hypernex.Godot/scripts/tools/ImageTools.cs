using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using FFmpeg.Godot;
using Godot;
using Hypernex.CCK;

namespace Hypernex.Tools
{
    public static class ImageTools
    {
        public static bool LoadImage(TextureRect rect, byte[] buffer)
        {
            Image img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
            if (IsPng(buffer))
            {
                new Thread(() =>
                {
                    img.LoadPngFromBuffer(buffer);
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (GodotObject.IsInstanceValid(rect))
                            rect.Texture = ImageTexture.CreateFromImage(img);
                    });
                }).Start();
                return true;
            }
            if (IsJpg(buffer))
            {
                new Thread(() =>
                {
                    img.LoadJpgFromBuffer(buffer);
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (GodotObject.IsInstanceValid(rect))
                            rect.Texture = ImageTexture.CreateFromImage(img);
                    });
                }).Start();
                return true;
            }
            return false;
        }

        public static FFPlayGodot LoadFFmpeg(byte[] buffer, TextureRect texture, AudioStreamPlayer3D sound)
        {
            string hash = Convert.ToBase64String(MD5.HashData(buffer));
            string path = DownloadTools.GetFilePath($"media_{hash.Replace("=", null).Replace("/", null)}.cache");
            if (!File.Exists(path))
                File.WriteAllBytes(path, buffer);
            FFPlayGodot ff = new FFPlayGodot();
            FFTexturePlayer ffTex = new FFTexturePlayer();
            FFAudioPlayer ffAud = new FFAudioPlayer();
            ff.texturePlayer = ffTex;
            ff.audioPlayer = ffAud;
            ff.AddChild(ffTex);
            ff.AddChild(ffAud);
            // if (OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
                // ff._hwType = AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC;
            ffTex.OnDisplay = (tex) =>
            {
                texture.Texture = tex;
            };
            ffAud.audioSource = sound;
            texture.AddChild(ff);
            ff.Play(path, path);
            return ff;
        }

        public static void LoadFFmpeg(FFPlayGodot ff, byte[] buffer)
        {
            new Thread(() =>
            {
                // Stopwatch sw = new Stopwatch();
                // sw.Start();
                string hash = DownloadTools.GetBufferHash(buffer);
                // sw.Stop();
                string path = DownloadTools.GetFilePath($"media_{hash}.cache");
                if (!File.Exists(path))
                    File.WriteAllBytes(path, buffer);
                // ff.Log = _ => { };
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    if (GodotObject.IsInstanceValid(ff))
                        ff.Play(path, path);
                });
                // Logger.CurrentLogger.Log($"{sw.ElapsedMilliseconds}ms");
            }).Start();
        }

        public static bool IsVideoStream(Uri uri)
        {
            switch (uri.Scheme.ToLower())
            {
                case "rtmp":
                case "rtsp":
                case "srt":
                case "udp":
                case "tcp":
                    return true;
            }
            string fileName = Path.GetFileName(uri.LocalPath);
            string ext = Path.GetExtension(fileName);
            switch (ext)
            {
                case ".m3u8":
                case ".flv":
                    return true;
            }
            return false;
        }

        public static bool IsPng(byte[] buffer)
        {
            return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4e && buffer[3] == 0x47
            && buffer[4] == 0x0d && buffer[5] == 0x0a && buffer[6] == 0x1a && buffer[7] == 0x0a;
        }

        public static bool IsJpg(byte[] buffer)
        {
            return buffer[0] == 0xff && buffer[1] == 0xd8 && buffer[2] == 0xff;
        }
    }
}