using System;
using Godot;
using Hypernex.CCK;

public partial class GDLogger : Logger
{
    public override void Critical(Exception e)
    {
        GD.PrintRich($"[color=red]Critical: {e?.ToString()?.Replace("[", "[lb]")}[/color]");
    }

    public override void Debug(object o)
    {
        GD.PrintRich($"[color=gray]Debug: {o?.ToString()?.Replace("[", "[lb]")}[/color]");
    }

    public override void Error(object o)
    {
        GD.PrintRich($"[color=red]Error: {o?.ToString()?.Replace("[", "[lb]")}[/color]");
    }

    public override void Log(object o)
    {
        GD.PrintRich($"[color=green]Log: {o?.ToString()?.Replace("[", "[lb]")}[/color]");
    }

    public override void Warn(object o)
    {
        GD.PrintRich($"[color=yellow]Warning: {o?.ToString()?.Replace("[", "[lb]")}[/color]");
    }
}