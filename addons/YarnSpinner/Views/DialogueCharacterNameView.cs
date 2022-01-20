using Godot;
using System;

public class DialogueCharacterNameView : DialogueViewBase {
    /// <summary>
    /// Invoked when a line is received that contains a character name.
    /// The name is given as the parameter.
    /// </summary>
    /// <seealso cref="onNameNotPresent"/>
    [Signal]
    public delegate void onNameUpdate(string str);

    /// <summary>
    /// Invoked when the dialogue is started.
    /// </summary>
    [Signal]
    public delegate void onDialogueStarted();

    /// <summary>
    /// Invoked when a line is received that doesn't contain a
    /// character name.
    /// </summary>
    /// <remarks>
    /// Games can use this event to hide the name UI.
    /// </remarks>
    /// <seealso cref="onNameUpdate"/>
    [Signal]
    public delegate void onNameNotPresent();

    public override void DialogueStarted() {
        EmitSignal("onDialogueStarted");
    }

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        // Try and get the character name from the line
        string characterName = dialogueLine.CharacterName;

        // Did we find one?
        if (!string.IsNullOrEmpty(characterName)) {
            // Then notify the rest of the scene about it. This
            // generally involves updating a text view and making it
            // visible.
            EmitSignal("onNameUpdate", characterName);
        }
        else {
            // Otherwise, notify the scene about not finding it. This
            // generally involves making the name text view not
            // visible.
            EmitSignal("onNameNotPresent");
        }

        // Immediately mark this view as having finished its work
        onDialogueLineFinished();
    }
}
