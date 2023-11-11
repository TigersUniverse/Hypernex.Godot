using System.IO;
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

        public static VideoStream LoadFFmpeg(byte[] buffer)
        {
            string path = Path.GetTempFileName();
            File.WriteAllBytes(path, buffer);
            var asset = ClassDB.Instantiate("FFmpegVideoStream").AsGodotObject();
            if (asset is VideoStream vid)
            {
                vid.File = path;
                return vid;
            }
            // This should never happen.
            return null;
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