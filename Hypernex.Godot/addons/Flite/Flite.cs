using System;
using System.Collections.Generic;
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
        public const string LibName = "flite";
        public const CharSet LibCharSet = CharSet.Ansi;
        public const CallingConvention LibCallConv = CallingConvention.Cdecl;

        public static readonly List<IntPtr> GlobalStrings = new List<IntPtr>();

        [UnmanagedFunctionPointer(LibCallConv, CharSet = LibCharSet)]
        public delegate void lang_init(IntPtr v);
        [UnmanagedFunctionPointer(LibCallConv, CharSet = LibCharSet)]
        public delegate IntPtr lex_init();

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern int flite_init();

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void delete_voice(IntPtr u);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void cst_free(IntPtr u);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr flite_text_to_wave([MarshalAs(UnmanagedType.LPStr)] string text, IntPtr voice);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr flite_voice_select([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr flite_voice_load([MarshalAs(UnmanagedType.LPStr)] string voice_filename);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr register_cmu_us_kal16([MarshalAs(UnmanagedType.LPStr)] string voxdir);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void unregister_cmu_us_kal16(IntPtr vox);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern int flite_add_lang(char *langname, lang_init lang_init, lex_init lex_init);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void usenglish_init(IntPtr v);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr cmulex_init();

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void cmu_indic_lang_init(IntPtr v);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr cmu_indic_lex_init();

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern void cmu_grapheme_lang_init(IntPtr v);

        [DllImport(LibName, CharSet = LibCharSet, CallingConvention = LibCallConv)]
        public static extern IntPtr cmu_grapheme_lex_init();

        public static void AddEnglish()
        {
            AddLanguage("eng", usenglish_init, cmulex_init);
            AddLanguage("usenglish", usenglish_init, cmulex_init);
            AddLanguage("cmu_indic_lang",cmu_indic_lang_init, cmu_indic_lex_init);
            AddLanguage("cmu_grapheme_lang",cmu_grapheme_lang_init, cmu_grapheme_lex_init);
        }

        public static int AddLanguage(string str, lang_init lang, lex_init lex)
        {
            IntPtr j = Marshal.StringToHGlobalAnsi(str);
            GlobalStrings.Add(j);
            return flite_add_lang((char *)j, lang, lex);
        }

        public static CstWave FliteTextToWave(string text, IntPtr voice)
        {
            IntPtr ptr = flite_text_to_wave(text, voice);
            CstWave wave = Marshal.PtrToStructure<CstWave>(ptr);
            cst_free(ptr);
            return wave;
        }
    }
}