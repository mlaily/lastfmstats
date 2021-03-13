namespace ServerApp

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection;
open LastFmStatsServer

module Program =
    let exitCode = 0

    let CreateHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
            )

    let createDbIfNotExists (host: IHost) =
        use scope = host.Services.CreateScope()
        try
            let context = scope.ServiceProvider.GetRequiredService<MainContext>()
            context.Database.EnsureCreated() |> ignore
        with | ex ->
            let logger = scope.ServiceProvider.GetRequiredService<ILogger>()
            logger.LogError(ex, "An error occurred creating the DB.")

    [<EntryPoint>]
    let main args =
        let host = CreateHostBuilder(args).Build()
        
        createDbIfNotExists host

        host.Run()

        exitCode
