open System
open Fake
open Fake.IO
open Fake.DotNet
open Fake.Core

let esbuildPath = "build/esbuild/esbuild.exe"

type Configuration =
    | Debug
    | Release

let parseConfig argv =
    if argv |> Array.contains "debug"
    then Configuration.Debug
    else Configuration.Release

let buildServerApp config =
    printfn "Building server app..."
    DotNet.build
        (fun args ->
            { args with
                Configuration =
                    match config with
                    | Debug -> DotNet.BuildConfiguration.Debug
                    | Release -> DotNet.BuildConfiguration.Release
            })
        "src" // Build the whole solution

let buildClientApp config =
    /// Location of Fable js files expected by the html pages
    let wwwrootFableJsDir = "src/LastFmStatsServer/wwwroot/js/fable/"

    Shell.cleanDir wwwrootFableJsDir

    match config with
    | Debug ->
        printfn "Building client app in DEBUG configuration (watch mode)..."
        // In debug mode, we build fable js files directly into the wwwroot dir, ready to be served by the asp.net app:
        DotNet.exec
            (fun args -> args)
            "fable"
            $"""watch src/ClientApp --outDir {wwwrootFableJsDir} --sourceMaps --sourceMapsRoot "file:///" """
        |> printfn "%A"
        ()
    | Release ->
        printfn "Building cient app in RELEASE configuration..."
        if not <| File.exists esbuildPath then failwith $"esbuild not found at the provided location ({esbuildPath})"
        // In release mode, we build fable js files to an intermediate location, then bundle them into their final destination:
        /// Location of js files generated by Fable
        let fableOutputDir = "src/ClientApp/bin/fable-output/"
        /// List of entry points to keep when bundling js files:
        let entryPointScripts =
            [ "RefreshPage"
              "GraphPage" ]
            |> List.map (fun name -> $"{fableOutputDir}{name}.js")
        printfn $"Cleaning {fableOutputDir}..."
        Shell.cleanDir fableOutputDir
        printfn $"{fableOutputDir} is clean."
        DotNet.exec
            (fun args -> args)
            "fable"
            $"""src/ClientApp --outDir {fableOutputDir} --sourceMaps --sourceMapsRoot "file:///" """
        |> printfn "%A"
        let entryPoints = String.Join(" ", entryPointScripts)
        CreateProcess.fromRawCommandLine
            esbuildPath
            $"{entryPoints} --outdir={wwwrootFableJsDir} --bundle --splitting --format=esm --minify --target=es6"
        |> Proc.run
        |> printfn "%A"
        ()

[<EntryPoint>]
let main argv =
    printfn "Starting build script..."
    printfn "Args:"
    printfn "   debug       -> start debug loop"
    printfn "   release     -> build a production ready app (default)"
    printfn ""

    // Not sure when really useful, but likely required for some FAKE operations:
    let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

    let config = parseConfig argv

    // Current directory should be the root of the project...
    // TODO: find a better way to make sure we are where we expect to be...
    if Shell.testDir "src/LastFmStatsServer" = false then failwith "No src/LastFmStatsServer dir"

    DotNet.exec (fun args -> args) "tool restore" "" |> printfn "%A"

    // FIXME: currently only build the client app. the server app must be built separately.
    buildClientApp config
    // TODO: complete release build script to package everything properly
    //buildServerApp config

    0