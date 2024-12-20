#if TOOLS
using Godot;

namespace Util.SignalGraphs.Plugin;

[Tool]
public partial class SignalGraphsPlugin : EditorPlugin
{
    private PackedScene _graphEditorTemplate =
        GD.Load<PackedScene>("res://addons/signal_graphs/scenes/signal_graph_editor.tscn");
    private SignalGraphEditorRoot _graphEditor;
    
    public override void _EnterTree()
    {
        CreateEditor();
    }

    public override void _ExitTree()
    {
        RemoveEditor();
    }

    private void CreateEditor()
    {
        _graphEditor = _graphEditorTemplate.Instantiate<SignalGraphEditorRoot>();
        _graphEditor.Plugin = this;
        _graphEditor.UndoRedo = GetUndoRedo();
        AddControlToBottomPanel(_graphEditor, "Signal Graph");
    }

    public void RemoveEditor()
    {
        RemoveControlFromBottomPanel(_graphEditor);
    }

    public void Reload()
    {
        RemoveEditor();
        _graphEditor.QueueFree();
        _graphEditor = null;
        CreateEditor();
    }
}
#endif