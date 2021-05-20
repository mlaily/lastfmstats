using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Runtime.CompilerServices;
using Dapper;
using System.Text;
using System.Globalization;
using LastFmStatsServer.Business;
using LastFmStatsServer.Plumbing;
using ApiModels;

namespace LastFmStatsServer.Controllers
{
    [ApiController]
    [Route("api")]
    public class MiscController : ControllerBase
    {
        private readonly ILogger<MiscController> _logger;

        public MiscController(ILogger<MiscController> logger)
        {
            _logger = logger;
        }

        [HttpGet("colors")]
        public ColorChoice[] GetColorChoices()
        {
            return ColorChoice.GetValues();
        }

        [HttpGet("timezones")]
        public TimeZonesResponse GetTimeZones()
        {
            var timeZoneSource = TimeZoneInfo.GetSystemTimeZones();
            var timeZones = (from tz in timeZoneSource
                             select new ApiModels.TimeZone(tz.Id, tz.DisplayName))
                             .ToArray();
            return new TimeZonesResponse(timeZones, timeZones.First(x => x.Id == "UTC"));
        }
    }
}
