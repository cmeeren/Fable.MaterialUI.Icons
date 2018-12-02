#r "paket: groupref FakeBuild //"
#load ".fake/build.fsx/intellisense.fsx"

open System
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.JavaScript

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  ++ "output"
  |> Shell.cleanDirs
)

Target.create "DotnetRestore" (fun _ ->
  DotNet.restore
    (DotNet.Options.withWorkingDirectory __SOURCE_DIRECTORY__)
    "Fable.MaterialUI.Icons.sln"
)

Target.create "YarnUpgradeAll" (fun _ ->
  Yarn.exec "upgrade --latest" id
)

Target.create "YarnInstall" (fun _ ->
  Yarn.install id
)

Target.create "Generate" (fun _ ->
  let firstLower (str: string) = 
    string (Char.ToLower str.[0]) + str.Substring 1
  let names =
    !! "node_modules/@material-ui/icons/*.d.ts"
    |> Seq.filter (fun path -> not <| path.EndsWith("index.d.ts"))
    |> Seq.map (fun path ->
        let fullName = FileInfo.ofPath(path).Name
        fullName.Substring(0, fullName.Length - 5)
    )
  let bindingStart = """
//-------------------------------------------//
// This file is auto-generated see build.fsx //
//-------------------------------------------//

module Fable.MaterialUI.Icons

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import.React

//let inline private materialEl<'P when 'P :> IHTMLProp>
//    (a: ComponentClass<'P>) (b: IHTMLProp list) c : ReactElement =
//  from a (keyValueList CaseRules.LowerFirst b |> unbox) c

"""
  let bindingLines =
    names |> Seq.collect (fun name ->
      [
        yield sprintf "let inline %sIcon b : ReactElement = " (firstLower name)
        yield sprintf "  ofImport \"default\" \"@material-ui/icons/%s\" (keyValueList CaseRules.LowerFirst b) []" name
        yield ""
      ]
    )
  File.replaceContent "src/Fable.MaterialUI.Icons/Icons.fs" bindingStart
  File.write true "src/Fable.MaterialUI.Icons/Icons.fs" bindingLines  

  let testStart = """
//-------------------------------------------//
// This file is auto-generated see build.fsx //
//-------------------------------------------//

module ViewTree

open Fable.MaterialUI.Icons
open Fable.Helpers.React.Props
open Fable.Helpers.React

let root =
  fragment [] ([|
"""
  let testLines =
    names |> Seq.map (fun name ->
      sprintf "    %sIcon [ Id \"%s\" ]" (firstLower name) name
    )
  File.replaceContent "src/Test/ViewTree.fs" testStart
  File.write true "src/Test/ViewTree.fs" testLines
  File.write true "src/Test/ViewTree.fs" ["  |] |> Array.toList)"]
)

Target.create "Build" (fun _ ->
  !! "src/Fable.MaterialUI.Icons/Fable.MaterialUI.Icons.fsproj"
  |> Seq.iter (DotNet.build (fun x ->
      { x with
          Configuration = DotNet.BuildConfiguration.Release
      }
  ))
)

Target.create "Pack" (fun _ ->
  "src/Fable.MaterialUI.Icons/"
  |> DotNet.pack (fun x ->
      { x with
          Configuration = DotNet.BuildConfiguration.Release
      }
  )
)

Target.create "BuildTest" (fun _ ->
  Yarn.exec "build" id
)

Target.create "DevTest" (fun _ ->
  Yarn.exec "start" id
)

Target.create "Update" ignore

// Build order
"Clean"
  ==> "DotnetRestore"
  ==> "YarnInstall"
  ==> "Generate"
  ==> "Build"
  ==> "BuildTest"

"BuildTest"
  ==> "Pack"

"Build"
  ==> "DevTest"

"Clean"
  ==> "DotnetRestore"
  ==> "YarnUpgradeAll"
  ==> "YarnInstall"
  ==> "Generate"
  ==> "Build"
  ==> "BuildTest"
  ==> "Pack"
  ==> "Update"

// start build
Target.runOrDefault "Pack"
