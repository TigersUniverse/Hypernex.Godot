using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Game;
using Hypernex.Networking.Messages;

namespace Hypernex.Player
{
    public partial class PlayerChat : Node
    {
        [Export]
        public PlayerRoot root;
        public VoiceChat voice;
        public bool IsSpeaking => voice?.Recording ?? false;

        public override void _Ready()
        {
            root.OnUserSet = UserSet;
        }

        public void UserSet()
        {
            voice?.QueueFree();
            voice = new VoiceChat();
            voice.IsLocalPlayer = root.IsLocal;
            voice.OnSpeak = OnSpeak;
            AddChild(voice);
        }

        public void HandleVoice(PlayerVoice data)
        {
            if (data.Encoder.ToLower() == "opus")
                voice.Speak(data.Bytes, data.EncodeLength, data.SampleRate, data.Channels);
        }

        private void OnSpeak(VoiceChat.VoiceData data)
        {
            root.Instance.SendMessage(new PlayerVoice()
            {
                Auth = root.GetJoinAuth(),
                SampleRate = data.freq,
                Bytes = data.data,
                Channels = data.channels,
                EncodeLength = data.encodedLength,
                Encoder = "opus",
            });
        }

        public override void _Process(double delta)
        {
            if (voice != null)
            {
                voice.Recording = Input.IsActionPressed("chat_voice");
            }
        }
    }
}
