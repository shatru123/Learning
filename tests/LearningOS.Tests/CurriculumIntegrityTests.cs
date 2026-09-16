using LearningOS.Controllers;
using LearningOS.Data;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningOS.Tests;

public class CurriculumIntegrityTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public CurriculumIntegrityTests()
    {
        _dbPath = $"curriculum_test_{Guid.NewGuid():N}.db";
        _dbOptions = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        using var db = new LearningDbContext(_dbOptions);
        DbInitializer.InitializeAsync(db, NullLogger.Instance).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Exact100LearningDays_SequencedWithoutGapsOrDuplicates()
    {
        using var db = new LearningDbContext(_dbOptions);
        var days = await db.DayPlans.Include(d => d.Tasks).ToListAsync();

        // 1. Total learning days must be exactly 100
        Assert.Equal(100, days.Count);

        // 2. Day numbers must be 1 through 100 with no duplicates and no gaps
        var dayNumbers = days.Select(d => d.DayNumber!.Value).OrderBy(n => n).ToList();
        var expectedNumbers = Enumerable.Range(1, 100).ToList();
        Assert.Equal(expectedNumbers, dayNumbers);

        // 3. Every day must have a non-empty title and theme
        foreach (var day in days)
        {
            Assert.False(string.IsNullOrWhiteSpace(day.Title), $"Day {day.DayNumber} has empty title");
            Assert.False(string.IsNullOrWhiteSpace(day.Theme), $"Day {day.DayNumber} has empty theme");
            Assert.True(day.Tasks.Count > 0, $"Day {day.DayNumber} has no tasks");
        }

        // 4. Initial status must be Planned
        Assert.All(days, d => Assert.Equal(DayStatus.Planned, d.Status));
    }

    [Fact]
    public async Task RestDaysAndLeaveDays_AreDistinctFrom100LearningDays()
    {
        using var db = new LearningDbContext(_dbOptions);

        // Add a rest day and a leave day record
        var restDay = new RestDayRecord
        {
            Date = new DateOnly(2026, 9, 27),
            Reason = "Scheduled Sunday rest & recovery",
            CreatedAt = DateTime.UtcNow
        };
        db.RestDayRecords.Add(restDay);

        var leaveDay = new LeaveDayRecord
        {
            StartDate = new DateOnly(2026, 10, 2),
            EndDate = new DateOnly(2026, 10, 2),
            Reason = "Personal family commitment",
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveDayRecords.Add(leaveDay);

        await db.SaveChangesAsync();

        // Verify distinct records in separate tables
        var restCount = await db.RestDayRecords.CountAsync();
        var leaveCount = await db.LeaveDayRecords.CountAsync();
        Assert.Equal(1, restCount);
        Assert.Equal(1, leaveCount);

        // Verify learning days count is still exactly 100 and day numbering is intact
        var learningDays = await db.DayPlans.ToListAsync();
        Assert.Equal(100, learningDays.Count);
        var dayNumbers = learningDays.Select(d => d.DayNumber!.Value).OrderBy(n => n).ToList();
        Assert.Equal(Enumerable.Range(1, 100).ToList(), dayNumbers);
    }

    [Fact]
    public async Task UserSettings_WorkingHoursSchedule_PersistedAndRetrievedCorrectly()
    {
        using var db = new LearningDbContext(_dbOptions);
        var auditService = new AuditService(db, NullLogger<AuditService>.Instance);
        var controller = new SettingsController(db, auditService);

        var updatedSettings = new UserSettings
        {
            WorkdayStartHour = 10,
            WorkdayEndHour = 19,
            PreferredStudyStartTime = "21:00",
            PreferredStudyEndTime = "23:30",
            WeekdayDailyAvailableMinutes = 150,
            WeekendDailyAvailableMinutes = 300,
            ProtectWorkingHours = true,
            MaxExtraRecoveryMinutesPerDay = 75,
            DailyTargetStudyMinutes = 150
        };

        var updateResult = await controller.UpdateSettings(updatedSettings);
        var okUpdate = Assert.IsType<OkObjectResult>(updateResult.Result);
        var saved = Assert.IsType<UserSettings>(okUpdate.Value);

        Assert.Equal(10, saved.WorkdayStartHour);
        Assert.Equal(19, saved.WorkdayEndHour);
        Assert.Equal("21:00", saved.PreferredStudyStartTime);
        Assert.Equal("23:30", saved.PreferredStudyEndTime);
        Assert.Equal(150, saved.WeekdayDailyAvailableMinutes);
        Assert.Equal(300, saved.WeekendDailyAvailableMinutes);
        Assert.True(saved.ProtectWorkingHours);
        Assert.Equal(75, saved.MaxExtraRecoveryMinutesPerDay);
        Assert.Equal(150, saved.DailyTargetStudyMinutes);

        // Retrieve again via GET
        var getResult = await controller.GetSettings();
        var okGet = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrieved = Assert.IsType<UserSettings>(okGet.Value);

        Assert.Equal(10, retrieved.WorkdayStartHour);
        Assert.Equal(19, retrieved.WorkdayEndHour);
        Assert.Equal("21:00", retrieved.PreferredStudyStartTime);
        Assert.Equal("23:30", retrieved.PreferredStudyEndTime);
        Assert.Equal(150, retrieved.WeekdayDailyAvailableMinutes);
        Assert.Equal(300, retrieved.WeekendDailyAvailableMinutes);
        Assert.True(retrieved.ProtectWorkingHours);
        Assert.Equal(75, retrieved.MaxExtraRecoveryMinutesPerDay);
        Assert.Equal(150, retrieved.DailyTargetStudyMinutes);
    }

    [Fact]
    public async Task CurriculumPhases_CoverAllKeyMilestonesAndDomains()
    {
        using var db = new LearningDbContext(_dbOptions);
        var days = await db.DayPlans.Include(d => d.Tasks).ToListAsync();

        // Phase 1: Days 1-25 (Advanced C#, .NET Internals & High-Perf Data Access)
        var phase1 = days.Where(d => d.DayNumber >= 1 && d.DayNumber <= 25).ToList();
        Assert.Equal(25, phase1.Count);
        Assert.Contains(phase1, d => d.Title.Contains("Memory Management") || d.Title.Contains("Garbage Collection"));
        Assert.Contains(phase1, d => d.Title.Contains("Span<T>") || d.Title.Contains("Memory<T>"));
        Assert.Contains(phase1, d => d.Title.Contains("Dapper") || d.Title.Contains("EF Core"));

        // Phase 2: Days 26-50 (Microservices, Distributed Systems & Cloud-Native)
        var phase2 = days.Where(d => d.DayNumber >= 26 && d.DayNumber <= 50).ToList();
        Assert.Equal(25, phase2.Count);
        Assert.Contains(phase2, d => d.Title.Contains("RabbitMQ") || d.Title.Contains("MassTransit"));
        Assert.Contains(phase2, d => d.Title.Contains("YARP") || d.Title.Contains("Microservices"));
        Assert.Contains(phase2, d => d.Title.Contains("Kubernetes") || d.Title.Contains("Docker"));

        // Phase 3: Days 51-75 (Applied AI & LLM Engineering)
        var phase3 = days.Where(d => d.DayNumber >= 51 && d.DayNumber <= 75).ToList();
        Assert.Equal(25, phase3.Count);
        Assert.Contains(phase3, d => d.Title.Contains("pgvector") || d.Title.Contains("Vector"));
        Assert.Contains(phase3, d => d.Title.Contains("RAG") || d.Title.Contains("Chunking"));
        Assert.Contains(phase3, d => d.Title.Contains("Tool Calling") || d.Title.Contains("Function"));
        Assert.Contains(phase3, d => d.Title.Contains("Agent") || d.Title.Contains("Semantic Kernel"));

        // Phase 4: Days 76-90 (Portfolio Projects)
        var phase4 = days.Where(d => d.DayNumber >= 76 && d.DayNumber <= 90).ToList();
        Assert.Equal(15, phase4.Count);
        Assert.Contains(phase4, d => d.Title.Contains("Portfolio"));

        // Phase 5: Days 91-100 (System Design & Interview Mastery)
        var phase5 = days.Where(d => d.DayNumber >= 91 && d.DayNumber <= 100).ToList();
        Assert.Equal(10, phase5.Count);
        Assert.Contains(phase5, d => d.Title.Contains("System Design") || d.Title.Contains("Interview"));
    }
}
