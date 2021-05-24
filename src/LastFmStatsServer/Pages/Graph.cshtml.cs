using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ApiModels;
using LastFmStatsServer.Business;
using LastFmStatsServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LastFmStatsServer.Pages
{
    public class GraphModel : PageModel
    {
        public string UserName { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public ColorChoice Color { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public ReadOnlyCollection<TimeZoneInfo> TimeZones { get; private set; }

        public void OnGet(
            string userName = null,
            ColorChoice color = default,
            string timeZone = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            UserName = userName?.Trim();
            Color = color;
            TimeZones = TimeZoneInfo.GetSystemTimeZones();
            TimeZone = Utils.GetTimeZoneOrUTC(timeZone);
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
