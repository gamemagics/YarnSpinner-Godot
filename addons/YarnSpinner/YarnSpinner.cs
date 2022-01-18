#if TOOLS

using Godot;
using System;

[Tool]
public class YarnSpinner : EditorPlugin {
    public override void _EnterTree() {
        var script = GD.Load<Script>("res://addons/YarnSpinner/DialogueRunner.cs");
        var icon = GD.Load<Texture>("res://addons/YarnSpinner/icon.png");
        AddCustomType("DialogueRunner", "Control", script, icon);
    }

    public override void _ExitTree() {
        RemoveCustomType("DialogueRunner");
    }
}

#endif