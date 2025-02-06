using System;
using Godot;
using Godot.Collections;

namespace Hypernex.CCK.GodotVersion.Extensions
{
    [Tool]
    [GlobalClass]
    public partial class GltfAudioExt : GltfDocumentExtension
    {
        public const string EXT_NAME = "HYPERNEX_audio_source_3d";
        public const string EXTS = "extensions";

        public static byte[] GetDataForStream(AudioStream stream)
        {
            switch (stream)
            {
                case AudioStreamMP3 mp3:
                    return mp3.Data;
                case AudioStreamWav wav:
                    {
                        byte[] data = new byte[sizeof(int) + sizeof(byte) + wav.Data.Length];
                        BitConverter.TryWriteBytes(new Span<byte>(data, 0, sizeof(int)), wav.MixRate);
                        data[sizeof(int)] = (byte)(wav.Stereo ? 2 : 1);
                        System.Array.Copy(wav.Data, 0, data, sizeof(int) + sizeof(byte), wav.Data.Length);
                        return data;
                    }
                default:
                    return null;
            }
        }

        public static AudioStream GetStreamForData(string type, byte[] buffer)
        {
            switch (type)
            {
                case "mp3_file":
                    {
                        AudioStreamMP3 mp3 = new AudioStreamMP3();
                        mp3.Data = buffer;
                        return mp3;
                    }
                case "wav_godot":
                    {
                        AudioStreamWav wav = new AudioStreamWav();
                        wav.MixRate = BitConverter.ToInt32(buffer, 0);
                        wav.Stereo = buffer[sizeof(int)] == 2;
                        return wav;
                    }
                default:
                    return null;
            }
        }

        public static string GetTypeForStream(AudioStream stream)
        {
            switch (stream)
            {
                case AudioStreamMP3:
                    return "mp3_file";
                case AudioStreamWav:
                    return "wav_godot";
                default:
                    return null;
            }
        }

        public static string AttenModelToString(AudioStreamPlayer3D.AttenuationModelEnum atten)
        {
            switch (atten)
            {
                case AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance:
                    return "linear";
                case AudioStreamPlayer3D.AttenuationModelEnum.InverseSquareDistance:
                    return "linear_sq";
                case AudioStreamPlayer3D.AttenuationModelEnum.Logarithmic:
                    return "log";
                default:
                    return "none";
            }
        }

        public static AudioStreamPlayer3D.AttenuationModelEnum StringToAttenModel(string atten)
        {
            switch (atten)
            {
                case "linear":
                    return AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
                case "linear_sq":
                    return AudioStreamPlayer3D.AttenuationModelEnum.InverseSquareDistance;
                case "log":
                    return AudioStreamPlayer3D.AttenuationModelEnum.Logarithmic;
                default:
                    return AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
            }
        }

        public override string[] _GetSupportedExtensions()
        {
            return new string[] { EXT_NAME };
        }

        public override Error _ImportPreflight(GltfState state, string[] extensions)
        {
            return Error.Ok;
        }

        public override Error _ExportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
        {
            var data = gltfNode.GetAdditionalData(EXT_NAME);
            if (data.VariantType == Variant.Type.Nil)
                return Error.Ok;
            if (!json.ContainsKey(EXTS))
                json.Add(EXTS, new Dictionary());
            json[EXTS].AsGodotDictionary()[EXT_NAME] = gltfNode.GetAdditionalData(EXT_NAME);
            return Error.Ok;
        }

        public override void _ConvertSceneNode(GltfState state, GltfNode gltfNode, Node sceneNode)
        {
            if (sceneNode is AudioStreamPlayer3D player3d)
            {
                var dict = new Dictionary();
                dict["atten_model"] = AttenModelToString(player3d.AttenuationModel);
                dict["volume"] = Mathf.DbToLinear(player3d.VolumeDb);
                dict["max_volume"] = Mathf.DbToLinear(player3d.MaxDb);
                dict["min_distance"] = player3d.UnitSize;
                dict["max_distance"] = player3d.MaxDistance;
                dict["pan_amount"] = player3d.PanningStrength;
                dict["pitch"] = player3d.PitchScale;
                dict["auto_play"] = player3d.Autoplay;
                dict["loop"] = player3d.Get("parameters/looping").AsBool();

                byte[] data = GetDataForStream(player3d.Stream);
                if (data == null)
                {
                    gltfNode.SetAdditionalData(EXT_NAME, dict);
                    state.AddUsedExtension(EXT_NAME, true);
                    return;
                }
                dict["file_type"] = GetTypeForStream(player3d.Stream);
                int idx = state.AppendDataToBuffers(data, true);
                dict["buffer_view"] = idx;
                gltfNode.SetAdditionalData(EXT_NAME, dict);
                state.AddUsedExtension(EXT_NAME, true);
            }
        }

        public override Error _ParseNodeExtensions(GltfState state, GltfNode gltfNode, Dictionary extensions)
        {
            if (extensions.TryGetDict(EXT_NAME, out Dictionary dict))
                gltfNode.SetAdditionalData(EXT_NAME, dict);
            return Error.Ok;
        }

        public override Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return null;
            var data = val.AsGodotDictionary();
            AudioStreamPlayer3D player3d = new AudioStreamPlayer3D();
            if (data.TryGetString("atten_model", out string atten))
                player3d.AttenuationModel = StringToAttenModel(atten);
            if (data.TryGetFloat("volume", out float volume))
                player3d.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume, 0f, 1f));
            if (data.TryGetFloat("max_volume", out float max_volume))
                player3d.MaxDb = Mathf.LinearToDb(Mathf.Clamp(max_volume, 0f, 1f));
            if (data.TryGetFloat("min_distance", out float unitSize))
                player3d.UnitSize = unitSize;
            if (data.TryGetFloat("max_distance", out float maxDist))
                player3d.MaxDistance = maxDist;
            if (data.TryGetFloat("pan_amount", out float pan))
                player3d.PanningStrength = pan;
            if (data.TryGetFloat("pitch", out float pitch))
                player3d.PitchScale = pitch;
            if (data.TryGetBool("auto_play", out bool autoplay))
                player3d.Autoplay = autoplay;

            if (data.TryGetInt32("buffer_view", out int idx))
            {
                if (data.TryGetString("file_type", out string type))
                {
                    byte[] buffer = state.GetBufferViews()[idx].LoadBufferViewData(state);
                    AudioStream stream = GetStreamForData(type, buffer);
                    player3d.Stream = stream;
                }
            }

            if (data.TryGetBool("loop", out bool loop))
            {
                player3d.SetMeta("loop", loop);
                player3d.Set("parameters/looping", loop);
            }
            return player3d;
        }
    }
}
