using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class AudioStreamConverter : IObjectConverter
    {
        public bool CanConvert(string type)
        {
            return ClassDB.IsParentClass(type, nameof(AudioStream));
        }

        public GodotObject Convert(ConvertDB db, string type, object data)
        {
            JObject obj = (JObject)data;
            AudioStream audio = null;
            switch ((string)obj["type"])
            {
                case "wav":
                    AudioStreamWav wav = new AudioStreamWav();
                    wav.Data = obj["data"].ToObject<byte[]>();
                    wav.Format = (AudioStreamWav.FormatEnum)(long)obj["format"];
                    wav.MixRate = (int)(long)obj["mix_rate"];
                    wav.Stereo = (bool)obj["stereo"];
                    audio = wav;
                    break;
                case "mp3":
                    AudioStreamMP3 mp3 = new AudioStreamMP3();
                    mp3.Data = obj["data"].ToObject<byte[]>();
                    audio = mp3;
                    break;
            }
            return audio;
        }

        public object ConvertObject(ConvertDB db, string type, GodotObject data)
        {
            Dictionary<string, JToken> dict = new Dictionary<string, JToken>();
            AudioStream audio = (AudioStream)data;
            switch (audio)
            {
                case AudioStreamWav wav:
                    dict.Add("type", "wav");
                    dict.Add("data", wav.Data);
                    dict.Add("format", (long)wav.Format);
                    dict.Add("mix_rate", (long)wav.MixRate);
                    dict.Add("stereo", wav.Stereo);
                    break;
                case AudioStreamMP3 mp3:
                    dict.Add("type", "mp3");
                    dict.Add("data", mp3.Data);
                    break;
            }
            return dict;
        }
    }
}
