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
        settings.WorkdayStartHour = updated.WorkdayStartHour >= 0 && updated.WorkdayStartHour <= 23 ? updated.WorkdayStartHour : 9;
        settings.WorkdayEndHour = updated.WorkdayEndHour >= 0 && updated.WorkdayEndHour <= 23 ? updated.WorkdayEndHour : 18;
        settings.PreferredStudyStartTime = !string.IsNullOrWhiteSpace(updated.PreferredStudyStartTime) ? updated.PreferredStudyStartTime : "20:00";
        settings.PreferredStudyEndTime = !string.IsNullOrWhiteSpace(updated.PreferredStudyEndTime) ? updated.PreferredStudyEndTime : "22:30";
        settings.WeekdayDailyAvailableMinutes = updated.WeekdayDailyAvailableMinutes > 0 ? updated.WeekdayDailyAvailableMinutes : 120;
        settings.WeekendDailyAvailableMinutes = updated.WeekendDailyAvailableMinutes > 0 ? updated.WeekendDailyAvailableMinutes : 240;
        settings.ProtectWorkingHours = updated.ProtectWorkingHours;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "SettingsUpdated",
            "UserSettings",
            settings.Id.ToString(),
            $"Updated settings: Workday {settings.WorkdayStartHour:D2}:00-{settings.WorkdayEndHour:D2}:00, Study {settings.PreferredStudyStartTime}-{settings.PreferredStudyEndTime}, Weekday={settings.WeekdayDailyAvailableMinutes}m, Weekend={settings.WeekendDailyAvailableMinutes}m, Cap={settings.MaxExtraRecoveryMinutesPerDay}m");

        return Ok(settings);
    }
}
