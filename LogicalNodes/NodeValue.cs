using Godot;

namespace LogicalNodes;

[Tool]
[GlobalClass]
public partial class NodeValue : ValueSource
{
    [Export]
    public NodePath NodePath;

    [Export] public bool ErrorOnMissingNode = true;
    [Export] public bool EvaluateNestedSourceNodes = true;

    public NodeValue()
    {
    }

    public NodeValue(NodePath nodePath)
    {
        NodePath = nodePath;
    }

    public Variant Get(Node source)
    {
        if (source == null) return default;
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        if(EvaluateNestedSourceNodes && referencedNode is ValueSourceNode newSourceNode) return newSourceNode.GetValue(source);
        return referencedNode;
    }
    public T Get<[MustBeVariant]T>(Node source) where T : Node
    {
        if (source == null) return default;
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        if(EvaluateNestedSourceNodes && referencedNode is ValueSourceNode newSourceNode) return newSourceNode.GetValue<T>(source);
        return referencedNode as T;
    }

    public override Variant GetValue(Node source)
    {
        return Get(source);
    }

    public override string ToString()
    {
        return NodePath ?? "<null>";
    }
}