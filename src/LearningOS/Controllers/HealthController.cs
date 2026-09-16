using LearningOS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IHostEnvironment _env;

    public HealthController(LearningDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            bool canConnect = await _db.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    database = "Unable to connect to datastore",
                    environment = _env.EnvironmentName,
                    timestamp = DateTime.UtcNow
                });
            }

            int dayCount = await _db.DayPlans.CountAsync();

            return Ok(new
            {
                status = "Healthy",
                database = _db.Database.ProviderName,
                isAuthoritativeRelational = _db.Database.IsRelational(),
                totalLearningDays = dayCount,
                environment = _env.EnvironmentName,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
