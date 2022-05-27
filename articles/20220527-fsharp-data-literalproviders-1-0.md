---
title: FSharp.Data.LiteralProviders 1.0 is here!
tags: [release, library, literalproviders]
---

I am happy to announce that the library [FSharp.Data.LiteralProviders](https://github.com/tarmil/fsharp.data.literalproviders) has reached version 1.0!

FSharp.Data.LiteralProviders is an F# type provider library that provides compile-time constants from various sources, such as environment variables or files:

```fsharp
open FSharp.Data.LiteralProviders

// Get a value from an environment variable, another one from a file,
// and pass them to another type provider.

let [<Literal>] ConnectionString = Env<"CONNECTION_STRING">.Value
let [<Literal>] GetUserDataQuery = TextFile<"GetUserData.sql">.Text

type GetUserData = FSharp.Data.SqlCommandProvider<GetUserDataQuery, ConnectionString>

let getUserData (userId: System.Guid) =
    GetUserData.Create().Execute(UserId = userId)
```

Here is a summary of the new features in v1.0.

## Running an external command

The `Exec` provider runs an external command during compilation and provides its output.

```fsharp
open FSharp.Data.LiteralProviders

let [<Literal>] Branch = Exec<"git", "branch --show-current">.Output
```

More options are available to pass input, get the error output, the exit code, etc.
See [the documentation](https://github.com/Tarmil/FSharp.Data.LiteralProviders/blob/master/README.md#exec).

## Conditionals

The sub-namespaces `String`, `Int` and `Bool` provide a collection of compile-time conditional operators for the corresponding types.

For example, you can compare two integer values with `Int.LT`; combine two booleans with `Bool.OR`; or choose between two strings with `String.IF`.

```fsharp
open FSharp.Data.LiteralProviders

// Compute the version: get the latest git tag, and add the branch if it's not master or main.

let [<Literal>] TagVersion = Exec<"git", "describe --tags">.Output

let [<Literal>] Branch = Exec<"git", "branch --show-current">.Output

// Note: the `const` keyword is an F# language quirk, necessary when nesting type providers.
let [<Literal>] IsMainBranch =
    Bool.OR<
        const(String.EQ<Branch, "master">.Value),
        const(String.EQ<Branch, "main">.Value)
    >.Value

let [<Literal>] Version =
    String.IF<IsMainBranch,
        Then = TagVersion,
        Else = const(TagVersion + "-" + Branch)
    >.Value
```

See [the documentation](https://github.com/Tarmil/FSharp.Data.LiteralProviders/blob/master/README.md#conditionals) for all the operators available.

## Value as int and as bool

The providers try to parse string values as integer and as boolean. If any of these succeed, a value suffixed with `AsInt` or `AsBool` is provided.

```fsharp
open FSharp.Data.LiteralProviders

let [<Literal>] runNumberAsString = Env<"GITHUB_RUN_NUMBER">.Value // eg. "42"

let [<Literal>] runNumberAsInt = Env<"GITHUB_RUN_NUMBER">.ValueAsInt // eg. 42
```
