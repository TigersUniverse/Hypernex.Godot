using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using FragLabs.Audio.Codecs;
using Godot;
using NWaves.Filters.Base;
using NWaves.Filters.Fda;
using NWaves.Operations;
using NWaves.Signals;
using FileAccess = Godot.FileAccess;

public partial class VoiceChat : Node
{
    public const int Bitrate = 64000;
    public const float FrameSize = 20f;
    public const int MaxBytes = 5760;
    public const int OpusSampleRate = 24000;

    public struct VoiceData
    {
        // public int sampleRate;
        public float[] pcm;
        public byte[] data;
        public int encodedLength;
        public int freq;
        public int channels;
    }

    [Export]
    public NodePath CustomVoiceAudioPlayer;
    [Export]
    public bool Recording = false;
    [Export]
    public bool Listen = false;
    [Export]
    public bool DirectListen = false;
    [Export]
    public float BufferLength = 0.1f;
    [Export]
    public bool IsLocalPlayer = false;
    private AudioEffectCapture capture;
    private AudioEffectSpectrumAnalyzerInstance analyzer;
    private AudioEffectRecord record;
    public VoiceMic mic;
    private AudioStreamGenerator generator;
    private AudioStreamGeneratorPlayback playback;
    private Queue<float> recieve_buffer = new Queue<float>();
    private bool prev_frame_recording = false;
    private Node voiceNode;
    private AudioStreamPlayer voice;
    private AudioStreamPlayer3D voice3d;
    private OpusDecoder decoder;
    private OpusEncoder encoder;
    private FirFilter firFilter;
    private Queue<float> queue = new Queue<float>();
    private double last_speak;
    public bool IsSpeaking => Time.GetUnixTimeFromSystem() - last_speak <= 0.5d || Recording;
    private long discarded;
    private double playbackPosition;
    private ulong playPositionTicks;
    private Thread audioThread;
    private bool stopped = false;
    private System.Threading.Mutex mutex;
    public Action<VoiceData> OnSpeak = _ => { };
    public AudioBus Bus { get; private set; }
    public float Loudness { get; private set; }

