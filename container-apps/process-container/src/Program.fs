open System.IO
open Markdig
open Markdig.Syntax
open Markdig.Extensions.Yaml
open Rendering
open System.Net.Http
open System

type Metadata() = 
    let mutable title: string = ""
    let mutable theme: string = ""
    let mutable visible: bool = false

    member this.Title with get() = title and set(v) = title <- v
    member this.Theme with get() = theme and set(v) = theme <- v
    member this.Visible with get() = visible and set(v) = visible <- v
    
type Chapter = {
    Index: int
    Title: string
    Theme: string
    Content: MarkdownDocument
    Visible: bool
}

type Theme = {
    Title: string
    Chapters: Chapter array
}

type Themes = Theme array

let rec deleteFolder (folder) =
    if Directory.Exists folder then
        let elements = Directory.EnumerateFileSystemEntries(folder, "*.*", SearchOption.AllDirectories)
        for element in elements do
            let attr = File.GetAttributes(element)
            if (attr &&& FileAttributes.Directory) = FileAttributes.Directory then
                deleteFolder element
                Directory.Delete element
            else
                File.Delete element

let createFolder (folder) =
    if Directory.Exists(folder) = false then
        Directory.CreateDirectory(folder) |> ignore

let resetFolder (folder) =
    deleteFolder folder
    createFolder folder

let copyFolder (sourceFolder) (targetFolder) =
    resetFolder targetFolder
    for file in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories) do
        let relativePath = Path.GetRelativePath(sourceFolder, Path.GetDirectoryName(file))
        if relativePath = "." || relativePath.StartsWith(".") = false then
            let targetFolder = Path.Combine(targetFolder, relativePath)
            let targetFile = Path.Combine(targetFolder, Path.GetFileName(file))
            createFolder(targetFolder)
            File.Copy(file, targetFile)

let workFolder = Path.Combine(Directory.GetCurrentDirectory(), "work");
let repoFolder = Path.Combine(workFolder, "blog-main")
let markdownFolder = Path.Combine(repoFolder, "markdown")
let templateFolder = Path.Combine(repoFolder, "template")
let outputFolder = Path.Combine(workFolder, "output")

resetFolder workFolder

let copyFromTemplate (file) =
    File.Copy (Path.Combine(templateFolder, file), Path.Combine(outputFolder, file))

let downloadUrl = Environment.GetEnvironmentVariable("InputFile");

let httpClient = new HttpClient()

#if DEBUG

copyFolder (Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../")) repoFolder

#else

open System.IO.Compression

let downloadTask = 
    task {
        let! data = httpClient.GetStreamAsync(downloadUrl);

        ZipFile.ExtractToDirectory(data, workFolder)
    }

downloadTask.GetAwaiter().GetResult();

#endif

resetFolder outputFolder

copyFromTemplate "favicon.ico"
copyFromTemplate "style.css"
copyFromTemplate "robots.txt"

let pageTemplate = Path.Combine(templateFolder, @"page.html")

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
        let slug = chapter.Title.ToLowerInvariant().Replace(" ", "-")
        $"{slug}.html"

let themeIndexLinkHtml (theme: Theme) =
    $"<a href=\"/{toThemeSlug(theme)}/index.html\">{theme.Title}</a>"

let chapterUrl (theme: Theme) (chapter: Chapter) =
    $"{toThemeSlug(theme)}/{toChapterSlug(chapter)}"

let themeChapterLinkHtml (titlePrefix: string) (theme: Theme) (chapter: Chapter) =
    $"<a href=\"/{chapterUrl theme chapter}\">{titlePrefix}{chapter.Title}</a>"

let populatePageTemplate (themes: Themes) (theme: Theme) (chapter: Chapter) (html: string) =
    let wrapLiAndConcat (current) (renderItem) (input) = 
        let className (item) = 
            match current = item with
            | true -> @" class=""selected"""
            | false -> ""

        input 
        |> Array.map (fun item -> $"<li{className(item)}>{renderItem(item)}</li>") 
        |> String.concat ""

    let themesHtml = themes |> wrapLiAndConcat theme themeIndexLinkHtml

    let chaptersHtml = theme.Chapters |> wrapLiAndConcat chapter (themeChapterLinkHtml "" theme)

    let maxChapterIndex = theme.Chapters.Length - 1

    let siblingChapter = 
        match chapter.Index with 
        | i when i = 0 && i < maxChapterIndex -> (None, Some theme.Chapters[i + 1])
        | i when i > 0 && i < maxChapterIndex -> (Some theme.Chapters[i - 1], Some theme.Chapters[i + 1])
        | i when i > 0 && i = maxChapterIndex -> (Some theme.Chapters[i - 1], None)
        | _ -> (None, None)

    let previousLink = themeChapterLinkHtml "Previous page: " theme
    let nextLink = themeChapterLinkHtml "Next page: " theme

    let linksHtml =
        match siblingChapter with
        | (Some p, Some n) -> [| previousLink p; nextLink n |] |> String.concat " | "
        | (Some p, None) -> previousLink p
        | (None, Some n) -> nextLink n
        | _ -> ""

    let html = 
        html.Replace("{themes}", themesHtml)
            .Replace("{theme}", chapter.Theme)
            .Replace("{chapters}", chaptersHtml)
            .Replace("{title}", chapter.Title)
            .Replace("{body}", chapter.Content.ToHtml(markdownPipeline))
            .Replace("{links}", linksHtml)

    HtmlHelper.PrettyPrint(html)

let getPageTemplate () =
    File.ReadAllText pageTemplate

let writeFile (fileName: string) (content) =
    let fileFolder = Path.GetDirectoryName(fileName)
    if Directory.Exists fileFolder = false then
        Directory.CreateDirectory(fileFolder) |> ignore

    File.WriteAllText(fileName, content)

let writeDocument (theme, chapter, html) =
    let fileName = Path.Combine(outputFolder, (chapterUrl theme chapter).Replace('/', Path.DirectorySeparatorChar))
    writeFile fileName html

let writeIndex (theme, chapter, html) =
    let fileName = Path.Combine(outputFolder, "index.html")
    writeFile fileName html

let template = getPageTemplate()

let themes = getThemes()

let pages = 
    themes
    |> Array.map (fun theme -> 
        theme.Chapters 
        |> Array.map (fun chapter -> (theme, chapter, populatePageTemplate themes theme chapter template)))
    |> Array.collect id

pages |> Array.iter writeDocument

let coverPage = pages |> Array.head |> writeIndex

if Directory.EnumerateFiles outputFolder |> Seq.isEmpty = false then

    let publishFolder = Environment.GetEnvironmentVariable("PublishFolder")

    deleteFolder publishFolder

    copyFolder outputFolder publishFolder

exit 0
