namespace ServerApp

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.OpenApi.Models
open Microsoft.EntityFrameworkCore;
open LastFmStatsServer
open Dapper
open System.Globalization
open System.Data

type UtcDateTimeOffsetTypeHandler() =
    inherit SqlMapper.TypeHandler<DateTimeOffset>()
    override this.Parse(value: obj) : DateTimeOffset =
        // If a TZ info is present, use it. Otherwise, default to UTC
        DateTimeOffset.Parse(value :?> string, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal)
    override this.SetValue(parameter: IDbDataParameter, value: DateTimeOffset) =
        // Warning: Dapper cannot read a DateTimeOffset value, but is able to write one,
        // so this code is actually never called...
        parameter.Value <- value

type Startup(configuration: IConfiguration) =
    member _.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member _.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services.AddControllers() |> ignore
        services.AddSwaggerGen(fun c ->
            c.SwaggerDoc("v1", new OpenApiInfo(Title = "LastFmStatsServer", Version = "v1")))
            |> ignore

        services.AddDbContext<MainContext>(fun options ->
            options.UseSqlite("Data Source=main.db") |> ignore)
            |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
            app.UseSwagger() |> ignore
            app.UseSwaggerUI(fun c -> c.SwaggerEndpoint("/swagger/v1/swagger.json", "LastFmStatsServer v1")) |> ignore
        app.UseRouting()
            .UseCors(fun x -> x.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS") |> ignore)
            .UseAuthorization()
            .UseEndpoints(fun endpoints ->
                endpoints.MapControllers() |> ignore
             ) |> ignore

        SqlMapper.AddTypeHandler(typeof<DateTimeOffset>, UtcDateTimeOffsetTypeHandler())

        // https://github.com/StackExchange/Dapper/pull/720
        SqlMapper.AssumeColumnsAreStronglyTyped <- false
