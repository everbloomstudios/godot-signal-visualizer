using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using Godot.Collections;

namespace Util.SignalGraphs.Plugin.Graph.Elements;

public partial class SignalNodeGraphNode : GraphNode
{
    public ulong NodeInstanceId;
    
    private SignalGraphEditor _editor;
    private Godot.Collections.Dictionary<StringName, int> _methodPorts;
    private Godot.Collections.Dictionary<StringName, int> _signalPorts;
    private Array<StringName> _forceShownMethods = new();
    private Array<StringName> _forceShownSignals = new();
    private Control _collapsiblePanel;
    
    private List<StoredConnection> _storedInputConnections = new();
    private List<StoredConnection> _storedOutputConnections = new();

    public Node Node => InstanceFromId(NodeInstanceId) as Node;

    public override void _Ready()
    {
        this.NodeSelected += OnNodeSelected;
        this.NodeDeselected += OnNodeDeselected;
    }

    public bool Setup(Node node, Dictionary savedData, SignalGraphEditor editor)
    {
        this._editor = editor;
        this.NodeInstanceId = node.GetInstanceId();
        this.Name = NodeInstanceId.ToString();
        this.Title = node.Name;
        
        _signalPorts = new Godot.Collections.Dictionary<StringName, int>();
        _methodPorts = new Godot.Collections.Dictionary<StringName, int>();

        // Create collapsible panel
        _collapsiblePanel = new VBoxContainer();
        var addButton = new Button()
        {
            Text = "+"
        };
        addButton.Pressed += OnAddButtonPressed;
        _collapsiblePanel.AddChild(addButton);

        // Add ports and any children
        bool anyPorts = CreateAndAddContents(node);
        
        // Deserialize saved data
        this.PositionOffset = savedData?["position_offset"].AsVector2() ?? Vector2.Zero;
        
        return anyPorts;
    }

    private bool RebuildContents()
    {
        this.ClearAllSlots();
        foreach (var child in GetChildren())
        {
            this.RemoveChild(child);
            if(child != _collapsiblePanel) child.QueueFree();
        }

        return CreateAndAddContents(Node);
    }

    public bool CreateAndAddContents(Node node)
    {
        var anyPorts = false;
        if (_collapsiblePanel != null && this.IsAncestorOf(_collapsiblePanel))
        {
            this.RemoveChild(_collapsiblePanel);
        }
        
        anyPorts |= AddSignalPorts(node);
        anyPorts |= AddMethodPorts(node);
        AddCollapsiblePanel();

        return anyPorts;
    }

    private void AddCollapsiblePanel()
    {
        UpdateCollapsiblePanelVisibility();
        this.AddChild(_collapsiblePanel);
        this.SetSlot(_collapsiblePanel.GetIndex(),
            true,
            SignalGraphEditor.PortTypeAddMethod,
            new Color(0x73f280ff),
            true,
            SignalGraphEditor.PortTypeAddSignal,
            new Color(0xff786bff));
    }

    private bool AddSignalPorts(Node node)
    {
        var anyPorts = false;
        _signalPorts.Clear();
        var leftPortIndex = 0;
        foreach (var signal in node.GetSignalList())
        {
            var signalName = signal["name"].AsStringName();

            if (!ShouldShowSignal(signalName, signal, node)) continue;

            var rowControl = CreateRow(null, signalName, SignalGraphEditor.IconNameSignal);
            rowControl.TooltipText = $"Signal: {SignalGraphEditor.GetMethodSignatureText(signal)}";
            this.AddChild(rowControl);
            
            this.SetSlot(this.GetChildCount() - 1,
                false,
                -1,
                Colors.Black,
                true,
                SignalGraphEditor.PortTypeSignal,
                Colors.White);
            _signalPorts[signalName] = leftPortIndex;

            leftPortIndex++;
            anyPorts = true;
        }

        return anyPorts;
    }

    private bool AddMethodPorts(Node node)
    {
        var anyPorts = false;
        _methodPorts.Clear();
        var rightPortIndex = 0;
        var connectedMethodNames = _editor.GDSToCSBridge.GetConnectedMethodNames(node);
        // var typeMethods = node.GetType().GetMethods();
        
        foreach (var method in node.GetMethodList())
        {
            var methodName = method["name"].AsStringName();
            if (_methodPorts.ContainsKey(methodName)) continue; // skip method overloads
            
            if(!ShouldShowMethod(methodName, method, node, connectedMethodNames)) continue;

            var rowControl = CreateRow(SignalGraphEditor.IconNameMethod, methodName, null);
            rowControl.TooltipText = $"Method: {SignalGraphEditor.GetMethodSignatureText(method)}";
            
            this.AddChild(rowControl);
            _methodPorts[methodName] = rightPortIndex;
            
            this.SetSlot(this.GetChildCount() - 1,
                true,
                SignalGraphEditor.PortTypeSignal,
                Colors.Aqua,
                false,
                -1,
                Colors.Black);

            rightPortIndex++;
            anyPorts = true;
        }

        return anyPorts;
    }

