using System.Collections.Generic;
using Godot;
using SoundWaveGame.Objects;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    public void PopulateFromScene(Node sceneRoot)
    {
        _sceneRoot = sceneRoot;
        _useContextPosition = false;
        AddGraphNodeForNode(sceneRoot, true, true);
        PopulateNodeConnections();
    }

    private SignalNodeGraphNode CreateGraphNodeForNode(Node node, bool requireConnections = false)
    {
        SignalNodeGraphNode graphNode = null;
        if (node.Owner == _sceneRoot || node == _sceneRoot)
        {
            graphNode = new SignalNodeGraphNode();
            var savedData = node.HasMeta(__graphDataMetaName) ? node.GetMeta(__graphDataMetaName).AsGodotDictionary() : null;
            bool anyConnections = graphNode.Setup(node, savedData, this);
            if (requireConnections && !anyConnections)
            {
                graphNode.QueueFree();
                graphNode = null;
            }
            else
            {
                if (_useContextPosition)
                {
                    graphNode.PositionOffset = LocalToGraphPosition(_contextPosition);
                }
            }
        }

        return graphNode;
    }

    private SignalNodeGraphNode AddGraphNodeForNode(Node node, bool recursive = false, bool requireConnections = false)
    {
        var graphNode = CreateGraphNodeForNode(node, requireConnections);
        if (graphNode != null)
        {
            this.AddChild(graphNode);
        }

        if (recursive)
        {
            int childCount = node.GetChildCount();
            for (var childIndex = 0; childIndex < childCount; childIndex++)
            {
                var child = node.GetChild(childIndex);
                AddGraphNodeForNode(child, true, requireConnections);
            }
        }

        return graphNode;
    }

    private void PopulateNodeConnections()
    {
        var tempConnectionList = new List<ParsedConnection>();
        foreach (var child in this.GetChildren())
        {
            if (child is not SignalNodeGraphNode graphNode) continue;
            var node = graphNode.Node;
            if (node == null) continue;

            tempConnectionList.Clear();
            GDSToCSBridge.DumpIncomingConnections(node, tempConnectionList);
            foreach (var connection in tempConnectionList)
            {
                var signal = connection.Signal;
                var callable = connection.Callable;
                if (node is GameEffector)
                {
                    // GD.Print($"{(signal.Owner as Node)?.Name} . {signal.Name} => {node.Name}.{callable.Method} (flags {connection.Flags})");
                    // GD.Print($"Callable method: {callable.Method} ({callable.ArgumentCount}); with flags {connection.Flags} and bound args: {callable.BoundArgumentCount}");
                }
                var flags = connection.Flags;

                if (signal.Owner is not Node owner) continue;

                var ownerGraphNode = GetGraphNodeForNode(owner);
                if (ownerGraphNode == null) continue;

                int fromPort = ownerGraphNode.GetSignalPortId(signal.Name);
                int toPort = graphNode.GetMethodPortId(callable.Method);

                if (fromPort == -1 || toPort == -1) continue;

                ConnectNode(ownerGraphNode.Name, fromPort, graphNode.Name, toPort);
            }
        }
    }
}