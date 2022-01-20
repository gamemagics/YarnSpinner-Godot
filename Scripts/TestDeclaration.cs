using Godot;
using System;
using System.Collections.Generic;
using Yarn.Compiler;

public class TestDeclaration : Node, IDeclaration {
    List<Declaration> declarations = new List<Declaration>();

    public List<Declaration> GetDeclarations() {
        return declarations;
    }

    public override void _Ready() {
        
    }
}
