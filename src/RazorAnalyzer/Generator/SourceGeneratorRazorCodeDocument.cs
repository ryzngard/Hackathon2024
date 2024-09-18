using Microsoft.AspNetCore.Razor.Language;

namespace RazorAnalyzer.Generator;

internal class SourceGeneratorRazorCodeDocument(RazorCodeDocument codeDocument)
{
    public RazorCodeDocument CodeDocument => codeDocument;
}