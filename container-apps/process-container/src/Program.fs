open System
open System.Threading
open System.Threading.Tasks

open Types
open IO
open Azure

let cts = new CancellationTokenSource ()
AppDomain.CurrentDomain.ProcessExit.Add(fun (_) -> cts.Cancel())

let mainTask = task {
    while cts.IsCancellationRequested = false do
        let! message = receiver.ReceiveMessageAsync(cancellationToken = cts.Token)

        if message <> null then
            do! receiver.CompleteMessageAsync(message)
            
            do! Operations.startAsync()

            Operations.copyStatics()

            let template = Template.getPageTemplate()

            let themes = Template.getThemes()

            let pages = 
                themes
                |> Array.map (fun theme -> 
                    theme.Chapters 
                    |> Array.map (fun chapter -> (theme, chapter, Operations.populatePageTemplate themes theme chapter template)))
                |> Array.collect id

            pages |> Array.iter Operations.writeDocument

            pages |> Array.head |> Operations.writeIndex |> ignore

            Operations.publish ()

        else
            try
                do! Task.Delay(TimeSpan.FromMinutes(1), cancellationToken = cts.Token)
            with 
            | :? TaskCanceledException -> ignore()
}

Task.WaitAll mainTask

exit 0
