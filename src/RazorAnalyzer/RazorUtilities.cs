using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RazorAnalyzer
{
    internal static class RazorUtilities
    {
        internal static bool IsGeneratedRazorFile(SemanticModelAnalysisContext context)
        {
            var tree = context.SemanticModel.SyntaxTree;
            return tree.FilePath.Contains(".razor.g.cs");
        }
    }
}
