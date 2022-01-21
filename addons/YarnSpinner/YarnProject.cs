using Godot;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Linq;
using System.IO;

[Tool]
public class YarnProject : Resource {
    public byte[] compiledYarnProgram;
    public Localization baseLocalization;
    public List<string> searchAssembliesForActions = new List<string>();
    public List<Localization> localizations = new List<Localization>();

    private string projectName = null;
    private string[] sourceScripts;
    private string declarationPath = null;
    private LanguageToSourceAsset[] languages;

    [Export]
    public string ProjectName {
        set {
            string temp = projectName;
            projectName = value;
#if TOOLS
            Compile();
            Save(temp);
#endif
        }
        get { return projectName; }
    }

    [Export]
    public string[] SourceScripts {
        set {
            sourceScripts = value;
#if TOOLS
            Compile();
            Save();
#endif
        }
        get { return sourceScripts; }
    }

#if TOOLS
    private void Compile() {
        if (sourceScripts == null || sourceScripts.Length == 0) {
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
    }

    private void Save(string prevName = null) {
        GD.Print("Save to " + projectName);

        ResourceSaver.Save("res://Dialogues/" + projectName, this);
        ResourcePath = "res://Dialogues/" + projectName;

        if (prevName != null) {
            GD.Print("Remove " + prevName);
            var fp = new Godot.File();
            var err = fp.Open("res://Dialogues/" + prevName, Godot.File.ModeFlags.Read);
            if (err == Error.Ok) {
                string path = fp.GetPathAbsolute();
                fp.Close();
                System.IO.File.Delete(path);
            }
        }
    }
#endif

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
