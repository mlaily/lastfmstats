using Dapper;
using LastFmStatsServer.Dapper;
using LastFmStatsServer.Plumbing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ApiModels;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace LastFmStatsServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GeneralOptions>(Configuration.GetSection(GeneralOptions.SectionName));

            services.AddRazorPages();

            services.AddControllers(x => x
                .Filters.Add(new HttpResponseExceptionFilter<GenericError>())
            ).AddJsonOptions(x =>
            {
                // The types are used directly on the client side so
                // it's important to keep the names unchanged
                x.JsonSerializerOptions.PropertyNamingPolicy = null;
                x.JsonSerializerOptions.Converters.Add(new FSharpUnionConverterFactory());
                x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            });
            
            services.AddSwaggerGen(x => x
                .SwaggerDoc("v1", new OpenApiInfo { Title = "LastFmStatsServer", Version = "v1" }));

            services.AddDbContext<MainContext>((serviceProvider, optionsBuilder) =>
            {
                var generalOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<GeneralOptions>>();
                optionsBuilder.UseSqlite(generalOptions.Value.DatabaseConnectionString ?? throw new InvalidOperationException(nameof(GeneralOptions.DatabaseConnectionString) + " is null."));
                //.UseSqlite("Data Source=main.db")
                //.EnableSensitiveDataLogging(true)
                //.LogTo(Console.WriteLine)
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MainContext mainDbContext, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LastFmStatsServer v1"));
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions()
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
            });

            // Remove trailing slashes for SEO blahblahblah
            app.UseRewriter(new RewriteOptions().AddRedirect("(.*?)/+$", "$1", 301));

            // Force css and js files to be served as utf-8. Is there a better way?
            var fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
            fileExtensionContentTypeProvider.Mappings[".css"] = "text/css; charset=utf-8";
            fileExtensionContentTypeProvider.Mappings[".js"] = "application/javascript; charset=utf-8";

            app.UseStaticFiles(new StaticFileOptions()
            {
                ContentTypeProvider = fileExtensionContentTypeProvider,
            });

            app.UseRouting();

            // Used to be necessary when the SPA and the API were served on different Origins (different localhost ports):
            //app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS"));

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            // Dapper cannot read back DateTimeOffset values
            SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new UtcDateTimeOffsetTypeHandler());

            // https://github.com/StackExchange/Dapper/pull/720
            SqlMapper.AssumeColumnsAreStronglyTyped = false;

            try
            {
                mainDbContext.Database.EnsureCreated();
                // DbInitializer.Initialize(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred creating the DB.");
            }
        }
    }
}
