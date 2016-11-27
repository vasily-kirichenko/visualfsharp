﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.Immutable
open System.Threading
open System.Threading.Tasks
open System.Linq
open System.Runtime.CompilerServices

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Completion
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Editor
open Microsoft.CodeAnalysis.Editor.Implementation.Debugging
open Microsoft.CodeAnalysis.Editor.Shared.Utilities
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Options
open Microsoft.CodeAnalysis.Text

open Microsoft.VisualStudio.FSharp.LanguageService
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Shell.Interop

open Microsoft.FSharp.Compiler.Parser
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.ItemDescriptionIcons

type internal FSharpCompletionProvider(workspace: Workspace, serviceProvider: SVsServiceProvider) =
    inherit CompletionProvider()

    static let declarationItemsCache = ConditionalWeakTable<string, FSharpDeclarationListItem>()
    
    let xmlMemberIndexService = serviceProvider.GetService(typeof<IVsXMLMemberIndexService>) :?> IVsXMLMemberIndexService
    let documentationBuilder = XmlDocumentation.CreateDocumentationBuilder(xmlMemberIndexService, serviceProvider.DTE)

    static let isStartingNewWord (text: SourceText, characterPosition: int) =
        let ch = text.[characterPosition]
        
        if (not (Char.IsLetter ch)) then false
        // Only want to trigger if we're the first character in an identifier.  If there's a
        // character before or after us, then we don't want to trigger.
        elif characterPosition > 0 && Char.IsLetterOrDigit (text.[characterPosition - 1]) then false
        elif characterPosition < text.Length - 1 && Char.IsLetterOrDigit(text.[characterPosition + 1]) then false
        else true

    static member ShouldTriggerCompletionAux(sourceText: SourceText, caretPosition: int, trigger: CompletionTriggerKind, getInfo: (unit -> DocumentId * string * string list)) =
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

    static member ProvideCompletionsAsyncAux(sourceText: SourceText, caretPosition: int, options: FSharpProjectOptions, filePath: string, textVersionHash: int) = async {
        let! parseResults = FSharpLanguageService.Checker.ParseFileInProject(filePath, sourceText.ToString(), options)
        let! checkFileAnswer = FSharpLanguageService.Checker.CheckFileInProject(parseResults, filePath, textVersionHash, sourceText.ToString(), options)
        let checkFileResults = 
            match checkFileAnswer with
            | FSharpCheckFileAnswer.Aborted -> failwith "Compilation isn't complete yet or was cancelled"
            | FSharpCheckFileAnswer.Succeeded(results) -> results

        let textLine = sourceText.Lines.GetLineFromPosition(caretPosition)
        let textLinePos = sourceText.Lines.GetLinePosition(caretPosition)
        let fcsTextLineNumber = textLinePos.Line + 1 // Roslyn line numbers are zero-based, FSharp.Compiler.Service line numbers are 1-based
        let textLineColumn = textLinePos.Character

        let qualifyingNames, partialName = QuickParse.GetPartialLongNameEx(textLine.ToString(), textLineColumn - 1) 
        let! declarations = checkFileResults.GetDeclarationListInfo(Some(parseResults), fcsTextLineNumber, textLineColumn, textLine.ToString(), qualifyingNames, partialName)

        let results = List<CompletionItem>()

        for declarationItem in declarations.Items do
            // FSROSLYNTODO: This doesn't yet reflect pulbic/private/internal into the glyph
            // FSROSLYNTODO: We should really use FSharpSymbol information here.  But GetDeclarationListInfo doesn't provide it, and switch to GetDeclarationListSymbols is a bit large at the moment
            let glyph = 
                match declarationItem.GlyphMajor with 
                | GlyphMajor.Class -> Glyph.ClassPublic
                | GlyphMajor.Constant -> Glyph.ConstantPublic
                | GlyphMajor.Delegate -> Glyph.DelegatePublic
                | GlyphMajor.Enum -> Glyph.EnumPublic
                | GlyphMajor.EnumMember -> Glyph.EnumMember
                | GlyphMajor.Event -> Glyph.EventPublic
                | GlyphMajor.Exception -> Glyph.ClassPublic
                | GlyphMajor.FieldBlue -> Glyph.FieldPublic
                | GlyphMajor.Interface -> Glyph.InterfacePublic
                | GlyphMajor.Method -> Glyph.MethodPublic
                | GlyphMajor.Method2 -> Glyph.ExtensionMethodPublic
                | GlyphMajor.Module -> Glyph.ModulePublic
                | GlyphMajor.NameSpace -> Glyph.Namespace
                | GlyphMajor.Property -> Glyph.PropertyPublic
                | GlyphMajor.Struct -> Glyph.StructurePublic
                | GlyphMajor.Typedef -> Glyph.ClassPublic
                | GlyphMajor.Type -> Glyph.ClassPublic
                | GlyphMajor.Union -> Glyph.EnumPublic
                | GlyphMajor.Variable -> Glyph.Local
                | GlyphMajor.ValueType -> Glyph.StructurePublic
                | GlyphMajor.Error -> Glyph.Error
                | _ -> Glyph.ClassPublic

            let completionItem = CommonCompletionItem.Create(declarationItem.Name, glyph=Nullable(glyph))
            declarationItemsCache.Remove(completionItem.DisplayText) |> ignore // clear out stale entries if they exist
            declarationItemsCache.Add(completionItem.DisplayText, declarationItem)
            results.Add(completionItem)

        return results
    }


    override this.ShouldTriggerCompletion(sourceText: SourceText, caretPosition: int, trigger: CompletionTrigger, _: OptionSet) =
        let getInfo() = 
            let documentId = workspace.GetDocumentIdInCurrentContext(sourceText.Container)
            let document = workspace.CurrentSolution.GetDocument(documentId)
        
            let defines = 
                match FSharpLanguageService.GetOptions(document.Project.Id) with
                | None -> []
                | Some(options) -> CompilerEnvironment.GetCompilationDefinesForEditing(document.Name, options.OtherOptions |> Seq.toList)
            document.Id, document.FilePath, defines

        FSharpCompletionProvider.ShouldTriggerCompletionAux(sourceText, caretPosition, trigger.Kind, getInfo)
    
    override this.ProvideCompletionsAsync(context: Microsoft.CodeAnalysis.Completion.CompletionContext) =
        async {
            match FSharpLanguageService.GetOptions(context.Document.Project.Id) with
            | Some(options) ->
                let! sourceText = context.Document.GetTextAsync(context.CancellationToken) |> Async.AwaitTask
                let! textVersion = context.Document.GetTextVersionAsync(context.CancellationToken) |> Async.AwaitTask
                let! results = FSharpCompletionProvider.ProvideCompletionsAsyncAux(sourceText, context.Position, options, context.Document.FilePath, textVersion.GetHashCode())
                context.AddItems(results)
            | None -> ()
        } |> CommonRoslynHelpers.StartAsyncUnitAsTask context.CancellationToken
        

    override this.GetDescriptionAsync(_: Document, completionItem: CompletionItem, cancellationToken: CancellationToken): Task<CompletionDescription> =
        async {
            let exists, declarationItem = declarationItemsCache.TryGetValue(completionItem.DisplayText)
            if exists then
                let! description = declarationItem.DescriptionTextAsync
                let datatipText = XmlDocumentation.BuildDataTipText(documentationBuilder, description) 
                return CompletionDescription.FromText(datatipText)
            else
                return CompletionDescription.Empty
        } |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken
