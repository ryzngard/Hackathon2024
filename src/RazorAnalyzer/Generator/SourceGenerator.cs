using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor;

using RazorAnalyzer.Analyzers;

namespace RazorAnalyzer.Generator;

[Generator]
public sealed class SourceGenerator() : IIncrementalGenerator
{
    private static readonly ImmutableArray<IRazorAnalyzer> s_razorAnalyzers =
        [
            new LowercaseHTMLAnalyzer(),
            new CodeBlockStylingAnalyzer(),
            new VirtualizeAnalyzer(),
            new DisposableComponent(),
        ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analyzerConfigOptions = context.AnalyzerConfigOptionsProvider;
        var parseOptions = context.ParseOptionsProvider;
        var compilation = context.CompilationProvider;

        // determine if we should suppress this run and filter out all the additional files and references if so
        var additionalTexts = context.AdditionalTextsProvider;
        var metadataRefs = context.MetadataReferencesProvider;

        var razorSourceGeneratorOptions = analyzerConfigOptions
            .Combine(parseOptions)
            .Select(ComputeRazorSourceGeneratorOptions);

        var sourceItems = additionalTexts
            .Where(static (file) => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Combine(analyzerConfigOptions)
            .Select(ComputeProjectItems)
            .ReportDiagnostics(context);

        var hasRazorFiles = sourceItems.Collect()
            .Select(static (sourceItems, _) => sourceItems.Any());

        var importFiles = sourceItems.Where(static file =>
        {
            var path = file.FilePath;
            if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return string.Equals(fileName, "_Imports", StringComparison.OrdinalIgnoreCase);
            }
            else if (path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return string.Equals(fileName, "_ViewImports", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        });

        var componentFiles = sourceItems.Where(static file => file.FilePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

        var generatedDeclarationCode = componentFiles
            .Combine(importFiles.Collect())
            .Combine(razorSourceGeneratorOptions)
            .WithLambdaComparer((old, @new) => old.Right.Equals(@new.Right) && old.Left.Left.Equals(@new.Left.Left) && old.Left.Right.SequenceEqual(@new.Left.Right))
            .Select(static (pair, _) =>
            {
                var ((sourceItem, importFiles), razorSourceGeneratorOptions) = pair;

                var projectEngine = GetDeclarationProjectEngine(sourceItem, importFiles, razorSourceGeneratorOptions);

                var codeGen = projectEngine.Process(sourceItem);

                var result = codeGen.GetCSharpDocument().GeneratedCode;
                return result;
            });

        var generatedDeclarationSyntaxTrees = generatedDeclarationCode
            .Combine(parseOptions)
            .Select(static (pair, ct) =>
            {
                var (generatedDeclarationCode, parseOptions) = pair;
                return CSharpSyntaxTree.ParseText(generatedDeclarationCode, (CSharpParseOptions)parseOptions, cancellationToken: ct);
            });

        var declCompilation = generatedDeclarationSyntaxTrees
            .Collect()
            .Combine(compilation)
            .Select(static (pair, _) =>
            {
                return pair.Right.AddSyntaxTrees(pair.Left);
            });

        var tagHelpersFromCompilation = declCompilation
            .Combine(razorSourceGeneratorOptions)
            .Select(static (pair, _) =>
            {

                var (compilation, razorSourceGeneratorOptions) = pair;
                var results = new List<TagHelperDescriptor>();

                var tagHelperFeature = GetStaticTagHelperFeature(compilation);

                tagHelperFeature.CollectDescriptors(compilation.Assembly, results);

                return results;
            })
            .WithLambdaComparer(static (a, b) => a!.SequenceEqual(b!));

        var tagHelpersFromReferences = compilation
            .Combine(razorSourceGeneratorOptions)
            .Combine(hasRazorFiles)
            .WithLambdaComparer(static (a, b) =>
            {
                var ((compilationA, razorSourceGeneratorOptionsA), hasRazorFilesA) = a;
                var ((compilationB, razorSourceGeneratorOptionsB), hasRazorFilesB) = b;

                // When using the generator cache in the compiler it's possible to encounter metadata references that are different instances
                // but ultimately represent the same underlying assembly. We compare the module version ids to determine if the references are the same
                if (!compilationA.References.SequenceEqual(compilationB.References, new LambdaComparer<MetadataReference>((old, @new) =>
                {
                    if (ReferenceEquals(old, @new))
                    {
                        return true;
                    }

                    if (old is null || @new is null)
                    {
                        return false;
                    }

                    var oldSymbol = compilationA.GetAssemblyOrModuleSymbol(old);
                    var newSymbol = compilationB.GetAssemblyOrModuleSymbol(@new);

                    if (SymbolEqualityComparer.Default.Equals(oldSymbol, newSymbol))
                    {
                        return true;
                    }

                    if (oldSymbol is IAssemblySymbol oldAssembly && newSymbol is IAssemblySymbol newAssembly)
                    {
                        var oldModuleMVIDs = oldAssembly.Modules.Select(GetMVID);
                        var newModuleMVIDs = newAssembly.Modules.Select(GetMVID);
                        return oldModuleMVIDs.SequenceEqual(newModuleMVIDs);

                        static Guid GetMVID(IModuleSymbol m) => m.GetMetadata()?.GetModuleVersionId() ?? Guid.Empty;
                    }

                    return false;
                })))
                {
                    return false;
                }

                if (razorSourceGeneratorOptionsA != razorSourceGeneratorOptionsB)
                {
                    return false;
                }

                return hasRazorFilesA == hasRazorFilesB;
            })
            .Select(static (pair, _) =>
            {

                var ((compilation, razorSourceGeneratorOptions), hasRazorFiles) = pair;
                if (!hasRazorFiles)
                {
                    // If there's no razor code in this app, don't do anything.
                    return null;
                }

                var tagHelperFeature = GetStaticTagHelperFeature(compilation);

                // Typically a project with Razor files will have many tag helpers in references.
                // So, we start with a larger capacity to avoid extra array copies.
                var results = new List<TagHelperDescriptor>(capacity: 128);

                foreach (var reference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                    {
                        tagHelperFeature.CollectDescriptors(assembly, results);
                    }
                }

                return results;
            });

        var allTagHelpers = tagHelpersFromCompilation
            .Combine(tagHelpersFromReferences)
            .Select(static (pair, _) =>
            {
                return AllTagHelpers.Create(tagHelpersFromCompilation: pair.Left, tagHelpersFromReferences: pair.Right);
            });

        var withOptions = sourceItems
            .Combine(importFiles.Collect())
            .WithLambdaComparer((old, @new) => old.Left.Equals(@new.Left) && old.Right.SequenceEqual(@new.Right))
            .Combine(razorSourceGeneratorOptions);

        var isAddComponentParameterAvailable = metadataRefs
            .Where(r => r.Display is { } display && display.EndsWith("Microsoft.AspNetCore.Components.dll", StringComparison.Ordinal))
            .Collect()
            .Select((refs, _) =>
            {
                var compilation = CSharpCompilation.Create("components", references: refs);
                return compilation.GetTypesByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder")
                    .Any(static t =>
                        t.DeclaredAccessibility == Accessibility.Public &&
                        t.GetMembers("AddComponentParameter")
                            .Any(static m => m.DeclaredAccessibility == Accessibility.Public));
            });

        IncrementalValuesProvider<(string, RazorCodeDocument, ImmutableArray<Diagnostic>)> processed(bool designTime)
        {
            return withOptions
                .Combine(isAddComponentParameterAvailable)
                .Select((pair, _) =>
                {
                    var (((sourceItem, imports), razorSourceGeneratorOptions), isAddComponentParameterAvailable) = pair;

                    var projectEngine = GetGenerationProjectEngine(sourceItem, imports, razorSourceGeneratorOptions, isAddComponentParameterAvailable);

                    var document = projectEngine.ProcessDeclarationOnly(sourceItem);

                    return (projectEngine, sourceItem, document);
                })
                .Select((pair, _) =>
                {
                    var (projectEngine, sourceItem, document) = pair;
                    document = projectEngine.Process(sourceItem);
                    var diagnostics = new List<Diagnostic>();

                    foreach (var analyzer in s_razorAnalyzers)
                    {
                        diagnostics.AddRange(analyzer.GetDiagnostics(document));
                    }

                    return (sourceItem.RelativePhysicalPath, document, diagnostics.ToImmutableArray());
                });
        }

        var csharpDocuments = processed(designTime: false)
            .Select(static (pair, _) =>
            {
                var (filePath, document, diagnostics) = pair;
                return (filePath, csharpDocument: document.GetCSharpDocument(), diagnostics);
            })
            .WithLambdaComparer(static (a, b) =>
            {
                if (a.csharpDocument.Diagnostics.Length > 0 || b.csharpDocument.Diagnostics.Length > 0)
                {
                    // if there are any diagnostics, treat the documents as unequal and force RegisterSourceOutput to be called uncached.
                    return false;
                }

                return string.Equals(a.csharpDocument.GeneratedCode, b.csharpDocument.GeneratedCode, StringComparison.Ordinal);
            })
            .WithTrackingName("CSharpDocuments");

        var csharpDocumentsWithSuppressionFlag = csharpDocuments
            .WithTrackingName("DocumentsWithSuppression");

        context.RegisterImplementationSourceOutput(csharpDocumentsWithSuppressionFlag, static (context, pair) =>
        {
            var (filePath, csharpDocument, diagnostics) = pair;

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }


            // Add a generated suffix so tools, such as coverlet, consider the file to be generated
            var hintName = GetIdentifierFromPath(filePath) + ".g.cs";

            foreach (var razorDiagnostic in csharpDocument.Diagnostics)
            {
                var csharpDiagnostic = razorDiagnostic.AsDiagnostic();
                context.ReportDiagnostic(csharpDiagnostic);
            }

            context.AddSource(hintName, csharpDocument.GeneratedCode);
        });
    }

    private RazorSourceGenerationOptions? ComputeRazorSourceGeneratorOptions((AnalyzerConfigOptionsProvider, ParseOptions) pair, CancellationToken ct)
    {
        var (options, parseOptions) = pair;
        var globalOptions = options.GlobalOptions;

        globalOptions.TryGetValue("build_property.RazorConfiguration", out var configurationName);
        globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        globalOptions.TryGetValue("build_property.SupportLocalizedComponentNames", out var supportLocalizedComponentNames);
        globalOptions.TryGetValue("build_property.GenerateRazorMetadataSourceChecksumAttributes", out var generateMetadataSourceChecksumAttributes);

        if (!globalOptions.TryGetValue("build_property.RazorLangVersion", out var razorLanguageVersionString) ||
            !RazorLanguageVersion.TryParse(razorLanguageVersionString, out var razorLanguageVersion))
        {
            razorLanguageVersion = RazorLanguageVersion.Latest;
        }

        var razorConfiguration = new RazorConfiguration(razorLanguageVersion, configurationName ?? "default", Extensions: [], UseConsolidatedMvcViews: true);

        var razorSourceGenerationOptions = new RazorSourceGenerationOptions()
        {
            Configuration = razorConfiguration,
            GenerateMetadataSourceChecksumAttributes = generateMetadataSourceChecksumAttributes == "true",
            RootNamespace = rootNamespace ?? "ASP",
            SupportLocalizedComponentNames = supportLocalizedComponentNames == "true",
            CSharpLanguageVersion = ((CSharpParseOptions)parseOptions).LanguageVersion,
        };

        return razorSourceGenerationOptions;
    }

    private static (SourceGeneratorProjectItem?, Diagnostic?) ComputeProjectItems((AdditionalText, AnalyzerConfigOptionsProvider) pair, CancellationToken ct)
    {
        var (additionalText, globalOptions) = pair;
        var options = globalOptions.GetOptions(additionalText);

        if (!options.TryGetValue("build_metadata.AdditionalFiles.TargetPath", out var encodedRelativePath) ||
            string.IsNullOrWhiteSpace(encodedRelativePath))
        {
            var diagnostic = Diagnostic.Create(
                RazorDiagnostics.TargetPathNotProvided,
                Location.None,
                additionalText.Path);
            return (null, diagnostic);
        }

        options.TryGetValue("build_metadata.AdditionalFiles.CssScope", out var cssScope);
        var relativePath = Encoding.UTF8.GetString(Convert.FromBase64String(encodedRelativePath));

        var projectItem = new SourceGeneratorProjectItem(
            basePath: "/",
            filePath: '/' + relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace("//", "/"),
            relativePhysicalPath: relativePath,
            fileKind: additionalText.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ? FileKinds.Component : FileKinds.Legacy,
            additionalText: additionalText,
            cssScope: cssScope);
        return (projectItem, null);
    }

    private static string GetIdentifierFromPath(string filePath)
    {
        var builder = new StringBuilder(filePath.Length);

        for (var i = 0; i < filePath.Length; i++)
        {
            switch (filePath[i])
            {
                case ':' or '\\' or '/':
                case char ch when !char.IsLetterOrDigit(ch):
                    builder.Append('_');
                    break;
                default:
                    builder.Append(filePath[i]);
                    break;
            }
        }

        return builder.ToString();
    }

    private static RazorProjectEngine GetDeclarationProjectEngine(
        SourceGeneratorProjectItem item,
        IEnumerable<SourceGeneratorProjectItem> imports,
        RazorSourceGenerationOptions razorSourceGeneratorOptions)
    {
        var fileSystem = new SourceGeneratorRazorProjectFileSystem();
        fileSystem.Add(item);
        foreach (var import in imports)
        {
            fileSystem.Add(import);
        }

        var discoveryProjectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
        {
            //b.Features.Add(new DefaultTypeNameFeature());
            b.Features.Add(new ConfigureRazorCodeGenerationOptions(options =>
            {
                options.SuppressPrimaryMethodBody = true;
                options.SuppressChecksum = true;
                options.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
            }));

            b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

            CompilerFeatures.Register(b);
            RazorExtensions.Register(b);

            b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpLanguageVersion);
        });

        return discoveryProjectEngine;
    }

