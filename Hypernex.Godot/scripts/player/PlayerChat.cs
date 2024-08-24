using System.Collections.Generic;
using Godot;
using Hypernex.CCK;
using Hypernex.Game;
using Hypernex.Networking.Messages;
using Hypernex.Tools;

namespace Hypernex.Player
{
    public partial class PlayerChat : Node
    {
        [Export]
        public PlayerRoot root;
        [Export]
        public LineEdit textChat;
        [Export]
        public AudioStreamPlayer3D audioSource;
        [Export]
        public Range voiceMeter;
        public VoiceChat voice;
        public bool IsSpeaking => voice?.IsSpeaking ?? false;

        public override void _Ready()
        {
            if (textChat != null)
            {
                textChat.TextSubmitted += SubmitText;
                textChat.FocusExited += CancelText;
                CancelText();
            }
        }

        public override void _ExitTree()
        {
            if (textChat != null)
            {
                textChat.TextSubmitted -= SubmitText;
                textChat.FocusExited -= CancelText;
            }
        }

        public override void _Process(double delta)
        {
            var inputs = root.GetPart<PlayerInputs>();
            if (inputs != null && textChat != null)
            {
                if (inputs.textChatOpen && !textChat.Visible)
                {
                    textChat.Show();
                    textChat.GrabFocus();
                }
                else if (!inputs.textChatOpen)
                {
                    textChat.Clear();
                    textChat.Hide();
                }
            }
            if (voice != null && !Init.IsVRLoaded)
            {
                if (Input.IsActionJustPressed("chat_voice"))
                    voice.Recording = !voice.Recording;
            }
            if (IsInstanceValid(voice) && IsInstanceValid(voiceMeter))
            {
                if (!root.IsLocal || !Init.IsVRLoaded)
                {
                    voiceMeter.Visible = voice.IsSpeaking;
                    voiceMeter.Value = voice.Loudness * 80f;
                }
            }
        }

        private void CancelText()
        {
            textChat.Clear();
            textChat.Hide();
            var inputs = root.GetPart<PlayerInputs>();
            if (inputs != null)
            {
                inputs.textChatOpen = false;
            }
        }

        private void SubmitText(string newText)
        {
            root.Instance.SendMessage(new PlayerMessage()
            {
                Auth = root.GetJoinAuth(),
                Message = newText,
                MessageTags = new List<string>(),
            });
            CancelText();
        }

        public void UserSet()
        {
            voice?.QueueFree();
            voice = new VoiceChat();
            voice.IsLocalPlayer = root.IsLocal;
            voice.OnSpeak = OnSpeak;
            voice.BufferLength = 0.5f;
            voice.ProcessMode = ProcessModeEnum.Always;
            AddChild(voice);
            voice.CallDeferred(VoiceChat.MethodName.SetVoice, audioSource);
            // voice.SetVoice(audioSource);
        }

        public void HandleVoice(PlayerVoice data)
        {
            if (data.Encoder.ToLower() == "opus")
                voice.Speak(data.Bytes, data.EncodeLength, data.SampleRate, data.Channels);
        }

        public void HandleMessage(PlayerMessage data)
        {
            Logger.CurrentLogger.Debug($"Chat: {root.User.GetUsersName().Replace("[", "[lb]")}: {data.Message}");
            root.Controller.PlayMessage(data.Message);
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
    }
}
