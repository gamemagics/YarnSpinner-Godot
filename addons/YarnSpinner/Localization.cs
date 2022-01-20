using Godot;
using System;
using System.Collections.Generic;

public class Localization : Node {
    class StringDictionary : Dictionary<string, string> { }
    class AssetDictionary : Dictionary<string, Node> { }

    private StringDictionary _stringTable = new StringDictionary();
    private AssetDictionary _assetTable = new AssetDictionary();
    private StringDictionary _runtimeStringTable = new StringDictionary();

    private bool _containLocalizedAsset;
    private bool _usesAddressableAssets;

    private string _localeCode;

    [Export] public string LocaleCode {
        get => _localeCode;
        set => _localeCode = value;
    }

    [Export] public bool ContainLocalizedAsset {
        get => _containLocalizedAsset;
        set => _containLocalizedAsset = value;
    }

    [Export] public bool UsesAddressableAssets {
        get => _usesAddressableAssets;
        set => _usesAddressableAssets = value;
    }

    internal static string GetAddressForLine(string lineID, string language) {
        return $"line_{language}_{lineID.Replace("line:", "")}";
    }

    #region Localized Strings
    public string GetLocalizedString(string key) {
        string result;
        if (_runtimeStringTable.TryGetValue(key, out result)) {
            return result;
        }

        if (_stringTable.TryGetValue(key, out result)) {
            return result;
        }

        return null;
    }

    public bool ContainsLocalizedString(string key) => _runtimeStringTable.ContainsKey(key) || _stringTable.ContainsKey(key);

    public void AddLocalizedString(string key, string value) {
        _runtimeStringTable.Add(key, value);
    }

    public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings) {
        foreach (var entry in strings) {
            AddLocalizedString(entry.Key, entry.Value);
        }
    }

    public void AddLocalizedStrings(IEnumerable<StringTableEntry> stringTableEntries) {
        foreach (var entry in stringTableEntries) {
            AddLocalizedString(entry.ID, entry.Text);
        }
    }

    #endregion

    #region Localised Objects

    public T GetLocalizedObject<T>(string key) where T : Node {
        if (_usesAddressableAssets) {
            GD.Print($"Localization {key} uses addressable assets. Use the Addressable Assets API to load the asset.");
        }

        _assetTable.TryGetValue(key, out var result);

        if (result is T resultAsTargetObject) {
            return resultAsTargetObject;
        }

        return null;
    }

    public void SetLocalizedObject<T>(string key, T value) where T : Node => _assetTable.Add(key, value);

    public bool ContainsLocalizedObject<T>(string key) where T : Node => _assetTable.ContainsKey(key) && _assetTable[key] is T;

    public void AddLocalizedObject<T>(string key, T value) where T : Node => _assetTable.Add(key, value);

    public void AddLocalizedObjects<T>(IEnumerable<KeyValuePair<string, T>> objects) where T : Node {
        foreach (var entry in objects) {
            _assetTable.Add(entry.Key, entry.Value);
        }
    }
    #endregion

    public virtual void Clear() {
        _stringTable.Clear();
        _assetTable.Clear();
        _runtimeStringTable.Clear();
    }
    public IEnumerable<string> GetLineIDs() {
        var allKeys = new List<string>();

        var runtimeKeys = _runtimeStringTable.Keys;
        var compileTimeKeys = _stringTable.Keys;

        allKeys.AddRange(runtimeKeys);
        allKeys.AddRange(compileTimeKeys);

        return allKeys;
    }
}
