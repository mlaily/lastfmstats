using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetCurrentCultures();

            const string settingsPathVarName = "APP_SETTINGS_PATH";
            var settingsPath = Environment.GetEnvironmentVariable(settingsPathVarName) ?? throw new Exception($"{settingsPathVarName} is empty or not set.");

            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configure => configure
                    .AddJsonFile(settingsPath, optional: false, reloadOnChange: true))
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseStartup<Startup>())
                .Build()
                .Run();
        }

        private static void SetCurrentCultures()
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var formattingCulture = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();
            formattingCulture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            formattingCulture.DateTimeFormat.LongDatePattern = "dddd d MMMM yyyy";
            formattingCulture.DateTimeFormat.ShortTimePattern = "HH:mm";
            formattingCulture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            formattingCulture.NumberFormat.NumberDecimalSeparator = ".";
            formattingCulture.NumberFormat.CurrencyDecimalSeparator = ".";
            formattingCulture.NumberFormat.PercentDecimalSeparator = ".";

            CultureInfo.CurrentCulture = formattingCulture;
        }
    }
}
