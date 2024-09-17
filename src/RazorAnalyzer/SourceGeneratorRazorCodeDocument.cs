using Microsoft.AspNetCore.Razor.Language;

namespace RazorAnalyzer;

internal class SourceGeneratorRazorCodeDocument(RazorCodeDocument codeDocument)
{
    public RazorCodeDocument CodeDocument => codeDocument;
}