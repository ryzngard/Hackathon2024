﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RazorAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RazorAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RazorAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCodeBlockAction(CodeBlockAction);
            context.RegisterCompilationAction(AnalyzeCompilation);
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private void CodeBlockAction(CodeBlockAnalysisContext context)
        {
            throw new NotImplementedException();
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            if (!RazorUtilities.IsGeneratedRazorFile(context))
            {
                return;
            }
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
        }
    }
}
