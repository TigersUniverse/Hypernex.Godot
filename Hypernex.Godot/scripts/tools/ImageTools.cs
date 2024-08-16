using System;
using System.IO;
using System.Security.Cryptography;
using FFmpeg.Godot;
using Godot;

namespace Hypernex.Tools
{
    public static class ImageTools
    {
        public static Image LoadImage(byte[] buffer)
        {
            Image img = new Image();
            Error err;
            if (IsPng(buffer))
            {
                err = img.LoadPngFromBuffer(buffer);
                if (err == Error.Ok)
                    return img;
            }
            if (IsJpg(buffer))
            {
                err = img.LoadJpgFromBuffer(buffer);
                if (err == Error.Ok)
                    return img;
            }
            return null;
        }

        public static FFGodot LoadFFmpeg(byte[] buffer, TextureRect texture, AudioStreamPlayer3D sound)
        {
            string hash = Convert.ToBase64String(MD5.HashData(buffer));
            string path = DownloadTools.GetFilePath($"media_{hash}.cache");
            if (!File.Exists(path))
                File.WriteAllBytes(path, buffer);
            FFGodot ff = new FFGodot();
            ff.renderMesh = texture;
            ff.source = sound;
            texture.AddChild(ff);
            ff.Play(path, path);
            ff.Pause();
            return ff;
        }

        public static void LoadFFmpeg(FFGodot ff, byte[] buffer)
        {
            string hash = Convert.ToBase64String(MD5.HashData(buffer));
            string path = DownloadTools.GetFilePath($"media_{hash.Replace("=", null).Replace("/", null)}.cache");
            if (!File.Exists(path))
                File.WriteAllBytes(path, buffer);
            ff.Log = _ => { };
            ff.Play(path, path);
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