using System.Globalization;
using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

public class GrainKeyFormatTests
{
    [Test]
    public async Task Escapes_Delimiter_And_Escape_Characters()
    {
        string encoded = TypedGrainKeyWriter.Create().Write(@"a|b\c").Write("d");

        await Assert.That(encoded).IsEqualTo(@"a\|b\\c|d");

        using var reader = TypedGrainKeyReader.Create(encoded);
        await Assert.That(reader.ReadString()).IsEqualTo(@"a|b\c");
        await Assert.That(reader.ReadString()).IsEqualTo("d");
    }

    [Test]
    public async Task Preserves_Empty_Parts()
    {
        string encoded = TypedGrainKeyWriter.Create().Write("").Write("x").Write("");

        await Assert.That(encoded).IsEqualTo("|x|");

        using var reader = TypedGrainKeyReader.Create(encoded);
        await Assert.That(reader.PartCount).IsEqualTo(3);
        await Assert.That(reader.ReadString()).IsEqualTo("");
        await Assert.That(reader.ReadString()).IsEqualTo("x");
        await Assert.That(reader.ReadString()).IsEqualTo("");
    }

    [Test]
    public async Task Single_Empty_String_Round_Trips()
    {
        string encoded = TypedGrainKeyWriter.Create().Write("");

        using var reader = TypedGrainKeyReader.Create(encoded);
        await Assert.That(reader.ReadString()).IsEqualTo("");
    }

