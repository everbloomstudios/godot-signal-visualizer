using Godot;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var dataDict = data.AsGodotDictionary();
        if (dataDict["type"].AsString() != "nodes") return false;

        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dataDict = data.AsGodotDictionary();
        if (dataDict["type"].AsString() != "nodes") return;
        
        _contextPosition = atPosition;
        _useContextPosition = true;

        this.SetSelected(null);
        
        foreach (var nodePath in dataDict["nodes"].AsSystemArrayOfNodePath())
        {
            var node = this.GetNode(nodePath);
            if (node == null) continue;
            ulong instanceId = node.GetInstanceId();
            SignalNodeGraphNode existingGraphNode = null;
            foreach (var child in this.GetChildren())
            {
                if (child is not SignalNodeGraphNode graphNode) continue;
                if (graphNode.NodeInstanceId != instanceId) continue;
                existingGraphNode = graphNode;
                existingGraphNode.SetSelected(true);
            }

            if (existingGraphNode == null)
            {
                var graphNode = CreateGraphNodeForNode(node, false);
                graphNode.SetSelected(true);
                TransactionAddChild(graphNode, true, createAndCommit: true);
            }
        }
    }
}