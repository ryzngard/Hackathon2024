using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = RazorAnalyzer.Test.CSharpCodeFixVerifier<
    RazorAnalyzer.RazorAnalyzerAnalyzer,
    RazorAnalyzer.RazorAnalyzerCodeFixProvider>;

namespace RazorAnalyzer.Test
{
    [TestClass]
    public class RazorAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
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

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("RazorAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
