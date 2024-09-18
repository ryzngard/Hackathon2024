using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

using RazorAnalyzer.Generator;

namespace RazorAnalyzer.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });

                ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.AspNetCore.Components", "8.0.8")]);
                TestState.Sources.Add("public class Program { public static void Main(string args[]) { } }");
                TestState.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
            }

            protected override IEnumerable<Type> GetSourceGenerators()
            {
                yield return typeof(SourceGenerator);
            }
        }
    }
}
