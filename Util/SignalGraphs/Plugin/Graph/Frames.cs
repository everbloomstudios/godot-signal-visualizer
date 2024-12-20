using Godot;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private void AddFrame()
    {
        BeginTransaction("Add frame", backwardUndoOps: true);
        var frame = new GraphFrame();
        if (_useContextPosition)
        {
            frame.PositionOffset = LocalToGraphPosition(_contextPosition);
        }
        frame.Title = "New Frame";
        TransactionAddChild(frame, doReference: true, createAndCommit: false);

        EndTransaction();

        if (_selectedNodes.Count <= 0) return;
        
        BeginTransaction("Add frame", Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        Node commonParent = null;
        var commonParentNodeSet = false;
        StringName commonFrameName = null;
        var commonFrameSet = false;
        foreach (var nodeName in _selectedNodes)
        {
            // Take old node frame, try to find a common frame
            if (commonFrameName == null && !commonFrameSet)
            {
                commonFrameName = GetElementFrame(nodeName)?.Name;
                commonFrameSet = true;
            }
            else
            {
                if (commonFrameName != GetElementFrame(nodeName)?.Name)
                {
                    commonFrameName = null;
                }
            }
            
            // Attach selected nodes to new frame
            
            TransactionAttachToFrame(nodeName, frame.Name, createAndCommit: false);
            
            // If the node is a SignalNodeGraphNode (represents a node in the scene tree)
            var graphNode = this.GetNodeOrNull<SignalNodeGraphNode>(new NodePath(nodeName));
            var node = graphNode?.Node;
            if (node == null) continue;
            
            // Look for a common parent among all the selected graph nodes' nodes.
            if (commonParent == null && !commonParentNodeSet)
            {
                commonParent = node;
                commonParentNodeSet = true;
            }
            else
            {
                commonParent = GetCommonParent(commonParent, node);
            }
        }

        if (commonParent != null) frame.Title = commonParent.Name;
        if (commonFrameName != null)
        {
            TransactionAttachToFrame(frame.Name, commonFrameName, createAndCommit: false);
        }
        
        EndTransaction();
    }

    private Node GetCommonParent(Node a, Node b)
    {
        var commonParent = a;
        while (commonParent != null && commonParent != _sceneRoot && !commonParent.IsAncestorOf(b))
        {
            commonParent = commonParent.GetParent();
        }

        return commonParent;
    }
}