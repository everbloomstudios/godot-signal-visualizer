using Godot;
using Godot.Collections;

namespace LogicalNodes;

[Tool]
[GlobalClass]
[Icon($"{LogicalNodeIcons.IconRoot}/constant.png")]
public partial class ConstantValue : ValueSource
{
    [Export]
    public Variant.Type Type;
    [Export]
    public Variant Value;

    public override Variant GetValue(Node source)
    {
        return Value;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.Type)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
            property["usage"] = Variant.From(usage);
        }
        if (propName == PropertyName.Value)
        {
            property["type"] = Variant.From(Type);
        }
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}