# Simple but powerful: Complex and strongly typed Orleans grain keys

In Orleans grains are identified by grain keys. These can be strings, integers and guids. In addition, Orleans also supports "key" extensions to create two part component keys ex. 42 with a key extension of "foo".

I found that when working on complex multi tenant systems the grain key constraints were quite limiting. The grain keys can be strings so it is quite easy to create your own "composite keys" by encoding multiple values into the grain key string.

You have to do this when you want to get a grain you want to call and you have to decode it in the grain implementation to access the individual values of your composite key. I found that this very quickly became error prone, with a lot of repetitive code.

When my team started working on a new multi tenant SaaS platform we quickly realized that we needed a way to encode multiple values into grain keys and started working on a set of simple tools to accomplish this.

This repository contains a pretty complete set of tools that are very close to the implementation we ended up with. I would even say that I would probably go with this approach if I could and we might find time to refactor.

_There is a couple of pitfalls that you should consider. Take a look at the end of this readme for some of my thoughts._

## Defining a grain key

Mark a `partial record` (or `partial record struct`) with `[GrainKey]`. Its positional parameters are the parts of the key:

```csharp
[GrainKey]
public partial record UserGrainKey(Guid TenantId, int UserId);
```

That's all you write. A source generator, which ships in the package, generates the rest:

- implicit conversions to and from `string`, so the key works directly with Orleans' string key APIs
- `Parse`, `TryParse` and `IParsable<UserGrainKey>`, so the key also works with anything that binds `IParsable` types, such as ASP.NET Core route parameters

The parts are written in the order of the parameters. The key above is encoded as `29820855-a114-41ba-a5b7-f9b08fdd8774|92197999`.

A part can also be another `[GrainKey]` type. Its parts are written inline:

```csharp
[GrainKey]
public partial record FullName(string First, string Last);

[GrainKey]
public partial record CustomerGrainKey(Guid TenantId, FullName Name); // <tenant>|<first>|<last>
```

Mistakes show up as compile errors:

| Id | Reported when |
| --- | --- |
| `TGK001` | the key isn't `partial` |
| `TGK002` | the key is a class or struct rather than a record |
| `TGK003` | the key has no positional parameters |
| `TGK004` | the key is nested inside another type, generic or abstract |
| `TGK005` | a parameter's type isn't supported, or is nullable |

### Supported types and canonical forms

| Type | Encoded as |
| --- | --- |
| `string` | as is, with `\|` and `\` escaped by a leading `\` |
| `Guid` | `D` format, lowercase: `29820855-a114-41ba-a5b7-f9b08fdd8774` |
| `int`, `long` | invariant culture: `-42` |
| `bool` | `true` / `false` |
| `DateTime`, `DateTimeOffset` | round-trip `O` format; `DateTime.Kind` is preserved |
| `DateOnly`, `TimeOnly` | `O` format: `2024-02-29`, `23:59:59.9990000` |
| `TimeSpan` | `c` format: `3.04:05:06.0070000` |
| enums | member name: `Baz` |
| other `[GrainKey]` types | their parts, inline |

Parts are separated by `|`. If a string part contains `|` or `\`, it is escaped with a `\` (`a|b` is written as `a\|b`), so any string value is safe to use. Empty strings are kept as empty parts. Every value is written and read with the invariant culture, so keys don't change with the server's culture settings.

Decoding only accepts these exact forms. In Orleans a grain's identity is its key string, so `007` and `7`, or an uppercase and a lowercase `Guid`, would be two different grains with the same typed key. Anything that isn't in the form the encoder would produce is rejected.

### Errors

A key that can't be decoded throws a `GrainKeyFormatException` (a `FormatException`). It has the raw `Key`, the `KeyType`, the `PartIndex` that failed, and the original parsing error as its `InnerException`:

```
Invalid grain key 'nope' for UserGrainKey: part 0 'nope' is not a valid Guid.
```

To check a key without catching exceptions, use `TryParse`:

```csharp
if (UserGrainKey.TryParse(primaryKey, out var key))
{
    // use key.TenantId, key.UserId
}
```

## Using the keys with grains

Give the grain interface the key type with `IGrainWithTypedKey<TKey>`:

```csharp
public interface IUserGrain : IGrainWithTypedKey<UserGrainKey>
{
    Task<Response> Lock();
}
```

`IGrainWithTypedKey<TKey>` is an `IGrainWithStringKey`, so you get grain references with the normal Orleans APIs and pass the typed key:

```csharp
var userGrain = grainFactory.GetGrain<IUserGrain>(new UserGrainKey(tenantId, userId));
```

In the grain, derive from `TypedKeyGrain<TKey>` to get a `Key` property. The key is decoded the first time you use it and then cached:

```csharp
public class UserGrain : TypedKeyGrain<UserGrainKey>, IUserGrain
{
    public Task<Response> Lock()
    {
        return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully locked"));
    }
}
```

Grains that don't derive from `TypedKeyGrain<TKey>`, such as POCO grains that implement `IGrainBase`, can use the `GetTypedKey<TKey>()` extension instead. It decodes the key on every call.

## Writing a key by hand

The generator is built on two public types, `TypedGrainKeyWriter` and `TypedGrainKeyReader`. You can use them directly when you need full control, by implementing `ITypedGrainKey<TSelf>` yourself:

```csharp
public record UserGrainKey(Guid TenantId, int UserId) : ITypedGrainKey<UserGrainKey>
{
    public static implicit operator string(UserGrainKey self)
    {
        return TypedGrainKeyWriter.Create()
            .Write(self.TenantId)
            .Write(self.UserId);
    }

    public static implicit operator UserGrainKey(string self)
    {
        using var reader = TypedGrainKeyReader.Create<UserGrainKey>(self);

        return new UserGrainKey(reader.ReadGuid(), reader.ReadInt32());
    }
}
```

The writer and reader apply the same encoding as generated keys. Passing the key type to `Create<TKey>` is optional, but it makes error messages name the key. Disposing the reader checks that every part was read. For hand-written keys, `TypedGrainKey.Parse<TKey>` and `TypedGrainKey.TryParse<TKey>` provide the parse helpers.

## Things to consider when using this approach

While I think these tools are quite powerful and makes working with complex grain keys pretty easy there is a couple of things to consider.

Because we are just using the Orleans API's already in place like `IGrainFactory.GetGrain<>(string primaryKey)` it is completely possible to request a grain reference using the primary key `foo-bar` when the intent was to use the `UserGrainKey`, resulting in a runtime exception when trying to decode the grain key in the grain implementation.

Grain keys using the same number of parameters and types will also be interchangeable allowing you to activate grains with an unintended grain key.

While this is definitely something to consider, it would be not different than using pure string primary keys or key extensions.

A solution could be to encode the grain key type with the encoded payload or maybe having a `ITypedGrainKeyClient` and doing some runtime checking that grain keys match. Even cooler you could probably generate individual `GetGrain<TGrain>(TGrainKey key)` methods.

## Let me know what you think

Please feel free to reach out, I'm on the Orleans Discord at https://aka.ms/orleans-discord

You can also reach me on X at https://twitter.com/chrsparre