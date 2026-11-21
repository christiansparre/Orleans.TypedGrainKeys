using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Orleans.TypedGrainKeys.Generators;

/// <summary>
/// Generates the encode/decode implementation for records marked with <c>[GrainKey]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class GrainKeyGenerator : IIncrementalGenerator
{
    internal const string GrainKeyAttributeName = "Orleans.GrainKeyAttribute";

    /// <summary>The name of the parsing step, used by tests to verify incremental caching.</summary>
    public const string TrackingName = "GrainKeys";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider.ForAttributeWithMetadataName(
            GrainKeyAttributeName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, ct) => Parse(ctx, ct))
            .WithTrackingName(TrackingName);

        context.RegisterSourceOutput(results, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (result.Key is { } key)
            {
                spc.AddSource($"{key.FullyQualifiedName.Replace("global::", "")}.GrainKey.g.cs", GrainKeyEmitter.Emit(key));
            }
        });
    }

    private static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var location = syntax.Identifier.GetLocation();
        var diagnostics = new List<DiagnosticModel>();

        void Report(DiagnosticDescriptor descriptor, Location? at, params string[] arguments) =>
            diagnostics.Add(new DiagnosticModel(descriptor, at, new EquatableArray<string>(arguments)));

        if (!symbol.IsRecord)
        {
            Report(DiagnosticDescriptors.NotARecord, location, symbol.Name);
            return Failed(diagnostics);
        }

        if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            Report(DiagnosticDescriptors.NotPartial, location, symbol.Name);
        }

        if (symbol.ContainingType is not null)
        {
            Report(DiagnosticDescriptors.UnsupportedDeclaration, location, symbol.Name, "nested inside another type");
        }

        if (symbol.IsGenericType)
        {
            Report(DiagnosticDescriptors.UnsupportedDeclaration, location, symbol.Name, "generic");
        }

        if (symbol.IsAbstract)
        {
            Report(DiagnosticDescriptors.UnsupportedDeclaration, location, symbol.Name, "abstract");
        }

        var parameters = GetPositionalParameters(symbol, context.SemanticModel.Compilation, cancellationToken);
        if (parameters.Count == 0)
        {
            Report(DiagnosticDescriptors.NoParameters, location, symbol.Name);
        }

        var parts = new List<PartModel>();
        foreach (var parameter in parameters)
        {
            var kind = Classify(parameter.Type);
            if (kind is null)
            {
                Report(
                    DiagnosticDescriptors.UnsupportedPartType,
                    parameter.Locations.FirstOrDefault() ?? location,
                    parameter.Name,
                    symbol.Name,
                    parameter.Type.ToDisplayString());
                continue;
            }

            parts.Add(new PartModel(
                EscapeIdentifier(parameter.Name),
                kind.Value,
                parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        if (diagnostics.Count > 0)
        {
            return Failed(diagnostics);
        }

        var key = new KeyModel(
            Namespace: symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            Name: symbol.Name,
            FullyQualifiedName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsValueType: symbol.IsValueType,
            IsReadOnly: symbol.IsReadOnly,
            Parts: new EquatableArray<PartModel>(parts));

        return new ParseResult(key, new EquatableArray<DiagnosticModel>([]));
    }

    private static ParseResult Failed(List<DiagnosticModel> diagnostics) =>
        new(null, new EquatableArray<DiagnosticModel>(diagnostics));

    /// <summary>
    /// Finds the parameters of the record's primary constructor, from whichever partial declaration has the parameter list.
    /// </summary>
    private static IReadOnlyList<IParameterSymbol> GetPositionalParameters(INamedTypeSymbol symbol, Compilation compilation, CancellationToken cancellationToken)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not RecordDeclarationSyntax { ParameterList: { } parameterList })
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(parameterList.SyntaxTree);
            return parameterList.Parameters
                .Select(p => semanticModel.GetDeclaredSymbol(p, cancellationToken))
                .OfType<IParameterSymbol>()
                .ToList();
        }

        return [];
    }

    private static PartKind? Classify(ITypeSymbol type)
    {
        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return null;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_String: return PartKind.String;
            case SpecialType.System_Int32: return PartKind.Int32;
            case SpecialType.System_Int64: return PartKind.Int64;
            case SpecialType.System_Boolean: return PartKind.Boolean;
            case SpecialType.System_DateTime: return PartKind.DateTime;
            case SpecialType.System_Nullable_T: return null;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return PartKind.Enum;
        }

        if (type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            switch (type.MetadataName)
            {
                case "Guid": return PartKind.Guid;
                case "DateTimeOffset": return PartKind.DateTimeOffset;
                case "DateOnly": return PartKind.DateOnly;
                case "TimeOnly": return PartKind.TimeOnly;
                case "TimeSpan": return PartKind.TimeSpan;
            }
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return null;
        }

        var isGrainKey = type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == GrainKeyAttributeName);
        return isGrainKey ? PartKind.Nested : null;
    }

    private static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
}
