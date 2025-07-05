using Godot;

namespace LogicalNodes.Signals;

[Tool]
[GlobalClass]
public partial class InboundSignalArgument : ValueSource
{
    [Export] public int ArgumentIndex;
    
    public override Variant GetValue(Node source)
    {
        if (source is SignalPortInbound inbound) return inbound.GetArgument(ArgumentIndex);
        
        GD.PushError($"Cannot use {nameof(InboundSignalArgument)} value source from any node other than a {nameof(SignalPortInbound)} node.");
        return default;
    }

    public override string ToString()
    {
        return $"<arg {ArgumentIndex}>";
    }
}