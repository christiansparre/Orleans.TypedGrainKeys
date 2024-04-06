# Simple but powerful: Complex and strongly typed Orleans grain keys

In Orleans grains are identified by grain keys. These can be strings, integers and guids. In addition, Orleans also supports "key" extensions to create two part component keys ex. 42 with a key extension of "foo".

I found that when working on complex multi tenant systems the grain key constraints were quite limiting. The grain keys can be strings so it is quite easy to create your own "composite keys" by encoding multiple values into the grain key string.

You have to do this when you want to get a grain you want to call and you have to decode it in the grain implementation to access the individual values of your composite key. I found that this very quickly became error prone, with a lot of repetitive code.

When my team started working on a new multi tenant SaaS platform we quickly realized that we needed a way to encode multiple values into grain keys and started working on a set of simple tools to accomplish this.

This repository contains a pretty complete set of tools that are very close to the implementation we ended up with. I would even say that I would probably go with this approach if I could and we might find time to refactor.

_There is a couple of pitfalls that you should consider. Take a look at the end of this readme for some of my thoughts._

## The `ITypedGrainKey` interface

The following interface defines the strongly typed grain key and is very simple. It just requires you to implement two implicit operators for encoding and decoding the grain keys.

```csharp
public interface ITypedGrainKey<TSelf> where TSelf : ITypedGrainKey<TSelf>
{
    static abstract implicit operator string(TSelf self);
    static abstract implicit operator TSelf(string self);
}
```

An example of a `UserGrainKey` could look something like this.

```csharp
public record UserGrainKey(Guid TenantId, int UserId) : ITypedGrainKey<UserGrainKey>
{
    ...
}
```

## The `TypedGrainKeyWriter` and `TypedGrainKeyReader` helper types

The `writer` and `reader` types provides a convenient way to write and read grain keys. They help make sure types are encoded and decoded the same way and will help ensure that ex. the whole key was read.

Implementing the implicit `string` operator would look something like this:

```csharp
public static implicit operator string(UserGrainKey self)
{
    return TypedGrainKeyWriter.Create()
        .Write(self.TenantId)
        .Write(self.UserId);
}
```

The encoded grain key in this example would end up being something like this `29820855-a114-41ba-a5b7-f9b08fdd8774|92197999`

I this case the delimiter is `|`. You would need to determine the appropriate delimiter for your use case and choosing a more complex delimiter might be required if the values you might need could contain the delimiter.

The example in this repository just has the delimiter as a constant. But it would be easy to allow the `writer` and `reader` to be configured with differentiating delimiters. 

"Decoding" the grain key would look like this:

```csharp

public static implicit operator UserGrainKey(string self)
{
    using var reader = TypedGrainKeyReader.Create(self);

    return new UserGrainKey(reader.ReadGuid(), reader.ReadInt32());
}
```

You choose which `Write()` and `Read*()` methods you want. It can provide a good place to signal which types are intended to be used as part of a grain key and ensure that the values are encoded they way you want.

You might not want a `DateTimeOffset` like this one:

```csharp
public TypedGrainKeyWriter Write(DateTimeOffset part) => Write(part.ToString("O"));
```

But maybe one that writes `DateOnly` in a particular way:

```csharp
public TypedGrainKeyWriter Write(DateOnly part) => Write(part.ToString("yyyy-MM-dd"));
```

## How to use the grain keys

In Orleans you get references to grains by using the `IClusterClient` and `IGrainFactory` interfaces. Because the writing and reading of grain keys are implemented by implicit operators it becomes quite easy to use with the existing Orleans API's.

```csharp
var userGrain = IGrainFactory.GetGrain<IUserGrain>(new UserGrainKey(Guid.Parse("29820855-a114-41ba-a5b7-f9b08fdd8774"), 92197999));
```

In the grain implementation it is also pretty easy to get a hold of the typed grain key. A simple extension method on the `IAddressable` interface provides an easy way.

```csharp
public static TKey GetTypedKey<TKey>(this IAddressable addressable) where TKey: ITypedGrainKey<TKey>
{
    return addressable.GetPrimaryKeyString();
}
```

You could take it a step further and define a `GrainBase<TKey>` type and allow that to provide a helpful `Key` property.

```csharp
public abstract class GrainBase<TKey> : IGrainBase, IGrainWithTypedKey<TKey> where TKey : ITypedGrainKey<TKey>
{
    ...
    public TKey Key => this.GetPrimaryKeyString();
    ...
}
```
_See `GrainBase.cs` in the repository for the full implementation_.

You would then be able to use the `Key` property very conveniently like so:

```csharp
public Task<Response> Lock()
{
    return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully locked"));
}
```

The `IGrainWithTypedKey<TKey>` is not technically required, we could just use `IGrainWithStringKey`, but it offers a show of intent and would allow us to maybe do some cool code generation.

## Things to consider when using this approach

While I think these tools are quite powerful and makes working with complex grain keys pretty easy there is a couple of things to consider.

Because we are just using the Orleans API's already in place like `IGrainFactory.GetGrain<>(string primaryKey)` it is completely possible to request a grain reference using the primary key `foo-bar` when the intent was to use the `UserGrainKey`, resulting in a runtime exception when trying to decode the grain key in the grain implementation.

Grain keys using the same number of parameters and types will also be interchangeable allowing you to activate grains with an unintended grain key.

While this is definitely something to consider, it would be not different than using pure string primary keys or key extensions.

A solution could be to encode the grain key type with the encoded payload or maybe having a `ITypedGrainKeyClient` and doing some runtime checking that grain keys match. Even cooler you could probably generate individual `GetGrain<TGrain>(TGrainKey key)` methods.

## Let me know what you think

Please feel free to reach out, I'm on the Orleans Discord at https://aka.ms/orleans-discord

You can also reach me on X at https://twitter.com/chrsparre