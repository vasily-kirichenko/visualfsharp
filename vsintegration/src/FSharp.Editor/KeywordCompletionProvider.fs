// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Completion
open Microsoft.CodeAnalysis.Options
open Microsoft.CodeAnalysis.Text

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.ItemDescriptionIcons

type internal FSharpKeywordCompletionProvider(workspace: Workspace) =
    inherit CompletionProvider()

    let completionItems =
        FSharpKeywords.keywordsWithDescription
        |> List.map (fun (keyword, description) -> 
             CommonCompletionItem.Create(keyword, Nullable(Glyph.Keyword)).AddProperty("description", description))

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
        async { context.AddItems(completionItems) } 
        |> CommonRoslynHelpers.StartAsyncUnitAsTask context.CancellationToken

    override this.GetDescriptionAsync(_: Document, completionItem: CompletionItem, cancellationToken: CancellationToken): Task<CompletionDescription> =
        async {
            return CompletionDescription.FromText(completionItem.Properties.["description"])
        } |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken
