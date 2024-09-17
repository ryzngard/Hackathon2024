using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

using Microsoft.CodeAnalysis;

namespace RazorAnalyzer;

internal static class RazorDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidRazorLangVersionDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.InvalidRazorLangVersionRuleId,
        "InvalidRazorLangVersionDescriptor",
        "InvalidRazorLangVersionDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReComputingTagHelpersDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.ReComputingTagHelpersRuleId,
        "ReComputingTagHelpersDescriptor",
        "ReComputingTagHelpersDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TargetPathNotProvided = new DiagnosticDescriptor(
        DiagnosticIds.TargetPathNotProvidedRuleId,
        "TargetPathNotProvided",
        "TargetPathNotProvided",
        "RazorSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor GeneratedOutputFullPathNotProvided = new DiagnosticDescriptor(
        DiagnosticIds.GeneratedOutputFullPathNotProvidedRuleId,
        "GeneratedOutputFullPathNotProvided",
        "GeneratedOutputFullPathNotProvided",
        "RazorSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor CurrentCompilationReferenceNotFoundDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.CurrentCompilationReferenceNotFoundId,
        "CurrentCompilationReferenceNotFoundDescriptor",
        "CurrentCompilationReferenceNotFoundDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor SkippingGeneratedFileWriteDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.SkippingGeneratedFileWriteId,
        "SkippingGeneratedFileWriteDescriptor",
        "SkippingGeneratedFileWriteDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor SourceTextNotFoundDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.SourceTextNotFoundId,
        "SourceTextNotFoundDescriptor",
        "SourceTextNotFoundDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor UnexpectedProjectItemReadCallDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.UnexpectedProjectItemReadCallId,
        "UnexpectedProjectItemReadCallDescriptor",
        "UnexpectedProjectItemReadCallDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InvalidRazorContextComputedDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.InvalidRazorContextComputedId,
        "InvalidRazorContextComputedDescriptor",
        "InvalidRazorContextComputedDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MetadataReferenceNotProvidedDescriptor = new DiagnosticDescriptor(
        DiagnosticIds.MetadataReferenceNotProvidedId,
        "MetadataReferenceNotProvidedDescriptor",
        "MetadataReferenceNotProvidedDescriptor",
        "RazorSourceGenerator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    public static Diagnostic AsDiagnostic(this RazorDiagnostic razorDiagnostic)
    {
        var descriptor = new DiagnosticDescriptor(
            razorDiagnostic.Id,
            razorDiagnostic.GetMessage(),
            razorDiagnostic.GetMessage(),
            "Razor",
            razorDiagnostic.Severity switch
            {
                RazorDiagnosticSeverity.Error => DiagnosticSeverity.Error,
                RazorDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                _ => DiagnosticSeverity.Hidden,
            },
            isEnabledByDefault: true);

        var span = razorDiagnostic.Span;

        Location location;
        if (span == SourceSpan.Undefined)
        {
            // TextSpan.Empty
            location = Location.None;
        }
        else
        {
            var linePosition = new LinePositionSpan(
                new LinePosition(span.LineIndex, span.CharacterIndex),
                new LinePosition(span.LineIndex, span.CharacterIndex + span.Length));

            location = Location.Create(
               span.FilePath,
               new TextSpan(span.AbsoluteIndex, span.Length),
               linePosition);
        }

        return Diagnostic.Create(descriptor, location);
    }
}