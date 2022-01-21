using Godot;
using System;

public class LanguageToSourceAsset : Resource {
    [Export]
    public string LanguageID { set; get; }
    [Export]
    public string StringFile { set; get; }
}
