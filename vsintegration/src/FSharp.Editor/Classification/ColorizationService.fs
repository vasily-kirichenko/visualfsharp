﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Generic
open System.Threading
open System.Runtime.CompilerServices
open System.Diagnostics

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Editor
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text

open Microsoft.VisualStudio.FSharp.LanguageService
open Microsoft.FSharp.Compiler.SourceCodeServices

[<ExportLanguageService(typeof<IEditorClassificationService>, FSharpCommonConstants.FSharpLanguageName)>]
type internal FSharpColorizationService
    [<ImportingConstructor>]
    (
        checkerProvider: FSharpCheckerProvider,
        projectInfoManager: ProjectInfoManager
    ) =
    interface IEditorClassificationService with
        // Do not perform classification if we don't have project options (#defines matter)
        member this.AddLexicalClassifications(_: SourceText, _: TextSpan, _: List<ClassifiedSpan>, _: CancellationToken) = ()
        
        member this.AddSyntacticClassificationsAsync(document: Document, textSpan: TextSpan, result: List<ClassifiedSpan>, cancellationToken: CancellationToken) =
            async {
                let mutable times = Map.empty
                let sw = Stopwatch.StartNew()
                let defines = projectInfoManager.GetCompilationDefinesForEditingDocument(document)  
                times <- times |> Map.add "defines" sw.ElapsedMilliseconds
                sw.Restart()
                let! sourceText = document.GetTextAsync(cancellationToken)
                times <- times |> Map.add "source text" sw.ElapsedMilliseconds
                sw.Restart()
                result.AddRange(CommonHelpers.getColorizationData(document.Id, sourceText, textSpan, Some(document.FilePath), defines, cancellationToken))
                times <- times |> Map.add "get data" sw.ElapsedMilliseconds
                let lines = sourceText.Lines
                let startLine = lines.GetLineFromPosition(textSpan.Start).LineNumber
                let endLine = lines.GetLineFromPosition(textSpan.End).LineNumber
                Logging.Logging.logInfof "SyntacticClassification (lines %d..%d): %A" startLine endLine times
            } |> CommonRoslynHelpers.StartAsyncUnitAsTask cancellationToken

        member this.AddSemanticClassificationsAsync(document: Document, textSpan: TextSpan, result: List<ClassifiedSpan>, cancellationToken: CancellationToken) =
            asyncMaybe {
                let! options = projectInfoManager.TryGetOptionsForEditingDocumentOrProject(document)
                let! sourceText = document.GetTextAsync(cancellationToken)
                let! _, checkResults = checkerProvider.Checker.ParseAndCheckDocument(document, options, sourceText) 
                // it's crucial to not return duplicated or overlapping `ClassifiedSpan`s because Find Usages service crashes.
                let colorizationData = checkResults.GetExtraColorizationsAlternate() |> Array.distinctBy fst
                for (range, tokenColorKind) in colorizationData do
                    let span = CommonHelpers.fixupSpan(sourceText, CommonRoslynHelpers.FSharpRangeToTextSpan(sourceText, range))
                    if textSpan.Contains(span.Start) || textSpan.Contains(span.End - 1) || span.Contains(textSpan) then
                        result.Add(ClassifiedSpan(span, CommonHelpers.compilerTokenToRoslynToken(tokenColorKind)))
            } |> Async.Ignore |> CommonRoslynHelpers.StartAsyncUnitAsTask cancellationToken

        // Do not perform classification if we don't have project options (#defines matter)
        member this.AdjustStaleClassification(_: SourceText, classifiedSpan: ClassifiedSpan) : ClassifiedSpan = classifiedSpan



