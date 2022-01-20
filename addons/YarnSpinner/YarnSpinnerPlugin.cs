#if TOOLS

using Godot;
using System;

[Tool]
public class YarnSpinnerPlugin : EditorPlugin {
    public override void _EnterTree() {
        AddComponent("res://addons/YarnSpinner/DialogueRunner.cs", "DialogueRunner", "Control");
        AddComponent("res://addons/YarnSpinner/Localization.cs", "Localization", "Node");
        AddComponent("res://addons/YarnSpinner/YarnProject.cs", "YarnProject", "Node");
    }
    public override void _ExitTree() {
        RemoveCustomType("DialogueRunner");
        RemoveCustomType("Localization");
        RemoveCustomType("YarnProject");
    }
    private void AddComponent(string scriptName, string name, string parent) {
        var script = GD.Load<Script>(scriptName);
        var icon = GD.Load<Texture>("res://addons/YarnSpinner/icon.png");
        AddCustomType(name, parent, script, icon);
    }
}

#endif