    static VoiceChat()
    {
        NativeLibrary.SetDllImportResolver(typeof(OpusDecoder).Assembly, Resolver);
    }

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "opus")
        {
            return IntPtr.Zero;
        }
        string dir = Directory.GetCurrentDirectory();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(dir, "addons");
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                return NativeLibrary.Load(Path.Combine(dir, "opus.dll"));
            case "linux":
                return NativeLibrary.Load(Path.Combine(dir, "libopus.so"));
            case "android":
                var file = FileAccess.Open("res://libopus_android.so", FileAccess.ModeFlags.Read);
                var data = file.GetBuffer((long)file.GetLength());
                string path = Path.Combine(OS.GetUserDataDir(), "libopus_android.so");
                File.WriteAllBytes(path, data);
                return NativeLibrary.Load(path);
            default:
                return IntPtr.Zero;
        }
    }

    public override void _Ready()
    {
        CreateVoice(decoder?.OutputSamplingRate ?? OpusSampleRate);
        stopped = false;
        mutex = new System.Threading.Mutex();
        // audioThread = new Thread(() => AudioThreadLoop());
        // audioThread.Name = "VoiceChat Thread";
        // audioThread.Start();
    }

    public override void _ExitTree()
    {
        stopped = true;
        DestroyMic();
        DestroyVoice();
        // audioThread.Join();
        mutex.Dispose();
        encoder?.Dispose();
        decoder?.Dispose();
    }

    public override void _Process(double delta)
    {
        // Recording = Input.IsActionPressed("player_voice_chat") && GetMultiplayerAuthority() == Multiplayer.GetUniqueId(); // TODO: remove this line and put in the player classes
        if (Recording && IsLocalPlayer)
            CreateMic();
        else
            DestroyMic();

        ProcessMic(delta);
        TickAudio(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        // TickAudio(delta);
    }

    private void TickAudio(double delta)
    {
        playbackPosition += delta;

        double delta2 = delta;

        if (voice != null && playbackPosition + delta2 >= BufferLength)
        {
            // voice.Playing = true;
            playbackPosition = 0;
            // if (mutex.WaitOne(0))
            {
                playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                // mutex.ReleaseMutex();
            }
            ProcessVoice(delta);
        }
        else if (voice3d != null && playbackPosition + delta2 >= BufferLength)
        {
            // voice3d.Playing = true;
            playbackPosition = 0;
            // if (mutex.WaitOne(0))
            {
                playback = voice3d.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                // mutex.ReleaseMutex();
            }
            ProcessVoice(delta);
        }

        if (IsInstanceValid(analyzer))
        {
            var mag = analyzer.GetMagnitudeForFrequencyRange(1f, AudioServer.GetMixRate(), AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Average);
            Loudness = mag.Length();
        }
        else
            Loudness = 0f;
        ProcessVoice(delta);
    }

    private void AudioThreadLoop()
    {
        while (!stopped)
        {
            // break;
            int ms = 2;
            ProcessMic(ms / 1000d);
            // if (mutex.WaitOne(0))
            {
                // ProcessVoice(ms / 1000d);
                // mutex.ReleaseMutex();
            }
            Thread.Sleep(ms);
        }
    }

    private void CreateMic()
    {
        if (IsInstanceValid(mic))
            return;
        mic = new VoiceMic();
        AddChild(mic);
        int recordBusIndex = AudioServer.GetBusIndex(mic.Bus);
        for (int i = 0; i < AudioServer.GetBusEffectCount(recordBusIndex); i++)
        {
            if (AudioServer.GetBusEffect(recordBusIndex, i) is AudioEffectCapture c)
            {
                capture = c;
                // break;
            }
            if (AudioServer.GetBusEffect(recordBusIndex, i) is AudioEffectSpectrumAnalyzer && AudioServer.GetBusEffectInstance(recordBusIndex, i) is AudioEffectSpectrumAnalyzerInstance a)
            {
                analyzer = a;
            }
        }
        // capture.BufferLength = BufferLength;
        double[] arr = DesignFilter.FirWinLp(345, 0.15d);
        firFilter ??= new FirFilter(arr);
    }

    private void DestroyMic()
    {
        if (IsInstanceValid(mic))
        {
            mic.Free();
            mic = null;
            capture = null;
            record = null;
            // firFilter = null;
        }
    }

    private void CreateVoice(int sampleRate)
    {
        if (CustomVoiceAudioPlayer != null && !CustomVoiceAudioPlayer.IsEmpty)
        {
            Node player = GetNode(CustomVoiceAudioPlayer);
            if (player != null)
            {
                if (player is AudioStreamPlayer)
                {
                    voice = player as AudioStreamPlayer;
                    voice3d = null;
                    voiceNode = null;
                }
                else if (player is AudioStreamPlayer3D)
                {
                    voice = null;
                    voice3d = player as AudioStreamPlayer3D;
                    voiceNode = null;
                }
                else
                {
                    GD.PrintErr($"Node {CustomVoiceAudioPlayer} is not any kind of AudioStreamPlayer!");
                }
            }
            else
            {
                GD.PrintErr($"Node {CustomVoiceAudioPlayer} does not exist!");
            }
        }
        else
        {
            voice = new AudioStreamPlayer();
            voice3d = null;
            voiceNode = voice;
            AddChild(voice);
        }

        Bus = new AudioBus();
        Bus.SetParent("Voice");
        Bus.AddEffect(new AudioEffectSpectrumAnalyzer()
        {
            BufferLength = 0.1f,
            TapBackPos = 0.1f,
            FftSize = AudioEffectSpectrumAnalyzer.FftSizeEnum.Size256,
        });
        if (!Recording)
            analyzer = (AudioEffectSpectrumAnalyzerInstance)Bus.GetEffectInstance(0);

        generator = new AudioStreamGenerator();
        generator.MixRate = sampleRate;
        generator.BufferLength = BufferLength;
        if (voice != null)
        {
            Bus.SetParent(voice.GetMeta("VoiceBus", "Voice").AsString());
            voice.Bus = Bus.Name;
            voice.Stream = generator;
            voice.Playing = true;
            playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }
        else if (voice3d != null)
        {
            Bus.SetParent(voice3d.GetMeta("VoiceBus", "Voice").AsString());
            voice3d.Bus = Bus.Name;
            voice3d.Stream = generator;
            voice3d.Playing = true;
            playback = voice3d.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }
        playbackPosition = 0;
    }

    public void SetVoice(Node node)
    {
        CustomVoiceAudioPlayer = node.GetPath();
        DestroyVoice();
        CreateVoice(decoder?.OutputSamplingRate ?? OpusSampleRate);
    }

    public Node GetVoice()
    {
        if (IsInstanceValid(voice))
            return voice;
        if (IsInstanceValid(voice3d))
            return voice3d;
        return null;
    }

    private void DestroyVoice()
    {
        if (playback == null)
            return;
        if (voiceNode != null)
            voiceNode.QueueFree();
        voiceNode = null;
        voice = null;
        voice3d = null;
        // generator.Free();
        playback = null;
        Bus?.Dispose();
        Bus = null;
        // GD.Print("Destroy Voice");
    }

    public void Speak(float[] data, int dataLength)
    {
        last_speak = Time.GetUnixTimeFromSystem();
        for (int i = 0; i < dataLength; i++)
            recieve_buffer.Enqueue(data[i]);
    }

    public void Speak(byte[] data, int encodedLength, int freq, int channels)
    {
        last_speak = Time.GetUnixTimeFromSystem();
        if (string.IsNullOrEmpty(RenderingServer.GetVideoAdapterName()))
            return;
        if (decoder == null)
        {
            decoder = OpusDecoder.Create(freq, channels);
            decoder.MaxDataBytes = MaxBytes;
        }
        float[] decodedDataBytes = decoder.DecodeFloat(data, encodedLength, out int decodedLength);
        float[] decodedData = new float[decodedLength];
        Array.Copy(decodedDataBytes, decodedData, decodedData.Length);
        Speak(decodedData, decodedLength);
    }

    private void SendRpcSpeak(float[] pcmData, byte[] data, int encodedLength, int freq, int channels)
    {
        OnSpeak?.Invoke(new VoiceData()
        {
            // sampleRate = OpusSampleRate,
            pcm = pcmData,
            data = data,
            encodedLength = encodedLength,
            freq = freq,
            channels = channels,
        });
    }

    private void ProcessVoice(double delta)
    {
        if (playback == null)
            return;
        if (playback.GetFramesAvailable() < 1)
        {
            // GD.Print(recieve_buffer.Count);
            return;
        }

        // if (recieve_buffer.Count >= (generator.BufferLength * 0.5d - delta * 1) * OpusSampleRate)
        // if (recieve_buffer.Count >= playback.GetFramesAvailable())
        {
            int len = Mathf.Min(playback.GetFramesAvailable(), recieve_buffer.Count);
            // len = recieve_buffer.Count;
            for (int i = 0; i < len; i++)
            {
                // if (!Mathf.IsEqualApprox(recieve_buffer.Peek(), 0f, 0.00001f))
                {
                    playback.PushFrame(new Vector2(recieve_buffer.Peek(), recieve_buffer.Peek()));
                }
                recieve_buffer.Dequeue();
            }
            // GD.Print(playback.GetFramesAvailable());
            // if (recieve_buffer.Count > 0)
                // GD.Print(recieve_buffer.Count);
        }
    }

    private void ProcessMic(double delta)
    {
        if (Recording && IsInstanceValid(mic))
        {
            var mag = analyzer.GetMagnitudeForFrequencyRange(1f, AudioServer.GetMixRate(), AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Average);
            Loudness = mag.Length();

            // if (capture == null)
            //     CreateMic();

            if (!prev_frame_recording)
            {
                capture.ClearBuffer();
                queue.Clear();
            }

            // if (capture.GetFramesAvailable() >= capture.BufferLength * 0.5f * OpusSampleRate)
            {
                // for (long i = discarded; i < capture.GetDiscardedFrames(); i++)
                {
                    // queue.Enqueue(0f);
                }
                discarded = capture.GetDiscardedFrames();
                // GD.Print(discarded);

                Vector2[] stereo_data = capture.GetBuffer(capture.GetFramesAvailable());
                if (stereo_data.Length > 0)
                {
                    float[] data = new float[stereo_data.Length];
                    for (int i = 0; i < stereo_data.Length; i++)
                    {
                        float value = (stereo_data[i].X + stereo_data[i].Y) / 2f;
                        data[i] = value;
                    }

                    if (OpusSampleRate == (int)AudioServer.GetMixRate() && false)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            // if (!Mathf.IsEqualApprox(data[i], 0f, 0.00001f))
                                queue.Enqueue(data[i]);
                        }
                    }
                    else
                    {
                        // DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetMixRate(), data, false), OpusSampleRate);
                        DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetMixRate(), data, true), OpusSampleRate, firFilter);
                        for (int i = 0; i < signal.Samples.Length; i++)
                        {
                            if (!Mathf.IsEqualApprox(signal.Samples[i], 0f, 0.00001f) /*|| !Settings.User.VoiceDebug.HasFlag(Settings.VoiceDebugFlags.RemoveZeros)*/)
                                queue.Enqueue(signal.Samples[i]);
                        }
                    }
                }
            }

            int length = (int)(FrameSize / 1000f * OpusSampleRate);
            while (queue.Count >= length)
            {
                int stereo_data_length = length;

                float[] data = new float[stereo_data_length];
                byte[] dataBytes = new byte[stereo_data_length * sizeof(float)];
                float max_value = 0.0f;
                float min_value = 0.0f;

                for (int i = 0; i < stereo_data_length; i++)
                {
                    float value = queue.Dequeue();
                    max_value = Mathf.Max(value, max_value);
                    min_value = Mathf.Min(value, min_value);
                    data[i] = value;
                    byte[] d = new byte[sizeof(float)];
                    BinaryPrimitives.WriteSingleLittleEndian(d, value);
                    for (int j = 0; j < sizeof(float); j++)
                        dataBytes[i * sizeof(float) + j] = d[j];
                }

                if (encoder == null)
                {
                    encoder = OpusEncoder.Create(OpusSampleRate, 1, FragLabs.Audio.Codecs.Opus.Application.Audio);
                    encoder.Bitrate = Bitrate;
                    encoder.MaxDataBytes = MaxBytes;
                }

                byte[] encodedDataBytes = encoder.EncodeFloat(data, data.Length, out int encodedLength);
                // GD.PrintS(encodedDataBytes.Length, encodedLength);
                // GD.PrintS(encodedDataBytes.Join());
                byte[] encodedDataFinal = new byte[encodedLength];
                Array.Copy(encodedDataBytes, encodedDataFinal, encodedDataFinal.Length);

                if (DirectListen)
                    Speak(data, data.Length);
                // else if (Listen)
                //     RpcSpeak(encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);

                CallDeferred(nameof(SendRpcSpeak), data, encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);
                // Rpc(nameof(RpcSpeak), encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);
            }
        }
        else if (IsInstanceValid(mic))
        {
            capture.ClearBuffer();
        }
        else if (capture != null)
        {
            // capture.GetBuffer(capture.GetFramesAvailable());
            // DestroyMic();
        }
        prev_frame_recording = Recording;
    }
}