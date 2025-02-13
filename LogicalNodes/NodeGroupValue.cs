using System.Collections.Generic;
using Godot;

namespace LogicalNodes;

[Tool]
[GlobalClass]
[Icon($"{LogicalNodeIcons.IconRoot}/node_group.png")]
public partial class NodeGroupValue : ValueSource, IEnumerableValueSource<Node>, IEnumerableValueSource<Variant>
{
    [Export] public StringName GroupName;
    
    public override Variant GetValue(Node source)
    {
        return source.GetTree().GetFirstNodeInGroup(GroupName);
    }

    IEnumerable<Variant> IEnumerableValueSource<Variant>.EnumerateValues(Node source)
    {
        foreach (var node in source.GetTree().GetNodesInGroup(GroupName))
        {
            yield return node;
        }
    }

    IEnumerable<Node> IEnumerableValueSource<Node>.EnumerateValues(Node source)
    {
        foreach (var node in source.GetTree().GetNodesInGroup(GroupName))
        {
            yield return node;
        }
    }
}