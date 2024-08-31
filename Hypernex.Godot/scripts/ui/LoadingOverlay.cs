using System.Collections.Generic;
using System.Threading;
using Godot;
using Hypernex.CCK;

public partial class LoadingOverlay : Node
{
    [Export]
    public ProgressBar bar;
    [Export]
    public Control barContainer;
    [Export]
    public Control label;
    public Control root;

    public class LoadProgress
    {
        public string name;
        public float progress;
        public CancellationTokenSource tokenSource;
    }

    public Dictionary<string, LoadProgress> amounts = new Dictionary<string, LoadProgress>();
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
            amt += kvp.Value.progress;
        }
        barContainer.Visible = max != 0;
        root.Visible = max != 0 || isLoading != 0;
        if (max == 0)
            bar.Value = 1f;
        else
            bar.Value = amt / max;
    }

    public void StopAllDownloads()
    {
        foreach (var item in amounts)
        {
            item.Value.tokenSource.Cancel();
        }
        amounts.Clear();
    }

    public CancellationToken Add(string file, string name)
    {
        if (!amounts.ContainsKey(file))
            amounts.Add(file, new LoadProgress()
            {
                name = name,
                progress = 0f,
                tokenSource = new CancellationTokenSource(),
            });
        amounts[file].name = name;
        return amounts[file].tokenSource.Token;
    }

    public void Cancel(string file)
    {
        if (amounts.ContainsKey(file))
        {
            amounts[file].tokenSource.Cancel();
            amounts.Remove(file);
        }
    }

    public void Report(string file, float progress)
    {
        if (!amounts.ContainsKey(file))
            return;
        amounts[file].progress = progress;
        if (progress >= 1f)
        {
            amounts.Remove(file);
        }
    }
}