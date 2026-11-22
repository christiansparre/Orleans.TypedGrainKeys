using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Orleans.TypedGrainKeys.Generators;

/// <summary>
/// Reports grain key names used by more than one key (TGK010), and string keys passed to
/// <c>IGrainFactory.GetGrain</c> for grain interfaces that have a typed key (TGK011).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GrainKeyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.DuplicateName, DiagnosticDescriptors.StringKeyForTypedGrain];

    public override void Initialize(AnalysisContext context)
    {
        // Generated code, like the GetGrain extensions, legitimately passes the encoded string.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var grainKeyAttribute = compilation.GetTypeByMetadataName(GrainKeyGenerator.GrainKeyAttributeName);
        var grainWithTypedKey = compilation.GetTypeByMetadataName(TypedKeyGrainGenerator.TypedKeyInterfaceMetadataName);
        var grainFactory = compilation.GetTypeByMetadataName("Orleans.IGrainFactory");

        if (grainKeyAttribute is not null)
        {
            var names = new ConcurrentBag<(string Name, INamedTypeSymbol Key)>();

            context.RegisterSymbolAction(symbolContext =>
            {
                var key = (INamedTypeSymbol)symbolContext.Symbol;
                var attribute = key.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, grainKeyAttribute));
                if (attribute is { ConstructorArguments.Length: 1 } && attribute.ConstructorArguments[0].Value is string name)
                {
                    names.Add((name, key));
                }
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(endContext =>
            {
                foreach (var group in names.GroupBy(n => n.Name, System.StringComparer.Ordinal).Where(g => g.Count() > 1))
                {
                    var keys = group.Select(n => n.Key).OrderBy(k => k.ToDisplayString(), System.StringComparer.Ordinal).ToList();
                    foreach (var key in keys)
                    {
                        var others = string.Join(", ", keys.Where(k => !SymbolEqualityComparer.Default.Equals(k, key)).Select(k => $"'{k.ToDisplayString()}'"));
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateName, key.Locations.FirstOrDefault(), key.Name, group.Key, others));
                    }
                }
            });
        }

        if (grainWithTypedKey is not null && grainFactory is not null)
        {
            context.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, grainFactory, grainWithTypedKey),
                OperationKind.Invocation);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol grainFactory, INamedTypeSymbol grainWithTypedKey)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;

        if (method.Name != "GetGrain"
            || method.TypeArguments.Length != 1
            || method.Parameters.Length == 0
            || method.Parameters[0].Type.SpecialType != SpecialType.System_String)
        {
            return;
        }

        var containingType = method.ContainingType;
        if (!SymbolEqualityComparer.Default.Equals(containingType, grainFactory)
            && !containingType.AllInterfaces.Contains(grainFactory, SymbolEqualityComparer.Default))
        {
            return;
        }

        // Only concrete grain interfaces; a type parameter is checked where it's instantiated.
        if (method.TypeArguments[0] is not INamedTypeSymbol grainInterface)
        {
            return;
        }

        var typedKey = grainInterface.AllInterfaces
            .Append(grainInterface)
            .FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, grainWithTypedKey));

        if (typedKey is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StringKeyForTypedGrain,
            context.Operation.Syntax.GetLocation(),
            grainInterface.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            typedKey.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }
}
