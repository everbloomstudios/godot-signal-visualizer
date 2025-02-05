#if TOOLS
using Godot;
using Godot.Collections;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

[Tool]
public partial class SignalGraphEditor : GraphEdit
{
    private static readonly StringName __graphDataMetaName = "_signal_graph_data";

    public static readonly int PortTypeLocked = -1;
    public static readonly int PortTypeSignal = 0;
    public static readonly int PortTypeAddSignal = 9;
    public static readonly int PortTypeAddMethod = 10;
    
    [Export] public GDScriptToCSBridge GDSToCSBridge;
    
    private bool _useContextPosition;
    private Vector2 _contextPosition;

    private Node _sceneRoot;
    private Array<StringName> _selectedNodes = new Array<StringName>();
    private bool _draggingAcrossFrames = false;

    private SignalGraphEditorRoot EditorRoot => this.Owner as SignalGraphEditorRoot;
    private EditorUndoRedoManager UndoRedo => EditorRoot.UndoRedo;
    
    public override void _Ready()
    {
        SubscribeToInterfaceSignals();
    }

    private SignalOneShotGraphNode AddOneShot()
    {
        var graphNode = new SignalOneShotGraphNode();
        graphNode.Setup(null, this);
        
        if (_useContextPosition)
        {
            graphNode.PositionOffset = LocalToGraphPosition(_contextPosition);
        }
        TransactionAddChild(graphNode, true);
        
        return graphNode;
    }

    public void Clear()
    {
        this.ClearConnections();
        foreach (var child in this.GetChildren())
        {
            if (child is not GraphElement) continue;
            RemoveChild(child);
            child.QueueFree();
        }
    }

    public void Save(Node sceneRoot)
    {
        _sceneRoot = sceneRoot;

        foreach (var child in this.GetChildren())
        {
            if (child is not SignalNodeGraphNode graphNode) continue;
            var saveData = graphNode.GetSaveData();
            var node = graphNode.Node;
            if (node == null) continue;
            node.SetMeta(__graphDataMetaName, saveData);
        }
    }
}
#endif