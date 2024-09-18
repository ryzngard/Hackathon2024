using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace RazorAnalyzer.Analyzers;

internal static class RazorSyntaxExtensions
{
    public static string GetIdentifier(this RazorMetaCodeSyntax metaCode)
    {
        return metaCode
            .MetaCode
            .Where(t => !t.IsTrivia)
            .FirstOrDefault()
            ?.Content
        ?? "";
    }

    public static bool IsImplements(this RazorMetaCodeSyntax metaCodeSyntax)
    {
        return metaCodeSyntax.GetIdentifier() == "implements";
    }
}
