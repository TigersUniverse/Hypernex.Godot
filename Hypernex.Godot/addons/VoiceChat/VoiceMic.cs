using Godot;

public partial class VoiceMic : AudioStreamPlayer
{
    public override void _Ready()
    {
        int currentNumber = AudioServer.GetBusIndex("VoiceMicRecord");
        if (currentNumber == -1)
        {
            GD.PrintErr("VoiceMicRecord not found");
            return;
        }

        Bus = "VoiceMicRecord";
        Stream = new AudioStreamMicrophone();
        Play();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Playing)
            Play();
    }
}