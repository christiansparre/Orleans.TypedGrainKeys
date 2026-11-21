namespace Orleans.TypedGrainKeys.Tests.Keys;

[GrainKey]
public partial record TestGrainKey(
    string One,
    int Two,
    long Three,
    DateTime Four,
    DateTimeOffset Five,
    Guid Six,
    TestEnum Seven,
    FullName Eight);
