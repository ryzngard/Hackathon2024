using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Razor.Language;

namespace RazorAnalyzer;

internal sealed class AllTagHelpers : IReadOnlyList<TagHelperDescriptor>
{
    private static readonly List<TagHelperDescriptor> s_emptyList = new();

    public static readonly AllTagHelpers Empty = new(
        tagHelpersFromCompilation: null,
        tagHelpersFromReferences: null);

    private readonly List<TagHelperDescriptor> _tagHelpersFromCompilation;
    private readonly List<TagHelperDescriptor> _tagHelpersFromReferences;

    private AllTagHelpers(
        List<TagHelperDescriptor>? tagHelpersFromCompilation,
        List<TagHelperDescriptor>? tagHelpersFromReferences)
    {
        _tagHelpersFromCompilation = tagHelpersFromCompilation ?? s_emptyList;
        _tagHelpersFromReferences = tagHelpersFromReferences ?? s_emptyList;
    }

    public static AllTagHelpers Create(
        List<TagHelperDescriptor>? tagHelpersFromCompilation,
        List<TagHelperDescriptor>? tagHelpersFromReferences)
    {
        return tagHelpersFromCompilation is not { Count: > 0 } && tagHelpersFromReferences is not { Count: > 0 }
            ? new(tagHelpersFromCompilation, tagHelpersFromReferences)
            : Empty;
    }

    public TagHelperDescriptor this[int index]
    {
        get
        {
            if (index >= 0)
            {
                return index < _tagHelpersFromCompilation.Count
                    ? _tagHelpersFromCompilation[index]
                    : _tagHelpersFromReferences[index - _tagHelpersFromCompilation.Count];
            }

            throw new IndexOutOfRangeException();
        }
    }

    public int Count
        => _tagHelpersFromCompilation.Count + _tagHelpersFromReferences.Count;

    public IEnumerator<TagHelperDescriptor> GetEnumerator()
    {
        foreach (var tagHelper in _tagHelpersFromCompilation)
        {
            yield return tagHelper;
        }

        foreach (var tagHelper in _tagHelpersFromReferences)
        {
            yield return tagHelper;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}