using LearningOS.Data;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;

    public SettingsController(LearningDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<UserSettings>> GetSettings()
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings();
            _db.UserSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<UserSettings>> UpdateSettings([FromBody] UserSettings updated)
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings();
            _db.UserSettings.Add(settings);
        }

        settings.MaxExtraRecoveryMinutesPerDay = updated.MaxExtraRecoveryMinutesPerDay > 0 ? updated.MaxExtraRecoveryMinutesPerDay : 60;
        settings.DailyTargetStudyMinutes = updated.DailyTargetStudyMinutes > 0 ? updated.DailyTargetStudyMinutes : 120;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "SettingsUpdated",
            "UserSettings",
            settings.Id.ToString(),
            $"Updated settings: Max recovery extra cap = {settings.MaxExtraRecoveryMinutesPerDay}m, Daily study target = {settings.DailyTargetStudyMinutes}m");

        return Ok(settings);
    }
}
