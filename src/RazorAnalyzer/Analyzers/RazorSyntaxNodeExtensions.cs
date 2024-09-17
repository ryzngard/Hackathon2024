using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace RazorAnalyzer.Analyzers;

internal static class RazorSyntaxNodeExtensions
{
    public static Location GetLocation(this SyntaxNode syntaxNode, RazorSourceDocument document)
        => Location.Create(document.FilePath!, syntaxNode.Span, document.Text.Lines.GetLinePositionSpan(syntaxNode.Span));
}
