using Godot;
using System;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Linq;
using System.IO;

public class YarnProject : Node {
    public byte[] compiledYarnProgram;
    public Localization baseLocalization;
    public List<string> searchAssembliesForActions = new List<string>();
    public List<Localization> localizations = new List<Localization>();

    [Export] private string[] sourceScripts;
    [Export] private string declarationPath = null;

    public override void _Ready() {
        var job = CompilationJob.CreateFromFiles(sourceScripts);
        if (declarationPath != null) {
            var dnode = GetNode<IDeclaration>(declarationPath);
            job.VariableDeclarations = dnode.GetDeclarations();
        }

        CompilationResult compilationResult;
        compilationResult = Compiler.Compile(job);
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
