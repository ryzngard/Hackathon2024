using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace RazorAnalyzer.Analyzers;

public class CodeBlockStylingAnalyzer : IRazorAnalyzer
{
    public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
        "T1002",
        "Code block styling",
        "Do not use a code block, instead use a code behind file",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document)
    {
        var syntaxTree = document.GetSyntaxTree();
        var root = syntaxTree.Root;

        if (root.DescendantNodesAndSelf().FirstOrDefault(IsRazorCodeDirective) is RazorDirectiveSyntax directiveSyntax)
        {
            yield return Diagnostic.Create(
                Descriptor,
                directiveSyntax.GetLocation(document.Source)
            );

        }
    }

    private static bool IsRazorCodeDirective(SyntaxNode node)
    {
        return node is RazorDirectiveSyntax directiveSyntax
            && directiveSyntax.DirectiveDescriptor.Kind == DirectiveKind.CodeBlock;
    }
}
