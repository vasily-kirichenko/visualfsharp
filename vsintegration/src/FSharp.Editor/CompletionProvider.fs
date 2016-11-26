// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Generic
open System.Collections.Immutable
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Completion
open Microsoft.CodeAnalysis.Options
open Microsoft.CodeAnalysis.Text

open Microsoft.VisualStudio.FSharp.LanguageService
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Shell.Interop

open Microsoft.FSharp.Compiler.Parser
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.ItemDescriptionIcons

type internal FSharpCompletionProvider(workspace: Workspace, serviceProvider: SVsServiceProvider) =
    inherit CompletionProvider()

    static let declarationItemsCache = ConditionalWeakTable<string, FSharpDeclarationListItem>()
    
    let xmlMemberIndexService = serviceProvider.GetService(typeof<IVsXMLMemberIndexService>) :?> IVsXMLMemberIndexService
    let documentationBuilder = XmlDocumentation.CreateDocumentationBuilder(xmlMemberIndexService, serviceProvider.DTE)

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

        Utils.shouldTriggerCompletionAux(sourceText, caretPosition, trigger.Kind, getInfo)
    
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
