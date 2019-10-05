namespace Blog

open System
open System.IO
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server
open WebSharper.Web

type EndPoint =
    | [<EndPoint "GET /">] Page of string
    | [<EndPoint "GET /article">] Article of Page.ArticleUrl

module Content =

    let pages =
        Map [
            for f in Directory.GetFiles(Paths.pages, "*.md") do
                (Path.GetFileNameWithoutExtension f, Page.parse f)
        ]

    let articles =
        [
            None
            for f in Directory.GetFiles(Paths.articles, "*.md") do
                Some (Page.parseArticleUrl f, Page.parse f)
            None
        ]
        |> List.windowed 3
        |> List.map (function
            | [prev; Some (url, page); next] ->
                let prev = prev |> Option.map (fun (url, page) -> (url, page.metadata))
                let next = next |> Option.map (fun (url, page) -> (url, page.metadata))
                let page = { page with prev = prev; next = next }
                (url, page)
            | x -> failwithf "Shouldn't happen: %A" x
        )
        |> Map

    let latestArticlesByMonth =
        articles
        |> Seq.sortByDescending (fun (KeyValue(url, _)) -> url.date)
        |> Seq.truncate 20
        |> Seq.groupBy (fun (KeyValue(url, _)) -> (url.date.year, url.date.month))
        |> List.ofSeq

module Layout =
    open System.Globalization

    type MainTemplate = Templating.Template<const(Paths.layout + "/main.html")>

    type Css() = inherit Resources.BaseResource("/css/all.css")

    let culture = CultureInfo.GetCultureInfo("en-US")

    let menu (ctx: Context<EndPoint>) =
        [
            for (year, month), articles in Content.latestArticlesByMonth do
                MainTemplate.MenuMonth()
                    .Date(DateTime(year, month, 1).ToString("MMMM yyyy", culture))
                    .Articles([
                        for KeyValue(url, page) in articles do
                            MainTemplate.MenuArticle()
                                .Url(ctx.Link (Article url))
                                .Title(page.metadata.title)
                                .Doc()
                    ])
                    .Doc()
        ]

    let main (ctx: Context<EndPoint>) (page: Page.Page) (url: option<Page.ArticleUrl>) =
        let prevUrl, prevTitle =
            match page.prev with
            | Some (url, metadata) -> ctx.Link (Article url), "← " + metadata.title
            | None -> "#", ""
        let nextUrl, nextTitle =
            match page.next with
            | Some (url, metadata) -> ctx.Link (Article url), metadata.title + " →"
            | None -> "#", ""
        let byline =
            match url with
            | Some url ->
                let date = DateTime(url.date.year, url.date.month, url.date.day)
                MainTemplate.Byline()
                    .Author(page.metadata.author)
                    .Date(date.ToString("D", culture))
                    .Doc()
            | None -> Doc.Empty
        MainTemplate()
            .Title(page.metadata.title)
            .Subtitle(page.metadata.subtitle)
            .Menu(menu ctx)
            .Body([
                Doc.Verbatim page.html
                byline
                Doc.WebControl (new Require<Css>())
            ])
            .PrevUrl(prevUrl)
            .PrevTitle(prevTitle)
            .NextUrl(nextUrl)
            .NextTitle(nextTitle)
            .Doc()
        |> Content.Page

module Site =

    let simplePage name ctx =
        Layout.main ctx Content.pages.[name] None

    let articlePage url ctx =
        Layout.main ctx Content.articles.[url] (Some url)

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx action ->
            match action with
            | Page name -> simplePage name ctx
            | Article url -> articlePage url ctx
        )

[<Sealed>]
type Website() =
    interface IWebsite<EndPoint> with
        member this.Sitelet = Site.Main
        member this.Actions =
            [
                for KeyValue(name, _) in Content.pages do Page name
                for KeyValue(url, _) in Content.articles do Article url
            ]

[<assembly: Website(typeof<Website>)>]
do ()
