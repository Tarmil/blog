[<WebSharper.JavaScript>]
module Blog.Client

open WebSharper
open WebSharper.JavaScript

module Highlight =
    open WebSharper.HighlightJS

    [<Require(typeof<Resources.Languages.Fsharp>)>]
    [<Require(typeof<Resources.Languages.Diff>)>]
    [<Require(typeof<Resources.Languages.Xml>)>]
    [<Require(typeof<Resources.Styles.AtomOneLight>)>]
    let Run() =
        JS.Document.QuerySelectorAll("code[class^=language-]").ForEach(
            (fun (node, _, _, _) -> Hljs.HighlightBlock(node)),
            JS.Undefined
        )

let Main() =
    { new IControlBody with
        member this.ReplaceInDom(x) =
            Highlight.Run()
            x.ParentNode.RemoveChild(x) |> ignore }