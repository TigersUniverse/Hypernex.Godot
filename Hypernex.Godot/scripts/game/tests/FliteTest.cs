using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;

public partial class FliteTest : AudioStreamPlayer3D
{
    [Export]
    public LineEdit lineEdit;

    public override void _Ready()
    {
        NativeLibrary.SetDllImportResolver(typeof(Flite.FliteNativeApi).Assembly, Resolver);
    }

    public void PlayText()
    {
        Play(lineEdit.Text);
    }

    public unsafe void Play(string text)
    {
        Flite.FliteNativeApi.flite_init();
        var voice = Flite.FliteNativeApi.register_cmu_us_awb(null);
        var wave = Flite.FliteNativeApi.FliteTextToWave(text, voice);
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

    private IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Flite.FliteNativeApi.LibName)
        {
            return IntPtr.Zero;
        }
        string dir = Directory.GetCurrentDirectory();
        if (EngineDebugger.IsActive())
        {
            dir = Path.Combine(dir, "scripts", "plugins");
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                return NativeLibrary.Load(Path.Combine(dir, $"{Flite.FliteNativeApi.LibName}.dll"));
            case "linux":
                return NativeLibrary.Load(Path.Combine(dir, $"lib{Flite.FliteNativeApi.LibName}.so"));
            default:
                return IntPtr.Zero;
        }
    }
}