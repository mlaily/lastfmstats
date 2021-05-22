using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LastFmStatsServer.Pages
{
    public class RefreshModel : PageModel
    {
        public string UserName { get; set; }

        public void OnGet(string userName = null)
        {
            UserName = userName;
        }
    }
}