    private bool ShouldShowMethod(StringName methodName, Dictionary method, Node node,
        HashSet<StringName> connectedMethodNames = null)
    {
        // Check if method force-shown
        if (_forceShownMethods.Contains(methodName)) return true;

        // Check if method has an incoming connection
        connectedMethodNames ??= _editor.GDSToCSBridge.GetConnectedMethodNames(node);
        bool portConnected = connectedMethodNames.Contains(methodName);
        if (portConnected) return true;
        
        // Check if method has been annotated in C#
        // var annotated = false;
        // foreach (var methodInfo in typeMethods)
        // {
        //     if (methodInfo.Name != methodName) continue;
        //     if (methodInfo.GetCustomAttribute<GraphMethodAttribute>() == null) continue;
        //
        //     return true; //annotated
        // }

        return false;
    }

    private bool ShouldShowSignal(StringName signalName, Dictionary signal, Node node)
    {
        if (_forceShownSignals.Contains(signalName)) return true;
        
        foreach (var connection in node.GetSignalConnectionList(signalName))
        {
            var flags = connection["flags"].As<ConnectFlags>();
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            if ((flags & ConnectFlags.Persist) != 0) return true;
        }

        return false;
    }

    private void OnAddButtonPressed()
    {
        SignalGraphEditor.ShowSignalMethodSelector(this.Node, -1, SignalAddRequested, MethodAddRequested);
    }


    private static Control CreateRow(string iconLeft, string text, string iconRight)
    {
        var label = new Label()
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var iconRectLeft = CreateIconControl(iconLeft);
        var iconRectRight = CreateIconControl(iconRight);
        var maxMinimumSize = new Vector2(
            Mathf.Max(iconRectLeft.GetMinimumSize().X, iconRectRight.GetMinimumSize().X),
            Mathf.Max(iconRectLeft.GetMinimumSize().Y, iconRectRight.GetMinimumSize().Y)
            );
        iconRectLeft.CustomMinimumSize = iconRectRight.CustomMinimumSize = maxMinimumSize;
        
        var container = new HBoxContainer();
        container.AddChild(iconRectLeft);
        container.AddChild(label);
        container.AddChild(iconRectRight);
        return container;

        Control CreateIconControl(string iconName)
        {
            float height = label.GetMinimumSize().Y;
            return iconName != null
                ? new TextureRect()
                {
                    Texture = EditorInterface.Singleton.GetEditorTheme().GetIcon(iconName, "EditorIcons"),
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
                }
                : new Control()
                {
                    CustomMinimumSize = new Vector2(height, height)
                };
        }
    }

    public void OnNodeSelected()
    {
        UpdateCollapsiblePanelVisibility();

        if (this.Node is { } node)
        {
            EditorInterface.Singleton.EditNode(node);
        }
    }

    private void UpdateCollapsiblePanelVisibility()
    {
        _collapsiblePanel.Visible = Selected && this.GetRect().HasPoint(_editor.GetLocalMousePosition());
        this.ResetSize();
    }

    public void OnNodeDeselected()
    {
        _collapsiblePanel.Visible = false;
        this.ResetSize();
    }
    

    public int GetSignalPortId(StringName signalName)
    {
        return _signalPorts.GetValueOrDefault(signalName, -1);
    }

    public int GetMethodPortId(StringName methodName)
    {
        return _methodPorts.GetValueOrDefault(methodName, -1);
    }

    public StringName GetSignalPortName(int portId)
    {
        foreach (var entry in _signalPorts)
        {
            if (entry.Value == portId) return entry.Key;
        }

        return default;
    }

    public StringName GetMethodPortName(int portId)
    {
        foreach (var entry in _methodPorts)
        {
            if (entry.Value == portId) return entry.Key;
        }

        return default;
    }

    public Dictionary GetSaveData()
    {
        var dict = new Dictionary();
        dict["position_offset"] = PositionOffset;
        return dict;
    }

    public void MethodAddRequested(Dictionary method)
    {
        var methodName = method["name"].AsStringName();
        if(!_forceShownMethods.Contains(methodName)) _forceShownMethods.Add(methodName);
        
        StoreConnections(true);
        
        RebuildContents();
        _collapsiblePanel.Visible = true;
        
        RestoreConnections();
    }

    public void SignalAddRequested(Dictionary signal)
    {
        var signalName = signal["name"].AsStringName();
        if(!_forceShownSignals.Contains(signalName)) _forceShownSignals.Add(signalName);
        
        StoreConnections(true);
        
        RebuildContents();
        _collapsiblePanel.Visible = true;
        
        RestoreConnections();
    }

