using Godot;

namespace Util.SignalGraphs.Plugin;

[Tool]
public partial class SignalGraphEditorRoot : Control
{
	public SignalGraphsPlugin Plugin;
	public EditorUndoRedoManager UndoRedo;

	[Export] public Graph.SignalGraphEditor Editor;

	public void ReloadPlugin()
	{
		Plugin.Reload();
	}

	public void PopulateFromScene()
	{
		var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
		Editor?.Clear();
		Editor?.PopulateFromScene(sceneRoot);
	}

	public void SaveToScene()
	{
		var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
		Editor?.Save(sceneRoot);
	}
}
