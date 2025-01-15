using Godot;

namespace LogicalNodes;

public interface IValueSource
{
    public Variant GetValue(Node source);

    public T GetValue<[MustBeVariant] T>(Node source)
    {
        return GetValue(source).As<T>();
    }
}