
using System;
using System.IO;
using Godot;

namespace Hypernex.Player
{
    public static class TTSMessage
    {
        static TTSMessage()
        {
            InitTTS();
        }

        public static void InitTTS()
        {
            Flite.FliteNativeApi.flite_init();
            Flite.FliteNativeApi.AddEnglish();
        }

        public static unsafe void PlayMessageOn(this AudioStreamPlayer3D player, string text)
        {
            /*string temp = Path.Combine(OS.GetUserDataDir(), "cmu_us_rms.flitevox");
            if (!File.Exists(temp))
            {
                using Godot.FileAccess fileAccess = Godot.FileAccess.Open("res://voices/cmu_us_rms.flitevox", Godot.FileAccess.ModeFlags.Read);
                File.WriteAllBytes(temp, fileAccess.GetBuffer((long)fileAccess.GetLength()));
            }*/
            // var voice = Flite.FliteNativeApi.flite_voice_load(temp);
            var voice = Flite.FliteNativeApi.register_cmu_us_kal16(null);
            var wave = Flite.FliteNativeApi.FliteTextToWave(text, voice);
            Flite.FliteNativeApi.unregister_cmu_us_kal16(voice);
            // Flite.FliteNativeApi.delete_voice(voice);
            var generator = new AudioStreamGenerator();
            generator.MixRate = wave.sample_rate;
            generator.BufferLength = (float)wave.num_samples / wave.sample_rate;
            player.Stream = generator;
            player.Playing = true;
            var playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            int len = Mathf.Min(playback.GetFramesAvailable(), wave.num_samples);
            for (int i = 0; i < len; i++)
            {
                float sample = (float)wave.samples[i] / short.MaxValue;
                playback.PushFrame(new Vector2(sample, sample));
            }
        }

        public static unsafe AudioStreamPlayer3D PlayMessage(this Node3D node, string text)
        {
            var player = new AudioStreamPlayer3D();
            node.AddChild(player);
            PlayMessageOn(player, text);
            return player;
        }
    }
}