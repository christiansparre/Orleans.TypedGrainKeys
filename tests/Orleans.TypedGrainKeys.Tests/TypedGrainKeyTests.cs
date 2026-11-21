using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

public class TypedGrainKeyTests
{
    private const string Encoded = "test|1|2|9223372036854775807|1992-06-26T00:00:00.0000000|1994-06-12T14:00:00.0000000+01:00|a8dba878-3fb1-49e5-9235-b331336db282|Baz|John|McClane";

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
        await Assert.That(TestGrainKey.Format(Decoded)).IsEqualTo(Encoded);
    }

    [Test]
    public async Task Can_Read_TypedGrainKey()
    {
        await Assert.That(TestGrainKey.Parse(Encoded)).IsEqualTo(Decoded);
    }

    [Test]
    public async Task Static_Helpers_Use_The_Key_Implementation()
    {
        await Assert.That(TypedGrainKey.Format(Decoded)).IsEqualTo(Encoded);
        await Assert.That(TypedGrainKey.Parse<TestGrainKey>(Encoded)).IsEqualTo(Decoded);
    }

    [Test]
    public async Task Throws_When_Not_All_Parts_Are_Read()
    {
        var encoded = IncorrectReadTestGrainKey.Format(new IncorrectReadTestGrainKey("One", 2, 3));

        await Assert.That(() => IncorrectReadTestGrainKey.Parse(encoded)).Throws<GrainKeyFormatException>();
    }

    [Test]
    public async Task Keys_Start_With_Their_Name()
    {
        var key = new UserGrainKey(Guid.Parse("a8dba878-3fb1-49e5-9235-b331336db282"), 7);

        await Assert.That(UserGrainKey.GrainKeyName).IsEqualTo("user");
        await Assert.That(UserGrainKey.Format(key)).IsEqualTo("user|a8dba878-3fb1-49e5-9235-b331336db282|7");
    }

    [Test]
    public async Task A_Key_With_Another_Name_Is_Rejected()
    {
        // Same shape as a UserGrainKey (Guid, then a string that happens to be a number), but a different name.
        var other = HandWrittenGrainKey.Format(new HandWrittenGrainKey(Guid.NewGuid(), "7"));

        var exception = await Assert.That(() => UserGrainKey.Parse(other)).Throws<GrainKeyFormatException>();

        await Assert.That(exception!.PartIndex).IsEqualTo(0);
        await Assert.That(exception.Reason).IsEqualTo("it starts with 'hand-written' instead of the key name 'user'");
    }

    [Test]
    public async Task A_Key_Without_A_Name_Is_Rejected()
    {
        await Assert.That(() => UserGrainKey.Parse("a8dba878-3fb1-49e5-9235-b331336db282|7")).Throws<GrainKeyFormatException>();
    }

    [Test]
    public async Task An_Invalid_Hand_Written_Name_Is_Rejected()
    {
        await Assert.That(() => InvalidNameGrainKey.Format(new InvalidNameGrainKey(1))).Throws<InvalidOperationException>();
        await Assert.That(() => InvalidNameGrainKey.Parse("bad|name|1")).Throws<InvalidOperationException>();
    }
}
