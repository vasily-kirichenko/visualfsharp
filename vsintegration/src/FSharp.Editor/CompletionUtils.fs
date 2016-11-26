// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Threading

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Completion
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Text

module Utils =
    let private isStartingNewWord (text: SourceText, characterPosition: int) =
        let ch = text.[characterPosition]
        
        if (not (Char.IsLetter ch)) then false
        // Only want to trigger if we're the first character in an identifier.  If there's a
        // character before or after us, then we don't want to trigger.
        elif characterPosition > 0 && Char.IsLetterOrDigit (text.[characterPosition - 1]) then false
        elif characterPosition < text.Length - 1 && Char.IsLetterOrDigit(text.[characterPosition + 1]) then false
        else true

    let shouldTriggerCompletionAux(sourceText: SourceText, caretPosition: int, trigger: CompletionTriggerKind, getInfo: (unit -> DocumentId * string * string list)) =
        // Skip if we are at the start of a document
        if caretPosition = 0 then
            false

        // Skip if it was triggered by an operation other than insertion
        elif not (trigger = CompletionTriggerKind.Insertion) then
            false

        // Skip if we are not on a completion trigger
        else
          let ch = sourceText.[caretPosition - 1]
          if ch <> '.' && not ((ch = ' ' && (caretPosition = sourceText.Length - 1) || isStartingNewWord (sourceText, caretPosition - 1))) then
            false

          // Trigger completion if we are on a valid classification type
          else
            let documentId, filePath,  defines = getInfo()
            let triggerPosition = caretPosition - 1
            let textLine = sourceText.Lines.GetLineFromPosition(triggerPosition)
            let classifiedSpanOption =
                FSharpColorizationService.GetColorizationData(documentId, sourceText, textLine.Span, Some(filePath), defines, CancellationToken.None)
                |> Seq.tryFind(fun classifiedSpan -> classifiedSpan.TextSpan.Contains(triggerPosition))

            match classifiedSpanOption with
            | None -> false
            | Some(classifiedSpan) ->
                match classifiedSpan.ClassificationType with
                | ClassificationTypeNames.Comment -> false
                | ClassificationTypeNames.StringLiteral -> false
                | ClassificationTypeNames.ExcludedCode -> false
                | _ -> true // anything else is a valid classification type