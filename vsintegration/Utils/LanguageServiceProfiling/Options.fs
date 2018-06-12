module internal  LanguageServiceProfiling.Options

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices
open System
open System.IO

let private (</>) x y = Path.Combine(x, y)

type CompletionPosition = {
    Position: pos
    QualifyingNames : string list
    PartialName : string
}

type Options =
    { Options: FSharpProjectOptions
      FileToCheck: string
      FilesToCheck: string list
      SymbolText: string
      SymbolPos: pos
      CompletionPositions: CompletionPosition list }

let FCS (repositoryDir: string) : Options =
    let files =
          [| @"src\fsharp\FSharp.Compiler.Service\obj\Release\FSComp.fs"
             @"src\fsharp\FSharp.Compiler.Service\obj\Release\FSIstrings.fs"
             @"src\assemblyinfo\assemblyinfo.FSharp.Compiler.Private.dll.fs"
             @"src\assemblyinfo\assemblyinfo.shared.fs"
             @"src\utils\reshapedreflection.fs"
             @"src\utils\sformat.fsi"
             @"src\utils\sformat.fs"
             @"src\fsharp\sr.fsi"
             @"src\fsharp\sr.fs"
             @"src\utils\prim-lexing.fsi"
             @"src\utils\prim-lexing.fs"
             @"src\utils\prim-parsing.fsi"
             @"src\utils\prim-parsing.fs"
             @"src\utils\ResizeArray.fsi"
             @"src\utils\ResizeArray.fs"
             @"src\utils\HashMultiMap.fsi"
             @"src\utils\HashMultiMap.fs"
             @"src\utils\EditDistance.fs"
             @"src\utils\TaggedCollections.fsi"
             @"src\utils\TaggedCollections.fs"
             @"src\fsharp\QueueList.fs"
             @"src\absil\ildiag.fsi"
             @"src\absil\ildiag.fs"
             @"src\absil\illib.fs"
             @"src\utils\filename.fsi"
             @"src\utils\filename.fs"
             @"src\absil\zmap.fsi"
             @"src\absil\zmap.fs"
             @"src\absil\zset.fsi"
             @"src\absil\zset.fs"
             @"src\absil\bytes.fsi"
             @"src\absil\bytes.fs"
             @"src\fsharp\lib.fs"
             @"src\fsharp\ErrorResolutionHints.fs"
             @"src\fsharp\InternalCollections.fsi"
             @"src\fsharp\InternalCollections.fs"
             @"src\fsharp\rational.fsi"
             @"src\fsharp\rational.fs"
             @"src\fsharp\range.fsi"
             @"src\fsharp\range.fs"
             @"src\fsharp\ErrorLogger.fs"
             @"src\fsharp\ReferenceResolver.fs"
             @"src\absil\il.fsi"
             @"src\absil\il.fs"
             @"src\absil\ilx.fsi"
             @"src\absil\ilx.fs"
             @"src\absil\ilascii.fsi"
             @"src\absil\ilascii.fs"
             @"src\absil\ilprint.fsi"
             @"src\absil\ilprint.fs"
             @"src\absil\ilmorph.fsi"
             @"src\absil\ilmorph.fs"
             @"src\absil\ilsupp.fsi"
             @"src\absil\ilsupp.fs"
             @"src\fsharp\FSharp.Compiler.Service\ilpars.fs"
             @"src\fsharp\FSharp.Compiler.Service\illex.fs"
             @"src\absil\ilbinary.fsi"
             @"src\absil\ilbinary.fs"
             @"src\absil\ilread.fsi"
             @"src\absil\ilread.fs"
             @"src\absil\ilwritepdb.fsi"
             @"src\absil\ilwritepdb.fs"
             @"src\absil\ilwrite.fsi"
             @"src\absil\ilwrite.fs"
             @"src\absil\ilreflect.fs"
             @"src\utils\CompilerLocationUtils.fs"
             @"src\fsharp\PrettyNaming.fs"
             @"src\ilx\ilxsettings.fs"
             @"src\ilx\EraseClosures.fsi"
             @"src\ilx\EraseClosures.fs"
             @"src\ilx\EraseUnions.fsi"
             @"src\ilx\EraseUnions.fs"
             @"src\fsharp\UnicodeLexing.fsi"
             @"src\fsharp\UnicodeLexing.fs"
             @"src\fsharp\layout.fsi"
             @"src\fsharp\layout.fs"
             @"src\fsharp\ast.fs"
             @"src\fsharp\FSharp.Compiler.Service\pppars.fs"
             @"src\fsharp\FSharp.Compiler.Service\pars.fs"
             @"src\fsharp\lexhelp.fsi"
             @"src\fsharp\lexhelp.fs"
             @"src\fsharp\FSharp.Compiler.Service\pplex.fs"
             @"src\fsharp\FSharp.Compiler.Service\lex.fs"
             @"src\fsharp\LexFilter.fs"
             @"src\fsharp\tainted.fsi"
             @"src\fsharp\tainted.fs"
             @"src\fsharp\ExtensionTyping.fsi"
             @"src\fsharp\ExtensionTyping.fs"
             @"src\fsharp\QuotationPickler.fsi"
             @"src\fsharp\QuotationPickler.fs"
             @"src\fsharp\tast.fs"
             @"src\fsharp\TcGlobals.fs"
             @"src\fsharp\TastOps.fsi"
             @"src\fsharp\TastOps.fs"
             @"src\fsharp\TastPickle.fsi"
             @"src\fsharp\TastPickle.fs"
             @"src\fsharp\import.fsi"
             @"src\fsharp\import.fs"
             @"src\fsharp\infos.fs"
             @"src\fsharp\AccessibilityLogic.fs"
             @"src\fsharp\AttributeChecking.fs"
             @"src\fsharp\InfoReader.fs"
             @"src\fsharp\NicePrint.fs"
             @"src\fsharp\AugmentWithHashCompare.fsi"
             @"src\fsharp\AugmentWithHashCompare.fs"
             @"src\fsharp\NameResolution.fsi"
             @"src\fsharp\NameResolution.fs"
             @"src\fsharp\TypeRelations.fs"
             @"src\fsharp\SignatureConformance.fs"
             @"src\fsharp\MethodOverrides.fs"
             @"src\fsharp\MethodCalls.fs"
             @"src\fsharp\PatternMatchCompilation.fsi"
             @"src\fsharp\PatternMatchCompilation.fs"
             @"src\fsharp\ConstraintSolver.fsi"
             @"src\fsharp\ConstraintSolver.fs"
             @"src\fsharp\CheckFormatStrings.fsi"
             @"src\fsharp\CheckFormatStrings.fs"
             @"src\fsharp\FindUnsolved.fs"
             @"src\fsharp\QuotationTranslator.fsi"
             @"src\fsharp\QuotationTranslator.fs"
             @"src\fsharp\PostInferenceChecks.fsi"
             @"src\fsharp\PostInferenceChecks.fs"
             @"src\fsharp\TypeChecker.fsi"
             @"src\fsharp\TypeChecker.fs"
             @"src\fsharp\Optimizer.fsi"
             @"src\fsharp\Optimizer.fs"
             @"src\fsharp\DetupleArgs.fsi"
             @"src\fsharp\DetupleArgs.fs"
             @"src\fsharp\InnerLambdasToTopLevelFuncs.fsi"
             @"src\fsharp\InnerLambdasToTopLevelFuncs.fs"
             @"src\fsharp\LowerCallsAndSeqs.fs"
             @"src\fsharp\autobox.fs"
             @"src\fsharp\IlxGen.fsi"
             @"src\fsharp\IlxGen.fs"
             @"src\fsharp\CompileOps.fsi"
             @"src\fsharp\CompileOps.fs"
             @"src\fsharp\CompileOptions.fsi"
             @"src\fsharp\CompileOptions.fs"
             @"src\fsharp\fsc.fsi"
             @"src\fsharp\fsc.fs"
             @"src\fsharp\service\IncrementalBuild.fsi"
             @"src\fsharp\service\IncrementalBuild.fs"
             @"src\fsharp\service\Reactor.fsi"
             @"src\fsharp\service\Reactor.fs"
             @"src\fsharp\service\ServiceConstants.fs"
             @"src\fsharp\symbols\SymbolHelpers.fsi"
             @"src\fsharp\symbols\SymbolHelpers.fs"
             @"src\fsharp\symbols\Symbols.fsi"
             @"src\fsharp\symbols\Symbols.fs"
             @"src\fsharp\symbols\Exprs.fsi"
             @"src\fsharp\symbols\Exprs.fs"
             @"src\fsharp\service\ServiceLexing.fsi"
             @"src\fsharp\service\ServiceLexing.fs"
             @"src\fsharp\service\ServiceParseTreeWalk.fs"
             @"src\fsharp\service\ServiceNavigation.fsi"
             @"src\fsharp\service\ServiceNavigation.fs"
             @"src\fsharp\service\ServiceParamInfoLocations.fsi"
             @"src\fsharp\service\ServiceParamInfoLocations.fs"
             @"src\fsharp\service\ServiceUntypedParse.fsi"
             @"src\fsharp\service\ServiceUntypedParse.fs"
             @"src\utils\reshapedmsbuild.fs"
             @"src\fsharp\SimulatedMSBuildReferenceResolver.fs"
             @"src\fsharp\service\service.fsi"
             @"src\fsharp\service\service.fs"
             @"src\fsharp\service\SimpleServices.fsi"
             @"src\fsharp\service\SimpleServices.fs"
             @"src\fsharp\fsi\fsi.fsi"
             @"src\fsharp\fsi\fsi.fs" |]

    { Options =
        {ProjectFileName = repositoryDir </> @"src\fsharp\FSharp.Compiler.Private\FSharp.Compiler.Private.fsproj"
         ProjectId = None
         SourceFiles = files |> Array.map (fun x -> repositoryDir </> x)
         OtherOptions =
          [|@"-o:obj\Release\FSharp.Compiler.Private.dll"; "-g"; "--noframework";
            @"--baseaddress:0x06800000"; "--define:DEBUG";
            @"--define:CROSS_PLATFORM_COMPILER";
            @"--define:FX_ATLEAST_40"; "--define:BE_SECURITY_TRANSPARENT";
            @"--define:COMPILER";
            @"--define:ENABLE_MONO_SUPPORT"; "--define:FX_MSBUILDRESOLVER_RUNTIMELIKE";
            @"--define:FX_LCIDFROMCODEPAGE"; "--define:FX_RESX_RESOURCE_READER";
            @"--define:FX_RESIDENT_COMPILER"; "--define:SHADOW_COPY_REFERENCES";
            @"--define:EXTENSIONTYPING";
            @"--define:COMPILER_SERVICE_DLL_ASSUMES_FSHARP_CORE_4_4_0_0";
            @"--define:COMPILER_SERVICE_DLL"; "--define:NO_STRONG_NAMES"; "--define:TRACE";
            @"--doc:..\..\..\bin\v4.5\FSharp.Compiler.Service.xml"; "--optimize-";
            @"--platform:anycpu";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll";
            @"-r:" + (repositoryDir </> @"packages\System.Collections.Immutable\lib\netstandard1.0\System.Collections.Immutable.dll");
            @"-r:" + (repositoryDir </> @"packages\FSharp.Core\lib\net40\FSharp.Core.dll");
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll";
            @"-r:" + (repositoryDir </> @"packages\System.Reflection.Metadata\lib\portable-net45+win8\System.Reflection.Metadata.dll");
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Runtime.Serialization.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Collections.Concurrent.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Collections.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.Annotations.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.EventBasedAsync.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Contracts.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Debug.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Tools.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Tracing.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Dynamic.Runtime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Globalization.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.IO.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Expressions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Parallel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Queryable.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.NetworkInformation.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.Requests.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ObjectModel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.ILGeneration.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.Lightweight.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Resources.ResourceManager.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.InteropServices.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.InteropServices.WindowsRuntime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Numerics.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Json.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Xml.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Security.Principal.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Duplex.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Http.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.NetTcp.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Security.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.Encoding.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.Encoding.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.RegularExpressions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.Tasks.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.Tasks.Parallel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.ReaderWriter.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.XDocument.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.XmlSerializer.dll";
            @"--target:library"; "--nowarn:44,62,9,69,65,54,61,75"; "--warnaserror:76";
            @"--vserrors"; "--utf8output"; "--fullpaths"; "--flaterrors";
            @"--subsystemversion:6.00"; "--highentropyva+"; "/warnon:1182"; "--times";
            @"--no-jit-optimize"; "--jit-tracking"|]
         ReferencedProjects = [||]
         IsIncompleteTypeCheckEnvironment = false
         UseScriptResolutionRules = false
         LoadTime = DateTime.Now
         UnresolvedReferences = None;
         OriginalLoadReferences = []
         ExtraProjectInfo = None 
         Stamp = None }
      FilesToCheck = 
          files 
          |> Array.filter (fun s -> s.Contains "TypeChecker.fs" || 
                                    s.Contains "Optimizer.fs" || 
                                    s.Contains "IlxGen.fs" || 
                                    s.Contains "TastOps.fs" || 
                                    s.Contains "TcGlobals.fs" || 
                                    s.Contains "CompileOps.fs" || 
                                    s.Contains "CompileOptions.fs") 
          |>  Array.map (fun x -> repositoryDir </> x) 
          |> Array.toList
      FileToCheck = repositoryDir </> @"src\fsharp\TypeChecker.fs"
      SymbolText = "Some"
      SymbolPos = mkPos 120 7
      CompletionPositions = [] }

