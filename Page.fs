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
        failwithf "Invalid article filename: %s (should be YYMMDD-slug.md)" filename

let defaultMetadata = File.ReadAllText(Paths.layout + "/default.yml")

let yamlSerializer = SharpYaml.Serialization.Serializer()

let parseMetadata (yaml: string) =
    (defaultMetadata + "\n" + yaml)
    |> yamlSerializer.Deserialize<Metadata>

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
