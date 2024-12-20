using Godot;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private void OnPopupRequest(Vector2 atposition)
    {
        GD.Print($"Popup request at {atposition}");
        var menu = new PopupMenu();
        menu.AddItem("New Frame");
        menu.AddItem("New: One Shot");
        menu.AddItem("four");
        this.AddChild(menu);
        menu.Position = (Vector2I)(this.GetScreenPosition() + atposition);
        _useContextPosition = true;
        _contextPosition = atposition;
        
        menu.Show();
        menu.CloseRequested += menu.QueueFree;
        menu.IndexPressed += (index) =>
        {
            if (index == 0) AddFrame();
            if (index == 1) AddOneShot();
        };
    }
}