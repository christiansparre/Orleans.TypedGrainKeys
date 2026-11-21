using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Orleans.TypedGrainKeys.Generators;

/// <summary>
/// Adds a cached <c>Key</c> property to every <c>partial</c> grain class whose grain interface is an
/// <c>IGrainWithTypedKey&lt;TKey&gt;</c>. Works with any base class, including <c>Grain</c>,
/// <c>Grain&lt;TState&gt;</c> and POCO grains implementing <c>IGrainBase</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class TypedKeyGrainGenerator : IIncrementalGenerator
{
    internal const string TypedKeyInterfaceMetadataName = "Orleans.IGrainWithTypedKey`1";

    /// <summary>The name of the parsing step, used by tests to verify incremental caching.</summary>
    public const string TrackingName = "TypedKeyGrains";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, ct) => Parse(ctx, ct))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!)
            .WithTrackingName(TrackingName);

        context.RegisterSourceOutput(results, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (result.Grain is { } grain)
            {
                spc.AddSource(grain.HintName, Emit(grain));
            }
        });
    }

    private static GrainParseResult? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax, cancellationToken) is not { } symbol)
        {
            return null;
        }

        // A partial class can have several declarations with base lists; handle it once, from the first.
        var first = symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(cancellationToken))
            .OfType<ClassDeclarationSyntax>()
            .First(d => d.BaseList is not null);
        if (first.SyntaxTree != syntax.SyntaxTree || first.Span != syntax.Span)
        {
            return null;
        }

        var typedInterface = context.SemanticModel.Compilation.GetTypeByMetadataName(TypedKeyInterfaceMetadataName);
        if (typedInterface is null)
        {
            return null;
        }

        var keyTypes = GetKeyTypes(symbol, typedInterface);
        if (keyTypes.Count == 0)
        {
            return null;
        }

        var location = syntax.Identifier.GetLocation();

        if (keyTypes.Count > 1)
        {
            var names = string.Join(", ", keyTypes.Select(k => k.ToDisplayString()));
            return Failed(DiagnosticDescriptors.GrainHasConflictingKeys, location, symbol.Name, names);
        }

        var keyType = keyTypes[0];
        var keyTypeName = keyType.ToDisplayString();

        // An existing Key member wins. Only warn when the grain opted in by being partial.
        var inherited = FindInheritedKey(symbol.BaseType, keyType, typedInterface);
        if (inherited == InheritedKey.Compatible)
        {
            return null;
        }

        if (symbol.GetMembers("Key").Any() || inherited == InheritedKey.Conflicting)
        {
            return IsPartial(symbol)
                ? Failed(DiagnosticDescriptors.GrainKeyMemberExists, location, symbol.Name, keyTypeName)
                : null;
        }

        for (var type = symbol; type is not null; type = type.ContainingType)
        {
            if (!IsPartial(type))
            {
                var at = type.Equals(symbol, SymbolEqualityComparer.Default) ? location : type.Locations.FirstOrDefault();
                return Failed(DiagnosticDescriptors.GrainNotPartial, at, type.Name, keyTypeName, symbol.Name);
            }
        }

        var containingTypes = new List<string>();
        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            containingTypes.Insert(0, Declaration(type));
        }

        var grain = new GrainModel(
            Namespace: symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            ContainingTypes: new EquatableArray<string>(containingTypes),
            Declaration: Declaration(symbol),
            KeyType: keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            HintName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "").Replace('<', '{').Replace('>', '}') + ".TypedKeyGrain.g.cs");

        return new GrainParseResult(grain, new EquatableArray<DiagnosticModel>([]));
    }

    private static GrainParseResult Failed(DiagnosticDescriptor descriptor, Location? location, params string[] arguments) =>
        new(null, new EquatableArray<DiagnosticModel>([new DiagnosticModel(descriptor, location, new EquatableArray<string>(arguments))]));

    private static List<ITypeSymbol> GetKeyTypes(INamedTypeSymbol type, INamedTypeSymbol typedInterface) =>
        type.AllInterfaces
            .Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, typedInterface))
            .Select(i => i.TypeArguments[0])
            .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
            .ToList();

    private enum InheritedKey
    {
        None,
        Compatible,
        Conflicting
    }

    /// <summary>
    /// Looks for a <c>Key</c> member a base class already has, or will get from this generator
    /// (generated members of base classes in the same compilation aren't visible yet).
    /// </summary>
    private static InheritedKey FindInheritedKey(INamedTypeSymbol? type, ITypeSymbol keyType, INamedTypeSymbol typedInterface)
    {
        for (; type is not null; type = type.BaseType)
        {
            var member = type.GetMembers("Key").FirstOrDefault();
            if (member is not null)
            {
                return member is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, keyType)
                    ? InheritedKey.Compatible
                    : InheritedKey.Conflicting;
            }

            if (type.DeclaringSyntaxReferences.Length > 0 && IsPartial(type))
            {
                var baseKeys = GetKeyTypes(type, typedInterface);
                if (baseKeys.Count == 1 && SymbolEqualityComparer.Default.Equals(baseKeys[0], keyType))
                {
                    return InheritedKey.Compatible;
                }
            }
        }

        return InheritedKey.None;
    }

    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax d && d.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static string Declaration(INamedTypeSymbol type)
    {
        var keyword = type switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class"
        };

        var typeParameters = type.TypeParameters.Length == 0
            ? ""
            : "<" + string.Join(", ", type.TypeParameters.Select(p => p.Name)) + ">";

        return $"partial {keyword} {type.Name}{typeParameters}";
    }

    private static string Emit(GrainModel grain)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (grain.Namespace is not null)
        {
            sb.AppendLine($"namespace {grain.Namespace};");
            sb.AppendLine();
        }

        var indent = "";
        foreach (var declaration in grain.ContainingTypes.Append(grain.Declaration))
        {
            sb.AppendLine($"{indent}{declaration}");
            sb.AppendLine($"{indent}{{");
            indent += "    ";
        }

        var k = grain.KeyType;
        sb.AppendLine($"{indent}private {k} __typedGrainKey = default!;");
        sb.AppendLine($"{indent}private bool __typedGrainKeyDecoded;");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>The decoded primary key of this grain. It is decoded on first use and then cached.</summary>");
        sb.AppendLine($"{indent}public {k} Key");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    get");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (!__typedGrainKeyDecoded)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            __typedGrainKey = global::Orleans.AddressableExtensions.GetTypedKey<{k}>(this);");
        sb.AppendLine($"{indent}            __typedGrainKeyDecoded = true;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}        return __typedGrainKey;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");

        foreach (var _ in grain.ContainingTypes.Append(grain.Declaration))
        {
            indent = indent.Substring(4);
            sb.AppendLine($"{indent}}}");
        }

        return sb.ToString();
    }
}
