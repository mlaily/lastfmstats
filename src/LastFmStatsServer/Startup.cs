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
            services.AddRazorPages();

            services.AddControllers(x => x
                .Filters.Add(new HttpResponseExceptionFilter<GenericError>())
            ).AddJsonOptions(x =>
            {
                // The types are used directly on the client side so
                // it's important to keep the names unchanged
                x.JsonSerializerOptions.PropertyNamingPolicy = null;
                x.JsonSerializerOptions.Converters.Add(new FSharpUnionConverterFactory());
                //x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            });

            services.AddSwaggerGen(x => x
                .SwaggerDoc("v1", new OpenApiInfo { Title = "LastFmStatsServer", Version = "v1" }));

            services.AddDbContext<MainContext>(x => x
                .UseSqlite("Data Source=main.db")
                //.EnableSensitiveDataLogging(true)
                //.LogTo(Console.WriteLine)
                );

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LastFmStatsServer v1"));
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS"));

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            // Make SQLite support DateTimeOffset
            SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new UtcDateTimeOffsetTypeHandler());

            // https://github.com/StackExchange/Dapper/pull/720
            SqlMapper.AssumeColumnsAreStronglyTyped = false;
        }
    }
}
