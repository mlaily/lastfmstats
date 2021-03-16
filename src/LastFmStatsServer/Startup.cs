using Dapper;
using LastFmStatsServer.Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer
{

    public class HashConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableTo(typeof(Utils.Hash<ApiModels.ChartTrace>));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var typedValue = (Utils.Hash<ApiModels.ChartTrace>)value;    
            throw new NotImplementedException();
        }
    }

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

            services.AddControllers()
                //.AddNewtonsoftJson(x =>
                //{
                //    //x.SerializerSettings.Converters.Add(new HashConverter());
                //    //x.SerializerSettings.
                //});
                .AddJsonOptions(x =>
            {
                // The types are used directly on the client side so
                // it's important to keep the names unchanged
                x.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LastFmStatsServer", Version = "v1" });
            });

            services.AddDbContext<MainContext>(options => options
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

            app.UseRouting();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS"));

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Make SQLite support DateTimeOffset
            SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new UtcDateTimeOffsetTypeHandler());

            // https://github.com/StackExchange/Dapper/pull/720
            SqlMapper.AssumeColumnsAreStronglyTyped = false;
        }
    }
}