    private static StaticCompilationTagHelperFeature GetStaticTagHelperFeature(Compilation compilation)
    {
        var tagHelperFeature = new StaticCompilationTagHelperFeature(compilation);

        // the tagHelperFeature will have its Engine property set as part of adding it to the engine, which is used later when doing the actual discovery
        var discoveryProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, new SourceGeneratorRazorProjectFileSystem(), b =>
        {
            b.Features.Add(tagHelperFeature);
            b.Features.Add(new DefaultTagHelperDescriptorProvider());

            CompilerFeatures.Register(b);
            RazorExtensions.Register(b);
        });

        return tagHelperFeature;
    }

    private static RazorProjectEngine GetGenerationProjectEngine(
        SourceGeneratorProjectItem item,
        IEnumerable<SourceGeneratorProjectItem> imports,
        RazorSourceGenerationOptions razorSourceGeneratorOptions,
        bool isAddComponentParameterAvailable)
    {
        var fileSystem = new SourceGeneratorRazorProjectFileSystem();
        fileSystem.Add(item);
        foreach (var import in imports)
        {
            fileSystem.Add(import);
        }

        var projectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions?.Configuration ?? RazorConfiguration.Default, fileSystem, b =>
        {
            b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

            b.Features.Add(new ConfigureRazorCodeGenerationOptions(options =>
            {
                options.SuppressMetadataSourceChecksumAttributes = !razorSourceGeneratorOptions.GenerateMetadataSourceChecksumAttributes;
                options.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
                options.SuppressUniqueIds = razorSourceGeneratorOptions.TestSuppressUniqueIds;
                options.SuppressAddComponentParameter = !isAddComponentParameterAvailable;
            }));

            CompilerFeatures.Register(b);
            RazorExtensions.Register(b);

            b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpLanguageVersion);
        });

        return projectEngine;
    }
}