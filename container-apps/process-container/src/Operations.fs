module Operations

open System
open System.IO
open System.IO.Compression
open System.Net.Http

open Rendering
open Types
open IO
open Template
open Markdig

let httpClient = new HttpClient()
let downloadUrl = Environment.GetEnvironmentVariable("InputFile");

let copyFromTemplate (file) =
    File.Copy (Path.Combine(templateFolder, file), Path.Combine(outputFolder, file))

let startAsync () = 
    task {
        resetFolder workFolder
        
        #if DEBUG

        copyFolder (Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../")) repoFolder

        #else
            
        let! data = httpClient.GetStreamAsync(downloadUrl);
        ZipFile.ExtractToDirectory(data, workFolder)

        #endif

        resetFolder outputFolder
    }

let copyStatics () =
    copyFromTemplate "favicon.ico"
    copyFromTemplate "style.css"
    copyFromTemplate "robots.txt"

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

    html

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

let publish () =
    if Directory.EnumerateFiles outputFolder |> Seq.isEmpty = false then

        let publishFolder = Environment.GetEnvironmentVariable("PublishFolder")

        deleteFolder publishFolder

        copyFolder outputFolder publishFolder

        Threading.Thread.Sleep(1000)
