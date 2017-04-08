// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace rec Microsoft.VisualStudio.FSharp.Editor

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.ComponentModel.Composition

open Microsoft.CodeAnalysis

open Microsoft.VisualStudio
open Microsoft.VisualStudio.LanguageServices
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Utilities

open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text

[<Export(typeof<IWpfTextViewCreationListener>)>]
[<ContentType(FSharpConstants.FSharpContentTypeName)>]
[<TextViewRole(PredefinedTextViewRoles.Document)>]
type internal TextAdornmentTextViewCreationListener() =
    
    [<Export(typeof<AdornmentLayerDefinition>); Name("TypeInfoAdornment")>]
    [<Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)>]
    member val EditorAdornmentLayer: AdornmentLayerDefinition = null
    
    interface IWpfTextViewCreationListener with
       member __.TextViewCreated(textView) = TypeInfoAdornment(textView) |> ignore

type internal TypeInfoAdornment(view: IWpfTextView) =
    let componentModel = Package.GetGlobalService(typeof<ComponentModelHost.SComponentModel>) :?> ComponentModelHost.IComponentModel
    let workspace = componentModel.GetService<VisualStudioWorkspace>()
    let mutable document : Document option = None
    let mutable adornment : TextBlock option = None
    
    let getDocument() =
        match document with
        | Some x -> Some x
        | None ->
            let dte = Package.GetGlobalService(typeof<EnvDTE.DTE>) :?> EnvDTE.DTE
            if not (isNull dte) then
                let activeDocument = dte.ActiveDocument // sometimes we're constructed/invoked before ActiveDocument has been set
                if not (isNull activeDocument) then
                    document <-
                        workspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDocument.FullName) 
                        |> Seq.tryHead
                        |> Option.bind (fun documentId -> workspace.CurrentSolution.GetDocument(documentId) |> Option.ofObj)
            document
    
    // Raised whenever the rendered text displayed in the ITextView changes - whenever the view does a layout
    // (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification
    // changes), and also when the view scrolls or when its size changes.
    // Responsible for adding the adornment to any reformatted lines.
    do view.LayoutChanged.AddHandler(fun _ (e: TextViewLayoutChangedEventArgs) ->
        maybe {
            let! document = getDocument()
            let _ = document
            match adornment with
            | Some _ -> ()
            | None ->
                let! line = e.NewOrReformattedLines |> Seq.tryFind (fun x -> x.Start.GetContainingLine().LineNumber = 0)
                let! geometry = view.TextViewLines.GetMarkerGeometry(line.Extent) |> Option.ofObj
                let a = TextBlock(Width = 400., Height = geometry.Bounds.Height, Background = Brushes.Yellow, Opacity = 0.5)
                //Canvas.SetLeft(a, 0.)
                //Canvas.SetTop(a, geometry.Bounds.Top)
                let tag = 
                    IntraTextAdornmentTag(
                        a, 
                        removalCallback = (fun tag ui -> ()), 
                        topSpace = Nullable 30.,
                        baseline = Nullable 10.,
                        textHeight = Nullable 30.,
                        bottomSpace = Nullable 10.,
                        affinity = Nullable PositionAffinity.Predecessor)
                
                view.GetAdornmentLayer(PredefinedAdornmentLayers.InterLine) // "TypeInfoAdornment")
                    .AddAdornment(AdornmentPositioningBehavior.OwnerControlled, Nullable line.Extent, tag, a, fun tag ui -> adornment <- None) 
                |> ignore
                a.Text <- "fooooo!"
                adornment <- Some a
                
        } |> ignore)
