using Godot;
using Util.SignalGraphs.Plugin;

namespace LogicalNodes;

[Tool]
[GlobalClass]
[Icon($"{SignalGraphsPlugin.IconRoot}/none_of.png")]
public partial class NoneOfValues : ValueSource
{
    [Export]
    public ValueSource[] Operands;
    
    public override Variant GetValue(Node source)
    {
        if (Operands != null)
        {
            foreach (var operand in Operands)
            {
                if (operand.GetValue<bool>(source)) return false;
            }
        }
        return true;
    }
}