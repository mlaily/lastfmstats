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
open Microsoft.Extensions.DependencyInjection
open LastFmStatsServer
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Builder
open Microsoft.EntityFrameworkCore
open Giraffe
open Dapper
open System.Globalization
open System.Data
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Thoth.Json.Net
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Business

module Program =

    let errorHandler (ex: Exception) (logger: ILogger) =
        let genericMessage = "An unhandled exception has occurred while executing the request."
        logger.LogError(ex, genericMessage)
        clearResponse >=> setStatusCode 500 >=> text genericMessage

    // Reminder:
    // HttpHandler
    // HttpFunc -> HttpFunc
    // HttpFunc -> HttpContext -> HttpFuncResult
    // HttpFunc -> HttpContext -> Task<HttpContext option>
    // (HttpContext -> HttpFuncResult) -> (HttpContext -> HttpFuncResult)
    // (HttpContext -> Task<HttpContext option>) -> (HttpContext -> Task<HttpContext option>)
    // (HttpContext -> Task<HttpContext option>) -> HttpContext -> Task<HttpContext option>

    let jsonResult value : HttpHandler =
        match value with
        | Ok ok -> json ok
        | Error err -> HttpStatusCodeHandlers.RequestErrors.badRequest (json err)

    let toHandlerWithMainContext serializer getResult : HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            serializer (getResult ctx) next ctx

    let remotingHandler : HttpHandler =
        Remoting.createApi()
        //|> Remoting.withRouteBuilder (sprintf "/api/%s/%s")
        |> Remoting.fromContext mainApi
        |> Remoting.withBinarySerialization
        |> Remoting.buildHttpHandler

    let appHandler: HttpHandler =
        choose [
            //subRouteCi "/api/"
            //    (choose [
            //        GET_HEAD >=>
            //            choose [
            //                routeCif "%s/resume-from" (Business.getResumeTimestamp >> toHandlerWithMainContext jsonResult)
            //            ]
            //        POST >=>
            //            choose [
            //                //routeCif "%s" (Business.insertScrobbleData >> toHandlerWithMainContext)
            //            ]
            //    ])
            remotingHandler
            setStatusCode 404 >=> text "Not Found" ]

    let configureServices (services: IServiceCollection) =
        services
            .AddCors()
            .AddDbContext<MainContext>(fun c ->
                c.UseSqlite("Data Source=main.db") |> ignore)
            .AddGiraffe()
            |> ignore

        services
            .AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(
                Thoth.Json.Giraffe.ThothSerializer(
                    CaseStrategy.CamelCase,
                    extra = (Extra.empty |> Extra.withInt64),
                    skipNullField = true))
            |> ignore

    let configureApp (app: IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
        (match env.IsDevelopment() with
        | true ->
            app.UseDeveloperExceptionPage()
        | false ->
            app.UseGiraffeErrorHandler(errorHandler))
            // In dev and prod:
            .UseCors(fun c -> c.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS") |> ignore)
            .UseGiraffe(appHandler)

    let createDbIfNotExists (host: IHost) =
        use scope = host.Services.CreateScope()
        try
            let context = scope.ServiceProvider.GetRequiredService<MainContext>()
            context.Database.EnsureCreated() |> ignore
        with | ex ->
            let logger = scope.ServiceProvider.GetRequiredService<ILogger>()
            logger.LogError(ex, "An error occurred creating the DB.")

    type UtcDateTimeOffsetTypeHandler() =
        inherit SqlMapper.TypeHandler<DateTimeOffset>()
        override _.Parse(value: obj) : DateTimeOffset =
            // If a TZ info is present, use it. Otherwise, default to UTC
            DateTimeOffset.Parse(value :?> string, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal)
        override _.SetValue(parameter: IDbDataParameter, value: DateTimeOffset) =
            // Warning: Dapper cannot read a DateTimeOffset value, but is able to write one,
            // so this code is actually never called...
            parameter.Value <- value

    [<EntryPoint>]
    let main args =
        SqlMapper.AddTypeHandler(typeof<DateTimeOffset>, UtcDateTimeOffsetTypeHandler())
        // https://github.com/StackExchange/Dapper/pull/720
        SqlMapper.AssumeColumnsAreStronglyTyped <- false

        let host =
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(fun webBuilder ->
                    webBuilder
                        .ConfigureServices(configureServices)
                        .Configure(configureApp)
                        |> ignore)
                .Build()
        createDbIfNotExists host
        host.Run()
        0
