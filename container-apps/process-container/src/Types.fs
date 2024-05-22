module Types

open System
open Markdig.Syntax

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

type Message = { Now: DateTimeOffset }
