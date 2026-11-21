using Microsoft.CodeAnalysis;

namespace Orleans.TypedGrainKeys.Tests.Generator;

/// <summary>The generated key names and type-safe <c>GetGrain</c> extensions.</summary>
public class GrainFactoryExtensionTests
{
    private const string Prelude = """
        using System.Threading.Tasks;
        using Orleans;
        namespace App;
        [GrainKey("user")] public partial record UserKey(System.Guid TenantId, int UserId);
        [GrainKey("order")] public partial record OrderKey(System.Guid TenantId, int OrderId);
        public interface IUserGrain : IGrainWithTypedKey<UserKey> { Task<int> Get(); }
        public interface IPlainGrain : IGrainWithStringKey { }
        """;

    [Test]
    public async Task GetGrain_Accepts_The_Typed_Key()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public static class Usage
            {
                public static IUserGrain Get(IGrainFactory factory) => factory.GetGrain<IUserGrain>(new UserKey(System.Guid.NewGuid(), 1));
                public static IUserGrain WithPrefix(IGrainFactory factory) => factory.GetGrain<IUserGrain>(new UserKey(System.Guid.NewGuid(), 1), "App.UserGrain");
            }
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.CompilationErrors).IsEmpty();
    }

    [Test]
    [Arguments("factory.GetGrain<IUserGrain>(new OrderKey(System.Guid.NewGuid(), 1))")]
    [Arguments("factory.GetGrain<IPlainGrain>(new UserKey(System.Guid.NewGuid(), 1))")]
    public async Task GetGrain_Rejects_A_Key_Of_Another_Type(string call)
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public static class Usage { public static object Get(IGrainFactory factory) => {{call}}; }
            """);

        await Assert.That(run.CompilationErrors).IsNotEmpty();
    }

    [Test]
    public async Task Keys_No_Longer_Convert_To_Or_From_String()
    {
        var run = GeneratorHarness.Run($$"""
            {{Prelude}}
            public static class Usage
            {
                public static string ToText(UserKey key) => key;
                public static UserKey FromText(string text) => text;
            }
            """);

        await Assert.That(run.CompilationErrors.Select(e => e.Id)).IsEquivalentTo(["CS0029", "CS0029"]);
    }

    [Test]
    public async Task Internal_Keys_Get_Internal_Extensions()
    {
        var run = GeneratorHarness.Run("""
            using Orleans;
            namespace App;
            [GrainKey("secret")] internal partial record SecretKey(int Id);
            """);

        await Assert.That(run.CompilationErrors).IsEmpty();
        await Assert.That(run.GeneratedSources.Single()).Contains("internal static class SecretKeyGrainFactoryExtensions");
    }

    [Test]
    [Arguments("\"\"")]
    [Arguments("\"a|b\"")]
    [Arguments("\"has space\"")]
    [Arguments("\"bäd\"")]
    [Arguments("null")]
    public async Task Invalid_Names_Are_Reported(string name)
    {
        var run = GeneratorHarness.Run($$"""
            using Orleans;
            namespace App;
            [GrainKey({{name}})] public partial record Key(int Id);
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("TGK009");
        await Assert.That(run.GeneratedSources).IsEmpty();
    }

    [Test]
    public async Task Valid_Name_Characters_Are_Accepted()
    {
        var run = GeneratorHarness.Run("""
            using Orleans;
            namespace App;
            [GrainKey("Billing.Invoice-v2_x")] public partial record Key(int Id);
            """);

        await Assert.That(run.GeneratorDiagnostics).IsEmpty();
        await Assert.That(run.GeneratedSources.Single()).Contains("GrainKeyName => \"Billing.Invoice-v2_x\"");
    }

    [Test]
    public async Task Duplicate_Names_Are_Reported_On_Every_Key()
    {
        var diagnostics = await GeneratorHarness.Analyze("""
            using Orleans;
            namespace App;
            [GrainKey("user")] public partial record UserKey(int Id);
            [GrainKey("user")] public partial record CustomerKey(int Id);
            [GrainKey("order")] public partial record OrderKey(int Id);
            """);

        await Assert.That(diagnostics.Select(d => d.Id)).IsEquivalentTo(["TGK010", "TGK010"]);
        await Assert.That(diagnostics.Select(d => d.GetMessage()))
            .Contains("Grain key 'UserKey' has the name 'user', which is also used by 'App.CustomerKey'. Give each key a unique name so their keys can't be mixed up.");
    }

    [Test]
    public async Task String_Key_For_A_Typed_Grain_Is_Reported()
    {
        var diagnostics = await GeneratorHarness.Analyze($$"""
            {{Prelude}}
            public static class Usage
            {
                public static IUserGrain Get(IGrainFactory factory) => factory.GetGrain<IUserGrain>("user|whatever");
                public static IUserGrain FromClient(IClusterClient client) => client.GetGrain<IUserGrain>("user|whatever");
            }
            """);

        await Assert.That(diagnostics.Select(d => d.Id)).IsEquivalentTo(["TGK011", "TGK011"]);
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("'IUserGrain' has a typed key. Pass a 'UserKey' instead of a string, so the key type is checked at compile time.");
    }

    [Test]
    public async Task Typed_Keys_And_Plain_String_Grains_Are_Not_Reported()
    {
        var diagnostics = await GeneratorHarness.Analyze($$"""
            {{Prelude}}
            public static class Usage
            {
                public static IUserGrain Typed(IGrainFactory factory) => factory.GetGrain<IUserGrain>(new UserKey(System.Guid.NewGuid(), 1));
                public static IPlainGrain Plain(IGrainFactory factory) => factory.GetGrain<IPlainGrain>("anything");
            }
            """);

        await Assert.That(diagnostics).IsEmpty();
    }
}
