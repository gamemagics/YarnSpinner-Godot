using Godot;
using System;

public abstract class VariableStorageBehaviour : Node, Yarn.IVariableStorage {
    public abstract bool TryGetValue<T>(string variableName, out T result);

    public abstract void SetValue(string variableName, string stringValue);

    public abstract void SetValue(string variableName, float floatValue);

    public abstract void SetValue(string variableName, bool boolValue);

    public abstract void Clear();

    public abstract bool Contains(string variableName);
}
