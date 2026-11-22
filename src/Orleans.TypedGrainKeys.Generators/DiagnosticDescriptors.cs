using Microsoft.CodeAnalysis;

namespace Orleans.TypedGrainKeys.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "Orleans.TypedGrainKeys";

    public const string SupportedTypes =
        "string, Guid, int, long, bool, DateTime, DateTimeOffset, DateOnly, TimeOnly, TimeSpan, enums and other [GrainKey] types";

    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "TGK001",
        title: "Grain key must be partial",
        messageFormat: "Grain key '{0}' must be declared partial so its implementation can be generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotARecord = new(
        id: "TGK002",
        title: "Grain key must be a record",
        messageFormat: "Grain key '{0}' must be a record or record struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoParameters = new(
        id: "TGK003",
        title: "Grain key must have positional parameters",
        messageFormat: "Grain key '{0}' must declare its parts as positional parameters, for example 'record {0}(Guid TenantId, int UserId)'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedDeclaration = new(
        id: "TGK004",
        title: "Unsupported grain key declaration",
        messageFormat: "Grain key '{0}' must not be {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedPartType = new(
        id: "TGK005",
        title: "Unsupported grain key part type",
        messageFormat: "Parameter '{0}' of grain key '{1}' has type '{2}', which can't be part of a grain key. Supported types are " + SupportedTypes + ", and they must not be nullable.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GrainNotPartial = new(
        id: "TGK006",
        title: "Make the grain partial to generate its Key property",
        messageFormat: "Declare '{0}' partial to generate a Key property of type '{1}' for grain '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GrainKeyMemberExists = new(
        id: "TGK007",
        title: "Grain already has a Key member",
        messageFormat: "Grain '{0}' already has a member named 'Key', so no Key property of type '{1}' is generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GrainHasConflictingKeys = new(
        id: "TGK008",
        title: "Grain implements interfaces with different typed keys",
        messageFormat: "Grain '{0}' implements grain interfaces with different typed keys ({1}), so no Key property is generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidName = new(
        id: "TGK009",
        title: "Invalid grain key name",
        messageFormat: "'{0}' is not a valid name for grain key '{1}'. A name must be non-empty and only contain letters, digits, '-', '_' and '.'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateName = new(
        id: "TGK010",
        title: "Grain key name is used by another key",
        messageFormat: "Grain key '{0}' has the name '{1}', which is also used by {2}. Give each key a unique name so their keys can't be mixed up.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor StringKeyForTypedGrain = new(
        id: "TGK011",
        title: "Use the typed key to get a grain with a typed key",
        messageFormat: "'{0}' has a typed key. Pass a '{1}' instead of a string, so the key type is checked at compile time.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
