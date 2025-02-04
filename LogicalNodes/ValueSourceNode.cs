using Godot;
using Util.SignalGraphs.Plugin;

namespace LogicalNodes;

[Tool]
[GlobalClass]
[Icon($"{SignalGraphsPlugin.IconRoot}/node_value.png")]
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