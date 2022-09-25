---
title: FSharp.SystemTextJson 1.0 released!
tags: [release, library, fsharp-systemtextjson]
---

More than three years after the first release, I am happy to announce [FSharp.SystemTextJson](https://github.com/tarmil/fsharp.systemtextjson) version 1.0!

FSharp.SystemTextJson is a library that provides support for F# types in .NET's standard [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-6-0).

Here is a summary of the new features in v1.0.

## `JsonName` attribute

System.Text.Json provides an attribute `JsonPropertyName` to change the name of a property in JSON.
In FSharp.SystemTextJson 1.0, the new attribute [`JsonName`](https://github.com/Tarmil/FSharp.SystemTextJson/blob/master/docs/Customizing.md#jsonname) is equivalent but provides more functionality:

* When used on a discriminated union case, `JsonName` can take a value of type `int` or `bool` instead of `string`.

    ```fsharp
    type MyUnion =
        | [<JsonName 1>] One of x: int
        | [<JsonName 2>] Two of y: string
    
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Default ||| JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))
    JsonSerializer.Serialize(Two "two", options)
    // => {"Case":2,"x":"two"}
    ```

* `JsonName` can take multiple values.
    When deserializing, all these values are treated as equivalent.
    When serializing, the first one is used.

    ```fsharp
    type Name =
        { [<JsonName("firstName", "first")>]
          First: string
          [<JsonName("lastName", "last")>]
          Last: string }

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    JsonSerializer.Deserialize<Name>("""{"first":"John","last":"Doe"}""", options)
    // => { First = "John"; Last = "Doe" }

    JsonSerializer.Serialize({ First = "John"; Last = "Doe" }, options)
    // => {"firstName":"John","lastName":"Doe"}
    ```

* `JsonName` has a settable property `Field: string`.
    It is used to set the JSON name of a union case field with the given name.

    ```fsharp
    type Contact =
        | [<JsonName("email", Field = "address")>]
          Email of address: string
        | Phone of number: string

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Default ||| JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))
    JsonSerializer.Serialize(Email "john.doe@example.com")
    // => {"Case":"Email","email":"john.doe@example.com"}
    ```

## Record properties

By default, FSharp.SystemTextJson only serializes the fields of a record.
There are now two ways to also serialize their properties:

* The option `includeRecordProperties: bool` enables serializing all record properties (except those that have the attribute `JsonIgnore`, just like fields).

    ```fsharp
    type User =
        { id: int
          name: string }

        member this.profileUrl = $"https://example.com/user/{this.id}/{this.name}"

        [<JsonIgnore>]
        member this.notIncluded = "This property is not included"

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter(includeRecordProperties = true))
    JsonSerializer.Serialize({ id = 1234; name = "john.doe" })
    // => {"id":1234,"name":"john.doe","profileUrl":"https://example.com/user/1234/john.doe"}
    ```

* The attribute `JsonInclude` can be used on a specific property to serialize it.

    ```fsharp
    type User =
        { id: int
          name: string }

        [<JsonInclude>]
        member this.profileUrl = $"https://example.com/user/{this.id}/{this.name}"

        member this.notIncluded = "This property is not included"

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    JsonSerializer.Serialize({ id = 1234; name = "john.doe" })
    // => {"id":1234,"name":"john.doe","profileUrl":"https://example.com/user/1234/john.doe"}
    ```

## BREAKING CHANGE: Missing fields

In FSharp.SystemTextJson 0.x, using default options, missing fields of type `option` or `voption` would be deserialized into `None` or `ValueNone`.
This was unintended behavior, which is corrected in version 1.0: these missing fields now throw an error.
To restore the previous behavior, either enable the option `IgnoreNullValues = true`, or or use the type [`Skippable`](https://github.com/Tarmil/FSharp.SystemTextJson/blob/master/docs/Format.md#skippable) instead of `option` or `voption`.

Additionally, the option `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` is now treated as a synonym for `IgnoreNullValues = true`.

```fsharp
type Name =
    { firstName: string
      lastName: string option }

let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter())
JsonSerializer.Deserialize<Name>("""{"firstName":"John"}""", options)
// => JsonException

let options2 = JsonSerializerOptions(IgnoreNullValues = true)
options2.Converters.Add(JsonFSharpConverter())
JsonSerializer.Deserialize<Name>("""{"firstName":"John"}""", options2)
// => { firstName = "John"; lastName = None }

let options3 = JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)
options3.Converters.Add(JsonFSharpConverter())
JsonSerializer.Deserialize<Name>("""{"firstName":"John"}""", options3)
// => { firstName = "John"; lastName = None }

type NameWithSkippable =
    { firstName: string
      lastName: Skippable<string> }

let options4 = JsonSerializerOptions()
options4.Converters.Add(JsonFSharpConverter())
JsonSerializer.Deserialize<Name>("""{"firstName":"John"}""", options4)
// => { firstName = "John"; lastName = Skip }
```

## Built-in support in .NET 6 and JsonFSharpTypes

In .NET 6, support has been added in System.Text.Json for a number of F# types.
This support is different from FSharp.SystemTextJson in a number of ways:

* Records, tuples, lists, sets, maps: `null` is accepted by the deserializer, and returns a null value.
* Records: missing fields are deserialized to default value instead of throwing an error.
* Maps: only primitive keys are supported. Numbers and booleans are converted to string and used as JSON objet keys.
* Tuples: only supports up to 8 items, and serializes it as a JSON object with keys "Item1", "Item2", etc.
* Discriminated unions, struct tuples: not supported.

FSharp.SystemTextJson takes over the serialization of these types by default; but the option [`types: JsonFSharpTypes`](https://github.com/Tarmil/FSharp.SystemTextJson/blob/master/docs/Customizing.md#types) allows customizing which types should be serialized by `FSharp.SystemTextJson`, and which types should be left to `System.Text.Json`.

```fsharp
let options = JsonSerializerOptions()
// Only use FSharp.SystemTextJson for records and unions:
options.Converters.Add(JsonFSharpOptions(types = (JsonFSharpTypes.Records ||| JsonFSharpTypes.Unions)))

JsonSerializer.Serialize(Map [(1, "one"); (2, "two")], options)
// => {"1":"one","2":"two"}
// whereas FSharp.SystemTextJson would have serialized as:
// => [[1,"one"],[2,"two"]]
```

Happy coding!
