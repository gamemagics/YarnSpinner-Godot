using Godot;
using System;

public enum DeclaerationType {
    STRING, BOOLEAN, NUMBER
}

public class Declaration : Resource {
    [Export] public string Name { get; set; }
    [Export] public DeclaerationType Type { get; set; }
    [Export] public string DefaultValue { get; set; }
}
