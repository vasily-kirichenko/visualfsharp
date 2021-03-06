// #Regression #NoMT #EntryPoint 
// Regression test for FSHARP1.0:1304
// Explicit program entry point: [<ExtryPoint>]
// Attribute is last declaration on first file
//<CmdLine>Hello</CmdLine>
//<Expects id="FS0433" status="error" id="(9,5-9,9)">A function labeled with the 'EntryPointAttribute' attribute must be the last declaration in the last file in the compilation sequence, and can only be used when compiling to a \.exe</Expects>
module TestModule

[<EntryPoint>]
let main args =
   let res = if(args.Length=1 && args.[0]="Hello") then 0 else 1
   exit(res)


