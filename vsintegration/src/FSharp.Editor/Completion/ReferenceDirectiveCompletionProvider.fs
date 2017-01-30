// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace rec Microsoft.VisualStudio.FSharp.Editor

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
open Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
open Microsoft.CodeAnalysis.Editor.Shared.Utilities
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Host
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

open Microsoft.VisualStudio.FSharp.Editor.Logging
open System.Diagnostics

type internal FSharpReferenceDirectiveCompletionProvider
    (
        _workspace: Workspace,
        _serviceProvider: SVsServiceProvider,
        projectInfoManager: ProjectInfoManager
    ) =
    inherit CommonCompletionProvider()

    //let getFileSystemDiscoveryService(textSnapshot: ITextSnapshot) : ICurrentWorkingDirectoryDiscoveryService =
    //    CurrentWorkingDirectoryDiscoveryService.GetService(textSnapshot);
 
    //let s_commitRules : ImmutableArray<CharacterSetModificationRule> =
    //    ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '"', '\\', ','))
 
    //let s_filterRules : ImmutableArray<CharacterSetModificationRule> = ImmutableArray<CharacterSetModificationRule>.Empty
 
    //let s_rules = 
    //    CompletionItemRules.Create(
    //        filterCharacterRules = s_filterRules, 
    //        commitCharacterRules = s_commitRules, 
    //        enterKeyRule = EnterKeyRule.Never)
    
    //let getPathThroughLastSlash(SyntaxToken stringLiteral, int position)
    //{
    //    return PathCompletionUtilities.GetPathThroughLastSlash(
    //        quotedPath: stringLiteral.ToString(),
    //        quotedPathStart: stringLiteral.SpanStart,
    //        position: position);
    //}

    override __.IsInsertionTrigger(text, characterPosition, _options) = PathCompletionUtilities.IsTriggerCharacter(text, characterPosition)
 
    override this.ProvideCompletionsAsync(context) =
        asyncMaybe {
            let document = context.Document
            let position = context.Position
            let cancellationToken = context.CancellationToken
            let! sourceText = document.GetTextAsync(cancellationToken) |> liftAsync
            let defines = projectInfoManager.GetCompilationDefinesForEditingDocument(document)  
            let tokens = CommonHelpers.tokenizeLine(document.Id, sourceText, position, document.FilePath, defines)
            
            // first try to get the #r string literal token.  If we couldn't, then we're not in a #r
            // reference directive and we immediately bail.
            let! stringLiteralToken =
                tokens
                |> List.skipWhile (fun x -> x.CharClass = FSharpTokenCharKind.WhiteSpace)
                |> function
                   | hashToken :: rest when hashToken.TokenName = "HASH" ->
                       match rest |> List.skipWhile (fun x -> x.CharClass = FSharpTokenCharKind.WhiteSpace) with
                       | x :: _ when x.Tag = FSharpTokenTag.STRING -> Some x
                       | _ -> None
                   | _ -> None
            
            let textChangeSpan =
                let line = sourceText.Lines.GetLineFromPosition(position)
                let stringLiteralSpan = TextSpan(line.Start + stringLiteralToken.LeftColumn, stringLiteralToken.FullMatchedLength)

                PathCompletionUtilities.GetTextChangeSpan(
                    quotedPath = sourceText.ToString(stringLiteralSpan),
                    quotedPathStart = stringLiteralSpan.Start,
                    position = position)
            
            let gacHelper = Microsoft.CodeAnalysis.Editor.Completion.FileSystem.GlobalAssemblyCacheCompletionHelper(this, textChangeSpan, itemRules: s_rules);
            //var text = await document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            //var snapshot = text.FindCorrespondingEditorTextSnapshot();
            //if (snapshot == null)
            //{
            //    // Passing null to GetFileSystemDiscoveryService raises an exception.
            //    // Instead, return here since there is no longer snapshot for this document.
            //    return;
            //}
            
            //var referenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver;
            
            //// TODO: https://github.com/dotnet/roslyn/issues/5263
            //// Avoid dependency on a specific resolvers.
            //// The search paths should be provided by specialized workspaces:
            //// - InteractiveWorkspace for interactive window 
            //// - ScriptWorkspace for loose .csx files (we don't have such workspace today)
            //ImmutableArray<string> searchPaths;
            
            //RuntimeMetadataReferenceResolver rtResolver;
            //WorkspaceMetadataFileReferenceResolver workspaceResolver;
            
            //if ((rtResolver = referenceResolver as RuntimeMetadataReferenceResolver) != null)
            //{
            //    searchPaths = rtResolver.PathResolver.SearchPaths;
            //}
            //else if ((workspaceResolver = referenceResolver as WorkspaceMetadataFileReferenceResolver) != null)
            //{
            //    searchPaths = workspaceResolver.PathResolver.SearchPaths;
            //}
            //else
            //{
            //    return;
            //}
            
            //var fileSystemHelper = new FileSystemCompletionHelper(
            //    this, textChangeSpan,
            //    GetFileSystemDiscoveryService(snapshot),
            //    Glyph.OpenFolder,
            //    Glyph.Assembly,
            //    searchPaths: searchPaths,
            //    allowableExtensions: new[] { ".dll", ".exe" },
            //    exclude: path => path.Contains(","),
            //    itemRules: s_rules);
            
            //var pathThroughLastSlash = GetPathThroughLastSlash(stringLiteral, position);
            
            //var documentPath = document.Project.IsSubmission ? null : document.FilePath;
            //context.AddItems(gacHelper.GetItems(pathThroughLastSlash, documentPath));
            //context.AddItems(fileSystemHelper.GetItems(pathThroughLastSlash, documentPath));
            return ()
        } |> CommonRoslynHelpers.StartAsyncUnitAsTask context.CancellationToken

