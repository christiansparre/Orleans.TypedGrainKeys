using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

public class TypedGrainKeyTests
{
    private const string Encoded = "1|2|9223372036854775807|1992-06-26T00:00:00.0000000|1994-06-12T14:00:00.0000000+01:00|a8dba878-3fb1-49e5-9235-b331336db282|Baz|John|McClane";

    private static readonly TestGrainKey Decoded = new(
        One: "1",
        Two: 2,
        Three: long.MaxValue,
        Four: new DateTime(1992, 6, 26),
        Five: new DateTimeOffset(1994, 6, 12, 14, 00, 00, TimeSpan.FromHours(1)),
        Six: Guid.Parse("a8dba878-3fb1-49e5-9235-b331336db282"),
        Seven: TestEnum.Baz,
        Eight: new FullName("John", "McClane"));

    [Test]
    public async Task Can_Write_TypedGrainKey()
    {
        string encoded = Decoded;

        await Assert.That(encoded).IsEqualTo(Encoded);
    }

    [Test]
    public async Task Can_Read_TypedGrainKey()
    {
        TestGrainKey grainKey = Encoded;

        await Assert.That(grainKey).IsEqualTo(Decoded);
    }

    [Test]
    public async Task Throws_When_Not_All_Parts_Are_Read()
    {
        string encoded = new IncorrectReadTestGrainKey("One", 2, 3);

        await Assert.That(() => (IncorrectReadTestGrainKey)encoded).Throws<GrainKeyFormatException>();
    }
}
