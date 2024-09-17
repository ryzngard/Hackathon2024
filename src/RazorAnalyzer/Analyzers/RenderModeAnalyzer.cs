using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace RazorAnalyzer.Analyzers;

public class RenderModeAnalyzer : IRazorAnalyzer
{
    public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
        "T1001",
        "Invalid render mode",
        "I'm not sure yet",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document)
    {
        var codeGenerationOptions = document.GetCodeGenerationOptions();
        var languageVersion = document.GetParserOptions().Version;

        if (languageVersion < RazorLanguageVersion.Version_7_0)
        {
            yield break;
        }

        // check for explicit render mode

    }
}
