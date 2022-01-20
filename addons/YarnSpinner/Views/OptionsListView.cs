using Godot;
using System;
using System.Collections.Generic;

public class OptionsListView : DialogueViewBase {
    [Export] string optionViewPrefab;

    RichTextLabel lastLineText;

    [Export] float fadeTime = 0.1f;

    [Export] bool showUnavailableOptions = false;

    // A cached pool of OptionView objects so that we can reuse them
    List<OptionView> optionViews = new List<OptionView>();

    // The method we should call when an option has been selected.
    Action<int> OnOptionSelected;

    // The line we saw most recently.
    LocalizedLine lastSeenLine;

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        // Don't do anything with this line except note it and
        // immediately indicate that we're finished with it. RunOptions
        // will use it to display the text of the previous line.
        lastSeenLine = dialogueLine;
        onDialogueLineFinished();
    }

    public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
        // Hide all existing option views
        foreach (var optionView in optionViews) {
            optionView.Disabled = true;
        }

        // If we don't already have enough option views, create more
        while (dialogueOptions.Length > optionViews.Count) {
            var optionView = CreateNewOptionView();
            optionView.Disabled = true;
        }

        // Set up all of the option views
        int optionViewsCreated = 0;

        for (int i = 0; i < dialogueOptions.Length; i++) {
            var optionView = optionViews[i];
            var option = dialogueOptions[i];

            if (option.IsAvailable == false && showUnavailableOptions == false) {
                // Don't show this option.
                continue;
            }

            optionView.Disabled = false;

            optionView.Option = option;

            optionViewsCreated += 1;
        }

        // Update the last line, if one is configured
        if (lastLineText != null) {
            if (lastSeenLine != null) {
                lastLineText.Visible = true;
                lastLineText.Text = lastSeenLine.Text.Text;
            }
            else {
                lastLineText.Visible = false;
            }
        }

        // Note the delegate to call when an option is selected
        OnOptionSelected = onOptionSelected;

        /// <summary>
        /// Creates and configures a new <see cref="OptionView"/>, and adds
        /// it to <see cref="optionViews"/>.
        /// </summary>
        OptionView CreateNewOptionView() {
            var optionView = GD.Load<OptionView>(optionViewPrefab);
            AddChild(optionView);

            optionView.OnOptionSelected = OptionViewWasSelected;
            optionViews.Add(optionView);

            return optionView;
        }

        /// <summary>
        /// Called by <see cref="OptionView"/> objects.
        /// </summary>
        void OptionViewWasSelected(DialogueOption option) {
        }
    }
}