let VFPT (repositoryDir: string) : Options =
    { Options =
        {ProjectFileName = repositoryDir </> @"src\FSharp.Editing\FSharp.Editing.fsproj"
         ProjectId = None
         SourceFiles =
          [|@"src\FSharp.Editing\AssemblyInfo.fs";
            @"src\FSharp.Editing\Common\Utils.fs";
            @"src\FSharp.Editing\Common\CompilerLocationUtils.fs";
            @"src\FSharp.Editing\Common\UntypedAstUtils.fs";
            @"src\FSharp.Editing\Common\TypedAstUtils.fs";
            @"src\FSharp.Editing\Common\Lexer.fs";
            @"src\FSharp.Editing\Common\XmlDocParser.fs";
            @"src\FSharp.Editing\Common\IdentifierUtils.fs";
            @"src\FSharp.Editing\ProjectSystem\AssemblyContentProvider.fs";
            @"src\FSharp.Editing\ProjectSystem\OpenDocumentsTracker.fs";
            @"src\FSharp.Editing\ProjectSystem\LanguageService.fs";
            @"src\FSharp.Editing\ProjectSystem\SolutionProvider.fs";
            @"src\FSharp.Editing\Navigation\NavigableItemsCollector.fs";
            @"src\FSharp.Editing\Navigation\NavigateToIndex.fs";
            @"src\FSharp.Editing\Coloring\DepthParser.fs";
            @"src\FSharp.Editing\Coloring\OpenDeclarationsGetter.fs";
            @"src\FSharp.Editing\Coloring\UnopenedNamespacesResolver.fs";
            @"src\FSharp.Editing\Coloring\HighlightUsageInFile.fs";
            @"src\FSharp.Editing\Coloring\PrintfSpecifiersUsageGetter.fs";
            @"src\FSharp.Editing\Symbols\SourceCodeClassifier.fs";
            @"src\FSharp.Editing\CodeGeneration\CodeGeneration.fs";
            @"src\FSharp.Editing\CodeGeneration\SignatureGenerator.fs";
            @"src\FSharp.Editing\CodeGeneration\UnionPatternMatchCaseGenerator.fs";
            @"src\FSharp.Editing\CodeGeneration\InterfaceStubGenerator.fs";
            @"src\FSharp.Editing\CodeGeneration\RecordStubGenerator.fs";
            @"src\FSharp.Editing\TaskListCommentExtractor.fs"|]
          |> Array.map (fun x -> repositoryDir </> x)
         OtherOptions =
          [|@"-o:obj\Release\FSharp.Editing.dll"; "--debug:pdbonly"; "--noframework";
            @"--define:TRACE"; "--doc:bin\Release\FSharp.Editing.XML"; "--optimize+";
            @"-r:" + (repositoryDir </> @"packages\FSharp.Compiler.Service\lib\net45\FSharp.Compiler.Private.dll");
            @"-r:" + (repositoryDir </> @"packages\FSharp.Compiler.Service\lib\net45\FSharp.Compiler.Service.MSBuild.v12.dll");
            @"-r:" + (repositoryDir </> @"packages\FSharp.Core\lib\net40\FSharp.Core.dll");
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll";
            @"-r:" + (repositoryDir </> @"packages\System.Collections.Immutable\lib\netstandard1.0\System.Collections.Immutable.dll");
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll";
            @"-r:" + (repositoryDir </> @"packages\System.Reflection.Metadata\lib\netstandard1.1\System.Reflection.Metadata.dll");
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Collections.Concurrent.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Collections.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.Annotations.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ComponentModel.EventBasedAsync.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Contracts.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Debug.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Tools.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Diagnostics.Tracing.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Dynamic.Runtime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Globalization.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.IO.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Expressions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Parallel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Linq.Queryable.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.NetworkInformation.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Net.Requests.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ObjectModel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.ILGeneration.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Emit.Lightweight.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Reflection.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Resources.ResourceManager.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.InteropServices.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.InteropServices.WindowsRuntime.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Numerics.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Json.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Runtime.Serialization.Xml.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Security.Principal.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Duplex.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Http.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.NetTcp.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Primitives.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.ServiceModel.Security.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.Encoding.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.Encoding.Extensions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Text.RegularExpressions.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.Tasks.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Threading.Tasks.Parallel.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.ReaderWriter.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.XDocument.dll";
            @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\Facades\System.Xml.XmlSerializer.dll";
            @"--target:library"; "--nowarn:52"; "--warn:5"; "--warnaserror";
            @"--warnaserror:76"; "--vserrors"; "--utf8output"; "--fullpaths";
            @"--flaterrors"; "--subsystemversion:6.00"; "--highentropyva+";
            @"--warnon:1182"|];
         ReferencedProjects = [||]
         IsIncompleteTypeCheckEnvironment = false
         UseScriptResolutionRules = false
         LoadTime = DateTime.Now
         UnresolvedReferences = None
         OriginalLoadReferences = []
         ExtraProjectInfo = None 
         Stamp = None }
      FilesToCheck = []
      FileToCheck = repositoryDir </> @"src\FSharp.Editing\CodeGeneration\RecordStubGenerator.fs"
      SymbolText = "option"
      SymbolPos = mkPos 19 23 
      CompletionPositions = 
        [{
            Position = mkPos 20 4
            QualifyingNames = []
            PartialName = ""
        }]
      }

