using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace RazorAnalyzer.Analyzers;

public static class Descriptors
{
    public static readonly DiagnosticDescriptor LowercaseHtmlDescriptor = new(
        "T1000",
        "Do not use uppercase html",
        "Do not use uppercase html",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VirtualizeDescriptor = new(
        "T1001",
        "Invalid parent for virtualize",
        "Virtualize inserts a <div> element by default for spacer elements. Either specify a spacer element with SpacerElement=\"element\" or change the enclosing markup.",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CodeBlockStyling = new(
        "T1002",
        "Code block styling",
        "Do not use a code block, instead use a code behind file",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DisposableDescriptor = new(
        "T1003",
        "Item must have @implements IDisposable",
        "This component has a public Dispose method but does not implement IDisposable, make sure to add '@implements IDisposable'",
        "Correctness",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

}
