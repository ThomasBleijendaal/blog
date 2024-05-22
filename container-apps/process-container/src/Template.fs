module Template

open IO
open Types

open System.IO
open Markdig
open Markdig.Syntax
open Markdig.Extensions.Yaml
open Rendering
open System.Text.RegularExpressions

let markdownPipeline = MarkdownHelper.GetPipeline();
let yamlDeserializer = YamlHelper.GetYamlDeserializer();

let getMarkdown (markdownDocument: MarkdownDocument) (fileContents: string) : Metadata =
    let metadata = markdownDocument.Descendants<YamlFrontMatterBlock>() |> Seq.head

    let yamlData = fileContents.Substring(metadata.Span.Start, metadata.Span.Length).Replace("---", "")
    let yamlInput = new StringReader(yamlData)

    yamlDeserializer.Deserialize<Metadata>(yamlInput)

let naturalSort (file: string) =
    Path.GetFileName(file).Split('-')[0] |> int

let getThemes () : Themes =
    Directory.EnumerateDirectories(markdownFolder)
    |> Seq.sortBy naturalSort
    |> Seq.map (fun theme -> 
        Directory.EnumerateFiles(theme)
        |> Seq.sortBy naturalSort
        |> Seq.mapi (fun index page -> 
            let fileLines = File.ReadAllLines page

            let fileContents = MarkdownHelper.InlineReferences(repoFolder, fileLines)

            let document = Markdown.Parse(fileContents, markdownPipeline);

            let metadata = getMarkdown document fileContents

            { Index = index; Title = metadata.Title; Theme = metadata.Theme; Content = document; Visible = metadata.Visible }))
    |> Seq.collect id
    |> Seq.filter (fun chapter -> chapter.Visible)
    |> Seq.groupBy (fun chapter -> chapter.Theme)
    |> Seq.map (fun (theme, chapters) -> { Title = theme; Chapters = chapters |> Seq.toArray})
    |> Seq.toArray

let toThemeSlug (theme: Theme) =
    theme.Title.ToLowerInvariant().Replace(" ", "-")

let toChapterSlug (chapter: Chapter) =
    match chapter with
    | { Index = 0 } -> "index.html"
    | _ -> 
        let slug = Regex.Replace(chapter.Title.ToLowerInvariant().Replace(" ", "-"), "[^a-z0-9\-]", "")
        $"{slug}.html"

let themeIndexLinkHtml (theme: Theme) =
    $"<a href=\"/{toThemeSlug(theme)}/index.html\">{theme.Title}</a>"

let chapterUrl (theme: Theme) (chapter: Chapter) =
    $"{toThemeSlug(theme)}/{toChapterSlug(chapter)}"

let themeChapterLinkHtml (titlePrefix: string) (theme: Theme) (chapter: Chapter) =
    $"<a href=\"/{chapterUrl theme chapter}\">{titlePrefix}{chapter.Title}</a>"

let getPageTemplate () =
    File.ReadAllText pageTemplate
