using System.Linq;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace RazorAnalyzer.Analyzers;

internal static class SyntaxTreeExtensions
{
    public static RazorDirectiveSyntax? GetCodeBlock(this RazorSyntaxTree syntaxTree)
    {
        return syntaxTree.Root.DescendantNodesAndSelf().FirstOrDefault(IsRazorCodeDirective) as RazorDirectiveSyntax;

        static bool IsRazorCodeDirective(SyntaxNode node)
        {
            return node is RazorDirectiveSyntax directiveSyntax
                && directiveSyntax.DirectiveDescriptor.Kind == DirectiveKind.CodeBlock;
        }
    }
}
