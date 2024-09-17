using System.Collections.Generic;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace RazorAnalyzer;

public interface IRazorAnalyzer
{
    IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document);
}
