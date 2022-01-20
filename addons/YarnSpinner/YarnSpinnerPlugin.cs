#if TOOLS

using Godot;
using System;

[Tool]
public class YarnSpinnerPlugin : EditorPlugin {
    public override void _EnterTree() {
        AddComponent("res://addons/YarnSpinner/DialogueRunner.cs", "DialogueRunner", "Control");
        AddComponent("res://addons/YarnSpinner/Localization.cs", "Localization", "Node");
        AddComponent("res://addons/YarnSpinner/YarnProject.cs", "YarnProject", "Node");
        AddComponent("res://addons/YarnSpinner/Commands/DefaultActions.cs", "DefaultActions", "Node");
        AddComponent("res://addons/YarnSpinner/LineProviders/TextLineProvider.cs", "TextLineProvider", "Node");
        AddComponent("res://addons/YarnSpinner/Views/CharacterColorView.cs", "CharacterColorView", "Control");
        AddComponent("res://addons/YarnSpinner/Views/DialogueCharacterNameView.cs", "DialogueCharacterNameView", "Control");
        AddComponent("res://addons/YarnSpinner/Views/LineView.cs", "LineView", "Control");
        AddComponent("res://addons/YarnSpinner/Views/OptionsListView.cs", "OptionsListView", "Control");
    }
    public override void _ExitTree() {
        RemoveCustomType("DialogueRunner");
        RemoveCustomType("Localization");
        RemoveCustomType("YarnProject");
        RemoveCustomType("DefaultActions");
        RemoveCustomType("TextLineProvider");
        RemoveCustomType("CharacterColorView");
        RemoveCustomType("DialogueCharacterNameView");
        RemoveCustomType("LineView");
        RemoveCustomType("OptionsListView");
    }
    private void AddComponent(string scriptName, string name, string parent) {
        var script = GD.Load<Script>(scriptName);
        var icon = GD.Load<Texture>("res://addons/YarnSpinner/icon.png");
        AddCustomType(name, parent, script, icon);
    }
}

#endif