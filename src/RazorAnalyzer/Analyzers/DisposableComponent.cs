using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RazorAnalyzer.Analyzers;

// https://github.com/dotnet/aspnetcore/issues/51714
public class DisposableComponent : IRazorAnalyzer
{
    public IEnumerable<Diagnostic> GetDiagnostics(RazorCodeDocument document)
    {
        var codeBlock = document.GetSyntaxTree().GetCodeBlock();

        // If no code block assume code behind and the experience is handled there
        if (codeBlock is null)
        {
            return [];
        }

        var csharpDocument = document.GetCSharpDocument();
        var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode);
        var root = syntaxTree.GetRoot();
        var methodDeclarations = root.DescendantNodes(descendIntoTrivia: false).Where(n => n.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration));

        var hasPublicDispose = false;
        foreach (var node in methodDeclarations)
        {
            var methodDeclaration = node as MethodDeclarationSyntax;
            if (methodDeclaration is null)
            {
                continue;
            }

            if (methodDeclaration.ExplicitInterfaceSpecifier is not null)
            {
                continue;
            }

            if (methodDeclaration.Identifier.Text != "Dispose")
            {
                continue;
            }

            if (!methodDeclaration.Modifiers.Any(m => m.IsKeyword() && m.Text == "public"))
            {
                continue;
            }

            hasPublicDispose = true;
            break;
        }

        if (!hasPublicDispose)
        {
            return [];
        }

        // Make sure there's an @implements IDisposable
        var razorSyntax = document.GetSyntaxTree();
        var razorDirectiveBodies = razorSyntax.Root.DescendantNodes().Where(n => n is RazorDirectiveBodySyntax).Cast<RazorDirectiveBodySyntax>();

        foreach (var razorDirectiveBody in razorDirectiveBodies)
        {
            if (razorDirectiveBody.Keyword is RazorMetaCodeSyntax metaCodeSyntax && metaCodeSyntax.IsImplements())
            {
                var csharpLiteral = razorDirectiveBody
                    .CSharpCode
                    .DescendantNodes(n => n is CSharpStatementLiteralSyntax)
                    .Cast<CSharpStatementLiteralSyntax>()
                    .FirstOrDefault(n => n.LiteralTokens.Any(t => t.Kind != Microsoft.AspNetCore.Razor.Language.SyntaxKind.Whitespace));

                if (csharpLiteral is not null && csharpLiteral.LiteralTokens.Any(t => t.Content.Contains("IDisposable")))
                {
                    return [];
                }
            }
        }

        return [
            Diagnostic.Create(
                Descriptors.DisposableDescriptor,
                codeBlock.GetLocation(document.Source))
        ];
    }
}
