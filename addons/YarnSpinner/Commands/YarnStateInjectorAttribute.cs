using Godot;
using System;

public class YarnStateInjectorAttribute : Attribute {
    public string Injector { get; set; }

    public YarnStateInjectorAttribute(string injector) => Injector = injector;
}
