using System;
using System.IO;
using System.Text;
using Godot;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Hypernex.Tools
{
    public static class JsonTools
    {
        public static JsonSerializer Serializer { get; set; } = new JsonSerializer();
        public static MessagePackSerializerOptions MsgPackOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public static string JsonSerialize(object data)
        {
            using MemoryStream ms = new MemoryStream();
            using StreamWriter s = new StreamWriter(ms);
            s.AutoFlush = true;
            using JsonTextWriter writer = new JsonTextWriter(s);
            Serializer.Serialize(writer, data);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static T JsonDeserialize<T>(string data)
        {
            using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using StreamReader s = new StreamReader(ms);
            using JsonTextReader reader = new JsonTextReader(s);
            return Serializer.Deserialize<T>(reader);
        }

        public static void JsonPopulate(string data, object target)
        {
            using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using StreamReader s = new StreamReader(ms);
            using JsonTextReader reader = new JsonTextReader(s);
            Serializer.Populate(reader, target);
        }

        public static string BsonSerialize(object data)
        {
            using MemoryStream ms = new MemoryStream();
            using BsonDataWriter writer = new BsonDataWriter(ms);
            Serializer.Serialize(writer, data);
            return Convert.ToBase64String(ms.ToArray());
        }

        public static T BsonDeserialize<T>(string data)
        {
            using MemoryStream ms = new MemoryStream(Convert.FromBase64String(data));
            using BsonDataReader reader = new BsonDataReader(ms);
            return Serializer.Deserialize<T>(reader);
        }

        public static void BsonPopulate(string data, object target)
        {
            using MemoryStream ms = new MemoryStream(Convert.FromBase64String(data));
            using BsonDataReader reader = new BsonDataReader(ms);
            Serializer.Populate(reader, target);
        }

        public static string MsgPackSerialize(object data)
        {
            return Convert.ToBase64String(MessagePackSerializer.ConvertFromJson(JsonSerialize(data), MsgPackOptions));
        }

        public static T MsgPackDeserialize<T>(string data)
        {
            return JsonDeserialize<T>(MessagePackSerializer.ConvertToJson(Convert.FromBase64String(data), MsgPackOptions));
        }

        public static void MsgPackPopulate(string data, object target)
        {
            JsonPopulate(MessagePackSerializer.ConvertToJson(Convert.FromBase64String(data), MsgPackOptions), target);
        }
    }
}