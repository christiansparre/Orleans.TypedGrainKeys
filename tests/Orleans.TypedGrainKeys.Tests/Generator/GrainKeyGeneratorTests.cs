using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Orleans.TypedGrainKeys.Generators;

namespace Orleans.TypedGrainKeys.Tests.Generator;

public class GrainKeyGeneratorTests
{
    [Test]
    [Arguments("public partial record Key(System.Guid TenantId, int UserId);")]
    [Arguments("public partial record struct Key(System.Guid TenantId, int UserId);")]
    [Arguments("public readonly partial record struct Key(System.Guid TenantId, int UserId);")]
    [Arguments("internal sealed partial record Key(string Name);")]
    [Arguments("public partial record Key(string @class, int @event);")]
    [Arguments("public partial record Key(Color Color, System.DateOnly Day, System.TimeOnly Time, System.TimeSpan Span, bool Flag, long Big, System.DateTime At, System.DateTimeOffset AtOffset); public enum Color { Red }")]
    public async Task Generates_Compiling_Code_For_Valid_Keys(string declaration)
    {
        var run = GeneratorHarness.Run($$"""
            using Orleans;
            namespace App;
            [GrainKey]
            {{declaration}}
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(run.GeneratedSources).HasSingleItem();
    }

    [Test]
    public async Task Generates_Code_For_Keys_In_The_Global_Namespace()
    {
        var run = GeneratorHarness.Run("""
            [Orleans.GrainKey]
            public partial record Key(int Id);
            """);

        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(run.GeneratedSources.Single()).DoesNotContain("namespace");
    }

    [Test]
    public async Task Supports_Positional_Parameters_On_Another_Partial_Declaration()
    {
        var run = GeneratorHarness.Run("""
            using Orleans;
            namespace App;
            [GrainKey]
            public partial record Key;
            public partial record Key(System.Guid TenantId);
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
    }

    [Test]
    public async Task Nested_Grain_Keys_Are_Written_Inline()
    {
        var run = GeneratorHarness.Run("""
            using Orleans;
            namespace App;
            [GrainKey] public partial record Name(string First, string Last);
            [GrainKey] public partial record Key(System.Guid TenantId, Name Name);
            """);

        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(run.GeneratedSources.Single(s => s.Contains("partial record Key")))
            .Contains("global::App.Name.WriteGrainKeyParts(writer, key.Name);");
    }

    [Test]
    [Arguments("public record Key(int Id);", "TGK001")]
    [Arguments("public partial class Key { }", "TGK002")]
    [Arguments("public partial struct Key { }", "TGK002")]
    [Arguments("public partial record Key;", "TGK003")]
    [Arguments("public partial record Key();", "TGK003")]
    [Arguments("public partial class Outer { [GrainKey] public partial record Key(int Id); }", "TGK004")]
    [Arguments("public partial record Key<T>(int Id);", "TGK004")]
    [Arguments("public abstract partial record Key(int Id);", "TGK004")]
    [Arguments("public partial record Key(double Value);", "TGK005")]
    [Arguments("public partial record Key(string? Name);", "TGK005")]
    [Arguments("public partial record Key(int? Id);", "TGK005")]
    [Arguments("public partial record Key(Other Other); public record Other(int Id);", "TGK005")]
    public async Task Reports_Diagnostics_For_Invalid_Keys(string declaration, string expectedId)
    {
        // Nested declarations carry their own attribute; everything else gets it here.
        var attribute = declaration.Contains("[GrainKey]") ? "" : "[GrainKey]";
        var run = GeneratorHarness.Run($$"""
            using Orleans;
            namespace App;
            {{attribute}}
            {{declaration}}
            """);

        await Assert.That(run.GeneratorDiagnostics.Select(d => d.Id)).Contains(expectedId);
        await Assert.That(run.GeneratedSources).IsEmpty();
    }

    [Test]
    public async Task Unsupported_Part_Diagnostic_Points_At_The_Parameter()
    {
        const string source = """
            using Orleans;
            namespace App;
            [GrainKey]
            public partial record Key(int Id, double Value);
            """;

        var diagnostic = GeneratorHarness.Run(source).GeneratorDiagnostics.Single();

        var span = diagnostic.Location.SourceSpan;
        await Assert.That(source.Substring(span.Start, span.Length)).IsEqualTo("Value");
        await Assert.That(diagnostic.GetMessage()).Contains("'Value' of grain key 'Key' has type 'double'");
    }

    [Test]
    public async Task Caches_Results_When_An_Unrelated_File_Changes()
    {
        var compilation = GeneratorHarness.CreateCompilation("""
            using Orleans;
            namespace App;
            [GrainKey]
            public partial record Key(System.Guid TenantId, int UserId);
            """);

        var driver = GeneratorHarness.CreateDriver().RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace App; public class Unrelated { }")));

        var outputs = driver.GetRunResult().Results.Single()
            .TrackedSteps[GrainKeyGenerator.TrackingName]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToList();

        await Assert.That(outputs).IsNotEmpty();
        await Assert.That(outputs.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();
    }
}
