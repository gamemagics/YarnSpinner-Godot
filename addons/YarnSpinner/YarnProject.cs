using Godot;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

[Tool]
public class YarnProject : Resource {
    public byte[] compiledYarnProgram;
    public Localization baseLocalization;
    public List<string> searchAssembliesForActions = new List<string>();
    public List<Localization> localizations = new List<Localization>();

    private string projectName = null;
    private string[] sourceScripts = null;
    private string declarationPath = null;
    private LanguageToSourceAsset[] languages = null;

    private string defaultLanguage = null;

    public YarnProject() {

    }

    [Export]
    public string ProjectName {
        set {
            projectName = value;
            Compile();
        }
        get { return projectName; }
    }

    [Export]
    public string[] SourceScripts {
        set {
            sourceScripts = value;
            Compile();
        }
        get { return sourceScripts; }
    }

    [Export]
    public LanguageToSourceAsset[] Languages {
        set {
            languages = value;
            Compile();
        }
        get { return languages; }
    }

    [Export]
    public string DefaultLanguage {
        set {
            defaultLanguage = value;
            Compile();
        }
        get { return defaultLanguage; }
    }

    private static byte[] GetHash(string inputString) {
        using (HashAlgorithm algorithm = SHA256.Create()) {
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
    }

    internal static string GetHashString(string inputString, int limitCharacters = -1) {
        var sb = new StringBuilder();
        foreach (byte b in GetHash(inputString)) {
            sb.Append(b.ToString("x2"));
        }

        if (limitCharacters == -1) {
            // Return the entire string
            return sb.ToString();
        }
        else {
            // Return a substring (or the entire string, if
            // limitCharacters is longer than the string)
            return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
        }
    }

    private void Compile() {
        if (sourceScripts == null || sourceScripts.Length == 0 || languages == null || defaultLanguage == null) {
            return;
        }

        string content = "";
        foreach (var s in sourceScripts) {
            var fp = new Godot.File();
            fp.Open(s, Godot.File.ModeFlags.Read);
            content += fp.GetAsText();
        }

        var job = CompilationJob.CreateFromString(sourceScripts[0], content);
        //if (declarationPath != null) {
        //    var dnode = GetNode<IDeclaration>(declarationPath);
        //    var ds = dnode.GetDeclarations();
        //    if (ds.Count > 0) {
        //        job.VariableDeclarations = dnode.GetDeclarations();
        //    }
        //}

        CompilationResult compilationResult = Compiler.Compile(job);
        var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

        if (errors.Count() > 0) {
            foreach (var error in errors) {
                GD.PrintErr(error);
            }
            return;
        }

        if (compilationResult.Program == null) {
            GD.PrintErr("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
            return;
        }

        using (var memoryStream = new MemoryStream())
        using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream)) {
            // Serialize the compiled program to memory
            compilationResult.Program.WriteTo(outputStream);
            outputStream.Flush();

            compiledYarnProgram = memoryStream.ToArray();
        }

        foreach (var lang in languages) {
            if (lang.LanguageID.Empty()) {
                GD.PrintErr($"Not creating a localization for {projectName} because the language ID wasn't provided. Add the language ID to the localization in the Yarn Project's inspector.");
                continue;
            }

            IEnumerable<StringTableEntry> stringTable;

            // Where do we get our strings from? If it's the default
            // language, we'll pull it from the scripts. If it's from
            // any other source, we'll pull it from the CSVs.
            if (lang.LanguageID == defaultLanguage) {
                // We'll use the program-supplied string table.
                stringTable = compilationResult.StringTable.Select(x => new StringTableEntry {
                    ID = x.Key,
                    Language = defaultLanguage,
                    Text = x.Value.text,
                    File = x.Value.fileName,
                    Node = x.Value.nodeName,
                    LineNumber = x.Value.lineNumber.ToString(),
                    Lock = GetHashString(x.Value.text, 8),
                });

                // We don't need to add a default localization.
                //shouldAddDefaultLocalization = false;
            }
            else {
                try {
                    if (lang.StringFile == null) {
                        // We can't create this localization because we
                        // don't have any data for it.

                        // TODO: Generate One

                        GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                        continue;
                    }

                    stringTable = StringTableEntry.ParseFromCSV(lang.StringFile);
                }
                catch (System.ArgumentException e) {
                    GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because an error was encountered during text parsing: {e}");
                    continue;
                }
            }
        }
    }

    public Localization GetLocalization(string localeCode) {

        // If localeCode is null, we use the base localization.
        if (localeCode == null) {
            return baseLocalization;
        }

        foreach (var loc in localizations) {
            if (loc.LocaleCode == localeCode) {
                return loc;
            }
        }

        // We didn't find a localization. Fall back to the Base
        // localization.
        return baseLocalization;
    }

    /// <summary>
    /// Deserializes a compiled Yarn program from the stored bytes in
    /// this object.
    /// </summary>
    public Yarn.Program GetProgram() {
        return Yarn.Program.Parser.ParseFrom(compiledYarnProgram);
    }
}
