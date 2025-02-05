#if  TOOLS
using Godot;
using Godot.Collections;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private void SubscribeToInterfaceSignals()
    {
        this.PopupRequest += OnPopupRequest;
        this.ConnectionRequest += OnConnectionRequest;
        this.DisconnectionRequest += OnDisconnectionRequest;
        this.ConnectionDragStarted += OnConnectionDragStarted;
        this.NodeSelected += OnNodeSelected;
        this.NodeDeselected += OnNodeDeselected;
        this.BeginNodeMove += OnBeginNodeMove;
        this.EndNodeMove += OnEndNodeMove;
        this.ChildExitingTree += OnNodeRemoved;
        this.DeleteNodesRequest += OnDeleteNodesRequest;
    }

    private void OnConnectionDragStarted(StringName fromNode, long fromPort, bool isOutput)
    {
        var node = this.GetNode(new NodePath(fromNode));
        if (node is SignalNodeGraphNode graphNode)
        {
            int slot = isOutput
                ? graphNode.GetOutputPortSlot((int)fromPort)
                : graphNode.GetInputPortSlot((int)fromPort);

            int slotType = isOutput
                ? graphNode.GetSlotTypeRight(slot)
                : graphNode.GetSlotTypeLeft(slot);

            if (slotType == PortTypeAddMethod || slotType == PortTypeAddSignal)
            {
                this.ForceConnectionDragEnd();
                ShowSignalMethodSelector(graphNode.Node, slotType == PortTypeAddMethod ? 0 : 1, graphNode.SignalAddRequested, graphNode.MethodAddRequested);
            }
        }
    }

    private void OnDeleteNodesRequest(Array nodes)
    {
        TransactionDeleteNodes(nodes);
    }

    private void OnBeginNodeMove()
    {
        _draggingAcrossFrames = Input.IsKeyPressed(Key.Shift);
        if (_draggingAcrossFrames)
        {
            foreach (var nodeName in _selectedNodes)
            {
                this.DetachGraphElementFromFrame(nodeName);
            }
        }
    }

    private void OnEndNodeMove()
    {
        _draggingAcrossFrames = Input.IsKeyPressed(Key.Shift);
        if (_draggingAcrossFrames)
        {
            Vector2 mousePos = GetLocalMousePosition();
            GraphFrame hoveredFrame = null;
            foreach (var elem in GetChildren())
            {
                if (elem is GraphFrame frame && !_selectedNodes.Contains(frame.Name) && frame.GetRect().HasPoint(mousePos))
                {
                    hoveredFrame = frame;
                }
            }

            foreach (var nodeName in _selectedNodes)
            {
                if (hoveredFrame != null)
                {
                    this.DetachGraphElementFromFrame(nodeName);
                    this.AttachGraphElementToFrame(nodeName, hoveredFrame.Name);
                }
                else
                {
                    this.DetachGraphElementFromFrame(nodeName);
                }
            }
        }
    }

    private void OnNodeSelected(Node node)
    {
        GD.Print($"Node selected: {node}; named {node.Name}");
        _selectedNodes.Add(node.Name);
    }

    private void OnNodeDeselected(Node node)
    {
        _selectedNodes.Remove(node.Name);
    }

    private void OnNodeRemoved(Node node)
    {
        _selectedNodes.Remove(node.Name);
    }

    private void OnConnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        BeginTransaction("Connect graph nodes", backwardUndoOps: true);
        var fromNode = this.GetNode(new NodePath(fromNodeName));
        var toNode = this.GetNode(new NodePath(toNodeName));
        if (fromNode is SignalNodeGraphNode fromGraphNode &&
            toNode is SignalNodeGraphNode toGraphNode &&
            fromGraphNode.Node is {} fromSceneNode &&
            toGraphNode.Node is {} toSceneNode)
        {
            UndoRedo.AddUndoMethod(this, MethodName.UpdateSceneTree);
            var callable = new Callable(toSceneNode,
                toGraphNode.GetMethodPortName((int)toPort));
            this.TransactionConnectSignal(fromSceneNode,
                fromGraphNode.GetSignalPortName((int)fromPort),
                callable, createAndCommit: false);
            UndoRedo.AddDoMethod(this, MethodName.UpdateSceneTree);
        }
        this.TransactionConnect(fromNodeName, (int)fromPort, toNodeName, (int)toPort, createAndCommit: false);
        EndTransaction();
    }

    private void OnDisconnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        BeginTransaction("Disconnect graph nodes", backwardUndoOps: true);
        var fromNode = this.GetNode(new NodePath(fromNodeName));
        var toNode = this.GetNode(new NodePath(toNodeName));
        if (fromNode is SignalNodeGraphNode fromGraphNode &&
            toNode is SignalNodeGraphNode toGraphNode &&
            fromGraphNode.Node is {} fromSceneNode &&
            toGraphNode.Node is {} toSceneNode)
        {
            UndoRedo.AddUndoMethod(this, MethodName.UpdateSceneTree);
            var callable = new Callable(toSceneNode,
                toGraphNode.GetMethodPortName((int)toPort));
            this.TransactionDisconnectSignal(fromSceneNode,
                fromGraphNode.GetSignalPortName((int)fromPort),
                callable, createAndCommit: false);
            UndoRedo.AddDoMethod(this, MethodName.UpdateSceneTree);
        }
        this.TransactionDisconnect(fromNodeName, (int)fromPort, toNodeName, (int)toPort, createAndCommit: false);
        EndTransaction();
    }

    public void UpdateSceneTree()
    {
        if(_sceneRoot != null)
            _sceneRoot.Name = _sceneRoot.Name;
    }
}
#endif