namespace Orleans.TypedGrainKeys.Tests.Keys;

[GrainKey]
public partial record FullName(string First, string Last);
