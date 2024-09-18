using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;

using SyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace RazorAnalyzer.Analyzers;

public class LowercaseHTMLAnalyzer : IRazorAnalyzer
{
    public IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document)
    {
        var syntaxTree = document.GetSyntaxTree();

        var root = syntaxTree.Root;
        var tagsToLookFor = root.DescendantNodesAndSelf()
            .Where(n => n is MarkupStartTagSyntax || n is MarkupEndTagSyntax)
            .Cast<MarkupSyntaxNode>();

        foreach (var tag in tagsToLookFor)
        {
            var hasUppercase = tag.ChildNodes().Any(n => n is SyntaxToken token && token.Content.Any(char.IsUpper));
            if (hasUppercase)
            {
                yield return Diagnostic.Create(
                    Descriptors.LowercaseHtmlDescriptor,
                    tag.GetLocation(document.Source)
                    );
            }
        }
    }
}
