using System;
using EditorUtil;
using Godot;
using Godot.Collections;
using Util.SignalGraphs.Plugin.Graph;

namespace LogicalNodes;

[Tool]
[GlobalClass]
public partial class NodeCallable : Resource
{
    [Export]
    public NodePath NodePath;
    [Export]
    public StringName Method;

    [Export] public bool ErrorOnMissingNode = true;
    
    public Variant Call(Node source)
    {
        if (source == null) return default;
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        return referencedNode?.Call(Method) ?? default;
    }
    public T Call<[MustBeVariant]T>(Node source)
    {
        if (source == null) return default;
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        return referencedNode != null ? referencedNode.Call(Method).As<T>() : default;
    }
    
#if TOOLS
    private NodePath _popupSelectedNodePath;
    private StringName _popupSelectedMethodName;
    
    [InspectorCustomControl(AnchorProperty = nameof(Godot.NodePath), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Slot", "EditorIcons");
        if(NodePath is {IsEmpty: false} && Method is {IsEmpty: false})
            button.Text = $"{NodePath} :: {Method}()";
        else
            button.Text = "Select Method...";
        button.Pressed += ShowNodePicker;
        
        // picker.ResourceChanged += (resource) =>
        // {
        //     var undoRedo = StandardUtilPlugin.GetUndoRedo();
        //     var prevResource = GetResource();
        //     undoRedo.CreateAction("Set resource reference");
        //     undoRedo.AddDoProperty(this, PropertyName.SetResource, resource);
        //     undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        //     undoRedo.AddUndoProperty(this, PropertyName.SetResource, prevResource);
        //     undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        //     undoRedo.CommitAction();
        // };
        return button;
    }

    private void ShowNodePicker()
    {
        EditorInterface.Singleton.PopupNodeSelector(new Callable(this, MethodName.OnNodeSelected));
    }

    private void OnNodeSelected(NodePath nodePath)
    {
        if (nodePath is not { IsEmpty: false }) return;
        
        var source = EditorInterface.Singleton.GetInspector().GetEditedObject() as Node;
        if (source == null)
        {
            GD.PrintErr("Currently inspected object is not a node. Cannot continue method selection.");
            return;
        }
        
        var node = source.Owner.GetNode(nodePath);
        GD.Print($"Selected node: {node}");

        if (node == null)
        {
            GD.PrintErr("Selected node path does not point to a node? Cannot continue method selection.");
            return;
        }
        _popupSelectedNodePath = source.GetPathTo(node);

        ShowMethodPicker(node);
    }

    private void ShowMethodPicker(Node node)
    {
        SignalGraphEditor.ShowSignalMethodSelector(node, 0, null, OnMethodSelected);
    }

    private void OnMethodSelected(Dictionary method)
    {
        var methodName = method["name"].AsStringName();
        _popupSelectedMethodName = methodName;
        GD.Print($"Selected method: {methodName}");
        CommitPopupChange();
    }

    private void CommitPopupChange()
    {
        var undoRedo = StandardUtilPlugin.GetUndoRedo();
        undoRedo.CreateAction("Set node callable");
        undoRedo.AddDoProperty(this, PropertyName.NodePath, _popupSelectedNodePath);
        undoRedo.AddDoProperty(this, PropertyName.Method, _popupSelectedMethodName);
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.NodePath, NodePath);
        undoRedo.AddUndoProperty(this, PropertyName.Method, Method);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif
}