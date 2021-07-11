module internal FromFake

// TODO: remove once Fake officially supports VS 2019
module MsBuild =
    open System.IO
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    type MsBuildEntry = {
        Version: string;
        Paths: string list;
    }

    let knownMsBuildEntries =
        [
            { Version = "16.0"; Paths = [@"\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
                                         @"\MSBuild\Current\Bin"
                                         @"\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin"] }
            { Version = "15.0"; Paths = [@"\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
                                         @"\MSBuild\15.0\Bin"
                                         @"\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"] }
            { Version = "14.0"; Paths = [@"\MSBuild\14.0\Bin"] }
            { Version = "12.0"; Paths = [@"\MSBuild\12.0\Bin"; @"\MSBuild\12.0\Bin\amd64"] }
        ]

    let oldMsBuildLocations =
        [ @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\";
          @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\";
          @"c:\Windows\Microsoft.NET\Framework\v3.5\"
        ]

    let toDict items =
        items |> Seq.map (fun f -> f.Version, f.Paths) |> Map.ofSeq

    let getAllKnownPaths =
        (knownMsBuildEntries |> List.collect (fun m -> m.Paths)) @ oldMsBuildLocations

    let monoVersionToUseMSBuildOn = System.Version("5.0")

    let msBuildExe =
        /// the value we're given can be a:
        ///     * full path to a file or
        ///     * just a directory
        /// if just a directory we can make it the path to a file by Path-Combining the tool name to the directory.
        let exactPathOrBinaryOnPath tool input =
            if Directory.Exists input
            then input </> tool
            else input

        let which tool = ProcessUtils.tryFindFileOnPath tool
        let msbuildEnvironVar = Fake.Core.Environment.environVarOrNone "MSBuild"

        let foundExe =
            match Fake.Core.Environment.isUnix, Fake.Core.Environment.monoVersion with
            | true, Some(_, Some(version)) when version >= monoVersionToUseMSBuildOn ->
                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "msbuild")
                    msbuildEnvironVar |> Option.bind which
                    which "msbuild"
                    which "xbuild"
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "msbuild"
            | true, _ ->
                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "xbuild")
                    msbuildEnvironVar |> Option.bind which
                    which "xbuild"
                    which "msbuild"
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "xbuild"
            | false, _ ->

                let tryFindFileInDirsThenPath paths tool =
                    match ProcessUtils.tryFindFile paths tool with
                    | Some path -> Some path
                    | None -> ProcessUtils.tryFindFileOnPath tool

                let configIgnoreMSBuild = None
                let findOnVSPathsThenSystemPath =
                    let dict = toDict knownMsBuildEntries
                    let vsVersionPaths =
                        defaultArg (Fake.Core.Environment.environVarOrNone "VisualStudioVersion" |> Option.bind dict.TryFind) getAllKnownPaths
                        |> List.map ((@@) Fake.Core.Environment.ProgramFilesX86)

                    tryFindFileInDirsThenPath vsVersionPaths "MSBuild.exe"

                let sources = [
                    msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "MSBuild.exe")
                    msbuildEnvironVar |> Option.bind which
                    configIgnoreMSBuild
                    findOnVSPathsThenSystemPath
                ]
                defaultArg (sources |> List.choose id |> List.tryHead) "MSBuild.exe"

        if foundExe.Contains @"\BuildTools\" then
            printfn "If you encounter msbuild errors make sure you have copied the required SDKs, see https://github.com/Microsoft/msbuild/issues/1697"
        elif foundExe.Contains @"\2017\" then
            printfn "Using msbuild of VS2017 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe
        elif foundExe.Contains @"\2019\" then
            printfn "Using msbuild of VS2019 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe        
        foundExe
