using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using Hypernex.Player;

public partial class FliteTest : AudioStreamPlayer3D
{
    [Export]
    public LineEdit lineEdit;

    public override void _Ready()
    {
        NativeLibrary.SetDllImportResolver(typeof(Flite.FliteNativeApi).Assembly, Resolver);
        // TTSMessage.InitTTS();
        lineEdit.GrabFocus();
    }

    public void PlayText()
    {
        Play(lineEdit.Text);
    }

    public unsafe void Play(string text)
    {
        this.PlayMessageOn(text);
        return;
        IntPtr voice = Flite.FliteNativeApi.flite_voice_load(Path.Combine(Directory.GetCurrentDirectory(), "voices", "cmu_us_rms.flitevox"));
        if (voice == IntPtr.Zero)
        {
            GD.PrintErr("0");
            return;
        }
        // var voice = Flite.FliteNativeApi.register_cmu_us_awb(null);
        var wave = Flite.FliteNativeApi.FliteTextToWave(text, voice);
        // Flite.FliteNativeApi.unregister_cmu_us_awb(voice);
        Flite.FliteNativeApi.delete_voice(voice);
        var generator = new AudioStreamGenerator();
        generator.MixRate = wave.sample_rate;
        generator.BufferLength = (float)wave.num_samples / wave.sample_rate;
        Stream = generator;
        Playing = true;
        var playback = GetStreamPlayback() as AudioStreamGeneratorPlayback;
        int len = Mathf.Min(playback.GetFramesAvailable(), wave.num_samples);
        for (int i = 0; i < len; i++)
        {
            float sample = (float)wave.samples[i] / short.MaxValue;
            playback.PushFrame(new Vector2(sample, sample));
        }
    }

    private IntPtr fliteLibHandle = IntPtr.Zero;

    private IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Flite.FliteNativeApi.LibName)
            return IntPtr.Zero;
        if (fliteLibHandle != IntPtr.Zero)
            return fliteLibHandle;
        string dir = Directory.GetCurrentDirectory();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(dir, "scripts", "plugins");
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                fliteLibHandle = NativeLibrary.Load(Path.Combine(dir, $"{Flite.FliteNativeApi.LibName}.dll"));
                break;
            case "linux":
                fliteLibHandle = NativeLibrary.Load(Path.Combine(dir, $"lib{Flite.FliteNativeApi.LibName}.so"));
                break;
            default:
                return IntPtr.Zero;
        }
        return fliteLibHandle;
    }
}