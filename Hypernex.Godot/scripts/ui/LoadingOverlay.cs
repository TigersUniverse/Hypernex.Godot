using System.Collections.Generic;
using Godot;
using Hypernex.CCK;

public partial class LoadingOverlay : Node
{
    [Export]
    public ProgressBar bar;

    public Dictionary<string, float> amounts = new Dictionary<string, float>();

    public override void _Process(double delta)
    {
        int max = amounts.Count;
        float amt = 0f;
        foreach (var kvp in amounts)
        {
            amt += kvp.Value;
        }
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