using System;
using Godot;

namespace Hypernex.UI
{
    public static class UITools
    {
        public static ThemedButton AddButton(this Control control, string text, Action<ThemedButton> callback)
        {
            var ui = new ThemedButton();
            ui.Name = text;
            ui.Text = text;
            ui.Pressed += () => callback?.Invoke(ui);
            control.AddChild(ui);
            return ui;
        }

        public static ThemedButton AddButton(this Control control, string text, UIButtonTheme theme, Action<ThemedButton> callback)
        {
            var ui = new ThemedButton();
            ui.Name = text;
            ui.Text = text;
            ui.ThemeTypeVariation = theme.ToString();//$"Button_{theme}";
            ui.Pressed += () => callback?.Invoke(ui);
            control.AddChild(ui);
            return ui;
        }

        public static RichTextLabel AddLabel(this Control control, string text, Action<RichTextLabel, Variant> callback)
        {
            var ui = new RichTextLabel();
            ui.Name = text;
            ui.Text = text;
            ui.BbcodeEnabled = true;
            ui.FitContent = true;
            ui.AutowrapMode = TextServer.AutowrapMode.Off;
            ui.ScrollActive = false;
            ui.ShortcutKeysEnabled = false;
            ui.MetaClicked += v => callback?.Invoke(ui, v);
            control.AddChild(ui);
            return ui;
        }

        public static VBoxContainer AddVBox(this Control control, string name = null)
        {
            var ui = new VBoxContainer();
            if (!string.IsNullOrEmpty(name))
                ui.Name = name;
            control.AddChild(ui);
            return ui;
        }

        public static HBoxContainer AddHBox(this Control control, string name = null)
        {
            var ui = new HBoxContainer();
            if (!string.IsNullOrEmpty(name))
                ui.Name = name;
            control.AddChild(ui);
            return ui;
        }

        public static OptionButton AddOptions(this Control control, params string[] options)
        {
            var ui = new OptionButton();
            for (int i = 0; i < options.Length; i++)
            {
                ui.AddItem(options[i], i);
            }
            ui.Selected = 0;
            control.AddChild(ui);
            return ui;
        }
    }
}