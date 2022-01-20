using Godot;
using System;

public class YarnActionAttribute : Attribute {
    public string Name { get; set; }

    public YarnActionAttribute(string name = null) => Name = name;
}
