using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using RazorAnalyzer.Analyzers;

using Xunit;

namespace RazorAnalyzer.Test;

public class AnalyzerTests
{
    [Fact]
    public Task AnalyzeDiagnostic()
        => TestAsync("""
            @page "/index"

            <p>Hello</p>
            """,
            "Index.razor");

    [Fact]
    public Task ReportUppercaseHtml()
        => TestAsync("""
            @page "/index"

            <P>Hello</P>
            """,
            "Index.razor",
            Diagnostic.Create(
                LowerCaseHTMLAnalyzer.Descriptor,
                Location.Create("Index.razor", new TextSpan(18, 3), new LinePositionSpan(new LinePosition(2, 0), new LinePosition(2, 3)))
            )
        );

    [Fact]
    public Task PreferCodeBehind()
        => TestAsync("""
            @page "/index"

            @code 
            {
                int Index = 0;
            }
            """,
            "Index.razor",
            Diagnostic.Create(
                CodeBlockStylingAnalyzer.Descriptor,
                Location.Create("Index.razor", new TextSpan(18, 3), new LinePositionSpan(new LinePosition(2, 0), new LinePosition(2, 3)))
            )
        );
            

    private static async Task TestAsync(string input, string filePath, params Diagnostic[] expectedDiagnostics)
    {
        var generator = new SourceGenerator();

        var compilation = CSharpCompilation.Create("TestProject",
            [CSharpSyntaxTree.ParseText("struct Test { }")],
            Basic.Reference.Assemblies.Net80.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([new TestAdditionalText(input, filePath)]);
        driver = driver.WithUpdatedAnalyzerConfigOptions(new OptionsProvider(filePath));

        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult();
        var runResult = result.Results[0];
        if (expectedDiagnostics.Length == 0)
        {
            Assert.Empty(runResult.Diagnostics);
        }
        else
        {
            foreach (var d in expectedDiagnostics)
            {
                Assert.Contains(d, runResult.Diagnostics);
            }
        }
    }

    private class OptionsProvider(string filePath) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalOptionsImpl(filePath);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }

        private sealed class GlobalOptionsImpl(string filePath) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
            {
                if (key == "build_metadata.AdditionalFiles.TargetPath")
                {
                    var bytes = Encoding.UTF8.GetBytes(filePath);
                    value = Convert.ToBase64String(bytes);
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
