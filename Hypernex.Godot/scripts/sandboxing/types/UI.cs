using System.Linq;
using Godot;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class UI
    {
        public static string GetText(Item item)
        {
            if (item.t is Label3D l3d)
                return l3d.Text;
            if (item.t is Label l)
                return l.Text;
            if (item.t is RichTextLabel rl)
                return rl.Text;
            if (item.t is Button btn)
                return btn.Text;
            if (item.t is LineEdit le)
                return le.Text;
            return string.Empty;
        }

        public static void SetText(Item item, string text)
        {
            if (item.t is Label3D l3d)
                l3d.Text = text;
            if (item.t is Label l)
                l.Text = text;
            if (item.t is RichTextLabel rl)
                rl.Text = text;
            if (item.t is Button btn)
                btn.Text = text;
            if (item.t is LineEdit le)
                le.Text = text;
        }

        public static void RegisterButtonClick(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
                b.Pressed += () => SandboxFuncTools.InvokeSandboxFunc(s);
        }

        public static void RemoveAllButtonClicks(Item item)
        {
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                foreach (var conn in b.GetSignalConnectionList(BaseButton.SignalName.Pressed).ToArray())
                {
                    b.Disconnect(BaseButton.SignalName.Pressed, conn["callable"].AsCallable());
                }
            }
        }

        public static bool GetToggle(Item item)
        {
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                return b.ButtonPressed;
            }
            return false;
        }

        public static void SetToggle(Item item, bool val)
        {
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                b.ButtonPressed = val;
            }
        }

        public static void RegisterToggleValueChanged(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                b.Toggled += p => SandboxFuncTools.InvokeSandboxFunc(s, p);
            }
        }

        public static void RemoveAllToggleValueChanged(Item item)
        {
            BaseButton b = item.t as BaseButton;
            if (GodotObject.IsInstanceValid(b))
            {
                foreach (var conn in b.GetSignalConnectionList(BaseButton.SignalName.Toggled).ToArray())
                {
                    b.Disconnect(BaseButton.SignalName.Toggled, conn["callable"].AsCallable());
                }
            }
        }

        public static float GetSlider(Item item)
        {
            Range sl = item.t as Range;
            if (GodotObject.IsInstanceValid(sl))
            {
                return (float)sl.Value;
            }
            return float.NaN;
        }

        public static void SetSlider(Item item, float val)
        {
            Range sl = item.t as Range;
            if (GodotObject.IsInstanceValid(sl))
            {
                sl.Value = val;
            }
        }

        public static void RegisterSliderValueChanged(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            Range sl = item.t as Range;
            if (GodotObject.IsInstanceValid(sl))
            {
                sl.ValueChanged += v => SandboxFuncTools.InvokeSandboxFunc(s, v);
            }
        }

        public static void RemoveAllSliderValueChanged(Item item)
        {
            Range sl = item.t as Range;
            if (GodotObject.IsInstanceValid(sl))
            {
                foreach (var conn in sl.GetSignalConnectionList(Range.SignalName.ValueChanged).ToArray())
                {
                    sl.Disconnect(Range.SignalName.ValueChanged, conn["callable"].AsCallable());
                }
            }
        }

        public static void SetSliderRange(Item item, float min, float max)
        {
            Range sl = item.t as Range;
            if (GodotObject.IsInstanceValid(sl))
            {
                sl.MinValue = min;
                sl.MaxValue = max;
                sl.Value = Mathf.Clamp(sl.Value, min, max);
            }
        }

        public static float GetScrollbar(Item item) => GetSlider(item);
        public static void SetScrollbar(Item item, float val) => SetSlider(item, val);
        public static void RegisterScrollbarValueChanged(Item item, object o) => RegisterSliderValueChanged(item, o);
        public static void RemoveAllScrollbarValueChanged(Item item) => RemoveAllSliderValueChanged(item);

        public static string GetInputFieldText(Item item) => GetText(item);
        public static void SetInputFieldText(Item item, string val) => SetText(item, val);

        public static void RegisterInputFieldTextChanged(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            LineEdit lineEdit = item.t as LineEdit;
            if (GodotObject.IsInstanceValid(lineEdit))
                lineEdit.TextChanged += t => SandboxFuncTools.InvokeSandboxFunc(s, t);
        }

        public static void RemoveAllInputFieldTextChanged(Item item)
        {
            LineEdit lineEdit = item.t as LineEdit;
            if (GodotObject.IsInstanceValid(lineEdit))
            {
                foreach (var conn in lineEdit.GetSignalConnectionList(LineEdit.SignalName.TextChanged).ToArray())
                {
                    lineEdit.Disconnect(LineEdit.SignalName.TextChanged, conn["callable"].AsCallable());
                }
            }
        }
    }
}