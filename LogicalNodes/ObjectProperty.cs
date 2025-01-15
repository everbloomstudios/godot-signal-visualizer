using EditorUtil;
using Godot;

namespace LogicalNodes;

[Tool]
[GlobalClass]
public partial class ObjectProperty : ValueSource
{
    [Export]
    public ValueSource Target;
    [Export]
    public StringName Property;

    [Export] public bool ErrorOnMissingNode = true;
    
    public Variant Get(Node source)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Get(Property);
    }
    public T Get<[MustBeVariant]T>(Node source)
    {
        if (Target == null) return default;
        return Target.GetValue(source).As<GodotObject>().Get(Property).As<T>();
    }

    public override Variant GetValue(Node source)
    {
        return Get(source);
    }

    public override string ToString()
    {
        return $"{Target} :: {Property}";
    }

#if TOOLS
    private NodePath _popupSelectedNodePath;
    private StringName _popupSelectedPropertyName;
    
    [InspectorCustomControl(AnchorProperty = nameof(Target), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Property", "EditorIcons");
        if (Target != null && Property is { IsEmpty: false })
            button.Text = this.ToString();
        else
            button.Text = "Select Property...";
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

        ShowPropertyPicker(node);
    }

    private void ShowPropertyPicker(Node node)
    {
        EditorInterface.Singleton.PopupPropertySelector(node, new Callable(this, MethodName.OnPropertySelected));
    }

    private void OnPropertySelected(NodePath propertyPath)
    {
        string property = propertyPath.GetConcatenatedSubNames();
        if (string.IsNullOrEmpty(property)) return;
        _popupSelectedPropertyName = property;
        GD.Print($"Selected property: {property}");
        CommitPopupChange();
    }

    private void CommitPopupChange()
    {
        var undoRedo = StandardUtilPlugin.GetUndoRedo();
        undoRedo.CreateAction("Set node property");
        undoRedo.AddDoProperty(this, PropertyName.Target, new NodeValue(_popupSelectedNodePath));
        undoRedo.AddDoProperty(this, PropertyName.Property, _popupSelectedPropertyName);
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.Target, Target);
        undoRedo.AddUndoProperty(this, PropertyName.Property, Property);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif
}