    private void StoreConnections(bool disconnect)
    {
        _storedInputConnections.Clear();
        _storedOutputConnections.Clear();

        foreach (var connection in _editor.GetConnectionList())
        {
            var fromNode = connection["from_node"].AsStringName();
            var toNode = connection["to_node"].AsStringName();
            int fromPort = connection["from_port"].AsInt32();
            int toPort = connection["to_port"].AsInt32();
            if (toNode == this.Name)
            {
                _storedInputConnections.Add(StoredConnection.CreateFromInput(fromNode, toNode, fromPort, toPort, this));
            } else if (fromNode == this.Name)
            {
                _storedOutputConnections.Add(StoredConnection.CreateFromOutput(fromNode, toNode, fromPort, toPort, this));
            } else continue;

            if (disconnect)
            {
                _editor.DisconnectNode(fromNode, fromPort, toNode, toPort);
            }
        }
    }

    private void RestoreConnections()
    {
        foreach (var storedConnection in _storedInputConnections)
        {
            GD.Print($"Restoring input connection: {storedConnection.ToString()}");
            storedConnection.RestoreInput(this, _editor);
        }
        foreach (var storedConnection in _storedOutputConnections)
        {
            GD.Print($"Restoring output connection: {storedConnection.ToString()}");
            storedConnection.RestoreOutput(this, _editor);
        }
    }

    private struct StoredConnection
    {
        // The name of the method/signal that this connection was made with, in this node.
        public StringName ThisPortName;
        
        // The name of the other graph node this connection was made with.
        public StringName OtherNodeName;
        // The index of the port via which the other graph node was connected.
        public int OtherNodePort;
        
        // NOTE: If OtherNodePort is -1, it has a special meaning: this connection represents a connection between this node and itself.
        // In which case, the OtherNodeName field will instead store the name of the method it was connected to, and ThisPortName will store the signal.

        public bool IsSelfConnection => OtherNodePort == -1;

        public static StoredConnection CreateFromSelf(StringName fromNode, StringName toNode, int fromPort, int toPort,
            SignalNodeGraphNode graphNode)
        {
            var signalName = graphNode.GetSignalPortName(fromPort);
            var methodName = graphNode.GetMethodPortName(toPort);
            return new StoredConnection()
            {
                ThisPortName = signalName,
                OtherNodePort = -1,
                OtherNodeName = methodName
            };
        }
        
        public static StoredConnection CreateFromInput(StringName fromNode, StringName toNode, int fromPort, int toPort, SignalNodeGraphNode graphNode)
        {
            if (fromNode == toNode)
            {
                // self connection
                return CreateFromSelf(fromNode, toNode, fromPort, toPort, graphNode);
            }
            
            return new StoredConnection()
            {
                ThisPortName = graphNode.GetMethodPortName(toPort),
                OtherNodePort = fromPort,
                OtherNodeName = fromNode
            };
        }
        
        public static StoredConnection CreateFromOutput(StringName fromNode, StringName toNode, int fromPort, int toPort, SignalNodeGraphNode graphNode)
        {
            if (fromNode == toNode)
            {
                // self connection
                return CreateFromSelf(fromNode, toNode, fromPort, toPort, graphNode);
            }
            
            return new StoredConnection()
            {
                ThisPortName = graphNode.GetSignalPortName(fromPort),
                OtherNodePort = toPort,
                OtherNodeName = toNode
            };
        }

        private void RestoreSelf(SignalNodeGraphNode graphNode, SignalGraphEditor editor)
        {
            int fromPort = graphNode.GetSignalPortId(ThisPortName);
            int toPort = graphNode.GetMethodPortId(OtherNodeName);
            editor.ConnectNode(graphNode.Name, fromPort, graphNode.Name, toPort);
        }

        public void RestoreInput(SignalNodeGraphNode graphNode, SignalGraphEditor editor)
        {
            if (this.IsSelfConnection)
            {
                RestoreSelf(graphNode, editor);
                return;
            }
            
            int toPort = graphNode.GetMethodPortId(ThisPortName);
            editor.ConnectNode(OtherNodeName, OtherNodePort, graphNode.Name, toPort);
        }

        public void RestoreOutput(SignalNodeGraphNode graphNode, SignalGraphEditor editor)
        {
            if (this.IsSelfConnection)
            {
                RestoreSelf(graphNode, editor);
                return;
            }
            
            int fromPort = graphNode.GetSignalPortId(ThisPortName);
            editor.ConnectNode(graphNode.Name, fromPort, OtherNodeName, OtherNodePort);
        }

        public override string ToString()
        {
            return $"{nameof(ThisPortName)}: {ThisPortName}, {nameof(OtherNodeName)}: {OtherNodeName}, {nameof(OtherNodePort)}: {OtherNodePort}";
        }
    }
}