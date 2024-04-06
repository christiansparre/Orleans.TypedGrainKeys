namespace Orleans.TypedGrainKeys.Tests.Grains;

[GenerateSerializer]
public record struct Response(string Message);