using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class InterruptionFlag {
    public bool Interrupted { get; private set; } = false;
    public void Set() => Interrupted = true;
    public void Clear() => Interrupted = false;
}

public class LineView : DialogueViewBase {
    internal enum ContinueActionType {
        None,
        KeyCode,
        InputSystemAction,
        InputSystemActionFromAsset,
    }

    internal RichTextLabel lineText = null;

    internal bool showCharacterNameInLineView = true;

    internal RichTextLabel characterNameText = null;

    internal bool useTypewriterEffect = false;

    [Signal]
    internal delegate void onCharacterTyped();

    internal float typewriterEffectSpeed = 0f;

    internal Control continueButton = null;

    internal ContinueActionType continueActionType;

    InterruptionFlag interruptionFlag = new InterruptionFlag();

    LocalizedLine currentLine = null;

    public override void DismissLine(Action onDismissalComplete) {
        currentLine = null;
        onDismissalComplete();
    }

    public override void OnLineStatusChanged(LocalizedLine dialogueLine) {
        switch (dialogueLine.Status) {
            case LineStatus.Presenting:
                break;
            case LineStatus.Interrupted:
                // We have been interrupted. Set our interruption flag,
                // so that any animations get skipped.
                interruptionFlag.Set();
                break;
            case LineStatus.FinishedPresenting:
                // The line has finished being delivered by all views.
                // Display the Continue button.
                if (continueButton != null) {
                    continueButton.Visible = true;
                }
                break;
            case LineStatus.Dismissed:
                break;
        }
    }

    private void OnCharacterTyped() {
        EmitSignal("onCharacterTyped");
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        currentLine = dialogueLine;

        lineText.Visible = true;

        if (continueButton != null) {
            continueButton.Visible = true;
        }

        interruptionFlag.Clear();

        if (characterNameText == null) {
            if (showCharacterNameInLineView) {
                lineText.Text = dialogueLine.Text.Text;
            }
            else {
                lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
            }
        }
        else {
            characterNameText.Text = dialogueLine.CharacterName;
            lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
        }

        onDialogueLineFinished();
    }

    public void OnContinueClicked() {
        if (currentLine == null) {
            // We're not actually displaying a line. No-op.
            return;
        }
        ReadyForNextLine();
    }
}
