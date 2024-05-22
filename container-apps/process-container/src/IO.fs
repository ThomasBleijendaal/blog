module IO

open System
open System.IO

let workFolder = Path.Combine(Directory.GetCurrentDirectory(), "work");
let repoFolder = Path.Combine(workFolder, "blog-main")
let markdownFolder = Path.Combine(repoFolder, "markdown")
let templateFolder = Path.Combine(repoFolder, "template")
let outputFolder = Path.Combine(workFolder, "output")
let pageTemplate = Path.Combine(templateFolder, @"page.html")

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
