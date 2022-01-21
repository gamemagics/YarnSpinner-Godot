using Godot;
using System;

public class RPGView : DialogueViewBase {
    [Export] private string textPath;
    private RichTextLabel textLabel;

    [Export] private string speakerPath;
    private RichTextLabel speakerLabel;

    [Export] private string[] optionsPath;
    private Button[] optionsButton;

    Action<int> pressedAction;

    public override void _Ready() {
        base._Ready();
        textLabel = GetNode<RichTextLabel>(textPath);
        speakerLabel = GetNode<RichTextLabel>(speakerPath);
        optionsButton = new Button[optionsPath.Length];
        for (int i = 0; i < optionsPath.Length; ++i) {
            optionsButton[i] = GetNode<Button>(optionsPath[i]);
        }
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        if (textLabel != null) {
            textLabel.Text = dialogueLine.TextWithoutCharacterName.Text;
        }
        if (speakerLabel != null) {
            speakerLabel.Text = dialogueLine.CharacterName;
        }

        base.RunLine(dialogueLine, onDialogueLineFinished);
    }

    public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
        pressedAction = onOptionSelected;
        for (int i = 0; i < dialogueOptions.Length; ++i) {
            optionsButton[i].Text = dialogueOptions[i].Line.Text.Text;
            optionsButton[i].Visible = true;
            optionsButton[i].Connect("pressed", this, "SelectOption" + i.ToString());
        }
    }

    private void SelectOption0() {
        for (int i = 0; i < optionsButton.Length; ++i) {
            optionsButton[i].Visible = false;
        }

        pressedAction.Invoke(0);
    }

    private void SelectOption1() {
        for (int i = 0; i < optionsButton.Length; ++i) {
            optionsButton[i].Visible = false;
        }

        pressedAction.Invoke(1);
    }
}
