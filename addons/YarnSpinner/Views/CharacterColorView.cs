using Godot;
using System;
using System.Collections.Generic;

public class CharacterColorView : DialogueViewBase {
    [Serializable]
    public class CharacterColorData {
        public string characterName;
        public Color displayColor = new Color(1, 1, 1);
    }

    Color defaultColor = new Color(1, 1, 1);

    CharacterColorData[] colorData;

    List<RichTextLabel> lineTexts = new List<RichTextLabel>();

    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        var characterName = dialogueLine.CharacterName;

        Color colorToUse = defaultColor;

        if (string.IsNullOrEmpty(characterName) == false) {
            foreach (var color in colorData) {
                if (color.characterName.Equals(characterName, StringComparison.InvariantCultureIgnoreCase)) {
                    colorToUse = color.displayColor;
                    break;
                }
            }
        }

        foreach (var text in lineTexts) {
            text.PushColor(colorToUse); // TODO:
        }

        onDialogueLineFinished();
    }
}
