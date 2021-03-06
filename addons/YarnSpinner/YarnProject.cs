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
    private LanguageToSourceAsset[] languages = null;
    private Declaration[] declarations = null;

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
    public string DefaultLanguage {
        set {
            defaultLanguage = value;
            Compile();
        }
        get { return defaultLanguage; }
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
    public Declaration[] Declarations {
        set {
            declarations = value;
            Compile();
        }
        get { return declarations; }
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
            fp.Close();
        }

        var job = CompilationJob.CreateFromString(sourceScripts[0], content);
        if (declarations != null && declarations.Length > 0) {
            var finalDec = new List<Yarn.Compiler.Declaration>();
            Yarn.Compiler.Declaration v = new Yarn.Compiler.Declaration();
            foreach (var dec in declarations) {
                switch (dec.Type) {
                    case DeclaerationType.STRING:
                        v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.String, dec.DefaultValue);
                        break;
                    case DeclaerationType.BOOLEAN:
                        v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.Boolean, bool.Parse(dec.DefaultValue));
                        break;
                    case DeclaerationType.NUMBER:
                        v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.Number, float.Parse(dec.DefaultValue));
                        break;
                }

                finalDec.Add(v);
            }

            job.VariableDeclarations = finalDec;
        }

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
#if TOOLS
                        var temp = compilationResult.StringTable.Select(x => new StringTableEntry {
                            ID = x.Key,
                            Language = defaultLanguage,
                            Text = x.Value.text,
                            File = x.Value.fileName,
                            Node = x.Value.nodeName,
                            LineNumber = x.Value.lineNumber.ToString(),
                            Lock = GetHashString(x.Value.text, 8),
                        });

                        var fp = new Godot.File();
                        fp.Open(sourceScripts[0], Godot.File.ModeFlags.Read);
                        string target = fp.GetPathAbsolute();
                        fp.Close();
                        target = target.ReplaceN(".yarn.tres", "(" + lang.LanguageID + ").csv.tres");

                        GD.Print("Generate " + target);
                        using (var writer = new StreamWriter(target)) {
                            writer.WriteLine("language,id,text,file,node,lineNumber,lock,comment");
                            foreach (var item in temp) {
                                writer.WriteLine($"{lang.LanguageID},{item.ID},{item.Text},{item.File},{item.Node},{item.LineNumber},{item.Lock},");
                            }

                            lang.StringFile = target;
                        }   
#else
                    GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                    continue;
#endif
                    }

                    var reader = new Godot.File();
                    reader.Open(lang.StringFile, Godot.File.ModeFlags.Read);
                    string source = reader.GetAsText();
                    stringTable = StringTableEntry.ParseFromCSV(source);
                }
                catch (System.ArgumentException e) {
                    GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because an error was encountered during text parsing: {e}");
                    continue;
                }
            }

            var newLocalization = new Localization();
            newLocalization.LocaleCode = lang.LanguageID;
            newLocalization.AddLocalizedStrings(stringTable);
            localizations.Add(newLocalization);

            if (lang.LanguageID == defaultLanguage) {
                // If this is our default language, set it as such
                baseLocalization = newLocalization;
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
