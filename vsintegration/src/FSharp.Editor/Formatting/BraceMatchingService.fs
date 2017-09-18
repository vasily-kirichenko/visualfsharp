﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.ComponentModel.Composition
open Microsoft.CodeAnalysis.Editor
open Microsoft.FSharp.Compiler.SourceCodeServices

[<ExportBraceMatcher(FSharpConstants.FSharpLanguageName)>]
type internal FSharpBraceMatchingService 
    [<ImportingConstructor>]
    (
        checkerProvider: FSharpCheckerProvider,
        projectInfoManager: FSharpProjectOptionsManager
    ) =

    static member GetBraceMatchingResult(checker: FSharpChecker, sourceText, fileName, options: FSharpProjectOptions, position: int) = 
        async {
            let! matchedBraces = checker.MatchBraces(fileName, sourceText.ToString(), options.ParsingOptions)
            let isPositionInRange range = 
                match RoslynHelpers.TryFSharpRangeToTextSpan(sourceText, range) with
                | None -> false
                | Some range ->
                    let length = position - range.Start
                    length >= 0 && length <= range.Length
            return matchedBraces |> Array.tryFind(fun (left, right) -> isPositionInRange left || isPositionInRange right)
        }
        
    interface IBraceMatcher with
        member this.FindBracesAsync(document, position, cancellationToken) = 
            asyncMaybe {
                let! options = projectInfoManager.TryGetOptionsForEditingDocumentOrProject(document)
                let! sourceText = document.GetTextAsync(cancellationToken)
                let! (left, right) = FSharpBraceMatchingService.GetBraceMatchingResult(checkerProvider.Checker, sourceText, document.Name, options, position)
                let! leftSpan = RoslynHelpers.TryFSharpRangeToTextSpan(sourceText, left)
                let! rightSpan = RoslynHelpers.TryFSharpRangeToTextSpan(sourceText, right)
                return BraceMatchingResult(leftSpan, rightSpan)
            } 
            |> Async.map Option.toNullable
            |> RoslynHelpers.StartAsyncAsTask cancellationToken
