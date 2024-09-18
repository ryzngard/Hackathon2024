using System;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace RazorAnalyzer.Generator;

internal class SourceGeneratorProjectItem : RazorProjectItem, IEquatable<SourceGeneratorProjectItem>
{
    private readonly string _fileKind;

    public SourceGeneratorProjectItem(string basePath, string filePath, string relativePhysicalPath, string fileKind, AdditionalText additionalText, string? cssScope)
    {
        BasePath = basePath;
        FilePath = filePath;
        RelativePhysicalPath = relativePhysicalPath;
        _fileKind = fileKind;
        AdditionalText = additionalText;
        CssScope = cssScope;
        var text = AdditionalText.GetText();
        if (text is not null)
        {
            RazorSourceDocument = RazorSourceDocument.Create(text.ToString(), AdditionalText.Path);
        }
    }

    public AdditionalText AdditionalText { get; }

    public RazorSourceDocument? RazorSourceDocument { get; set; }

    public override string BasePath { get; }

    public override string FilePath { get; }

    public override string? PhysicalPath { get; }

    public override bool Exists { get; }

    public override string FileKind => _fileKind ?? base.FileKind;

    public override string? CssScope { get; }

    public override string RelativePhysicalPath { get; }

    public bool Equals(SourceGeneratorProjectItem? other)
    {
        if (ReferenceEquals(AdditionalText, other?.AdditionalText))
        {
            return true;
        }

        // In the compiler server when the generator driver cache is enabled the
        // additional files are always different instances even if their content is the same.
        // It's technically possible for these hashes to collide, but other things would
        // also break in those cases, so for now we're okay with this.
        var thisHash = AdditionalText.GetText()?.GetContentHash() ?? [];
        var otherHash = other?.AdditionalText.GetText()?.GetContentHash() ?? [];
        return thisHash.SequenceEqual(otherHash);
    }

    public override int GetHashCode() => AdditionalText.GetHashCode();

    public override bool Equals(object? obj) => obj is SourceGeneratorProjectItem projectItem && Equals(projectItem);

    public override Stream Read()
    {
        if (RazorSourceDocument is null)
        {
            return Stream.Null;
        }

        var text = RazorSourceDocument.Text.ToString();
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}
