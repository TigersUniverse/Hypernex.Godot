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

public partial class VoiceChat : Node
{
    public const int Bitrate = 64000;
    public const float FrameSize = 20f;
    public const int MaxBytes = 4000;
    public const int OpusSampleRate = 24000;

    public struct VoiceData
    {
        // public int sampleRate;
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
            dir = Path.Combine(dir, "scripts", "plugins");
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                return NativeLibrary.Load(Path.Combine(dir, "opus.dll"));
            case "linux":
                return NativeLibrary.Load(Path.Combine(dir, "libopus.so"));
            default:
                return IntPtr.Zero;
        }
    }

    public override void _Ready()
    {
        if (IsLocalPlayer)
            CreateMic();
        CreateVoice(decoder?.OutputSamplingRate ?? OpusSampleRate);
        stopped = false;
        mutex = new System.Threading.Mutex();
        audioThread = new Thread(() => AudioThreadLoop());
        audioThread.Name = "VoiceChat Thread";
        audioThread.Start();
        GD.Print("VoiceChat Ready!");
    }

    public override void _ExitTree()
    {
        stopped = true;
        DestroyMic();
        DestroyVoice();
        audioThread.Join();
        mutex.Dispose();
        encoder?.Dispose();
        decoder?.Dispose();
    }

    public override void _Process(double delta)
    {
        TickAudio(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        // TickAudio(delta);
    }

    private void TickAudio(double delta)
    {
        playbackPosition += delta;
        /*if (voice != null)
            playbackPosition = voice.GetPlaybackPosition();
        else if (voice3d != null)
            playbackPosition = voice.GetPlaybackPosition();*/
        double delta2 = BufferLength;

        if (voice != null && playbackPosition + delta2 >= BufferLength)
        {
            // voice.Playing = true;
            // playbackPosition -= BufferLength;
            // voice.Play(Mathf.Max(0, (float)playbackPosition));
            playbackPosition = 0;
            playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            ProcessVoice(delta);
        }
        else if (voice3d != null && playbackPosition + delta2 >= BufferLength)
        {
            // voice3d.Playing = true;
            // playbackPosition -= BufferLength;
            // voice3d.Play(Mathf.Max(0, (float)playbackPosition));
            playbackPosition = 0;
            playback = voice3d.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            ProcessVoice(delta);
        }
    }

    private void AudioThreadLoop()
    {
        while (!stopped)
        {
            int ms = 2;
            ProcessMic(ms / 1000d);
            Thread.Sleep(ms);
        }
    }

    private void CreateMic()
    {
        mic = new VoiceMic();
        AddChild(mic);
        int recordBusIndex = AudioServer.GetBusIndex(mic.Bus);
        for (int i = 0; i < AudioServer.GetBusEffectCount(recordBusIndex); i++)
        {
            if (AudioServer.GetBusEffect(recordBusIndex, i) is AudioEffectCapture c)
            {
                capture = c;
                break;
            }
        }
        double[] arr = DesignFilter.FirWinLp(345, 0.15d);
        firFilter = new FirFilter(arr);
    }

    private void DestroyMic()
    {
        if (mic != null)
        {
            mic.QueueFree();
            mic = null;
            capture = null;
            record = null;
            firFilter = null;
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

        generator = new AudioStreamGenerator();
        generator.MixRate = sampleRate;
        generator.BufferLength = BufferLength;
        if (voice != null)
        {
            voice.Stream = generator;
            voice.Playing = true;
            playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }
        else if (voice3d != null)
        {
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

    private void DestroyVoice()
    {
        if (playback == null)
            return;
        if (voiceNode != null)
            voiceNode.QueueFree();
        voiceNode = null;
        voice = null;
        voice3d = null;
        playback = null;
    }

    public void Speak(float[] data, int dataLength)
    {
        last_speak = Time.GetUnixTimeFromSystem();
        for (int i = 0; i < dataLength; i++)
            recieve_buffer.Enqueue(data[i]);
    }

    public void Speak(byte[] data, int encodedLength, int freq, int channels)
    {
        if (decoder == null)
        {
            decoder = OpusDecoder.Create(freq, channels);
            decoder.MaxDataBytes = MaxBytes;
        }
        float[] decodedDataBytes = decoder.DecodeFloat(data, encodedLength, out int decodedLength);
        Speak(decodedDataBytes, decodedLength);
    }

    private void SendRpcSpeak(byte[] data, int encodedLength, int freq, int channels)
    {
        OnSpeak?.Invoke(new VoiceData()
        {
            // sampleRate = OpusSampleRate,
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
            return;
        }

        {
            int len = Mathf.Min(playback.GetFramesAvailable(), recieve_buffer.Count);
            for (int i = 0; i < len; i++)
            {
                // if (!Mathf.IsEqualApprox(recieve_buffer.Peek(), 0f, 0.00001f))
                {
                    playback.PushFrame(new Vector2(recieve_buffer.Peek(), recieve_buffer.Peek()));
                }
                recieve_buffer.Dequeue();
            }
        }
    }

    private void ProcessMic(double delta)
    {
        if (!IsLocalPlayer)
            return;
        if (Recording)
        {
            if (!prev_frame_recording)
            {
                capture.ClearBuffer();
                queue.Clear();
            }

            {
                discarded = capture.GetDiscardedFrames();

                Vector2[] stereo_data = capture.GetBuffer(capture.GetFramesAvailable());
                if (stereo_data.Length > 0)
                {
                    float[] data = new float[stereo_data.Length];
                    for (int i = 0; i < stereo_data.Length; i++)
                    {
                        float value = (stereo_data[i].X + stereo_data[i].Y) / 2f;
                        data[i] = value;
                    }

                    if (OpusSampleRate == (int)AudioServer.GetMixRate())
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (!Mathf.IsEqualApprox(data[i], 0f, 0.00001f))
                                queue.Enqueue(data[i]);
                        }
                    }
                    else
                    {
                        // DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetMixRate(), data, false), OpusSampleRate);
                        DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetMixRate(), data, false), OpusSampleRate, firFilter);
                        for (int i = 0; i < signal.Samples.Length; i++)
                        {
                            if (!Mathf.IsEqualApprox(signal.Samples[i], 0f, 0.00001f))
                                queue.Enqueue(signal.Samples[i]);
                        }
                    }
                }
            }

            int length = Mathf.RoundToInt(FrameSize / 1000f * OpusSampleRate);
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
                    encoder = OpusEncoder.Create(OpusSampleRate, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
                    encoder.Bitrate = Bitrate;
                    encoder.MaxDataBytes = MaxBytes;
                }

                byte[] encodedDataBytes = encoder.EncodeFloat(data, data.Length, out int encodedLength);
                byte[] encodedDataFinal = new byte[data.Length * sizeof(float)];
                Array.Copy(encodedDataBytes, encodedDataFinal, encodedDataFinal.Length);

                if (DirectListen)
                    Speak(data, data.Length);
                else if (Listen)
                    Speak(encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);

                CallDeferred(nameof(SendRpcSpeak), encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);
            }
        }
        prev_frame_recording = Recording;
    }
}
