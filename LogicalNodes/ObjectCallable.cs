using System;
using System.Text;
using EditorUtil;
using Godot;
using Godot.Collections;
#if TOOLS
using Util.SignalGraphs.Plugin.Graph;
#endif

namespace LogicalNodes;

[Tool]
[GlobalClass]
[Icon($"{LogicalNodeIcons.IconRoot}/callable.png")]
public partial class ObjectCallable : ValueSource
{
    [Export]
    public ValueSource Target;
    [Export]
    public StringName Method;

    [Export] public ValueSource[] Args;
    
    public Variant Call(Node source)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Call(Method, EvaluateArgs(source));
    }
    public T Call<[MustBeVariant]T>(Node source)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Call(Method, EvaluateArgs(source)).As<T>();
    }
    
    public Variant CallWithArgs(Node source, params Variant[] args)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Call(Method, args);
    }
    public T CallWithArgs<[MustBeVariant]T>(Node source, params Variant[] args)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Call(Method, args).As<T>();
    }

    public Variant[] EvaluateArgs(Node source)
    {
        if (Args == null) return null;
        var args = new Variant[Args.Length];
        for (int i = 0; i < Args.Length; i++)
        {
            args[i] = Args[i]?.GetValue(source) ?? default;
        }

        return args;
    }

    public override Variant GetValue(Node source)
    {
        return Call(source);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Target);
        sb.Append("  :: ");
        sb.Append(Method);
        sb.Append('(');
        if (Args is { Length: > 0 })
        {
            for (var i = 0; i < Args.Length; i++)
            {
                sb.Append(Args[i]);
                if(i < Args.Length-1)
                    sb.Append(", ");
            }
        }
        sb.Append(')');
        return sb.ToString();
    }

#if TOOLS
    private NodePath _popupSelectedNodePath;
    private StringName _popupSelectedMethodName;
    
    [InspectorCustomControl(AnchorProperty = nameof(Target), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Slot", "EditorIcons");
        if(Target != null && Method is {IsEmpty: false})
            button.Text = this.ToString();
        else
            button.Text = "Select Method...";
        button.Pressed += ShowNodePicker;
        
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
        undoRedo.AddDoProperty(this, PropertyName.Target, new NodeValue(_popupSelectedNodePath));
        undoRedo.AddDoProperty(this, PropertyName.Method, _popupSelectedMethodName);
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.Target, Target);
        undoRedo.AddUndoProperty(this, PropertyName.Method, Method);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif
}