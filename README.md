# Simple but powerful: Complex and strongly typed Orleans grain keys

> [!WARNING]
> This library is experimental. The API and the key format may still change in breaking ways, so be careful about storing keys or relying on it in production.

In Orleans grains are identified by grain keys. These can be strings, integers and guids. In addition, Orleans also supports "key" extensions to create two part component keys ex. 42 with a key extension of "foo".

I found that when working on complex multi tenant systems the grain key constraints were quite limiting. The grain keys can be strings so it is quite easy to create your own "composite keys" by encoding multiple values into the grain key string.

You have to do this when you want to get a grain you want to call and you have to decode it in the grain implementation to access the individual values of your composite key. I found that this very quickly became error prone, with a lot of repetitive code.

When my team started working on a new multi tenant SaaS platform we quickly realized that we needed a way to encode multiple values into grain keys and started working on a set of simple tools to accomplish this.

This repository contains a pretty complete set of tools that are very close to the implementation we ended up with. I would even say that I would probably go with this approach if I could and we might find time to refactor.

_There is a couple of pitfalls that you should consider. Take a look at the end of this readme for some of my thoughts._

## Defining a grain key

Mark a `partial record` (or `partial record struct`) with `[GrainKey]` and give it a name. Its positional parameters are the parts of the key:

```csharp
[GrainKey("user")]
public partial record UserGrainKey(Guid TenantId, int UserId);
```

That's all you write. A source generator, which ships in the package, generates the rest:

- `Format` and `Parse`, to encode the key as an Orleans string key and decode it again
- `TryParse` and `IParsable<UserGrainKey>`, so the key also works with anything that binds `IParsable` types, such as ASP.NET Core route parameters
- a `GetGrain<TGrainInterface>(UserGrainKey key)` extension on `IGrainFactory` (see [Using the keys with grains](#using-the-keys-with-grains))

The key is encoded as its name followed by its parts, in the order of the parameters. The key above is encoded as `user|29820855-a114-41ba-a5b7-f9b08fdd8774|92197999`.

The name makes keys of different types incompatible, even when they have the same parts: decoding `user|...` as an `OrderGrainKey` fails. Names must be unique, and can only contain letters, digits, `-`, `_` and `.`. Changing a name changes every encoded key of that type, so treat it like a database column name.

Keys don't convert to or from `string` implicitly, so a key can't silently be used where a plain string, or a key of another type, is expected.

A part can also be another `[GrainKey]` type. Its parts are written inline, without its name:

```csharp
[GrainKey("full-name")]
public partial record FullName(string First, string Last);

[GrainKey("customer")]
public partial record CustomerGrainKey(Guid TenantId, FullName Name); // customer|<tenant>|<first>|<last>
```

Mistakes in a key declaration show up as compile errors:

| Id | Reported when |
| --- | --- |
| `TGK001` | the key isn't `partial` |
| `TGK002` | the key is a class or struct rather than a record |
| `TGK003` | the key has no positional parameters |
| `TGK004` | the key is nested inside another type, generic or abstract |
| `TGK005` | a parameter's type isn't supported, or is nullable |
| `TGK009` | the key's name is empty or contains characters other than letters, digits, `-`, `_` and `.` |
| `TGK010` | two keys in the same project have the same name |

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
Invalid grain key 'user|nope|7' for UserGrainKey: part 1 'nope' is not a valid Guid.
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

Get a grain reference by passing the typed key to `GetGrain`, on any `IGrainFactory`, including `IClusterClient` and a grain's own `GrainFactory`:

```csharp
var userGrain = grainFactory.GetGrain<IUserGrain>(new UserGrainKey(tenantId, userId));
```

This is the generated extension, not Orleans' `GetGrain(string)`. It only accepts grain interfaces declared with that key type, so passing an `OrderGrainKey` for an `IUserGrain` is a compile error. The optional `grainClassNamePrefix` parameter works as in Orleans.

`IGrainWithTypedKey<TKey>` is still an `IGrainWithStringKey` underneath, so `GetGrain<IUserGrain>("some string")` compiles. The analyzer reports it as `TGK011`, since the string isn't checked until the grain decodes its key.

In the grain, declare the class `partial` and the generator adds a `Key` property of the grain's key type. The key is decoded the first time you use it and then cached:

```csharp
public partial class UserGrain : Grain, IUserGrain
{
    public Task<Response> Lock()
    {
        return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully locked"));
    }
}
```

Because nothing is inherited, this works with any base class: `Grain`, `Grain<TState>`, your own grain base classes, or POCO grains that implement `IGrainBase`. If a base class gets the generated `Key`, its derived grains use it.

Grain classes that aren't `partial` get no `Key` property, and can call the `GetTypedKey<TKey>()` extension instead, which decodes the key on every call. The generator reports these diagnostics for grains:

| Id | Severity | Reported when |
| --- | --- | --- |
| `TGK006` | Info | a grain with a typed key, or a type it's nested in, isn't `partial` |
| `TGK007` | Warning | a `partial` grain already has a member named `Key` |
| `TGK008` | Warning | a grain implements grain interfaces with different typed keys |
| `TGK011` | Warning | a string key is passed to `GetGrain` for a grain interface with a typed key |

## Writing a key by hand

The generator is built on two public types, `TypedGrainKeyWriter` and `TypedGrainKeyReader`. You can use them directly when you need full control, by implementing `ITypedGrainKey<TSelf>` yourself:

```csharp
public record UserGrainKey(Guid TenantId, int UserId) : ITypedGrainKey<UserGrainKey>
{
    public static string GrainKeyName => "user";

    public static string Format(UserGrainKey key)
    {
        return TypedGrainKeyWriter.Create<UserGrainKey>()
            .Write(key.TenantId)
            .Write(key.UserId)
            .Build();
    }

    public static UserGrainKey Parse(string key)
    {
        using var reader = TypedGrainKeyReader.Create<UserGrainKey>(key);

        return new UserGrainKey(reader.ReadGuid(), reader.ReadInt32());
    }
}
```

The writer and reader apply the same encoding as generated keys. `TypedGrainKeyWriter.Create<TKey>()` writes the key's name first, and `TypedGrainKeyReader.Create<TKey>()` checks and skips it, so you only handle the parts. Disposing the reader checks that every part was read. `TypedGrainKey.Format<TKey>`, `Parse<TKey>` and `TryParse<TKey>` work with any key.

A hand-written key doesn't get the generated `GetGrain` extension or the duplicate name check (`TGK010`). Use `grainFactory.GetGrain<IUserGrain>(UserGrainKey.Format(key))` instead.

## Things to consider when using this approach

While I think these tools are quite powerful and makes working with complex grain keys pretty easy there is a couple of things to consider.

Underneath, a typed key is still an Orleans string key. The generated `GetGrain` extension and the key names cover the common mistakes: passing the wrong key type doesn't compile, and a key of another type, or with the same parts but a different name, fails to decode. But nothing stops code from calling `GetGrain<IUserGrain>("foo-bar")` directly. The analyzer warns about it (`TGK011`), but if it happens anyway, the grain throws a `GrainKeyFormatException` when it decodes its key.

The name and the encoding are part of every grain's identity. Changing a key's name, or the order or types of its parameters, means existing grains, and any state or references stored under their keys, can no longer be found with the new key.

## Let me know what you think

Please feel free to reach out, I'm on the Orleans Discord at https://aka.ms/orleans-discord

You can also reach me on X at https://twitter.com/chrsparre