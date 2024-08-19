using System;
using Godot;
using Hypernex.Configuration;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class SettingsController : Node
    {
        [Export]
        public Label volumeLabel;
        [Export]
        public Slider volumeSlider;
        [Export]
        public CheckButton externalUrlsToggle;

        public int WorldBusIdx => AudioServer.GetBusIndex("World");

        public override void _EnterTree()
        {
            ConfigManager.OnConfigLoaded += ReloadConfig;
            ConfigManager.OnConfigSaved += ReloadConfig;
            volumeSlider.ValueChanged += VolumeChanged;
            externalUrlsToggle.Pressed += UrlsToggleChanged;
        }

        public override void _ExitTree()
        {
            ConfigManager.OnConfigLoaded -= ReloadConfig;
            ConfigManager.OnConfigSaved -= ReloadConfig;
            volumeSlider.ValueChanged -= VolumeChanged;
            externalUrlsToggle.Pressed -= UrlsToggleChanged;
        }

        private void UrlsToggleChanged()
        {
            ConfigManager.LoadedConfig.UseTrustedURLs = externalUrlsToggle.ButtonPressed;
        }

        private void ReloadConfig(Config config)
        {
            externalUrlsToggle.ButtonPressed = ConfigManager.LoadedConfig.UseTrustedURLs;
            volumeSlider.Value = ConfigManager.SelectedConfigUser.WorldAudioVolume * 100f;
            volumeLabel.Text = $"Volume {(int)volumeSlider.Value}%";
            AudioServer.SetBusVolumeDb(WorldBusIdx, Mathf.LinearToDb(ConfigManager.SelectedConfigUser.WorldAudioVolume));
        }

        private void VolumeChanged(double value)
        {
            ConfigManager.SelectedConfigUser.WorldAudioVolume = (float)volumeSlider.Value / 100f;
            volumeLabel.Text = $"Volume {(int)volumeSlider.Value}%";
            AudioServer.SetBusVolumeDb(WorldBusIdx, Mathf.LinearToDb(ConfigManager.SelectedConfigUser.WorldAudioVolume));
        }
    }
}