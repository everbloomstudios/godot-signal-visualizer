using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private Vector2 LocalToGraphPosition(Vector2 position)
    {
        return (position + ScrollOffset) / Zoom;
    }

    private SignalNodeGraphNode GetGraphNodeForNode(Node node)
    {
        return GetNode<SignalNodeGraphNode>(node.GetInstanceId().ToString());
    }
    
    public static string GetMethodSignatureText(Dictionary info)
    {
        var args = info["args"].AsGodotArray();
        var rawDefaults = info["default_args"];
        var defaults = rawDefaults.VariantType != Variant.Type.Nil ? rawDefaults.AsGodotArray() : null;
        var sb = new StringBuilder();
        sb.Append(info["name"].AsString());
        sb.Append('(');
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i].AsGodotDictionary();
            if (i != 0) sb.Append(", ");

            sb.Append(arg["name"].AsString());
            sb.Append(": ");
            var type = arg["type"].As<Variant.Type>();
            if (type == Variant.Type.Object)
            {
                sb.Append(arg["class_name"].AsString());
            }
            else
            {
                sb.Append(type);
            }

            if (defaults != null)
            {
                var defaultValue = defaults.ElementAtOrDefault(i - (args.Count - defaults.Count));
                if (defaultValue.VariantType != Variant.Type.Nil)
                {
                    sb.Append(" = ");
                    sb.Append(defaultValue);
                }
            }
        }
        sb.Append(')');
        return sb.ToString();
    }
}