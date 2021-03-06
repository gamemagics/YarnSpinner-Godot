using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class TextLineProvider : LineProviderBehaviour {
    public override LocalizedLine GetLocalizedLine(Yarn.Line line) {
        var text = YarnProject.GetLocalization("Chinese (Simplified)").GetLocalizedString(line.ID);
        return new LocalizedLine() {
            TextID = line.ID,
            RawText = text,
            Substitutions = line.Substitutions
        };
    }

    public override void PrepareForLines(IEnumerable<string> lineIDs) {
        // No-op; text lines are always available
    }

    public override bool LinesAvailable => true;
}
