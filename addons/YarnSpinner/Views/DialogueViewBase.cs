using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class DialogueViewBase : RichTextLabel {
    internal System.Action onUserWantsLineContinuation;

    private List<IEnumerator> coroutines = new List<IEnumerator>();

    public virtual void DialogueStarted() {
        // Default implementation does nothing.
    }

    public virtual void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        // The default implementation does nothing, and immediately
        // calls onDialogueLineFinished.
        onDialogueLineFinished?.Invoke();
    }

    public virtual void OnLineStatusChanged(LocalizedLine dialogueLine) {
        // Default implementation is a no-op.
    }

    public virtual void DismissLine(Action onDismissalComplete) {
        // The default implementation does nothing, and immediately
        // calls onDialogueLineFinished.
        onDismissalComplete?.Invoke();
    }

    public virtual void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
        // The default implementation does nothing.
    }

    public virtual void NodeComplete(string nextNode, Action onComplete) {
        // The default implementation does nothing.            
    }

    public virtual void DialogueComplete() {
        // Default implementation does nothing.
    }

    public void ReadyForNextLine() {
        // Call the continuation callback, if we have it.
        onUserWantsLineContinuation?.Invoke();
    }

    public void StopAllCoroutines() {
        coroutines.Clear();
    }

    private IEnumerator StartCoroutine(IEnumerator enumerator) {
        if (enumerator.MoveNext()) {
            coroutines.Add(enumerator);
        }

        return null;
    }

    public override void _Process(float delta) {
        List<IEnumerator> dead = new List<IEnumerator>();
        foreach (var c in coroutines) {
            if (!c.MoveNext()) {
                dead.Add(c);
            }
        }

        foreach (var c in dead) {
            coroutines.Remove(c);
        }

        dead.Clear();
    }
}