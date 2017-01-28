// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.OLE.Interop
open System.ComponentModel.Composition
open Microsoft.VisualStudio
open Microsoft.VisualStudio.Editor
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.TextManager.Interop
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Shell.Interop
open Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.VisualStudio.FSharp.LanguageService
open Microsoft.CodeAnalysis.Editor
open Microsoft.VisualStudio.Text.Operations
open Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
open Microsoft.CodeAnalysis.Text.Shared.Extensions
open Microsoft.CodeAnalysis.Shared.Extensions

[<ExportCommandHandler("BlockCommentEditingCommandHandler", FSharpCommonConstants.FSharpContentTypeName)>]
[<Order(After = PredefinedCommandHandlerNames.Completion)>]
type internal BlockCommentEditingCommandHandler
    [<ImportingConstructor>]
    (
        undoHistoryRegistry: ITextUndoHistoryRegistry,
        editorOperationsFactoryService: IEditorOperationsFactoryService
    ) =
    
    inherit AbstractBlockCommentEditingCommandHandler(undoHistoryRegistry, editorOperationsFactoryService)
 
    let blockCommentEndsRightAfterCaret(caretPosition: SnapshotPoint) = 
        let snapshot = caretPosition.Snapshot
        if (int caretPosition) + 2 <= snapshot.Length then snapshot.GetText(caretPosition.Position, 2) = "*/" else false
 
    let getFirstNonWhitespaceOffset(line: string) =
        line |> Seq.tryFindIndex (not << Char.IsWhiteSpace)

    let getPaddingOrIndentation(currentLine: ITextSnapshotLine, caretPosition: int, firstNonWhitespacePosition: int, exteriorText: string) =
        assert(caretPosition >= firstNonWhitespacePosition + exteriorText.Length)
 
        let firstNonWhitespaceOffset = firstNonWhitespacePosition - currentLine.Start.Position
        assert(firstNonWhitespaceOffset > -1)
 
        let lineText = currentLine.GetText()
        if lineText.Length = firstNonWhitespaceOffset + exteriorText.Length then
            //     *|
            " "
        else
            let interiorText = lineText.Substring(firstNonWhitespaceOffset + exteriorText.Length)
            let interiorFirstNonWhitespaceOffset = 
                match getFirstNonWhitespaceOffset(interiorText) with
                | None -> -1
                | Some x -> x
     
            if interiorFirstNonWhitespaceOffset = 0 then
                //    /****|
                " "
            else
                let interiorFirstWhitespacePosition = firstNonWhitespacePosition + exteriorText.Length
                if interiorFirstNonWhitespaceOffset = -1 || caretPosition <= interiorFirstWhitespacePosition + interiorFirstNonWhitespaceOffset then
                    // *  |
                    // or
                    // *  |  1.
                    //  ^^
                    currentLine.Snapshot.GetText(interiorFirstWhitespacePosition, caretPosition - interiorFirstWhitespacePosition)
                else
                    // *   1. |
                    //  ^^^
                    currentLine.Snapshot.GetText(interiorFirstWhitespacePosition, interiorFirstNonWhitespaceOffset)
 
    let isCaretInsideBlockCommentSyntax(_caretPosition: SnapshotPoint) =
        //let document = SnapshotSourceText.From(caretPosition.Snapshot).GetOpenDocumentInCurrentContextWithChanges()
        //if isNull document then false
        //else true // todo insert real logic here
        true

    override __.GetExteriorTextForNextLine(caretPosition: SnapshotPoint) =
        let currentLine = caretPosition.GetContainingLine()
        let firstNonWhitespacePosition = 
            match ITextSnapshotLineExtensions.GetFirstNonWhitespacePosition(currentLine) |> Option.ofNullable with
            | None -> -1
            | Some x -> x
        
        if firstNonWhitespacePosition = -1 then null
        else
            let currentLineStartsWithBlockCommentStartString = ITextSnapshotLineExtensions.StartsWith(currentLine, firstNonWhitespacePosition, "/*", ignoreCase = false)
            let currentLineStartsWithBlockCommentEndString = ITextSnapshotLineExtensions.StartsWith(currentLine, firstNonWhitespacePosition, "*/", ignoreCase = false)
            let currentLineStartsWithBlockCommentMiddleString = ITextSnapshotLineExtensions.StartsWith(currentLine, firstNonWhitespacePosition, "*", ignoreCase = false)
            
            if not currentLineStartsWithBlockCommentStartString && not currentLineStartsWithBlockCommentMiddleString then null
            elif not (isCaretInsideBlockCommentSyntax(caretPosition)) then null
            elif currentLineStartsWithBlockCommentStartString then
                if blockCommentEndsRightAfterCaret(caretPosition) then 
                    //      /*|*/
                    " "
                elif caretPosition.Position = firstNonWhitespacePosition + 1 then
                    //      /|*
                    null // The newline inserted could break the syntax in a way that this handler cannot fix, let's leave it.
                else
                    //      /*|
                    " *" + getPaddingOrIndentation(currentLine, caretPosition.Position, firstNonWhitespacePosition, "/*")
            elif currentLineStartsWithBlockCommentEndString then
                if blockCommentEndsRightAfterCaret(caretPosition) then
                    //      /*
                    //      |*/
                    " "
                elif caretPosition.Position = firstNonWhitespacePosition + 1 then
                    //      *|/
                    "*"
                else
                    //      /*
                    //   |   */
                    " * "
            elif currentLineStartsWithBlockCommentMiddleString then
                if blockCommentEndsRightAfterCaret(caretPosition) then
                    //      *|*/
                    ""
                elif caretPosition.Position > firstNonWhitespacePosition then
                    //      *|
                    "*" + getPaddingOrIndentation(currentLine, caretPosition.Position, firstNonWhitespacePosition, "*")
                else
                    //      /*
                    //   |   *
                    " * "
            else null
 
    