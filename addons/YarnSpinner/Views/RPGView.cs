using Godot;
using System;

public class RPGView : DialogueViewBase {
    [Export] private string textPath;
    private RichTextLabel textLabel;

    public override void _Ready() {
        base._Ready();
        textLabel = GetNode<RichTextLabel>(textPath);
        if (textLabel == null) {
            GD.PrintErr("Can't find " + textPath);
        }
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        base.RunLine(dialogueLine, onDialogueLineFinished);
        if (textLabel != null) {
            textLabel.Text = dialogueLine.TextWithoutCharacterName.Text;
        }
    }
}
