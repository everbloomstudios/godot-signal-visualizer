using EditorUtil;
using Godot;

namespace LogicalNodes;

[Tool]
[GlobalClass]
public partial class ConstantValue : ValueSource
{
    [Export]
    public Variant Value;

    public override Variant GetValue(Node source)
    {
        return Value;
    }
    
#if TOOLS
    // [InspectorCustomControl(AnchorProperty = nameof(Value), AnchorMode = InspectorPropertyAnchorMode.Before)]
    // public Control SelectMethod()
    // {
    //     var control = new EditorProperty();
    //     button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Property", "EditorIcons");
    //     if(NodePath is {IsEmpty: false} && Property is {IsEmpty: false})
    //         button.Text = $"{NodePath} . {Property}()";
    //     else
    //         button.Text = "Select Property...";
    //     button.Pressed += ShowNodePicker;
    //     
    //     return control;
    // }
#endif
}