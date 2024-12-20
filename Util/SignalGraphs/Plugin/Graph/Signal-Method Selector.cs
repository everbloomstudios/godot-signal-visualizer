using Godot;
using Godot.Collections;
using Util.SignalGraphs.Plugin.Graph.Elements;

namespace Util.SignalGraphs.Plugin.Graph;

public partial class SignalGraphEditor
{
    private int _lastSelectedSignalMethodTabIndex = 0;
    
    public void ShowSignalMethodSelector(SignalNodeGraphNode graphNode, int forceStartTab)
    {
        var node = graphNode.Node;
        if (node == null) return;
        
        if (forceStartTab < 0) forceStartTab = _lastSelectedSignalMethodTabIndex;
        _lastSelectedSignalMethodTabIndex = forceStartTab;
        
        var dialog = CreateSignalMethodSelector(forceStartTab, out var searchBar, out var tree, out var submitButton, out var tabGroup);
        var theme = dialog.Theme;
        
        // EditorInterface.Singleton.PopupDialogCentered(dialog);
        var signalList = new Array<Dictionary>();
        var methodList = new Array<Dictionary>();

        var script = node.GetScript().As<Script>();
        while (script != null)
        {
            // GD.Print($"Script: {script.ResourcePath}");
            signalList.AddRange(script.GetScriptSignalList());
            methodList.AddRange(script.GetScriptMethodList());
            script = script.GetBaseScript();
        }
        StringName className = node.GetClass();
        while (className != null && !className.IsEmpty)
        {
            // GD.Print($"Class: {className}");
            signalList.AddRange(ClassDB.ClassGetSignalList(className));
            methodList.AddRange(ClassDB.ClassGetMethodList(className));
            className = ClassDB.Singleton.GetParentClass(className);
        }

        void PopulateTree(string filter)
        {
            var selectedHandle = tree.GetSelected()?.GetMetadata(0).AsVector2I() ?? new Vector2I(-1, 0);
            
            tree.Clear();

            var root = tree.CreateItem();
            
            if (tabGroup.GetPressedButton().GetIndex() == 0)
            {
                var methodRoot = tree.CreateItem(root);
                methodRoot.SetText(0, "Methods");

                for (var methodIndex = 0; methodIndex < methodList.Count; methodIndex++)
                {
                    var handle = new Vector2I(0, methodIndex);

                    var method = methodList[methodIndex];
                    string text = SignalGraphEditor.GetMethodSignatureText(method);

                    if (!string.IsNullOrEmpty(filter) &&
                        !text.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = tree.CreateItem(methodRoot);
                    item.SetIcon(0, theme.GetIcon(SignalGraphEditor.IconNameMethod, "EditorIcons"));
                    item.SetText(0, text);
                    item.SetMetadata(0, handle);
                    if (selectedHandle == handle)
                    {
                        item.Select(0);
                    }
                }
            }

            if (tabGroup.GetPressedButton().GetIndex() == 1)
            {
                var signalRoot = tree.CreateItem(root);
                signalRoot.SetText(0, "Signals");
                
                for (var signalIndex = 0; signalIndex < signalList.Count; signalIndex++)
                {
                    var handle = new Vector2I(1, signalIndex);
                    
                    var signal = signalList[signalIndex];
                    string text = SignalGraphEditor.GetMethodSignatureText(signal);

                    if (!string.IsNullOrEmpty(filter) &&
                        !text.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = tree.CreateItem(signalRoot);
                    item.SetIcon(0, theme.GetIcon(SignalGraphEditor.IconNameSignal, "EditorIcons"));
                    item.SetText(0, text);
                    item.SetMetadata(0, handle);
                    if (selectedHandle == handle)
                    {
                        item.Select(0);
                    }
                }
            }
            
            OnSelectionChanged();
        }
        dialog.WindowInput += OnWindowInput;

        tree.ItemSelected += OnSelectionChanged;
        tree.ItemActivated += TreeOnItemActivated;

        submitButton.Pressed += OnSubmitted;

        tabGroup.Pressed += OnTabGroupPressed;
        searchBar.TextChanged += PopulateTree;
        // This is necessary so that the tab group gets garbage collected and doesn't cause the assembly reload to fail on recompilation.
        dialog.TreeExiting += () => tabGroup.Pressed -= OnTabGroupPressed;

        PopulateTree("");
        
        dialog.Visible = false;
        this.AddChild(dialog);
        dialog.PopupCentered();
        
        return;

        void OnWindowInput(InputEvent evt)
        {
            if (evt.IsAction("ui_cancel"))
            {
                dialog.SetInputAsHandled();
                dialog.QueueFree();
            }

            if (evt.IsAction("ui_accept") && !dialog.IsInputHandled())
            {
                dialog.SetInputAsHandled();
                if(tree.GetSelected() != null)
                    OnSubmitted();
            }
        }
        
        void TreeOnItemActivated()
        {
            tree.AcceptEvent();
            if(tree.GetSelected() != null)
                OnSubmitted();
        }
        
        void OnSelectionChanged()
        {
            submitButton.Disabled = tree.GetSelected() == null;
        }

        void OnTabGroupPressed(BaseButton btn)
        {
            _lastSelectedSignalMethodTabIndex = btn.GetIndex();
            PopulateTree(searchBar.Text);
        }

        void OnSubmitted()
        {
            var selectedHandle = tree.GetSelected()?.GetMetadata(0).AsVector2I() ?? new Vector2I(-1, 0);
            int selectedType = selectedHandle.X;
            int selectedIndex = selectedHandle.Y;
            if (selectedType == 0)
            {
                // method
                var method = methodList[selectedIndex];
                var methodName = method["name"].AsStringName();
                GD.Print($"Selected method: {method}");
                graphNode.MethodAddRequested(method);
            } else if (selectedType == 1)
            {
                // signal
                var signal = signalList[selectedIndex];
                GD.Print($"Selected signal: {signal}");
                graphNode.SignalAddRequested(signal);
            }
            dialog.QueueFree();
            // GD.Print($"Submit {tree.GetSelected()}");
        }
    }

    
    private Window CreateSignalMethodSelector(int forceStartTab, out LineEdit searchBar, out Tree tree, out Button submitButton,
        out ButtonGroup tabGroup)
    {
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new Window()
        {
            Title = "Select Method/Signal",
            Borderless = false,
            Size = new Vector2I(500, 400),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };

        var bg = new PanelContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        dialog.AddChild(bg);

        bg.AddThemeStyleboxOverride("panel", theme.GetStylebox("panel", "PopupPanel"));

        var vbox = new VBoxContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        bg.AddChild(vbox);

        var content = new VBoxContainer()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(content);
        var tabRow = new HBoxContainer();
        tabGroup = new ButtonGroup();
        var methodsButton = new Button()
        {
            Text = "Methods",
            ButtonGroup = tabGroup,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Icon = theme.GetIcon(IconNameMethod, "EditorIcons"),
            ToggleMode = true,
            ButtonPressed = forceStartTab <= 0,
            ThemeTypeVariation = "FlatMenuButton"
        };
        var signalsButton = new Button()
        {
            Text = "Signals",
            ButtonGroup = tabGroup,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Icon = theme.GetIcon(IconNameSignal, "EditorIcons"),
            ToggleMode = true,
            ButtonPressed = forceStartTab == 1,
            ThemeTypeVariation = "FlatMenuButton"
        };
        tabRow.AddChild(methodsButton);
        tabRow.AddChild(signalsButton);
        content.AddChild(tabRow);
        
        content.AddChild(searchBar = new LineEdit()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            PlaceholderText = "Filter",
            RightIcon = theme.GetIcon("Search", "EditorIcons")
        });
        
        content.AddChild(tree = new Tree()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HideRoot = true
        });

        var buttonRow = new HFlowContainer()
        {
            Alignment = FlowContainer.AlignmentMode.Center,
        };
        vbox.AddChild(buttonRow);
        var buttonSize = new Vector2(105, 34);
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(submitButton = new Button() { Text = "Add", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() {SizeFlagsHorizontal = Control.SizeFlags.ExpandFill});
        buttonRow.AddChild(new Button() { Text = "Close", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        
        dialog.CloseRequested += dialog.QueueFree;
        
        return dialog;
    }
}