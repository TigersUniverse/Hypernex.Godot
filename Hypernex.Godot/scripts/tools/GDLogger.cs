using System;
using Godot;
using Hypernex.CCK;

public partial class GDLogger : Logger
{
    public override void Critical(Exception e)
    {
        GD.PrintRich($"[color=red]Critical: {e}[/color]");
    }

    public override void Debug(object o)
    {
        GD.PrintRich($"[color=gray]Debug: {o}[/color]");
    }

    public override void Error(object o)
    {
        GD.PrintRich($"[color=red]Error: {o}[/color]");
    }

    public override void Log(object o)
    {
        GD.PrintRich($"[color=green]Log: {o}[/color]");
    }

    public override void Warn(object o)
    {
        GD.PrintRich($"[color=yellow]Warning: {o}[/color]");
    }
}