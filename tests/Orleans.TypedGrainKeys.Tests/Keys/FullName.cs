namespace Orleans.TypedGrainKeys.Tests.Keys;

[GrainKey("full-name")]
public partial record FullName(string First, string Last);
