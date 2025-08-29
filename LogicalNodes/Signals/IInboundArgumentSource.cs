using Godot;

namespace LogicalNodes.Signals;

public interface IInboundArgumentSource
{
    public Variant GetArgument(int index);
}