let ``FSharp.Compiler.Private`` (repositoryDir: string) : Options =
    let files =
          [| @"src\fsharp\FSharp.Compiler.Private\obj\Release\net40\FSComp.fs"
             @"src\fsharp\FSharp.Compiler.Private\obj\Release\net40\FSIstrings.fs"
             @"src\fsharp\FSharp.Compiler.Private\obj\Release\net40\FSharp.Compiler.Private.AssemblyVersion.fs"
             @"src\fsharp\FSharp.Compiler.Private\obj\Release\net40\FSharp.Compiler.Private.InternalsVisibleTo.fs"
             @"src\fsharp\Logger.fsi"
             @"src\fsharp\Logger.fs"
             @"src\utils\reshapedreflection.fs"
             @"src\utils\reshapedmsbuild.fs"
             @"src\utils\sformat.fsi"
             @"src\utils\sformat.fs"
             @"src\fsharp\sr.fsi"
             @"src\fsharp\sr.fs"
             @"src\utils\prim-lexing.fsi"
             @"src\utils\prim-lexing.fs"
             @"src\utils\prim-parsing.fsi"
             @"src\utils\prim-parsing.fs"
             @"src\utils\ResizeArray.fsi"
             @"src\utils\ResizeArray.fs"
             @"src\utils\HashMultiMap.fsi"
             @"src\utils\HashMultiMap.fs"
             @"src\utils\EditDistance.fs"
             @"src\utils\TaggedCollections.fsi"
             @"src\utils\TaggedCollections.fs"
             @"src\absil\ildiag.fsi"
             @"src\absil\ildiag.fs"
             @"src\absil\illib.fs"
             @"src\utils\filename.fsi"
             @"src\utils\filename.fs"
             @"src\absil\zmap.fsi"
             @"src\absil\zmap.fs"
             @"src\absil\zset.fsi"
             @"src\absil\zset.fs"
             @"src\absil\bytes.fsi"
             @"src\absil\bytes.fs"
             @"src\fsharp\InternalCollections.fsi"
             @"src\fsharp\InternalCollections.fs"
             @"src\fsharp\QueueList.fs"
             @"src\fsharp\lib.fs"
             @"src\fsharp\rational.fsi"
             @"src\fsharp\rational.fs"
             @"src\fsharp\range.fsi"
             @"src\fsharp\range.fs"
             @"src\fsharp\ErrorLogger.fs"
             @"src\fsharp\ErrorResolutionHints.fs"
             @"src\fsharp\ReferenceResolver.fs"
             @"src\absil\il.fsi"
             @"src\absil\il.fs"
             @"src\absil\ilx.fsi"
             @"src\absil\ilx.fs"
             @"src\absil\ilascii.fsi"
             @"src\absil\ilascii.fs"
             @"src\absil\ilprint.fsi"
             @"src\absil\ilprint.fs"
             @"src\absil\ilmorph.fsi"
             @"src\absil\ilmorph.fs"
             @"src\absil\ilsupp.fsi"
             @"src\absil\ilsupp.fs"
             @"src\fsharp\FSharp.Compiler.Private\ilpars.fs"
             @"src\fsharp\FSharp.Compiler.Private\illex.fs"
             @"src\absil\ilbinary.fsi"
             @"src\absil\ilbinary.fs"
             @"src\absil\ilread.fsi"
             @"src\absil\ilread.fs"
             @"src\absil\ilwrite.fsi"
             @"src\absil\ilwritepdb.fsi"
             @"src\absil\ilwritepdb.fs"
             @"src\absil\ilwrite.fs"
             @"src\absil\ilreflect.fs"
             @"src\utils\CompilerLocationUtils.fs"
             @"src\fsharp\PrettyNaming.fs"
             @"src\ilx\ilxsettings.fs"
             @"src\ilx\EraseClosures.fsi"
             @"src\ilx\EraseClosures.fs"
             @"src\ilx\EraseUnions.fsi"
             @"src\ilx\EraseUnions.fs"
             @"src\fsharp\UnicodeLexing.fsi"
             @"src\fsharp\UnicodeLexing.fs"
             @"src\fsharp\layout.fsi"
             @"src\fsharp\layout.fs"
             @"src\fsharp\ast.fs"
             @"src\fsharp\FSharp.Compiler.Private\pppars.fs"
             @"src\fsharp\FSharp.Compiler.Private\pars.fs"
             @"src\fsharp\lexhelp.fsi"
             @"src\fsharp\lexhelp.fs"
             @"src\fsharp\FSharp.Compiler.Private\pplex.fs"
             @"src\fsharp\FSharp.Compiler.Private\lex.fs"
             @"src\fsharp\LexFilter.fs"
             @"src\fsharp\tainted.fsi"
             @"src\fsharp\tainted.fs"
             @"src\fsharp\ExtensionTyping.fsi"
             @"src\fsharp\ExtensionTyping.fs"
             @"src\fsharp\QuotationPickler.fsi"
             @"src\fsharp\QuotationPickler.fs"
             @"src\fsharp\tast.fs"
             @"src\fsharp\TcGlobals.fs"
             @"src\fsharp\TastOps.fsi"
             @"src\fsharp\TastOps.fs"
             @"src\fsharp\TastPickle.fsi"
             @"src\fsharp\TastPickle.fs"
             @"src\fsharp\import.fsi"
             @"src\fsharp\import.fs"
             @"src\fsharp\infos.fs"
             @"src\fsharp\AccessibilityLogic.fs"
             @"src\fsharp\AttributeChecking.fs"
             @"src\fsharp\InfoReader.fs"
             @"src\fsharp\NicePrint.fs"
             @"src\fsharp\AugmentWithHashCompare.fsi"
             @"src\fsharp\AugmentWithHashCompare.fs"
             @"src\fsharp\NameResolution.fsi"
             @"src\fsharp\NameResolution.fs"
             @"src\fsharp\TypeRelations.fs"
             @"src\fsharp\SignatureConformance.fs"
             @"src\fsharp\MethodOverrides.fs"
             @"src\fsharp\MethodCalls.fs"
             @"src\fsharp\PatternMatchCompilation.fsi"
             @"src\fsharp\PatternMatchCompilation.fs"
             @"src\fsharp\ConstraintSolver.fsi"
             @"src\fsharp\ConstraintSolver.fs"
             @"src\fsharp\CheckFormatStrings.fsi"
             @"src\fsharp\CheckFormatStrings.fs"
             @"src\fsharp\FindUnsolved.fs"
             @"src\fsharp\QuotationTranslator.fsi"
             @"src\fsharp\QuotationTranslator.fs"
             @"src\fsharp\PostInferenceChecks.fsi"
             @"src\fsharp\PostInferenceChecks.fs"
             @"src\fsharp\TypeChecker.fsi"
             @"src\fsharp\TypeChecker.fs"
             @"src\fsharp\Optimizer.fsi"
             @"src\fsharp\Optimizer.fs"
             @"src\fsharp\DetupleArgs.fsi"
             @"src\fsharp\DetupleArgs.fs"
             @"src\fsharp\InnerLambdasToTopLevelFuncs.fsi"
             @"src\fsharp\InnerLambdasToTopLevelFuncs.fs"
             @"src\fsharp\LowerCallsAndSeqs.fs"
             @"src\fsharp\autobox.fs"
             @"src\fsharp\IlxGen.fsi"
             @"src\fsharp\IlxGen.fs"
             @"src\fsharp\CompileOps.fsi"
             @"src\fsharp\CompileOps.fs"
             @"src\fsharp\CompileOptions.fsi"
             @"src\fsharp\CompileOptions.fs"
             @"src\fsharp\fsc.fsi"
             @"src\fsharp\fsc.fs"
             @"src\fsharp\symbols\SymbolHelpers.fsi"
             @"src\fsharp\symbols\SymbolHelpers.fs"
             @"src\fsharp\symbols\Symbols.fsi"
             @"src\fsharp\symbols\Symbols.fs"
             @"src\fsharp\symbols\Exprs.fsi"
             @"src\fsharp\symbols\Exprs.fs"
             @"src\fsharp\symbols\SymbolPatterns.fsi"
             @"src\fsharp\symbols\SymbolPatterns.fs"
             @"src\fsharp\service\IncrementalBuild.fsi"
             @"src\fsharp\service\IncrementalBuild.fs"
             @"src\fsharp\service\Reactor.fsi"
             @"src\fsharp\service\Reactor.fs"
             @"src\fsharp\service\ServiceConstants.fs"
             @"src\fsharp\service\ServiceDeclarationLists.fsi"
             @"src\fsharp\service\ServiceDeclarationLists.fs"
             @"src\fsharp\service\ServiceLexing.fsi"
             @"src\fsharp\service\ServiceLexing.fs"
             @"src\fsharp\service\ServiceParseTreeWalk.fs"
             @"src\fsharp\service\ServiceNavigation.fsi"
             @"src\fsharp\service\ServiceNavigation.fs"
             @"src\fsharp\service\ServiceParamInfoLocations.fsi"
             @"src\fsharp\service\ServiceParamInfoLocations.fs"
             @"src\fsharp\service\ServiceUntypedParse.fsi"
             @"src\fsharp\service\ServiceUntypedParse.fs"
             @"src\fsharp\service\ServiceAssemblyContent.fsi"
             @"src\fsharp\service\ServiceAssemblyContent.fs"
             @"src\fsharp\service\ServiceXmlDocParser.fsi"
             @"src\fsharp\service\ServiceXmlDocParser.fs"
             @"src\utils\reshapedmsbuild.fs"
             @"src\fsharp\SimulatedMSBuildReferenceResolver.fs"
             @"src\fsharp\service\ExternalSymbol.fsi"
             @"src\fsharp\service\ExternalSymbol.fs"
             @"src\fsharp\service\QuickParse.fsi"
             @"src\fsharp\service\QuickParse.fs"
             @"src\fsharp\service\service.fsi"
             @"src\fsharp\service\service.fs"
             @"src\fsharp\service\ServiceInterfaceStubGenerator.fsi"
             @"src\fsharp\service\ServiceInterfaceStubGenerator.fs"
             @"src\fsharp\service\ServiceStructure.fsi"
             @"src\fsharp\service\ServiceStructure.fs"
             @"src\fsharp\service\ServiceAnalysis.fsi"
             @"src\fsharp\service\ServiceAnalysis.fs"
             @"src\fsharp\fsi\fsi.fsi"
             @"src\fsharp\fsi\fsi.fs"
             @"src\fsharp\MSBuildReferenceResolver.fs"
             @"src\fsharp\LegacyHostedCompilerForTesting.fs"
             @"C:\Users\vaskir\AppData\Local\Temp\.NETFramework,Version=v4.6.AssemblyAttributes.fs"
          |]

    { Options =
        {ProjectFileName = repositoryDir </> @"src\fsharp\FSharp.Compiler.Private\FSharp.Compiler.Private.fsproj"
         ProjectId = None
         SourceFiles = files |> Array.map (fun x -> repositoryDir </> x)
         OtherOptions =
          [| @"-o:obj\Release\net40\FSharp.Compiler.Private.dll"
             @"-g"
             @"--debug:pdbonly"
             @"--noframework"
             @"--baseaddress:0x06800000"
             @"--define:MSBUILD_AT_LEAST_14"
             @"--define:COMPILER"
             @"--define:TRACE"
             @"--define:ENABLE_MONO_SUPPORT"
             @"--define:BE_SECURITY_TRANSPARENT"
             @"--define:FX_LCIDFROMCODEPAGE"
             @"--doc:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\..\Release\net40\bin\FSharp.Compiler.Private.xml"
             @"--keyfile:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\fsharp\msft.pubkey"
             @"--delaysign+"
             @"--optimize+"
             @"-r:C:\git\visualfsharp\Release\net40\bin\FSharp.Core.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\ISymWrapper.dll"
             @"-r:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\..\packages\Microsoft.VisualFSharp.Msbuild.15.0.1.0.1\lib\net45\Microsoft.Build.dll"
             @"-r:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\..\packages\Microsoft.VisualFSharp.Msbuild.15.0.1.0.1\lib\net45\Microsoft.Build.Framework.dll"
             @"-r:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\..\packages\Microsoft.VisualFSharp.Msbuild.15.0.1.0.1\lib\net45\Microsoft.Build.Tasks.Core.dll"
             @"-r:C:\git\visualfsharp\src\fsharp\FSharp.Compiler.Private\..\..\..\packages\Microsoft.VisualFSharp.Msbuild.15.0.1.0.1\lib\net45\Microsoft.Build.Utilities.Core.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\mscorlib.dll"
             @"-r:C:\git\visualfsharp\packages\System.Collections.Immutable.1.3.1\lib\portable-net45+win8+wp8+wpa81\System.Collections.Immutable.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\System.Core.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\System.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\System.Numerics.dll"
             @"-r:C:\git\visualfsharp\packages\System.Reflection.Metadata.1.4.2\lib\portable-net45+win8\System.Reflection.Metadata.dll"
             @"-r:C:\git\visualfsharp\packages\System.ValueTuple.4.4.0\lib\netstandard1.0\System.ValueTuple.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Collections.Concurrent.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Collections.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ComponentModel.Annotations.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ComponentModel.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ComponentModel.EventBasedAsync.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Diagnostics.Contracts.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Diagnostics.Debug.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Diagnostics.Tools.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Diagnostics.Tracing.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Dynamic.Runtime.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Globalization.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.IO.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Linq.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Linq.Expressions.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Linq.Parallel.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Linq.Queryable.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Net.NetworkInformation.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Net.Primitives.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Net.Requests.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Net.WebHeaderCollection.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ObjectModel.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.Emit.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.Emit.ILGeneration.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.Emit.Lightweight.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.Extensions.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Reflection.Primitives.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Resources.ResourceManager.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Extensions.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Handles.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.InteropServices.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.InteropServices.WindowsRuntime.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Numerics.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Serialization.Json.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Serialization.Primitives.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Runtime.Serialization.Xml.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Security.Principal.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ServiceModel.Duplex.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ServiceModel.Http.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ServiceModel.NetTcp.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ServiceModel.Primitives.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.ServiceModel.Security.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Text.Encoding.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Text.Encoding.Extensions.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Text.RegularExpressions.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Threading.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Threading.Tasks.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Threading.Tasks.Parallel.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Threading.Timer.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Xml.ReaderWriter.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Xml.XDocument.dll"
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\Facades\System.Xml.XmlSerializer.dll"
             @"--target:library"
             @"--nowarn:2003,69,65,54,61,75,62,9"
             @"--warn:3"
             @"--warnaserror"
             @"--warnaserror:76"
             @"--vserrors"
             @"--preferreduilang:en-US"
             @"--utf8output"
             @"--fullpaths"
             @"--flaterrors"
             @"--subsystemversion:6.00"
             @"--highentropyva+"
             @"--times"
             @"/warnon:1182"
          |]
         ReferencedProjects = [||]
         IsIncompleteTypeCheckEnvironment = false
         UseScriptResolutionRules = false
         LoadTime = DateTime.Now
         UnresolvedReferences = None;
         OriginalLoadReferences = []
         ExtraProjectInfo = None 
         Stamp = None }
      FilesToCheck = 
          files 
          |> Array.filter (fun s -> s.Contains "TypeChecker.fs" || 
                                    s.Contains "Optimizer.fs" || 
                                    s.Contains "IlxGen.fs" || 
                                    s.Contains "TastOps.fs" || 
                                    s.Contains "TcGlobals.fs" || 
                                    s.Contains "CompileOps.fs" || 
                                    s.Contains "CompileOptions.fs") 
          |>  Array.map (fun x -> repositoryDir </> x) 
          |> Array.toList
      FileToCheck = repositoryDir </> @"src\fsharp\TypeChecker.fs"
      SymbolText = "Some"
      SymbolPos = mkPos 120 7
      CompletionPositions = [] }


let get (repositoryDir: string) : Options =
    let repositoryDir = Path.GetFullPath(repositoryDir)
    match DirectoryInfo(Path.GetFullPath(repositoryDir)).Name.ToLower() with
    | "visualfsharp" -> ``FSharp.Compiler.Private``(repositoryDir)
    | "fsharp.compiler.service" -> FCS(repositoryDir)
    | "fsharpvspowertools" -> VFPT(repositoryDir)
    | _ -> failwithf "%s is not supported" repositoryDir

