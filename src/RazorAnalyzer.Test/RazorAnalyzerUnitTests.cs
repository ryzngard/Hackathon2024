using System.Threading.Tasks;

using Xunit;

using VerifyCS = RazorAnalyzer.Test.CSharpAnalyzerVerifier<RazorAnalyzer.RazorAnalyzerAnalyzer>;

namespace RazorAnalyzer.Test
{
    public class RazorAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task TestMethod1()
        {
            var testCode = """
                @page "/index"

                <p>Testing</p>
                <p>@Index</P>
                @code 
                {
                    int Index = 0;
                }
                """;

            var test = new VerifyCS.Test();
            test.TestState.AdditionalFiles.Add(("index.razor", testCode));
            await test.RunAsync();
        }
    }
}
