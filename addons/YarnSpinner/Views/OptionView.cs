using Godot;
using System;

public class OptionView : Button {
    [Export] bool showCharacterName = false;

    public Action<DialogueOption> OnOptionSelected;

    DialogueOption _option;

    bool hasSubmittedOptionSelection = false;

    public DialogueOption Option {
        get => _option;

        set {
            _option = value;

            hasSubmittedOptionSelection = false;

            // When we're given an Option, use its text and update our
            // interactibility.
            if (showCharacterName) {
                Text = value.Line.Text.Text;
            }
            else {
                Text = value.Line.TextWithoutCharacterName.Text;
            }
            Disabled = !value.IsAvailable;
        }
    }

    // If we receive a submit or click event, invoke our "we just selected
    // this option" handler.
    public void OnSubmit(object eventData) {
        InvokeOptionSelected();
    }

    public void InvokeOptionSelected() {
        // We only want to invoke this once, because it's an error to
        // submit an option when the Dialogue Runner isn't expecting it. To
        // prevent this, we'll only invoke this if the flag hasn't been cleared already.
        if (hasSubmittedOptionSelection == false) {
            OnOptionSelected.Invoke(Option);
            hasSubmittedOptionSelection = true;
        }
    }

    public void OnPointerClick(object eventData) {
        InvokeOptionSelected();
    }

    // If we mouse-over, we're telling the UI system that this element is
    // the currently 'selected' (i.e. focused) element. 
    public override void _Pressed() {
        base._Pressed();
    }
}
