#if TOOLS

using Godot;
using System;

[Tool]
public class YarnSpinnerPlugin : EditorPlugin {
    public override void _EnterTree() {
        AddComponent("res://addons/YarnSpinner/DialogueRunner.cs", "DialogueRunner", "Control");
        AddComponent("res://addons/YarnSpinner/YarnProject.cs", "YarnProject", "Node");
        AddComponent("res://addons/YarnSpinner/Commands/DefaultActions.cs", "DefaultActions", "Node");
        AddComponent("res://addons/YarnSpinner/LineProviders/TextLineProvider.cs", "TextLineProvider", "Node");
        AddComponent("res://addons/YarnSpinner/LanguageToSourceAsset.cs", "LanguageToSourceAsset", "Resource");
        AddComponent("res://addons/YarnSpinner/Views/RPGView.cs", "RPGView", "Control");
    }
    public override void _ExitTree() {
        RemoveCustomType("DialogueRunner");
        RemoveCustomType("YarnProject");
        RemoveCustomType("DefaultActions");
        RemoveCustomType("TextLineProvider");
        RemoveCustomType("LanguageToSourceAsset");
        RemoveCustomType("RPGView");
    }
    private void AddComponent(string scriptName, string name, string parent) {
        var script = GD.Load<Script>(scriptName);
        var icon = GD.Load<Texture>("res://addons/YarnSpinner/icon.png");
        AddCustomType(name, parent, script, icon);
    }
}

#endif