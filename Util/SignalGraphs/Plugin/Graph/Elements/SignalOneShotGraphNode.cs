#if TOOLS
using Godot;
using Godot.Collections;

namespace Util.SignalGraphs.Plugin.Graph.Elements;

public partial class SignalOneShotGraphNode : GraphNode
{
    public bool Setup(Dictionary savedData, SignalGraphEditor editor)
    {
        this.Title = "One-Shot";
        this.AddChild(new Label()
        {
            Text = ""
        });
        this.SetSlot(0,
            true,
            SignalGraphEditor.PortTypeSignal,
            Colors.White,
            true,
            SignalGraphEditor.PortTypeSignal,
            Colors.White);
        
        this.PositionOffset = savedData?["position_offset"].AsVector2() ?? Vector2.Zero;

        return true;
    }

    public Dictionary GetSaveData()
    {
        var dict = new Dictionary();
        dict["position_offset"] = PositionOffset;
        return dict;
    }
}
#endif