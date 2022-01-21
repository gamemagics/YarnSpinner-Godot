using Godot;
using System;

public class LanguageToSourceAsset : Resource {
    private string languageID;

    [Export]
    public string LanguageID { 
        set { languageID = value; ResourceName = languageID; }
        get { return languageID; }
    }
    public string StringFile { set; get; }
}
