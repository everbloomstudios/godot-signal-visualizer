#if TOOLS
using Godot;
using Godot.Collections;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private void BeginTransaction(string name, UndoRedo.MergeMode mergeMode = Godot.UndoRedo.MergeMode.All,
        GodotObject customContext = null,
        bool backwardUndoOps = false)
    {
        customContext ??= _sceneRoot;
        UndoRedo.CreateAction(name, mergeMode, customContext, backwardUndoOps);
    }

    private void EndTransaction(bool execute = true)
    {
        UndoRedo.CommitAction(execute);
    }
    
    public void TransactionAddChild(Node child, bool doReference, bool createAndCommit = true)
    {
        if(createAndCommit) BeginTransaction("Create graph element",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(this, Node.MethodName.AddChild, child);
        UndoRedo.AddUndoMethod(this, Node.MethodName.RemoveChild, child);
        if(doReference) UndoRedo.AddDoReference(child);
        if (createAndCommit) EndTransaction();
    }
    public void TransactionRemoveChild(Node child, bool undoReference, bool createAndCommit = true)
    {
        if (createAndCommit) BeginTransaction("Remove graph element",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(this, Node.MethodName.RemoveChild, child);
        UndoRedo.AddUndoMethod(this, Node.MethodName.AddChild, child);
        if (undoReference) UndoRedo.AddUndoReference(child);
        if (createAndCommit) EndTransaction();
    }
    
    public void TransactionConnect(StringName fromNode, int fromPort, StringName toNode, int toPort, bool createAndCommit = true)
    {
        if(createAndCommit) BeginTransaction("Connect graph nodes",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(this, GraphEdit.MethodName.ConnectNode, fromNode, fromPort, toNode, toPort);
        UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNode, fromPort, toNode, toPort);
        if (createAndCommit) EndTransaction();
    }
    public void TransactionDisconnect(StringName fromNode, int fromPort, StringName toNode, int toPort, bool createAndCommit = true)
    {
        if(createAndCommit) BeginTransaction("Disconnect graph nodes",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNode, fromPort, toNode, toPort);
        UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.ConnectNode, fromNode, fromPort, toNode, toPort);
        if (createAndCommit) EndTransaction();
    }

    public void TransactionAttachToFrame(StringName nodeName, StringName frameName, bool createAndCommit = true)
    {
        var prevFrameName = this.GetElementFrame(nodeName)?.Name;
        if (prevFrameName == frameName) return;
        
        if(createAndCommit) BeginTransaction("Add graph nodes to frame",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        
        if (prevFrameName != null) UndoRedo.AddDoMethod(this, GraphEdit.MethodName.DetachGraphElementFromFrame, nodeName);
        if (frameName != null) UndoRedo.AddDoMethod(this, GraphEdit.MethodName.AttachGraphElementToFrame, nodeName, frameName);
        
        if (prevFrameName != null) UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.AttachGraphElementToFrame, nodeName, prevFrameName);
        if (frameName != null) UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.DetachGraphElementFromFrame, nodeName);
        
        if (createAndCommit) EndTransaction();
    }
    
    public void TransactionDeleteNodes(Array nodes)
    {
        BeginTransaction("Delete graph nodes",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        
        // First, remove connections
        var connectionList = this.GetConnectionList();
        foreach (var elem in nodes)
        {
            var nodeName = elem.AsStringName();

            for (var i = 0; i < connectionList.Count; i++)
            {
                var connection = connectionList[i];
                var fromNode = connection["from_node"].AsStringName();
                var toNode = connection["to_node"].AsStringName();
                if (fromNode == nodeName || toNode == nodeName)
                {
                    int fromPort = connection["from_port"].AsInt32();
                    int toPort = connection["to_port"].AsInt32();
                    TransactionDisconnect(fromNode, fromPort, toNode, toPort, false);
                    
                    connectionList.RemoveAt(i);
                    i--;
                }
            }
        }
        
        // Then, remove frame attachments
        foreach (var elem in nodes)
        {
            var nodeName = elem.AsStringName();

            // If inside a frame, remove that attachment
            TransactionAttachToFrame(nodeName, null, false);
            
            var node = GetNode(new NodePath(nodeName));
            if (node == null) continue;
            if (node.IsClass(nameof(GraphFrame)))
            {
                // If *a* frame, remove attachments with other nodes
                var attachedNodes = this.GetAttachedNodesOfFrame(nodeName);
                foreach (var attachedName in attachedNodes)
                {
                    TransactionAttachToFrame(attachedName, null, false);
                }
            }
        }
        
        // Lastly remove nodes from graph
        foreach (var elem in nodes)
        {
            var nodeName = elem.AsStringName();
            var node = GetNode(new NodePath(nodeName));
            if (node == null) continue;
            TransactionRemoveChild(node, undoReference: true, createAndCommit: false);
        }
        EndTransaction();
    }

    private void TransactionConnectSignal(Node fromNode, StringName signalName, Callable callable, ConnectFlags flags = ConnectFlags.Persist, bool createAndCommit = true)
    {
        if(createAndCommit) BeginTransaction("Connect signal",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(fromNode, GodotObject.MethodName.Connect, signalName, callable, (uint)flags);
        UndoRedo.AddUndoMethod(fromNode, GodotObject.MethodName.Disconnect, signalName, callable);
        if (createAndCommit) EndTransaction();
    }

    private void TransactionDisconnectSignal(Node fromNode, StringName signalName, Callable callable, bool createAndCommit = true)
    {
        if(createAndCommit) BeginTransaction("Disconnect signal",  Godot.UndoRedo.MergeMode.All, backwardUndoOps: true);
        UndoRedo.AddDoMethod(fromNode, GodotObject.MethodName.Disconnect, signalName, callable);
        var flags = ConnectFlags.Persist;
        foreach (var connection in fromNode.GetSignalConnectionList(signalName))
        {
            var connectionCallable = connection["callable"].AsCallable();
            if(!(connectionCallable.Method == callable.Method && connectionCallable.Target == callable.Target)) continue;
            flags = connection["flags"].As<ConnectFlags>();
        }
        UndoRedo.AddUndoMethod(fromNode, GodotObject.MethodName.Connect, signalName, callable, (uint)flags);
        if (createAndCommit) EndTransaction();
    }
}
#endif