using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Orleans.TypedGrainKeys.Generators;

namespace Orleans.TypedGrainKeys.Tests.Generator;

/// <summary>Runs <see cref="GrainKeyGenerator"/> over a source snippet and compiles the result.</summary>
internal static class GeneratorHarness
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(GrainKeyAttribute).Assembly.Location)
            .Append(typeof(Grain).Assembly.Location)
            .Distinct()
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();

    public static GeneratorRun Run(string source)
    {
        var compilation = CreateCompilation(source);

        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        return new GeneratorRun(
            generatorDiagnostics,
            output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray(),
            driver.GetRunResult().Results.Single().GeneratedSources.Select(s => s.SourceText.ToString()).ToImmutableArray());
    }

    public static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create(
        "GeneratorTests",
        [CSharpSyntaxTree.ParseText(source, ParseOptions)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static GeneratorDriver CreateDriver() => CSharpGeneratorDriver.Create(
        generators: [new GrainKeyGenerator().AsSourceGenerator()],
        parseOptions: ParseOptions,
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
}

internal sealed record GeneratorRun(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationErrors,
    ImmutableArray<string> GeneratedSources);
