using Godot;
using System;
using System.Collections.Generic;
using Yarn.Compiler;

public interface IDeclaration {
    List<Declaration> GetDeclarations();
}