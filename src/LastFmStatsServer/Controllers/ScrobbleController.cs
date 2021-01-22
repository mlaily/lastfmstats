using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastFmStatsServer.Controllers
{
    [ApiController]
    [Route("api/scrobbles")]
    public class ScrobbleController : ControllerBase
    {
        private readonly ILogger<ScrobbleController> _logger;

        public ScrobbleController(ILogger<ScrobbleController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{userName}")]
        public IActionResult Post(string userName, ScrobbleData[] data)
        {
            return new JsonResult(new { SavedCount = data.Length });
        }
    }
}
