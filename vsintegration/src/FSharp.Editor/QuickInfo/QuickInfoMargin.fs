// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open Microsoft.VisualStudio.Threading
open System.Windows.Threading
open System.Threading

[<NoComparison; NoEquality>]
type private CallInUIContext = 
    | CallInUIContext of ((unit -> unit) -> Async<unit>)
    static member FromCurrentThread() = 
        let uiContext = SynchronizationContext.Current
        CallInUIContext (fun f ->
            async {
                let ctx = SynchronizationContext.Current
                do! Async.SwitchToContext uiContext
                f()
                do! Async.SwitchToContext ctx
            })

type DocumentEventListener (events: IEvent<unit> list, delayMillis: uint16, update: CallInUIContext -> Async<unit>) =
    do if List.isEmpty events then invalidArg "events" "Events must be a non-empty list"
    let events = events |> List.reduce Event.merge
    let triggered = AsyncManualResetEvent()
    do events.Add (fun _ -> triggered.Set())
    let timer = DispatcherTimer(DispatcherPriority.ApplicationIdle, Interval = TimeSpan.FromMilliseconds (float delayMillis))
    let tokenSource = new CancellationTokenSource()
    let mutable disposed = false

    let startNewTimer() = 
        timer.Stop()
        timer.Start()
        
    let rec awaitPauseAfterChange() =
        async { 
            let! e = Async.eitherEvent(events, timer.Tick)
            match e with
            | Choice1Of2 _ -> 
                startNewTimer()
                do! awaitPauseAfterChange()
            | _ -> ()
        }
        
    do 
       let callUIContext = CallInUIContext.FromCurrentThread()
       let startUpdate (cts: CancellationTokenSource) = CommonRoslynHelpers.StartAsyncAsTask cts.Token (update callUIContext)

       let computation =
           async { 
              while true do
                  use cts = new CancellationTokenSource()
                  do! startUpdate cts
                  do! triggered.WaitAsync() |> Async.AwaitTask
                  triggered.Reset()
                  if not DocumentEventListener.SkipTimerDelay then
                      startNewTimer()
                      do! awaitPauseAfterChange()
                  cts.Cancel()           
           }
       
       CommonRoslynHelpers.StartAsyncAsTask tokenSource.Token computation |> ignore

    /// This is a none or for-all option for unit testing purpose only
    static member val SkipTimerDelay = false with get, set

    interface IDisposable with
        member __.Dispose() =
            if not disposed then
                tokenSource.Cancel()
                tokenSource.Dispose()
                timer.Stop()
                disposed <- true


