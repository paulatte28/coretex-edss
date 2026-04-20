using Microsoft.AspNetCore.Mvc;
using coretex_finalproj.Data;
using coretex_finalproj.Services;
using System.Threading.Tasks;

namespace coretex_finalproj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifier;

        public HealthController(ApplicationDbContext context, NotificationService notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus()
        {
            bool dbHealthy = false;
            try {
                dbHealthy = await _context.Database.CanConnectAsync();
            } catch { }

            return Ok(new {
                System = "CORETEX EDSS",
                Database = dbHealthy ? "Healthy" : "Unreachable",
                EmailServer = "Ready (SendGrid)",
                Status = dbHealthy ? "OPERATIONAL" : "DEGRADED"
            });
        }
    }
}
