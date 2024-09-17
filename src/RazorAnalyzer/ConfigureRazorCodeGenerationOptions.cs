using System;

using Microsoft.AspNetCore.Razor.Language;

namespace RazorAnalyzer;

internal class ConfigureRazorCodeGenerationOptions : IRazorProjectEngineFeature
{
    private Action<RazorCodeGenerationOptionsBuilder> _value;

    public ConfigureRazorCodeGenerationOptions(Action<RazorCodeGenerationOptionsBuilder> value)
    {
        _value = value;
    }

    public RazorProjectEngine ProjectEngine { get; set; }
}