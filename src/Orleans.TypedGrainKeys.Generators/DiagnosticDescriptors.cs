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
}
