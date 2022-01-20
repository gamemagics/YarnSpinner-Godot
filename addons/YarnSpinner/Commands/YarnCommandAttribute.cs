using Godot;
using System;

public class YarnCommandAttribute : YarnActionAttribute {
    [Obsolete("Use " + nameof(Name) + " instead.")]
    public string CommandString {
        get => Name;
        set => Name = value;
    }

    public string Injector { get; set; }

    public YarnCommandAttribute(string name = null) => Name = name;
}
