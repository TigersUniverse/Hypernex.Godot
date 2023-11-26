using System;
using Godot;

namespace Hypernex.UI
{
    public partial class ThemedButton : Button
    {
        public override void _Ready()
        {
            // ThemeChanged += UpdateTheme;
            UpdateTheme();
        }

        public override void _ExitTree()
        {
            // ThemeChanged -= UpdateTheme;
        }

        private void UpdateTheme()
        {
            string str = ThemeTypeVariation.ToString();
            if (Enum.TryParse<UIButtonTheme>(str, out var result))
            {
                Color col = ThemeManager.Instance.GetColor(result);
                Color txtCol = ThemeManager.Instance.GetTextColor(col);
                StyleBoxFlat ogBox = (StyleBoxFlat)ThemeManager.Instance.buttonBaseBox.Duplicate();
                ogBox.BgColor = col;
                StyleBoxFlat boxPressed = (StyleBoxFlat)ThemeManager.Instance.buttonBaseBox.Duplicate();
                boxPressed.BgColor = col.Darkened(ThemeManager.Instance.colorPressed);
                StyleBoxFlat boxDisabled = (StyleBoxFlat)ThemeManager.Instance.buttonBaseBox.Duplicate();
                boxDisabled.BgColor = col.Darkened(ThemeManager.Instance.colorDisabled);
                BeginBulkThemeOverride();
                AddThemeColorOverride("font_color", txtCol);
                AddThemeStyleboxOverride("disabled", boxDisabled);
                AddThemeStyleboxOverride("focus", ogBox);
                AddThemeStyleboxOverride("hover", boxPressed);
                AddThemeStyleboxOverride("normal", ogBox);
                AddThemeStyleboxOverride("pressed", boxPressed);
                EndBulkThemeOverride();
            }
        }
    }
}
