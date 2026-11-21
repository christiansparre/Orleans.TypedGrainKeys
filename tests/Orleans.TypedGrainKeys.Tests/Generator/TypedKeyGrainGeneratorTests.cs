using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Orleans.TypedGrainKeys.Generators;

namespace Orleans.TypedGrainKeys.Tests.Generator;

public class TypedKeyGrainGeneratorTests
{
    private const string Prelude = """
        using System.Threading.Tasks;
        using Orleans;
        using Orleans.Runtime;
        namespace App;
        [GrainKey] public partial record UserKey(System.Guid TenantId, int UserId);
        public interface IUserGrain : IGrainWithTypedKey<UserKey> { Task<int> Get(); }
        """;

    private const string UsesKey = "public Task<int> Get() => Task.FromResult(Key.UserId);";

    [Test]
    [Arguments($"public partial class UserGrain : Grain, IUserGrain {{ {UsesKey} }}")]
    [Arguments($"public partial class UserGrain(IGrainContext context) : IGrainBase, IUserGrain {{ public IGrainContext GrainContext {{ get; }} = context; {UsesKey} }}")]
    [Arguments($"public class State {{ }} public partial class UserGrain : Grain<State>, IUserGrain {{ {UsesKey} }}")]
    [Arguments($"public partial class UserGrain<T> : Grain, IUserGrain {{ {UsesKey} }}")]
    [Arguments($"public sealed partial class UserGrain : Grain, IUserGrain {{ {UsesKey} }}")]
    [Arguments($"public partial class Outer {{ public partial class UserGrain : Grain, IUserGrain {{ {UsesKey} }} }}")]
    [Arguments($"public partial class UserGrain : Grain {{ {UsesKey} }} public partial class UserGrain : IUserGrain {{ }}")]
    public async Task Generates_A_Key_Property_For_Partial_Grains(string declaration)
    {
        var run = GeneratorHarness.Run($"{Prelude}\n{declaration}");

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(GrainSources(run)).HasSingleItem();
    }

    [Test]
    public async Task Derived_Grains_Use_The_Key_Generated_For_Their_Base_Class()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public abstract partial class UserGrainBase : Grain, IUserGrain { public abstract Task<int> Get(); }
            public partial class UserGrain : UserGrainBase { public override Task<int> Get() => Task.FromResult(Key.UserId); }
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(GrainSources(run).Single()).Contains("partial class UserGrainBase");
    }

    [Test]
    public async Task Non_Partial_Grain_Gets_A_Suggestion_And_No_Key()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public class UserGrain : Grain, IUserGrain { public Task<int> Get() => Task.FromResult(0); }
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("TGK006");
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Info);
        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(GrainSources(run)).IsEmpty();
    }

    [Test]
    public async Task Grain_Nested_In_A_Non_Partial_Type_Names_That_Type()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public class Outer { public partial class UserGrain : Grain, IUserGrain { public Task<int> Get() => Task.FromResult(0); } }
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("TGK006");
        await Assert.That(diagnostic.GetMessage()).StartsWith("Declare 'Outer' partial");
    }

    [Test]
    public async Task Partial_Grain_With_Its_Own_Key_Member_Gets_A_Warning()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public partial class UserGrain : Grain, IUserGrain { public int Key => 1; public Task<int> Get() => Task.FromResult(Key); }
            """);

        await Assert.That(run.GeneratorDiagnostics.Single().Id).IsEqualTo("TGK007");
        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(GrainSources(run)).IsEmpty();
    }

    [Test]
    public async Task Non_Partial_Grain_With_Its_Own_Key_Member_Is_Left_Alone()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public class UserGrain : Grain, IUserGrain { public UserKey Key => this.GetTypedKey<UserKey>(); public Task<int> Get() => Task.FromResult(Key.UserId); }
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
    }

    [Test]
    public async Task Grain_With_Conflicting_Typed_Keys_Gets_A_Warning()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            [GrainKey] public partial record OtherKey(string Name);
            public interface IOtherGrain : IGrainWithTypedKey<OtherKey> { }
            public partial class UserGrain : Grain, IUserGrain, IOtherGrain { public Task<int> Get() => Task.FromResult(0); }
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("TGK008");
        await Assert.That(diagnostic.GetMessage()).Contains("App.UserKey, App.OtherKey");
        await Assert.That(GrainSources(run)).IsEmpty();
    }

    [Test]
    public async Task Classes_Without_A_Typed_Key_Are_Ignored()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public interface IPlainGrain : IGrainWithStringKey { }
            public partial class PlainGrain : Grain, IPlainGrain { }
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(GrainSources(run)).IsEmpty();
    }

    [Test]
    public async Task Caches_Results_When_An_Unrelated_File_Changes()
    {
        var compilation = GeneratorHarness.CreateCompilation($"{Prelude}\npublic partial class UserGrain : Grain, IUserGrain {{ {UsesKey} }}");

        var driver = GeneratorHarness.CreateDriver().RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace App; public class Unrelated { }")));

        var outputs = driver.StepReasons(TypedKeyGrainGenerator.TrackingName);

        await Assert.That(outputs).IsNotEmpty();
        await Assert.That(outputs.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();
    }

    private static List<string> GrainSources(GeneratorRun run) =>
        run.GeneratedSources.Where(source => source.Contains("__typedGrainKey")).ToList();
}
