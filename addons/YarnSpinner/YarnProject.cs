using Godot;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Linq;

public class YarnProject : Node {
    public Yarn.Program program;
    public Localization baseLocalization;
    public List<string> searchAssembliesForActions = new List<string>();
    public List<Localization> localizations = new List<Localization>();

    [Export] private string[] sourceScripts;
    [Export] private string declarationPath = null;
    [Export] private LanguageToSourceAsset[] languages;

    public override void _Ready() {
        string content = "";
        foreach (var s in sourceScripts) {
            var fp = new File();
            fp.Open(s, File.ModeFlags.Read);
            content += fp.GetAsText();
        }

        var job = CompilationJob.CreateFromString(sourceScripts[0], content);
        if (declarationPath != null) {
            var dnode = GetNode<IDeclaration>(declarationPath);
            var ds = dnode.GetDeclarations();
            if (ds.Count > 0) {
                job.VariableDeclarations = dnode.GetDeclarations();
            }
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

        program = compilationResult.Program;
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
        return program;
    }
}
