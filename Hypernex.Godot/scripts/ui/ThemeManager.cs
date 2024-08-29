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
        [Export]
        public Theme baseTheme;

        public Theme realTheme;

        public override void _Ready()
        {
            Instance = this;
            realTheme = baseTheme.Duplicate() as Theme;
            StyleBox box = realTheme.GetStylebox("normal", nameof(Button));
            {
                string typeName = nameof(Button);
                realTheme.AddType(typeName);
                // realTheme.SetTypeVariation(typeName, nameof(Button));
                Color col = GetColor(UIButtonTheme.None);
                Color txtCol = GetTextColor(col);
                StyleBoxFlat ogBox = (StyleBoxFlat)box.Duplicate();
                ogBox.BgColor = col;
                StyleBoxFlat boxPressed = (StyleBoxFlat)box.Duplicate();
                boxPressed.BgColor = col.Darkened(colorPressed);
                StyleBoxFlat boxDisabled = (StyleBoxFlat)box.Duplicate();
                boxDisabled.BgColor = col.Darkened(colorDisabled);
                realTheme.SetColor("font_color", typeName, txtCol);
                realTheme.SetStylebox("disabled", typeName, boxDisabled);
                realTheme.SetStylebox("focus", typeName, ogBox);
                realTheme.SetStylebox("hover", typeName, boxPressed);
                realTheme.SetStylebox("normal", typeName, ogBox);
                realTheme.SetStylebox("pressed", typeName, boxPressed);
            }
            foreach (UIButtonTheme btnTheme in Enum.GetValues<UIButtonTheme>())
            {
                string typeName = btnTheme.ToString();
                realTheme.AddType(typeName);
                realTheme.SetTypeVariation(typeName, nameof(Button));
                Color col = GetColor(btnTheme);
                Color txtCol = GetTextColor(col);
                StyleBoxFlat ogBox = (StyleBoxFlat)box.Duplicate();
                ogBox.BgColor = col;
                StyleBoxFlat boxPressed = (StyleBoxFlat)box.Duplicate();
                boxPressed.BgColor = col.Darkened(colorPressed);
                StyleBoxFlat boxDisabled = (StyleBoxFlat)box.Duplicate();
                boxDisabled.BgColor = col.Darkened(colorDisabled);
                realTheme.SetColor("font_color", typeName, txtCol);
                realTheme.SetStylebox("disabled", typeName, boxDisabled);
                realTheme.SetStylebox("focus", typeName, ogBox);
                realTheme.SetStylebox("hover", typeName, boxPressed);
                realTheme.SetStylebox("normal", typeName, ogBox);
                realTheme.SetStylebox("pressed", typeName, boxPressed);
            }
        }

        public Color GetTextColor(Color color)
        {
            return color.Luminance < 0.5f ? Colors.White : Colors.Black;
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