type QuickInfoMargin 
    (
        doc: ITextDocument,
        view: ITextView,
        checkerProvider: FSharpCheckerProvider,
        projectInfoManager: ProjectInfoManager
    ) =

    let updateLock = obj()
    let visual = FSharp.LanguageService.Base.UI.QuickInfoMargin()
    let buffer = view.TextBuffer
    let mutable currentWord: SnapshotSpan option = None

    let firstNonEmptyLine (str: string) =
        use reader = new StringReader (str)
        let rec loop (line:string) =
            if isNull line then None 
            elif  line.Length > 0 then Some line
            else loop (reader.ReadLine())
        loop (reader.ReadLine())

    let getNonEmptyLines (str: string) =
        use reader = new StringReader(str)
        [| let line = ref (reader.ReadLine())
           while not (isNull !line) do
             if (!line).Length > 0 then
                yield !line
             line := reader.ReadLine() |]

    let updateQuickInfo (tooltip: string option, errors: ((FSharpErrorSeverity * string list) []) option,
                         newWord: SnapshotSpan option) = lock updateLock <| fun () -> 
        currentWord <- newWord
        
        // helper function to lead a string builder across the collection of
        // errors accumulating lines annotated with their index number
        let errorString (errors:string list) (sb:StringBuilder) =
            match errors with
            | [e] -> sb.Append e
            | _ -> (sb, errors ) ||> List.foldi (fun sb i e ->
                sb.Append(sprintf "%d. %s" (i + 1) e).Append(" "))
        
        let currentInfo =
            match tooltip with
            | Some tt ->   // show type info if there aren't any errors
                match firstNonEmptyLine tt with
                | Some str ->
                    if str.StartsWith ("type ", StringComparison.Ordinal) then
                        let index = str.LastIndexOf ("=", StringComparison.Ordinal)
                        if index > 0 then str.[0..index-1] else str
                    else str
                | None -> ""
            | None -> ""  // if there are no results the panel will be empty        

        visual.tbQuickInfo.Text <- currentInfo

    let flattener (sb: StringBuilder) (str: string) : StringBuilder =
        if str.Length > 0 && Char.IsUpper str.[0] then (sb.Append ". ").Append(str.Trim())
        else (sb.Append " ").Append (str.Trim())

    let flattenLines (str:string) : string =
        if isNull str then "" else
        let flatstr =
            str
            |> getNonEmptyLines
            |> Array.foldi (fun (sb: StringBuilder) i line ->
                if i = 0 then sb.Append(line.Trim())
                else flattener sb line) (new StringBuilder())
            |> string
        match flatstr.ToCharArray() |> Array.tryLast with
        | None  -> ""
        | Some '.' -> flatstr
        | Some _ -> flatstr + "."

    let isSnapshotPointInSpan (point: SnapshotPoint, span: SnapshotSpan) =
        // The old snapshot might not be available anymore, we compare on updated snapshot
        let point = point.TranslateTo(span.Snapshot, PointTrackingMode.Positive)
        point.CompareTo span.Start >= 0 && point.CompareTo span.End <= 0
    
    let updateAtCaretPosition (CallInUIContext callInUIContext) =
        async {
            let caretPos = view.Caret.Position
            match Option.ofNullable <| caretPos.Point.GetPoint(buffer, view.Caret.Position.Affinity), currentWord with
            | Some point, Some cw when cw.Snapshot = view.TextSnapshot && isSnapshotPointInSpan(point, cw) -> ()
            | Some point, _ ->
                let! res = 
                    asyncMaybe {
                        let! _, _, checkFileResults = checkerProvider.Checker.ParseAndCheckFileInProject(doc.FilePath, textVersionHash, sourceText.ToString(), options, allowStaleResults = true)
                        let textLine = sourceText.Lines.GetLineFromPosition(position)
                        let textLineNumber = textLine.LineNumber + 1 // Roslyn line numbers are zero-based
                        let defines = CompilerEnvironment.GetCompilationDefinesForEditing(filePath, options.OtherOptions |> Seq.toList)
                        let! symbol = CommonHelpers.getSymbolAtPosition(documentId, sourceText, position, filePath, defines, SymbolLookupKind.Precise)
                        let! res = checkFileResults.GetStructuredToolTipTextAlternate(textLineNumber, symbol.Ident.idRange.EndColumn, textLine.ToString(), symbol.FullIsland, FSharpTokenTag.IDENT) |> liftAsync
                        match res with
                        | FSharpToolTipText [] 
                        | FSharpToolTipText [FSharpStructuredToolTipElement.None] -> return! None
                        | _ -> 
                
                        let! tooltip, newWord =
                            asyncMaybe {
                                let! newWord, longIdent = vsLanguageService.GetSymbol (point, doc.FilePath, project)
                                let lineStr = point.GetContainingLine().GetText()
                                let idents = String.split StringSplitOptions.None [|"."|] longIdent.Text |> Array.toList
                                let! (FSharpToolTipText tooltip) =
                                    vsLanguageService.GetOpenDeclarationTooltip(
                                        longIdent.Line + 1, longIdent.RightColumn, lineStr, idents, project,
                                        doc.FilePath)
                                let! tooltip =
                                    tooltip
                                    |> List.tryHead
                                    |> Option.bind (function
                                        | FSharpToolTipElement.Single (s, _) -> Some s
                                        | FSharpToolTipElement.Group ((s, _) :: _) -> Some s
                                        | _ -> None)
                                return Some tooltip, newWord
                            }
                        let! checkResults = 
                            vsLanguageService.ParseAndCheckFileInProject(doc.FilePath, project, AllowStaleResults.MatchingSource)
                        let! errors =
                            asyncMaybe {
                                let! errors = checkResults.CheckErrors
                                do! (if Array.isEmpty errors then None else Some())
                                return!
                                    seq { for e in errors do
                                            if String.Equals(doc.FilePath, e.FileName, StringComparison.InvariantCultureIgnoreCase) then
                                                match fromRange buffer.CurrentSnapshot (e.StartLineAlternate, e.StartColumn, e.EndLineAlternate, e.EndColumn) with
                                                | Some span when point.InSpan span -> yield e.Severity, flattenLines e.Message
                                                | _ -> () }
                                    |> Seq.groupBy fst
                                    |> Seq.sortBy (fun (severity, _) -> if severity = FSharpErrorSeverity.Error then 0 else 1)
                                    |> Seq.map (fun (s, es) -> s, es |> Seq.map snd |> Seq.distinct |> List.ofSeq)
                                    |> Seq.toArray
                                    |> function [||] -> None | es -> Some es
                            } |> Async.map Some
                        return tooltip, errors, Some newWord
                    }
                let res = res |> Option.getOrElse (None, None, None) 
                return! callInUIContext (fun () -> updateQuickInfo res)
            | None, _ -> return updateQuickInfo (None, None, None)
        } |> Async.Ignore

    let docEventListener = new DocumentEventListener ([ViewChange.layoutEvent view; ViewChange.caretEvent view], 200us, updateAtCaretPosition)

    interface IWpfTextViewMargin with
        member __.VisualElement = upcast visual
        member __.MarginSize = visual.ActualHeight + 2.
        member __.Enabled = true

        member x.GetTextViewMargin name =
            match name with
            | Constants.QuickInfoMargin -> upcast x
            | _ -> Unchecked.defaultof<_>

    interface IDisposable with
        member __.Dispose() = (docEventListener :> IDisposable).Dispose()
