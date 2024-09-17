using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RazorAnalyzer.Test;

internal class TestAdditionalText(string content, string path) : AdditionalText
{
    public override string Path => path;

    public override SourceText GetText(CancellationToken cancellationToken = default) 
        => SourceText.From(content);
}
