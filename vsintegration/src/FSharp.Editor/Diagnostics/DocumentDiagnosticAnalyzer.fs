// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Immutable
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.SolutionCrawler

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Range

open Microsoft.VisualStudio.FSharp.LanguageService
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
type internal DiagnosticsType =
    | Syntax
    | Semantic

module private Log =
    let private logLock = obj()
    
    let log fileName msg =
        Printf.kprintf (fun s ->
            lock logLock <| fun _ -> System.IO.File.AppendAllText (@"e:\DocumentDiagnosticAnalyzer.txt", sprintf "%O %s %s\n" DateTime.Now fileName s)) msg

[<DiagnosticAnalyzer(FSharpCommonConstants.FSharpLanguageName)>]
type internal FSharpDocumentDiagnosticAnalyzer() =
    inherit DocumentDiagnosticAnalyzer()

    let getChecker(document: Document) =
        document.Project.Solution.Workspace.Services.GetService<FSharpCheckerWorkspaceService>().Checker

    let getProjectInfoManager(document: Document) =
        document.Project.Solution.Workspace.Services.GetService<FSharpCheckerWorkspaceService>().ProjectInfoManager
    
    static let errorInfoEqualityComparer =
        { new IEqualityComparer<FSharpErrorInfo> with 
            member __.Equals (x, y) =
                x.FileName = y.FileName &&
                x.StartLineAlternate = y.StartLineAlternate &&
                x.EndLineAlternate = y.EndLineAlternate &&
                x.StartColumn = y.StartColumn &&
                x.EndColumn = y.EndColumn &&
                x.Severity = y.Severity &&
                x.Message = y.Message &&
                x.Subcategory = y.Subcategory &&
                x.ErrorNumber = y.ErrorNumber
            member __.GetHashCode x =
                let mutable hash = 17
                hash <- hash * 23 + x.StartLineAlternate.GetHashCode()
                hash <- hash * 23 + x.EndLineAlternate.GetHashCode()
                hash <- hash * 23 + x.StartColumn.GetHashCode()
                hash <- hash * 23 + x.EndColumn.GetHashCode()
                hash <- hash * 23 + x.Severity.GetHashCode()
                hash <- hash * 23 + x.Message.GetHashCode()
                hash <- hash * 23 + x.Subcategory.GetHashCode()
                hash <- hash * 23 + x.ErrorNumber.GetHashCode()
                hash 
        }

    let diagnosticsTable = ConditionalWeakTable<DocumentId * int * DiagnosticsType * FSharpProjectOptions, Diagnostic[]>()

    static member GetDiagnostics(checker: FSharpChecker, filePath: string, sourceText: SourceText, textVersionHash: int, options: FSharpProjectOptions, 
                                 diagnosticType: DiagnosticsType) = 
        async {
            let! parseResults = checker.ParseFileInProject(filePath, sourceText.ToString(), options) 
            let! errors = 
                async {
                    match diagnosticType with
                    | DiagnosticsType.Semantic ->
                        let! checkResultsAnswer = checker.CheckFileInProject(parseResults, filePath, textVersionHash, sourceText.ToString(), options) 
                        match checkResultsAnswer with
                        | FSharpCheckFileAnswer.Aborted -> return [||]
                        | FSharpCheckFileAnswer.Succeeded results ->
                            // In order to eleminate duplicates, we should not return parse errors here because they are returned by `AnalyzeSyntaxAsync` method.
                            let allErrors = HashSet(results.Errors, errorInfoEqualityComparer)
                            allErrors.ExceptWith(parseResults.Errors)
                            return Seq.toArray allErrors
                    | DiagnosticsType.Syntax ->
                        return parseResults.Errors
                }
            
            return
                HashSet(errors, errorInfoEqualityComparer)
                |> Seq.choose(fun error ->
                    if error.StartLineAlternate = 0 || error.EndLineAlternate = 0 then
                        // F# error line numbers are one-based. Compiler returns 0 for global errors (reported by ProjectDiagnosticAnalyzer)
                        None
                    else
                        // Roslyn line numbers are zero-based
                        let linePositionSpan = LinePositionSpan(LinePosition(error.StartLineAlternate - 1, error.StartColumn), LinePosition(error.EndLineAlternate - 1, error.EndColumn))
                        let textSpan = sourceText.Lines.GetTextSpan(linePositionSpan)
                        
                        // F# compiler report errors at end of file if parsing fails. It should be corrected to match Roslyn boundaries
                        let correctedTextSpan =
                            if textSpan.End <= sourceText.Length then 
                                textSpan 
                            else 
                                let start =
                                    min textSpan.Start (sourceText.Length - 1)
                                    |> max 0

                                TextSpan.FromBounds(start, sourceText.Length)
                        
                        let location = Location.Create(filePath, correctedTextSpan , linePositionSpan)
                        Some(CommonRoslynHelpers.ConvertError(error, location)))
                 |> Seq.toArray
        }

    static member GetDiagnosticsWithCache(checker: FSharpChecker, document: Document, textVersionHash: int, options: FSharpProjectOptions, 
                                          diagnosticType: DiagnosticsType, table: ConditionalWeakTable<DocumentId * int * DiagnosticsType * FSharpProjectOptions, Diagnostic[]>) =
        let key = (document.Id, textVersionHash, diagnosticType, options)                                       
        match table.TryGetValue key with
        | true, diagnostics ->
            Log.log document.FilePath "Returning %d diagnostics from cache" diagnostics.Length
            async.Return diagnostics
        | _ ->
            Log.log document.FilePath "NOT FOUND in cache, checking the file"
            async {
                let! cancellationToken = Async.CancellationToken
                let! sourceText = document.GetTextAsync(cancellationToken)
                let! diagnostics = FSharpDocumentDiagnosticAnalyzer.GetDiagnostics(checker, document.FilePath, sourceText, textVersionHash, options, diagnosticType)
                table.Remove key |> ignore
                table.Add(key, diagnostics)
                return diagnostics
            }

    override this.SupportedDiagnostics = CommonRoslynHelpers.SupportedDiagnostics()

    override this.AnalyzeSyntaxAsync(document: Document, cancellationToken: CancellationToken): Task<ImmutableArray<Diagnostic>> =
        let projectInfoManager = getProjectInfoManager document
        asyncMaybe {
            let! options = projectInfoManager.TryGetOptionsForEditingDocumentOrProject(document)
            let! textVersion = document.GetTextVersionAsync(cancellationToken)
            let! diagnostics =
                FSharpDocumentDiagnosticAnalyzer.GetDiagnosticsWithCache(getChecker document, document, textVersion.GetHashCode(), options, DiagnosticsType.Syntax, diagnosticsTable)
                |> liftAsync
            return diagnostics.ToImmutableArray()
        } 
        |> Async.map (Option.defaultValue ImmutableArray<Diagnostic>.Empty)
        |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken

    override this.AnalyzeSemanticsAsync(document: Document, cancellationToken: CancellationToken): Task<ImmutableArray<Diagnostic>> =
        let projectInfoManager = getProjectInfoManager document
        asyncMaybe {
            let! options = projectInfoManager.TryGetOptionsForDocumentOrProject(document) 
            let! textVersion = document.GetTextVersionAsync(cancellationToken)
            let! diagnostics =
                FSharpDocumentDiagnosticAnalyzer.GetDiagnosticsWithCache(getChecker document, document, textVersion.GetHashCode(), options, DiagnosticsType.Semantic, diagnosticsTable)
                |> liftAsync
            return diagnostics.ToImmutableArray()
        }
        |> Async.map (Option.defaultValue ImmutableArray<Diagnostic>.Empty)
        |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken

    interface IBuiltInAnalyzer with
        member __.GetAnalyzerCategory() : DiagnosticAnalyzerCategory = DiagnosticAnalyzerCategory.SemanticDocumentAnalysis
        member __.OpenFileOnly _ = true

