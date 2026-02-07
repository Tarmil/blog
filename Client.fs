[<WebSharper.JavaScript>]
module Blog.Client

open WebSharper
open WebSharper.JavaScript

module Highlight =
    open WebSharper.HighlightJS

    let Run() =
        Hljs.RegisterLanguage("fsharp", Language.Fsharp)
        Hljs.RegisterLanguage("diff", Language.Diff)
        Hljs.RegisterLanguage("xml", Language.Xml)
        Styles.AtomOneLight()
        JS.Document.QuerySelectorAll("code[class^=language-]").ForEach(
            (fun (node, _, _, _) -> Hljs.HighlightElement(node)),
            JS.Undefined
        )

let Main() =
    JS.ImportFile "./css/all.css"
    { new IControlBody with
        member this.ReplaceInDom(x) =
            Highlight.Run()
            x.ParentNode.RemoveChild(x) |> ignore }
