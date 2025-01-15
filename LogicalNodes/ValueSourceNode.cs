using Godot;

namespace LogicalNodes;

[Tool]
[GlobalClass]
public partial class ValueSourceNode : Node, IValueSource
{
    [Export]
    public ValueSource ValueSource;

    public Variant GetValue(Node source)
    {
        return ValueSource?.GetValue(this) ?? default;
    }

    public T GetValue<[MustBeVariant]T>(Node source)
    {
        return ValueSource != null ? ValueSource.GetValue<T>(this) : default;
    }
}