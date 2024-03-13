using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public static class HttpUtils
{
    public static async Task<byte[]> HttpGetAsync(Node node, string url)
    {
        using var client = new HttpClient();
        var uri = new Uri(url);
        var err = client.ConnectToHost($"{uri.Scheme}://{uri.Host}", uri.Port);
        if (err != Error.Ok)
            return Array.Empty<byte>();
        while (client.GetStatus() == HttpClient.Status.Connecting || client.GetStatus() == HttpClient.Status.Resolving)
        {
            client.Poll();
            // await node.ToSignal(node.GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        err = client.Request(HttpClient.Method.Get, uri.PathAndQuery, Array.Empty<string>());
        if (err != Error.Ok)
            return Array.Empty<byte>();
        while (client.GetStatus() == HttpClient.Status.Requesting)
        {
            client.Poll();
            // await node.ToSignal(node.GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        if (client.GetStatus() == HttpClient.Status.Body || client.GetStatus() == HttpClient.Status.Connected)
        {
            if (client.HasResponse())
            {
                var data = new List<byte>();
                while (client.GetStatus() == HttpClient.Status.Body)
                {
                    client.Poll();
                    byte[] chunk = client.ReadResponseBodyChunk();
                    if (chunk.Length == 0)
                        // await node.ToSignal(node.GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
                        await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
                    else
                        data.AddRange(chunk);
                }
                return data.ToArray();
            }
            GD.Print("Http: no response");
        }
        GD.Print("Http: not connected");
        return Array.Empty<byte>();
    }
}