    [Test]
    public async Task Writer_Without_Parts_Throws()
    {
        await Assert.That(() => TypedGrainKeyWriter.Create().Build()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Writer_Rejects_Null_String()
    {
        await Assert.That(() => TypedGrainKeyWriter.Create().Write((string)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Round_Trips_All_Supported_Types()
    {
        var guid = Guid.NewGuid();
        var dateTime = new DateTime(2024, 2, 29, 13, 45, 10, 123, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(2024, 2, 29, 13, 45, 10, TimeSpan.FromHours(-5));
        var dateOnly = new DateOnly(2024, 2, 29);
        var timeOnly = new TimeOnly(23, 59, 59, 999);
        var timeSpan = new TimeSpan(3, 4, 5, 6, 7);

        string encoded = TypedGrainKeyWriter.Create()
            .Write("text")
            .Write(guid)
            .Write(int.MinValue)
            .Write(long.MaxValue)
            .Write(true)
            .Write(dateTime)
            .Write(dateTimeOffset)
            .Write(dateOnly)
            .Write(timeOnly)
            .Write(timeSpan)
            .Write(TestEnum.Bar);

        using var reader = TypedGrainKeyReader.Create(encoded);
        await Assert.That(reader.ReadString()).IsEqualTo("text");
        await Assert.That(reader.ReadGuid()).IsEqualTo(guid);
        await Assert.That(reader.ReadInt32()).IsEqualTo(int.MinValue);
        await Assert.That(reader.ReadInt64()).IsEqualTo(long.MaxValue);
        await Assert.That(reader.ReadBoolean()).IsTrue();
        await Assert.That(reader.ReadDateTime()).IsEqualTo(dateTime);
        await Assert.That(reader.ReadDateTimeOffset()).IsEqualTo(dateTimeOffset);
        await Assert.That(reader.ReadDateOnly()).IsEqualTo(dateOnly);
        await Assert.That(reader.ReadTimeOnly()).IsEqualTo(timeOnly);
        await Assert.That(reader.ReadTimeSpan()).IsEqualTo(timeSpan);
        await Assert.That(reader.ReadEnum<TestEnum>()).IsEqualTo(TestEnum.Bar);
    }

    [Test]
    [Arguments(DateTimeKind.Utc)]
    [Arguments(DateTimeKind.Local)]
    [Arguments(DateTimeKind.Unspecified)]
    public async Task Preserves_DateTime_Kind(DateTimeKind kind)
    {
        var value = new DateTime(2024, 6, 1, 12, 0, 0, kind);

        string encoded = TypedGrainKeyWriter.Create().Write(value);

        using var reader = TypedGrainKeyReader.Create(encoded);
        var read = reader.ReadDateTime();
        await Assert.That(read).IsEqualTo(value);
        await Assert.That(read.Kind).IsEqualTo(kind);
    }

    [Test]
    public async Task Formats_And_Parses_Using_The_Invariant_Culture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";

        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            string encoded = TypedGrainKeyWriter.Create().Write(-5).Write(-5L);

            await Assert.That(encoded).IsEqualTo("-5|-5");

            using var reader = TypedGrainKeyReader.Create(encoded);
            await Assert.That(reader.ReadInt32()).IsEqualTo(-5);
            await Assert.That(reader.ReadInt64()).IsEqualTo(-5L);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public async Task Reading_Past_The_End_Throws()
    {
        var exception = Capture(() =>
        {
            using var reader = TypedGrainKeyReader.Create<UserGrainKey>("a");
            reader.ReadString();
            reader.ReadString();
        });

        await Assert.That(exception.PartIndex).IsEqualTo(1);
        await Assert.That(exception.KeyType).IsEqualTo(typeof(UserGrainKey));
        await Assert.That(exception.Reason).IsEqualTo("expected at least 2 parts but it has 1");
    }

    [Test]
    [Arguments("abc", typeof(FormatException))]
    [Arguments("99999999999", typeof(OverflowException))]
    public async Task Invalid_Value_Throws_With_Inner_Exception(string part, Type innerExceptionType)
    {
        var exception = Capture(() =>
        {
            using var reader = TypedGrainKeyReader.Create(part);
            reader.ReadInt32();
        });

        await Assert.That(exception.PartIndex).IsEqualTo(0);
        await Assert.That(exception.InnerException?.GetType()).IsEqualTo(innerExceptionType);
    }

    [Test]
    [Arguments("007")]
    [Arguments("+7")]
    [Arguments("-0")]
    [Arguments(" 7")]
    public async Task Non_Canonical_Int32_Throws(string part)
    {
        await AssertReadFails(part, reader => reader.ReadInt32());
    }

    [Test]
    [Arguments("A8DBA878-3FB1-49E5-9235-B331336DB282")]
    [Arguments("a8dba8783fb149e59235b331336db282")]
    [Arguments("{a8dba878-3fb1-49e5-9235-b331336db282}")]
    public async Task Non_Canonical_Guid_Throws(string part)
    {
        await AssertReadFails(part, reader => reader.ReadGuid());
    }

    [Test]
    [Arguments("baz")]
    [Arguments("2")]
    public async Task Non_Canonical_Enum_Throws(string part)
    {
        await AssertReadFails(part, reader => reader.ReadEnum<TestEnum>());
    }

    [Test]
    [Arguments("True")]
    [Arguments("1")]
    public async Task Non_Canonical_Boolean_Throws(string part)
    {
        await AssertReadFails(part, reader => reader.ReadBoolean());
    }

    [Test]
    [Arguments(@"a\")]
    [Arguments(@"a\x|b")]
    public async Task Invalid_Escape_Sequence_Throws(string key)
    {
        await Assert.That(() => TypedGrainKeyReader.Create(key)).Throws<GrainKeyFormatException>();
    }

    [Test]
    public async Task Dispose_With_Unread_Parts_Throws()
    {
        var exception = Capture(() =>
        {
            using var reader = TypedGrainKeyReader.Create("a|b");
            reader.ReadString();
        });

        await Assert.That(exception.Reason).IsEqualTo("it has 2 parts but only 1 were read");
    }

    [Test]
    public async Task Dispose_After_A_Failed_Read_Does_Not_Mask_The_Failure()
    {
        var exception = Capture(() =>
        {
            using var reader = TypedGrainKeyReader.Create("x|y");
            reader.ReadInt32();
        });

        await Assert.That(exception.Reason).IsEqualTo("part 0 'x' is not a valid Int32");
    }

    [Test]
    public async Task Error_Message_Names_The_Key_Type()
    {
        var exception = Capture(() => _ = (UserGrainKey)"nope");

        await Assert.That(exception.Key).IsEqualTo("nope");
        await Assert.That(exception.Message).Contains("UserGrainKey");
    }

    [Test]
    public async Task TryParse_Returns_The_Key_When_Valid()
    {
        var key = new UserGrainKey(Guid.NewGuid(), 42);

        var success = TypedGrainKey.TryParse<UserGrainKey>(key, out var parsed);

        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsEqualTo(key);
    }

    [Test]
    [Arguments("nope")]
    [Arguments("")]
    [Arguments(null)]
    public async Task TryParse_Returns_False_When_Invalid(string? encoded)
    {
        var success = TypedGrainKey.TryParse<UserGrainKey>(encoded, out var parsed);

        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task Typed_Key_With_Delimiters_And_Empty_Strings_Round_Trips()
    {
        var key = new TestGrainKey(
            One: "a|b",
            Two: -2,
            Three: 3,
            Four: new DateTime(2000, 1, 1),
            Five: new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Six: Guid.NewGuid(),
            Seven: TestEnum.Foo,
            Eight: new FullName("", @"Mc\Clane"));

        string encoded = key;
        TestGrainKey decoded = encoded;

        await Assert.That(decoded).IsEqualTo(key);
    }

    private static async Task AssertReadFails(string part, Action<TypedGrainKeyReader> read)
    {
        var exception = Capture(() =>
        {
            using var reader = TypedGrainKeyReader.Create(part);
            read(reader);
        });

        await Assert.That(exception.PartIndex).IsEqualTo(0);
    }

    private static GrainKeyFormatException Capture(Action action)
    {
        try
        {
            action();
        }
        catch (GrainKeyFormatException exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected a {nameof(GrainKeyFormatException)} to be thrown.");
    }
}
