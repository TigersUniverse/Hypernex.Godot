using System.Collections.Generic;
using System.Linq;
using DitzelGames.FastIK;
using Godot;
using Godot.Collections;

public partial class IKRunner : Node
{
    [Export]
    public Array<FastIKFabric> ends = new Array<FastIKFabric>();
    [Export]
    public Node root;

    public override void _Process(double delta)
    {
        Solve();
    }

    public void CreateFrom(Skeleton3D skeleton)
    {
        foreach (var ch in skeleton.FindChildren("*", owned: false).Where(x => x is BoneAttachment3D).Select(x => x as BoneAttachment3D))
        {
            if (ch.GetChildren().Count(x => x is BoneAttachment3D) != 1)
            {
                int chainCount = GetChainCount(ch.GetParent());
                if (chainCount < 2)
                    continue;
                var target = new Node3D() { Name = "Target" };
                target.Transform = skeleton.GetBoneGlobalRest(ch.BoneIdx);
                root.AddChild(target);
                var ik = new FastIKFabric();
                ik.Target = target;
                ik.ChainLength = chainCount;
                GD.Print($"ChainLength={ik.ChainLength} {ch.Name}");
                ch.AddChild(ik);
                ends.Add(ik);
            }
        }
    }

    public void Solve()
    {
        Queue<FastIKFabric> queue = new Queue<FastIKFabric>(ends);
        while (queue.Count > 0)
        {
            FastIKFabric ik = queue.Dequeue();
            if (ik == null)
                continue;
            var children = ik.GetParent().FindChildren("*", owned: false).Where(x => x is FastIKFabric && x != ik && ((FastIKFabric)x).Root.Node == ik.GetParent()).Select(x => x as FastIKFabric).ToArray();
            if (children.Length != 0)
            {
                Vector3 targetPosition = Vector3.Zero;
                foreach (var ch in children)
                {
                    targetPosition += ch.Root.GlobalPosition;
                }
                targetPosition /= children.Length;
                ik.Target.GlobalPosition = targetPosition;
            }
            ik.ResolveIK();
            queue.Enqueue(GetParentFabric(ik.GetParent()));
        }
    }

    private FastIKFabric GetParentFabric(Node node)
    {
        if (!root.IsAncestorOf(node))
            return null;
        foreach (var item in node.GetParent().GetChildren())
        {
            if (item is FastIKFabric ik)
                return ik;
        }
        return GetParentFabric(node.GetParent());
    }

    private int GetChainCount(Node node)
    {
        if (!root.IsAncestorOf(node))
            return 1;
        if (node.GetChildren().Count(x => x is BoneAttachment3D) > 1)
        {
            return 1;
        }
        return GetChainCount(node.GetParent()) + 1;
    }

    private void GetChildIk(Node node)
    {
    }
}