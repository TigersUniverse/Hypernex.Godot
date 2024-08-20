using Godot;

public static unsafe partial class DynamicallyLinkedBindings
{
#if GODOT_WINDOWS
    public const string avformat = "avformat-60";
    public const string avutil = "avutil-58";
    public const string avcodec = "avcodec-60";
    public const string avdevice = "avdevice-60";
    public const string avfilter = "avfilter-9";
    public const string swscale = "swscale-7";
    public const string swresample = "swresample-4";
#elif GODOT_LINUXBSD
    public const string avformat = "libavformat.so.60";
    public const string avutil = "libavutil.so.58";
    public const string avcodec = "libavcodec.so.60";
    public const string avdevice = "libavdevice.so.60";
    public const string avfilter = "libavfilter.so.9";
    public const string swscale = "libswscale.so.7";
    public const string swresample = "libswresample.so.4";
#elif GODOT_ANDROID
    public const string avformat = "libavformat.so";
    public const string avutil = "libavutil.so";
    public const string avcodec = "libavcodec.so";
    public const string avdevice = "libavdevice.so";
    public const string avfilter = "libavfilter.so";
    public const string swscale = "libswscale.so";
    public const string swresample = "libswresample.so";
#endif
}