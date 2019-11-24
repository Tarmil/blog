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

    let latestArticles =
        articles
        |> Seq.sortByDescending (fun (KeyValue(url, _)) -> url.date)
        |> Seq.truncate 20
        |> List.ofSeq

    let latestArticlesByMonth =
        latestArticles
        |> List.groupBy (fun (KeyValue(url, _)) -> (url.date.year, url.date.month))

module Layout =
    open System.Globalization
    open WebSharper.UI.Html

    type MainTemplate = Templating.Template<const(Paths.layout + "/main.html")>

    type Css() = inherit Resources.BaseResource("/css/all.css")

    let culture = CultureInfo.GetCultureInfo("en-US")

    let link (ctx: Context<EndPoint>) ep =
        let s = ctx.Link(ep)
        if s.EndsWith(".html") then s.[..s.Length-6] else s

    let menu (ctx: Context<EndPoint>) =
        [
            for (year, month), articles in Content.latestArticlesByMonth do
                MainTemplate.MenuMonth()
                    .Date(DateTime(year, month, 1).ToString("MMMM yyyy", culture))
                    .Articles([
                        for KeyValue(url, page) in articles do
                            MainTemplate.MenuArticle()
                                .Url(link ctx (Article url))
                                .Title(page.metadata.title)
                                .Doc()
                    ])
                    .Doc()
        ]

    let byline (url: Page.ArticleUrl option) (page: Page.Page) =
        match url with
        | Some url ->
            let date = DateTime(url.date.year, url.date.month, url.date.day)
            MainTemplate.Byline()
                .Author(page.metadata.author)
                .Date(date.ToString("D", culture))
                .Doc()
        | None -> Doc.Empty

    let tagsList (page: Page.Page) =
        Doc.Concat [
            for tag in page.metadata.tags do
                MainTemplate.Tag().Name(tag).Doc()
        ]

    let tweetButton (ctx: Context<EndPoint>) url (page: Page.Page) =
        let enc x = System.Net.WebUtility.UrlEncode x
        let urlStr = page.metadata.rootUrl + "/" + (link ctx (Article url)).Trim([|'/';'.'|]) // Quite hacky :/
        let tweetUrl =
            sprintf "https://twitter.com/intent/tweet?url=%s&text=%s&hashtags=%s"
                urlStr
                (enc page.metadata.title)
                (page.metadata.tags |> Seq.map enc |> String.concat ",")
        MainTemplate.TweetButton().Url(tweetUrl).Doc()

    let articleList (ctx: Context<EndPoint>) =
        [
            for KeyValue(url, page) in Content.latestArticles do
                yield MainTemplate.ArticleInList()
                    .Url(link ctx (Article url))
                    .Title(page.metadata.title)
                    .Body([Doc.Verbatim page.html; byline (Some url) page])
                    .Tags(tagsList page)
                    .Doc()
        ]

    let main (ctx: Context<EndPoint>) (page: Page.Page) (url: option<Page.ArticleUrl>) =
        let prevUrl, prevTitle =
            match page.prev with
            | Some (url, metadata) -> link ctx (Article url), "← " + metadata.title
            | None -> "#", ""
        let nextUrl, nextTitle =
            match page.next with
            | Some (url, metadata) -> link ctx (Article url), metadata.title + " →"
            | None -> "#", ""
        MainTemplate()
            .Title(page.metadata.title)
            .Subtitle(page.metadata.subtitle)
            .Menu(menu ctx)
            .Body([
                Templating.DynamicTemplate(page.html)
                    .With("Articles", articleList ctx)
                    .Doc()
                byline url page
                Doc.WebControl (new Require<Css>())
                client <@ Client.Main () @>
            ])
            .Tags([
                yield tagsList page
                match url with
                | None -> ()
                | Some url -> yield tweetButton ctx url page
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
