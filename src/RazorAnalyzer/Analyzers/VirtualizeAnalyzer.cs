using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace RazorAnalyzer.Analyzers;

// https://github.com/dotnet/aspnetcore/issues/43102
public class VirtualizeAnalyzer : IRazorAnalyzer
{
    public IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document)
    {
        var root = document.GetSyntaxTree().Root;
        var virtualize = root.DescendantNodesAndSelf()
            .Where(n => n is MarkupTagHelperStartTagSyntax { Name.Content: "Virtualize" } or MarkupStartTagSyntax { Name.Content: "Virtualize" });

        foreach (var virtualization in virtualize)
        {
            if (HasSpacerElement(virtualization))
            {
                continue;
            }

            var containingBlock = virtualization.Parent.Ancestors().Where(n => n is MarkupElementSyntax).FirstOrDefault() as MarkupElementSyntax;
            if (containingBlock is null)
            {
                continue;
            }

            if (IsInvalidParent(containingBlock))
            {
                yield return Diagnostic.Create(
                    Descriptors.VirtualizeDescriptor,
                    virtualization.GetLocation(document.Source));
            }
        }

    }

    private bool HasSpacerElement(SyntaxNode virtualization)
        => virtualization switch
        {
            MarkupStartTagSyntax startTag => startTag.Attributes.Any(a => a is MarkupAttributeBlockSyntax b && b.Name.LiteralTokens.Any(l => l.Content == "SpacerElement")),
            MarkupTagHelperStartTagSyntax tagHelperStartTag => tagHelperStartTag.Attributes.Any(a => a is MarkupTagHelperAttributeSyntax b && b.Name.LiteralTokens.Any(l => l.Content == "SpacerElement")),
            _ => false
        };

    private static bool IsInvalidParent(MarkupElementSyntax parent)
    {
        return parent.StartTag.Name.Content.ToLower() is
            "tbody" or 
            "thead" or 
            "table" or 
            "img";
    }
}
