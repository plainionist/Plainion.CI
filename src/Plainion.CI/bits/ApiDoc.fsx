#I "../../../bin/Debug/FAKE"
#I "../../../bin/Debug/FSharp.Formatting"
#load "Settings.fsx"
#load "FSharp.Formatting.fsx"
#r "FakeLib.dll"

open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// Web site location for the generated documentation
let website = "/Plainion"

let githubLink = "https://github.com/ronin4net/Plainion/ronin4net/Plainion"

// Specify more information about your project
let info =
    [ "project-name", "Plainion"
      "project-author", "ronin4net"
      "project-summary", "Provides .Net libraries to simplify development of software engineering tools"
      "project-github", githubLink
      "project-nuget", "http://nuget.org/packages/Plainion" ]

let root = website

let output     = Settings.outputPath @@ "doc"
let content    = Settings.projectRoot @@ "doc/content"
let files      = Settings.projectRoot @@ "doc/files"
let templates  = Settings.toolsHome @@ "bits/templates"
let formatting = Settings.toolsHome @@ "FSharp.Formatting/"
let docTemplate = "docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en",[ templates; formatting @@ "templates"
                          formatting @@ "templates/reference" ])

subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
                let name = d.Name
                if name.Length = 2 || name.Length = 3 then
                    layoutRootsAll.Add(
                            name, [templates @@ name
                                   formatting @@ "templates"
                                   formatting @@ "templates/reference" ]))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
    if directoryExists files then CopyRecursive files output true |> Log "Copying file: "
    ensureDirectory (output @@ "content")
    CopyRecursive (formatting @@ "styles") (output @@ "content") true |> Log "Copying styles and scripts: "

let binaries =
    let conventionBased = 
        directoryInfo Settings.outputPath 
        |> subDirectories
        |> Array.map (fun d -> d.FullName @@ (sprintf "%s.dll" d.Name))
        |> List.ofArray

    conventionBased

let libDirs =
    let conventionBasedbinDirs =
        directoryInfo Settings.outputPath 
        |> subDirectories
        |> Array.map (fun d -> d.FullName)
        |> List.ofArray

    conventionBasedbinDirs @ [Settings.outputPath]

// Build API reference from XML comments
let buildReference () =
    CleanDir (output @@ "reference")
    MetadataFormat.Generate
      ( binaries, output @@ "reference", layoutRootsAll.["en"],
        parameters = ("root", root)::info,
        sourceRepo = githubLink @@ "tree/master",
        sourceFolder = Settings.projectRoot @@ "src",
        publicOnly = true,libDirs = libDirs )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =

    // First, process files which are placed in the content root directory.

    Literate.ProcessDirectory
        ( content, docTemplate, output, replacements = ("root", root)::info,
          layoutRoots = layoutRootsAll.["en"],
          generateAnchors = true,
          processRecursive = false)

    // And then process files which are placed in the sub directories
    // (some sub directories might be for specific language).

    let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.TopDirectoryOnly)
    for dir in subdirs do
        let dirname = (new DirectoryInfo(dir)).Name
        let layoutRoots =
            // Check whether this directory name is for specific language
            let key = layoutRootsAll.Keys
                      |> Seq.tryFind (fun i -> i = dirname)
            match key with
            | Some lang -> layoutRootsAll.[lang]
            | None -> layoutRootsAll.["en"] // "en" is the default language

        Literate.ProcessDirectory
          ( dir, docTemplate, output @@ dirname, replacements = ("root", root)::info,
            layoutRoots = layoutRoots,
            generateAnchors = true )


Target "GenerateApiDoc" (fun _ ->
    copyFiles()

    if directoryExists content then 
        buildDocumentation() 

    buildReference()
)

RunTargetOrDefault "GenerateApiDoc"

