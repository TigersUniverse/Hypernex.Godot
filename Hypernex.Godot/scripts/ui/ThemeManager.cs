using System;
using Godot;

namespace Hypernex.UI
{
    public enum UIButtonTheme
    {
        None,
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
    }

    public partial class ThemeManager : Node
    {
        public static ThemeManager Instance;
        [Export]
        public StyleBoxFlat buttonBaseBox;
        [Export]
        public Color colorPrimary = Colors.Blue;
        [Export]
        public Color colorSecondary = Colors.Gray;
        [Export]
        public Color colorSuccess = Colors.LightGreen;
        [Export]
        public Color colorDanger = Colors.Red;
        [Export]
        public Color colorWarning = Colors.Yellow;
        [Export]
        public Color colorInfo = Colors.Cyan;
        [Export(PropertyHint.Range, "0,1")]
        public float colorPressed = 0.2f;
        [Export(PropertyHint.Range, "0,1")]
        public float colorDisabled = 0.2f;

        public override void _Ready()
        {
            Instance = this;
        }

        public Color GetColor(UIButtonTheme theme)
        {
            switch (theme)
            {
                default:
                    return colorSecondary;
                case UIButtonTheme.Primary:
                    return colorPrimary;
                case UIButtonTheme.Success:
                    return colorSuccess;
                case UIButtonTheme.Danger:
                    return colorDanger;
                case UIButtonTheme.Warning:
                    return colorWarning;
                case UIButtonTheme.Info:
                    return colorInfo;
            }
        }
    }
}