module internal GlobalAssemblyCacheCompletionHelper =
    open System.Runtime.InteropServices
    //type GlobalAssemblyCache() =
    //    let currentArchitectures = 
    //        [ ProcessorArchitecture.None
    //          ProcessorArchitecture.MSIL
    //          if IntPtr.Size = 4 then ProcessorArchitecture.X86 
    //          else ProcessorArchitecture.Amd64 ]
 
        //member __.GetAssemblySimpleNames(architectureFilter: ProcessorArchitecture list) =

    let [<Literal>] MAX_PATH = 260
 
    [<ComImport; InterfaceType(ComInterfaceType.InterfaceIsIUnknown); Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")>]
    type private IAssemblyEnum =
        [<PreserveSig>]
        abstract GetNextAssembly: byref FusionAssemblyIdentity.IApplicationContext * byref FusionAssemblyIdentity.IAssemblyName * uint -> int
 
        [<PreserveSig>]
        abstract Reset: unit -> int

        [<PreserveSig>]
        abstract Clone: byref IAssemblyEnum -> int
 
    [ComImport, Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAssemblyCache
    {
        void UninstallAssembly();
 
        void QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);
 
        void CreateAssemblyCacheItem();
        void CreateAssemblyScavenger();
        void InstallAssembly();
    }
 
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct ASSEMBLY_INFO
    {
        public uint cbAssemblyInfo;
        public readonly uint dwAssemblyFlags;
        public readonly ulong uliAssemblySizeInKB;
        public char* pszCurrentAssemblyPathBuf;
        public uint cchBuf;
    }
 
    [DllImport("clr", PreserveSig = true)]
    private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, FusionAssemblyIdentity.IApplicationContext pAppCtx, FusionAssemblyIdentity.IAssemblyName pName, ASM_CACHE dwFlags, IntPtr pvReserved);
 
    [DllImport("clr", PreserveSig = false)]
    private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);
 
    #endregion
 
    /// <summary>
    /// Enumerates assemblies in the GAC returning those that match given partial name and
    /// architecture.
    /// </summary>
    /// <param name="partialName">Optional partial name.</param>
    /// <param name="architectureFilter">Optional architecture filter.</param>
    public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
    {
        return GetAssemblyIdentities(FusionAssemblyIdentity.ToAssemblyNameObject(partialName), architectureFilter);
    }
 
    /// <summary>
    /// Enumerates assemblies in the GAC returning those that match given partial name and
    /// architecture.
    /// </summary>
    /// <param name="partialName">The optional partial name.</param>
    /// <param name="architectureFilter">The optional architecture filter.</param>
    public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
    {
        FusionAssemblyIdentity.IAssemblyName nameObj;
        if (partialName != null)
        {
            nameObj = FusionAssemblyIdentity.ToAssemblyNameObject(partialName);
            if (nameObj == null)
            {
                return SpecializedCollections.EmptyEnumerable<AssemblyIdentity>();
            }
        }
        else
        {
            nameObj = null;
        }
 
        return GetAssemblyIdentities(nameObj, architectureFilter);
    }
 
    /// <summary>
    /// Enumerates assemblies in the GAC returning their simple names.
    /// </summary>
    /// <param name="architectureFilter">Optional architecture filter.</param>
    /// <returns>Unique simple names of GAC assemblies.</returns>
    public override IEnumerable<string> GetAssemblySimpleNames(ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
    {
        var q = from nameObject in GetAssemblyObjects(partialNameFilter: null, architectureFilter: architectureFilter)
                select FusionAssemblyIdentity.GetName(nameObject);
        return q.Distinct();
    }
 
    private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(
        FusionAssemblyIdentity.IAssemblyName partialName,
        ImmutableArray<ProcessorArchitecture> architectureFilter)
    {
        return from nameObject in GetAssemblyObjects(partialName, architectureFilter)
                select FusionAssemblyIdentity.ToAssemblyIdentity(nameObject);
    }
 
    private const int S_OK = 0;
    private const int S_FALSE = 1;
 
    // Internal for testing.
    internal static IEnumerable<FusionAssemblyIdentity.IAssemblyName> GetAssemblyObjects(
        FusionAssemblyIdentity.IAssemblyName partialNameFilter,
        ImmutableArray<ProcessorArchitecture> architectureFilter)
    {
        IAssemblyEnum enumerator;
        FusionAssemblyIdentity.IApplicationContext applicationContext = null;
 
        int hr = CreateAssemblyEnum(out enumerator, applicationContext, partialNameFilter, ASM_CACHE.GAC, IntPtr.Zero);
        if (hr == S_FALSE)
        {
            // no assembly found
            yield break;
        }
        else if (hr != S_OK)
        {
            Exception e = Marshal.GetExceptionForHR(hr);
            if (e is FileNotFoundException)
            {
                // invalid assembly name:
                yield break;
            }
            else if (e != null)
            {
                throw e;
            }
            else
            {
                // for some reason it might happen that CreateAssemblyEnum returns non-zero HR that doesn't correspond to any exception:
#if SCRIPTING
                throw new ArgumentException(Microsoft.CodeAnalysis.Scripting.ScriptingResources.InvalidAssemblyName);
#else
                throw new ArgumentException(Microsoft.CodeAnalysis.WorkspaceDesktopResources.Invalid_assembly_name);
#endif
            }
        }
 
        while (true)
        {
            FusionAssemblyIdentity.IAssemblyName nameObject;
 
            hr = enumerator.GetNextAssembly(out applicationContext, out nameObject, 0);
            if (hr != 0)
            {
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
 
                break;
            }
 
            if (!architectureFilter.IsDefault)
            {
                var assemblyArchitecture = FusionAssemblyIdentity.GetProcessorArchitecture(nameObject);
                if (!architectureFilter.Contains(assemblyArchitecture))
                {
                    continue;
                }
            }
 
            yield return nameObject;
        }
    }
 
    public override AssemblyIdentity ResolvePartialName(
        string displayName,
        out string location,
        ImmutableArray<ProcessorArchitecture> architectureFilter,
        CultureInfo preferredCulture)
    {
        if (displayName == null)
        {
            throw new ArgumentNullException(nameof(displayName));
        }
 
        location = null;
        FusionAssemblyIdentity.IAssemblyName nameObject = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
        if (nameObject == null)
        {
            return null;
        }
 
        var candidates = GetAssemblyObjects(nameObject, architectureFilter);
        string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;
 
        var bestMatch = FusionAssemblyIdentity.GetBestMatch(candidates, cultureName);
        if (bestMatch == null)
        {
            return null;
        }
 
        location = GetAssemblyLocation(bestMatch);
        return FusionAssemblyIdentity.ToAssemblyIdentity(bestMatch);
    }
 
    internal static unsafe string GetAssemblyLocation(FusionAssemblyIdentity.IAssemblyName nameObject)
    {
        // NAME | VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE
        string fullName = FusionAssemblyIdentity.GetDisplayName(nameObject, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);
 
        fixed (char* p = new char[MAX_PATH])
        {
            ASSEMBLY_INFO info = new ASSEMBLY_INFO
            {
                cbAssemblyInfo = (uint)Marshal.SizeOf<ASSEMBLY_INFO>(),
                pszCurrentAssemblyPathBuf = p,
                cchBuf = MAX_PATH
            };
 
            IAssemblyCache assemblyCacheObject;
            CreateAssemblyCache(out assemblyCacheObject, 0);
            assemblyCacheObject.QueryAssemblyInfo(0, fullName, ref info);
            Debug.Assert(info.pszCurrentAssemblyPathBuf != null);
            Debug.Assert(info.pszCurrentAssemblyPathBuf[info.cchBuf - 1] == '\0');
 
            var result = Marshal.PtrToStringUni((IntPtr)info.pszCurrentAssemblyPathBuf, (int)info.cchBuf - 1);
            Debug.Assert(result.IndexOf('\0') == -1);
            return result;
        }










 
    let lazyAssemblySimpleNames = lazy (GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList())

    private readonly CompletionProvider _completionProvider;
    private readonly TextSpan _textChangeSpan;
    private readonly CompletionItemRules _itemRules;
 
    public GlobalAssemblyCacheCompletionHelper(
        CompletionProvider completionProvider, 
        TextSpan textChangeSpan, 
        CompletionItemRules itemRules = null)
    {
        _completionProvider = completionProvider;
        _textChangeSpan = textChangeSpan;
        _itemRules = itemRules;
    }
 
    public IEnumerable<CompletionItem> GetItems(string pathSoFar, string documentPath)
    {
        var containsSlash = pathSoFar.Contains(@"/") || pathSoFar.Contains(@"\");
        if (containsSlash)
        {
            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }
 
        return GetCompletionsWorker(pathSoFar).ToList();
    }
 
    private IEnumerable<CompletionItem> GetCompletionsWorker(string pathSoFar)
    {
        var comma = pathSoFar.IndexOf(',');
        if (comma >= 0)
        {
            var path = pathSoFar.Substring(0, comma);
            return from identity in GetAssemblyIdentities(path)
                    let text = identity.GetDisplayName()
                    select CommonCompletionItem.Create(text, glyph: Glyph.Assembly, rules: _itemRules);
        }
        else
        {
            return from displayName in s_lazyAssemblySimpleNames.Value
                    select CommonCompletionItem.Create(
                        displayName,
                        description: GlobalAssemblyCache.Instance.ResolvePartialName(displayName).GetDisplayName().ToSymbolDisplayParts(),
                        glyph: Glyph.Assembly,
                        rules: _itemRules);
        }
    }
 
    private IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string pathSoFar)
    {
        return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(pathSoFar),
            SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
    }
}