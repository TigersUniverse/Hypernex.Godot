using System.Collections.Generic;
using Godot;
using Hypernex.CCK;

public partial class LoadingOverlay : Node
{
    [Export]
    public ProgressBar bar;
    [Export]
    public Label label;
    public Control root;

    public Dictionary<string, float> amounts = new Dictionary<string, float>();
    public int isLoading = 0;

    public override void _Ready()
    {
        root = GetParent<Control>();
    }

    public override void _Process(double delta)
    {
        label.Visible = isLoading != 0;
        int max = amounts.Count;
        float amt = 0f;
        foreach (var kvp in amounts)
        {
            amt += kvp.Value;
        }
        bar.Visible = max != 0;
        root.Visible = max != 0 || isLoading != 0;
        if (max == 0)
            bar.Value = 1f;
        else
            bar.Value = amt / max;
    }

    public void Report(string file, float progress)
    {
        if (!amounts.ContainsKey(file))
            amounts.Add(file, 0f);
        amounts[file] = progress;
        if (progress >= 1f)
        {
            amounts.Remove(file);
        }
    }
}