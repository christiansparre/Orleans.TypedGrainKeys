namespace Orleans.TypedGrainKeys.Tests.Keys;

/// <summary>A record struct key using every supported part type.</summary>
[GrainKey("all-types")]
public readonly partial record struct AllTypesGrainKey(
    string Text,
    Guid Id,
    int Number,
    long BigNumber,
    bool Flag,
    DateTime At,
    DateTimeOffset AtOffset,
    DateOnly Day,
    TimeOnly Time,
    TimeSpan Duration,
    TestEnum Choice,
    FullName Name);
