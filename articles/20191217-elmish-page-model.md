---
title: Managing page-specific models in Elmish
tags: [fsbolero,elmish]
---

> This article is part of [F# Advent Calendar 2019](https://sergeytihon.com/2019/11/05/f-advent-calendar-in-english-2019/).

GUI applications (web or otherwise) often display their content in one of a number of pages.
You can have a login page, a dashboard page, a details page for a type of item, and so on.
Whichever page is currently displayed generally has some state (or model, in Elmish parlance) that only makes sense for this page, and can be dropped when switching to a different page.
In this article we'll look into a few ways that such a page-specific model can be represented in an Elmish application.
The code uses Bolero, but the ideas can apply to any Elmish-based framework, like Fable-Elmish or Fabulous.

Our running example will be a simple book collection app with two pages: a list of books with a text input to filter the books by title, and a book details page.

```fsharp
type Page =
    | List
    | Book of isbn: string

type Book =
    { isbn: string // We use the ISBN as unique identifier
      title: string
      publishDate: DateTime
      author: string }
```

Note that there can also be some state which, although only used by one page, should be stored in the main model anyway because it needs to persist between page switches.
For example, in our application, we don't want to reload the list of all book summaries whenever we switch back to the List page, so we will always store it in the main model.

## Page model in the main model

One way to go is to just store each page's model as a field of the main application model.
However, we quickly encounter a problem: the state for all pages needs to be initialized from the beginning, not just the initial page.

```fsharp
type ListPageModel =
    { filter: string }
    
type BookPageModel = Book

type Model =
    { page: Page
      books: Book list
      list: ListPageModel
      book: BookPageModel }

let initialModel =
    { page = List
      books = []
      list = { filter = "" }
      book = ??? // What should we put here?
    }
```

We can of course use an option:

```fsharp
type Model =
    { // We don't need to store the page anymore,
      // since that is determined by which model is Some.
      // page: Page
      books: Book list
      list: ListPageModel option
      book: BookPageModel option }

let initialModel =
    { books = []
      list = Some { filter = "" }
      book = None }
```

But this violates a principle that I would rather keep true: *illegal states should be unrepresentable*.
What this means is that it is possible to put the application in a nonsensical state, where the `page` is `Book` but the `book` is `None`.
Our `update` and `view` functions will have to deal with this using partial functions (ie. functions that aren't correct for all possible input values, and throw exceptions otherwise) such as `Option.get`.

```fsharp
type BookMsg =
    | SetAuthor of string
    // ...

type Msg =
    | Goto of Page
    | ListMsg of ListMsg
    | BookMsg of BookMsg
    // ...

let update msg model =
    match msg with
    | BookMsg bookMsg ->
        let bookModel = Option.get model.book // !!!!
        let bookModel, cmd = updateBook bookMsg bookModel
        { model with book = Some bookModel }, Cmd.map BookMsg cmd
    // ...

let view model dispatch =
    match model.page with
    | Book isbn ->
        let bookModel = Option.get model.book // !!!!
        bookView bookModel (dispatch << BookMsg)
    // ...
```

Additionally, when switching pages, in addition to initializing the state of the new page, we may need to make sure that we set the model of other pages to `None`.
In this particular example, each model is very light, so it doesn't really matter; but if there are many different pages and some of their models are large in memory, this can become a concern.

```fsharp
let update msg model =
    match msg with
    | Goto List ->
        { model with
            list = Some { filter = "" }
            book = None // Don't forget this!
        }, Cmd.none
    | Goto (Book isbn) ->
        match model.books |> List.tryFind (fun book -> book.isbn = isbn) with
        | Some book ->
            { model with
                list = None // Don't forget this!
                book = Some book
            }, Cmd.none
        | None ->
            model, Cmd.ofMsg (Error ("Unknown book: " + isbn))
    // ...
```

Despite these inconvenients, this style is a good choice for an application whose page are organized in a stack, where each page is only accessed directly from a parent page.
Actually, the fact that the model can contain several page states becomes an advantage when doing page transition animations, since during the animation, two pages are in fact displayed on the screen.
In particular, this is quite common for mobile applications.
Because of this, it is a recommended style in Fabulous, [as shown by the sample application FabulousContacts](https://github.com/TimLariviere/FabulousContacts/blob/47a921ce28e06114b37f49d00ea5d5343fede89d/FabulousContacts/App.fs#L23-L25).

## Page model in a union

### Separate page union and page model union

An alternative is to store the page model as a union, with one case per page just like the `Page` union, but with models as arguments.

```fsharp
type PageModel =
    | List of ListPageModel
    | Book of BookPageModel

type Model =
    { page: PageModel
      books: Book list }

let initialModel =
    { page = PageModel.List { filter = "" }
      books = [] }
```

The model is now correct by construction: it is not possible to accidentally construct an inconsistent state.

Unfortunately the types still allow receiving eg. a `BookMsg` when the current page is not `Book`; but such messages can just be ignored.
A nice way to do this is to match on the message and the page together:

```fsharp
let update msg model =
    match msg, model.page with
    | ListMsg listMsg, List listModel ->
        let listModel, cmd = updateList listMsg listModel
        { model with page = List listModel }, Cmd.map ListMsg cmd
    | ListMsg _, _ -> model, Cmd.none // Ignore irrelevant message
    | BookMsg bookMsg, Book bookModel ->
        let bookModel, cmd = updateBook bookMsg bookModel
        { model with page = Book bookModel }, Cmd.map BookMsg cmd
    | BookMsg _, _ -> model, Cmd.none // Ignore irrelevant message
    // ...
```

Note: we could handle all irrelevant messages at once in a final `| _ -> model, Cmd.none`, but then we would lose the exhaustiveness check on `msg`.
So if later we add a message but forget to handle it, the compiler wouldn't warn us.

As before, when switching to a page, the initial model is decided in the update handler for the `Goto` message.

```fsharp
let update msg model =
    match msg with
    | Goto Page.List ->
        let pageModel = PageModel.List { filter = "" }
        { model with page = pageModel }, Cmd.none
    | Goto (Page.Book isbn) ->
        match model.books |> List.tryFind (fun book -> book.isbn = isbn) with
        | Some book ->
            { model with page = PageModel.Book book }, Cmd.none
        | None ->
            model, Cmd.ofMsg (Error ("Unknown book: " + isbn))
    // ...
```

### Bolero's `PageModel<'T>`

[Bolero contains a facility](https://fsbolero.io/docs/Routing#page-models) to handle such a page model style.
It is essentially the same as the previous style, with some internal magic to avoid the need for a separate union type while still playing nice with Bolero's automatic URL routing system.

## Separate Elmish program

Finally, I have recently been experimenting with a way to sidestep the whole question of how to embed the messages and models of pages into the main message and model entirely: make each page a separate Elmish program.

This is a style that I haven't seen used in Fable or Fabulous, and in fact I have no idea whether it is possible to use it in those frameworks.
In Bolero, while it is still buggy and requires changes to the library itself, I hope to be able to make it available soon.

The idea is that we will have a root Program that will contain the common model (here, the list of books) and dispatch page switches to a nested `ProgramComponent`.
Each page is a different `ProgramComponent`, with its own model and message types.

Of course, each page still needs to be able to receive a model from the parent program (the list of books for `List`, and the book as initial model for `Book`), and to dispatch messages to the main update.
These two values can be passed to the component as Blazor parameters.
This is the base type that will be implemented by our page components:

```fsharp
[<AbstractClass>]
type NestedProgramComponent<'inModel, 'rootMsg, 'model, 'msg>() =
    inherit ProgramComponent<'model, 'msg>()

    let mutable oldInModel = Unchecked.defaultof<'inModel>

    [<Parameter>]
    member val InModel = oldInModel with get, set
    [<Parameter>]
    member val RootDispatch = Unchecked.defaultof<Dispatch<'rootMsg>> with get, set

    override this.OnParametersSet() =
        if not <| obj.ReferenceEquals (oldInModel, this.InModel) then
            oldInModel <- this.InModel
            this.Rerender()
```

For example, the `Book` component is implemented as follows:

```fsharp
type BookComponent() as this =
    inherit NestedProgramComponent<BookModel, Msg, BookModel, BookMsg>()
    
    let update message model =
        // Use this.RootDispatch to send messages to the root program
        // ...
        
    let view model dispatch =
        // ...

    override this.Program =
        Program.mkProgram (fun _ -> this.InModel, Cmd.none) update view
```

and with a convenience function to instantiate nested program components:

```fsharp
module Html =
    open Bolero.Html

    let ncomp<'T, 'inModel, 'rootMsg, 'model, 'msg
                when 'T :> NestedProgramComponent<'inModel, 'rootMsg, 'model, 'msg>>
            (inModel: 'inModel) (rootDispatch: Dispatch<'rootMsg>) =
        comp<'T> ["InModel" => inModel; "RootDispatch" => rootDispatch] []
```

we can include the appropriate page component inside the main view:

```fsharp
let view model dispatch =
    cond model.page <| function
    | List ->
        ncomp<ListComponent,_,_,_,_> model.books dispatch
    | Book isbn ->
        cond (model.books |> List.tryFind (fun book -> book.isbn = isbn)) <| function
        | Some book ->
            ncomp<BookComponent,_,_,_,_> book dispatch
        | None ->
            textf "Unknown book: %s" isbn
```

# Conclusion

The above approaches each have their advantages and inconvenients. They can even be mixed and matched, depending on how persistent different pages' models needs to be across page switches. Don't be afraid to experiment!
