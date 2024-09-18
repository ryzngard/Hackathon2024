using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace RazorAnalyzer.Generator;

internal sealed class StaticCompilationTagHelperFeature(Compilation compilation)
        : RazorEngineFeatureBase, ITagHelperFeature
{
    private ImmutableArray<ITagHelperDescriptorProvider> _providers;

    public void CollectDescriptors(ISymbol? targetSymbol, List<TagHelperDescriptor> results)
    {
        if (_providers.IsDefault)
        {
            return;
        }

        var context = new TagHelperDescriptorProviderContext(compilation, targetSymbol, results);

        foreach (var provider in _providers)
        {
            provider.Execute(context);
        }
    }

    IReadOnlyList<TagHelperDescriptor> ITagHelperFeature.GetDescriptors()
    {
        var results = new List<TagHelperDescriptor>();
        CollectDescriptors(targetSymbol: null, results);

        return results;
    }

    protected override void OnInitialized()
    {
        _providers = Engine.Features
            .OfType<ITagHelperDescriptorProvider>()
            .OrderBy(f => f.Order)
            .ToImmutableArray();
    }
}