using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Orleans.TypedGrainKeys.Generators;

namespace Orleans.TypedGrainKeys.Tests.Generator;

/// <summary>Runs the generators over a source snippet and compiles the result.</summary>
internal static class GeneratorHarness
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(GrainKeyAttribute).Assembly.Location)
            .Append(typeof(Grain).Assembly.Location)
            .Append(typeof(Grain<object>).Assembly.Location)
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
            driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()).ToImmutableArray());
    }

    /// <summary>Runs the generators, then the analyzer over the generated compilation, and returns the analyzer's diagnostics.</summary>
    public static async Task<ImmutableArray<Diagnostic>> Analyze(string source)
    {
        CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out var output, out _);

        return await output
            .WithAnalyzers([new GrainKeyAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    public static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create(
        "GeneratorTests",
        [CSharpSyntaxTree.ParseText(source, ParseOptions)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static GeneratorDriver CreateDriver() => CSharpGeneratorDriver.Create(
        generators: [new GrainKeyGenerator().AsSourceGenerator(), new TypedKeyGrainGenerator().AsSourceGenerator()],
        parseOptions: ParseOptions,
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
}

internal sealed record GeneratorRun(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationErrors,
    ImmutableArray<string> GeneratedSources);

internal static class GeneratorDriverExtensions
{
    /// <summary>The reasons for every output of the tracked step <paramref name="trackingName"/> in the last run.</summary>
    public static List<IncrementalStepRunReason> StepReasons(this GeneratorDriver driver, string trackingName) =>
        driver.GetRunResult().Results
            .SelectMany(r => r.TrackedSteps)
            .Where(step => step.Key == trackingName)
            .SelectMany(step => step.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToList();
}
