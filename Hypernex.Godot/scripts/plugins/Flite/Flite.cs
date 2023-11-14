using System;
using System.Runtime.InteropServices;

namespace Flite
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CstWave
    {
        public string type;
        public int sample_rate;
        public int num_samples;
        public int num_channels;
        public short *samples;
    }

    public static unsafe class FliteNativeApi
    {
        public const string LibName = "flite_cmu_us_awb";

        [DllImport(LibName)]
        public static extern int flite_init();

        [DllImport(LibName)]
        public static extern IntPtr flite_text_to_wave(string text, IntPtr voice);

        [DllImport(LibName)]
        public static extern IntPtr flite_voice_load(string voice_filename);

        [DllImport(LibName)]
        public static extern IntPtr register_cmu_us_awb(string voxdir);

        public static CstWave FliteTextToWave(string text, IntPtr voice)
        {
            return Marshal.PtrToStructure<CstWave>(flite_text_to_wave(text, voice));
        }
    }
}