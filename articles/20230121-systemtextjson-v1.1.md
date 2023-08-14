---
title: FSharp.SystemTextJson 1.1 released!
tags: [release, library, fsharp-systemtextjson]
---

I am happy to announce [FSharp.SystemTextJson](https://github.com/tarmil/fsharp.systemtextjson) version 1.1!

FSharp.SystemTextJson is a library that provides support for F# types in .NET's standard [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-6-0).

Here is a summary of the new features in v1.1.

## Fluent configuration

The library now comes with a new syntax for configuration.
Instead of a constructor with a mix of optional arguments and enum flags, you can now use a more consistent fluent syntax.

* The baseline options are declared using one of these static methods on the `JsonFSharpOptions` type: `Default()`, `NewtonsoftLike()`, `ThothLike()` or `FSharpLuLike()`.
* Then, fluent instance methods set various options and return a new instance of `JsonFSharpOptions`.
* Finally, a converter with the given options can be either added to an existing `JsonSerializerOptions` with the method `AddToJsonSerializerOptions()`, or to a new one with `ToJsonSerializerOptions()`.

For example:

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithUnionInternalTag()
        .WithUnionNamedFields()
        .WithUnionTagName("type")
        .ToJsonSerializerOptions()

type SomeUnion =
    | SomeUnionCase of x: int * y: string

JsonSerializer.Serialize(SomeUnionCase (1, "test"), options)
// --> {"type":"SomeUnionCase","x":1,"y":"test"}
```

Note that in the future, newly added options will be available via the fluent configuration, but they may not always be added to the constructor syntax; especially because this can break binary compatibility (see [this issue](https://github.com/Tarmil/FSharp.SystemTextJson/issues/132)).

## Skippable option fields

In version 1.0, the default behavior for fields of type `option` and `voption` changed: they are no longer serialized as a present or missing field, and instead as a null field.

While the pre-1.0 behavior can be recovered by using the `JsonSerializerOptions` property `DefaultIgnoreCondition`, this has other side-effects and multiple users have asked for a cleaner way to use options for missing fields.

This is now possible with the option `SkippableOptionFields`. This is the first option that is only available via fluent configuration, and not as a `JsonFSharpConverter` constructor argument.

```fsharp
let options =
    JsonFSharpOptions.Default()
        .WithSkippableOptionFields()
        .ToJsonSerializerOptions()

JsonSerializer.Serialize({| x = Some 42; y = None |}, options)
// --> {"x":42}
```

Happy coding!
