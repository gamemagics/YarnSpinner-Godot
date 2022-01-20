using Godot;
using System;

public class YarnFunctionAttribute : YarnActionAttribute {
    [Obsolete("Use " + nameof(Name) + " instead.")]
    public string FunctionName {
        get => Name;
        set => Name = value;
    }

    public YarnFunctionAttribute(string name = null) => Name = name;
}
