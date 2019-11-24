module Blog.Page

open System.IO
open FSharp.Text.RegexProvider
open FSharp.Text.RegexExtensions
open Markdig
open Markdig.Syntax
open Markdig.Extensions.Yaml
open Markdig.Renderers

type Date =
    {
        year: int
        month: int
        day: int
    }

type ArticleUrl =
    {
        date: Date
        slug: string
    }

[<CLIMutable>]
type Metadata =
    {
        title: string
        subtitle: string
        author: string
        tags: string[]
        overrideTags: bool
        rootUrl: string
    }

type Page =
    {
        metadata: Metadata
        html: string
        prev: (ArticleUrl * Metadata) option
        next: (ArticleUrl * Metadata) option
    }


type TitleRE = Regex<"""(?<year>\d\d\d\d)(?<month>\d\d)(?<day>\d\d)-(?<slug>.*)\.md""">

let parseArticleUrl path =
    let filename = Path.GetFileName path
    let m = TitleRE().TypedMatch(filename)
    if m.Success then
        {
            date = {
                year = m.year.AsInt
                month = m.month.AsInt
                day = m.day.AsInt
            }
            slug = m.slug.Value
        }
    else
        failwithf "Invalid article filename: %s (should be YYYYMMDD-slug.md)" filename

let emptyMetadata =
    {
        title = ""
        subtitle = ""
        author = ""
        tags = [||]
        overrideTags = false
        rootUrl = ""
    }

let yamlSerializer = SharpYaml.Serialization.Serializer()

let mergeMetadata (m1: Metadata) (m2: Metadata) =
    {
        title = if isNull m2.title then m1.title else m2.title
        subtitle = if isNull m2.subtitle then m1.subtitle else m2.subtitle
        author = if isNull m2.author then m1.author else m2.author
        tags =
            let m2tags = if isNull m2.tags then [||] else m2.tags
            if m2.overrideTags then m2tags else
            Array.append m1.tags m2tags |> Array.distinct
        overrideTags = m2.overrideTags
        rootUrl = if isNull m2.rootUrl then m1.rootUrl else m2.rootUrl
    }

let defaultMetadata =
    File.ReadAllText(Paths.layout + "/default.yml")
    |> yamlSerializer.Deserialize<Metadata>
    |> mergeMetadata emptyMetadata // remove the nulls from default serializer

let parseMetadata (yaml: string) =
    yaml
    |> yamlSerializer.Deserialize<Metadata>
    |> mergeMetadata defaultMetadata

let runMarkdown source =
    let pipeline =
        MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build()
    use writer = new StringWriter()
    let renderer = HtmlRenderer(writer)
    pipeline.Setup(renderer)
    let doc = Markdown.Parse(source, pipeline)
    renderer.Render(doc) |> ignore
    writer.Flush()
    doc, writer.ToString()

let parse path =
    let source = File.ReadAllText(path)
    let doc, html = runMarkdown source
    let metadata =
        doc.Descendants<YamlFrontMatterBlock>()
        |> Seq.tryHead
        |> function
            | None -> ""
            | Some yaml -> string yaml.Lines
        |> parseMetadata
    {
        metadata = metadata
        html = html
        prev = None
        next = None
    }
