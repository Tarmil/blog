---
title: FSharp.SystemTextJson 1.2 released!
tags: [release, library, fsharp-systemtextjson]
---

I am happy to announce [FSharp.SystemTextJson](https://github.com/tarmil/fsharp.systemtextjson) version 1.2!

FSharp.SystemTextJson is a library that provides support for F# types in .NET's standard [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-6-0).

Here is a summary of the new features in v1.2.

## Skippable Option fields ([#154](https://github.com/Tarmil/FSharp.SystemTextJson/issues/154))

Version 1.1 introduced the method `JsonFSharpOptions.WithSkippableOptionFields(?bool)` that, when set to true, causes `None` and `ValueNone` to always be skipped when serialized as the value of a record or union field.
However, even with this option left as false, `None` and `ValueNone` were still skipped if the `JsonSerializerOptions` have `DefaultIgnoreCondition` set to `WhenWritingNull`.

In version 1.2, a new overload of `JsonFSharpOptions.WithSkippableOptionFields` takes an enum as argument that brings more possibilities.

* `SkippableOptionFields.Always` is equivalent to `true`: record and union fields equal to `None` and `ValueNone` are always skipped.

* `SkippableOptionFields.FromJsonSerializerOptions` is equivalent to `false`: record and union fields equal to `None` and `ValueNone` are only skipped if `JsonSerializerOptions` have `DefaultIgnoreCondition` set to `WhenWritingNull`.
    Otherwise, they are serialized as JSON `null`.

* `SkippableOptionFields.Never` is new: `None` and `ValueNone` are never skipped, and always serialized as JSON `null`.

## Handling of dictionary and map keys ([#161](https://github.com/Tarmil/FSharp.SystemTextJson/issues/161) and [#162](https://github.com/Tarmil/FSharp.SystemTextJson/issues/162))

In version 1.2, FSharp.SystemTextJson now makes use of System.Text.Json's `ReadAsPropertyName` and `WriteAsPropertyName` features.
This manifests in two ways:

* Single-case unions can now be used as keys in a standard `Dictionary` (and related types).  
    **NOTE**: This requires System.Text.Json 8.0.

    ```fsharp
    let options = JsonFSharpOptions().ToJsonSerializerOptions()

    type CountryCode = CountryCode of string

    let countries = dict [
        CountryCode "us", "United States"
        CountryCode "fr", "France"
        CountryCode "gb", "United Kingdom"
    ]

    JsonSerializer.Serialize(countries, options)
    // --> {"us":"United States","fr":"France","gb":"United Kingdom"}
    ```

* The format for maps can now be customized using `JsonFSharpOptions.WithMapFormat(MapFormat)`.

    * `MapFormat.Object` always serializes maps as objects.
        The key type must be supported as key for dictionaries.  
        **NOTE**: This requires System.Text.Json 8.0.

        ```fsharp
        let options = JsonFSharpOptions().WithMapFormat(MapFormat.Object).ToJsonSerializerOptions()

        let countries = Map [
            Guid.NewGuid(), "United States"
            Guid.NewGuid(), "France"
            Guid.NewGuid(), "United Kingdom"
        ]

        JsonSerializer.Serialize(countries, options)
        // --> {"44e2a549-66c6-4515-970a-a1e85ce42624":"United States", ...
        ```

    * `MapFormat.ArrayOfPairs` always serializes maps as JSON arrays whose items are `[key,value]` pairs.

        ```fsharp
        let options = JsonFSharpOptions().WithMapFormat(MapFormat.Object).ToJsonSerializerOptions()

        let countries = Map [
            "us", "United States"
            "fr", "France"
            "uk", "United Kingdom"
        ]

        JsonSerializer.Serialize(countries, options)
        // --> [["us","United States"],["fr","France"],["uk","United Kingdom"]]
        ```

    * `MapFormat.ObjectOrArrayOfPairs` is the default, and the same behavior as v1.1.
        Maps whose keys are string or single-case unions wrapping string are serialized as JSON objects, and other maps are serialized as JSON arrays whose items are `[key,value]` pairs.

## Other improvements

* [#158](https://github.com/Tarmil/FSharp.SystemTextJson/issues/158): Throw an exception when trying to deserialize `null` into a record or union in any context, rather than only when they are in fields of records and unions.

* [#163](https://github.com/Tarmil/FSharp.SystemTextJson/issues/163): Add `StructuralComparison` to the type `Skippable<_>`. This allows using it with types that are themselves marked with `StructuralComparison`.

## Bug fixes

* [#160](https://github.com/Tarmil/FSharp.SystemTextJson/issues/160): Fix `WithSkippableOptionFields(false)` not working for `voption`.

* [#164](https://github.com/Tarmil/FSharp.SystemTextJson/issues/164): When deserializing a record with `JsonIgnoreCondition.WhenWritingNull`, when a non-nullable field is missing, throw a proper `JsonException` that includes the name of the field, rather than a `NullReferenceException`.

Happy coding!
