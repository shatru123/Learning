using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningOS.Tests;

public class RecoveryLogicTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public RecoveryLogicTests()
    {
        _dbPath = $"recovery_test_{Guid.NewGuid():N}.db";
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
    public async Task SmartRecovery_WorkloadCap_ProtectsAgainstOverloadingTomorrow()
    {
        // Requirement 5 & 7:
        // Day 20 missed with 3 tasks totaling 135 minutes.
        // Cap is 60 minutes.
        // Tomorrow must not be overloaded; overflow tasks must spill over to following day!
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var recovery = new RecoveryService(db, audit, NullLogger<RecoveryService>.Instance);

        var day20 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 20);
        var day21 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 21);
        var day22 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 22);

        int originalDay21Count = day21.Tasks.Count;
        int originalDay22Count = day22.Tasks.Count;

        // Mark Day 20 as missed
        await recovery.MarkDayMissedAsync(day20.Id, new MissedDayRequestDto { Reason = "Work", Note = "Late shift" });

        // Recover with MoveToTomorrow strategy and 60 min cap
        await recovery.RecoverDayAsync(day20.Id, new RecoverDayRequestDto
        {
            Strategy = "MoveToTomorrow",
            MaxExtraMinutesPerDay = 60
        });

        // Reload days
        var updatedDay21 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 21);
        var updatedDay22 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 22);

        // Day 21 received tasks within the 60-min cap
        var movedToDay21 = updatedDay21.Tasks.Where(t => t.OriginalDayNumber == 20).ToList();
        var movedToDay22 = updatedDay22.Tasks.Where(t => t.OriginalDayNumber == 20).ToList();

        Assert.NotEmpty(movedToDay21);
        Assert.True(movedToDay21.Sum(t => t.EstimatedMinutes) <= 60, "Tomorrow must not exceed max extra workload cap!");

        // The overflow went to day 22
        Assert.NotEmpty(movedToDay22);
    }

    [Fact]
    public async Task CompressRecovery_DistributesTasksAcrossMultipleDays()
    {
        // Requirement 4 Option C: Compress across 3-7 days
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var recovery = new RecoveryService(db, audit, NullLogger<RecoveryService>.Instance);

        var day10 = await db.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 10);
        await recovery.MarkDayMissedAsync(day10.Id, new MissedDayRequestDto { Reason = "Travel" });

        await recovery.RecoverDayAsync(day10.Id, new RecoverDayRequestDto
        {
            Strategy = "Compress",
            CompressDays = 3,
            MaxExtraMinutesPerDay = 60
        });

        // Verify tasks from day 10 were distributed to future days
        var distributedTasks = await db.LearningTasks
            .Where(t => t.OriginalDayNumber == 10)
            .ToListAsync();

        Assert.All(distributedTasks, t => Assert.True(t.CurrentDayNumber > 10));
    }

    [Fact]
    public async Task ExtendRoadmap_ShiftsUncompletedDays_ExtendsTargetDate()
    {
        // Requirement 8: Extend Roadmap
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var recovery = new RecoveryService(db, audit, NullLogger<RecoveryService>.Instance);

        var planBefore = await db.LearningPlans.FirstAsync();
        var originalEndDate = planBefore.EndDate;

        var result = await recovery.ExtendRoadmapAsync(new ExtendRoadmapRequestDto
        {
            DaysToExtend = 5,
            Reason = "5 sick days taken"
        });

        Assert.Equal(5, result.DaysExtended);
        Assert.Equal(originalEndDate.AddDays(5), result.NewEndDate);

        var planAfter = await db.LearningPlans.FirstAsync();
        Assert.Equal(originalEndDate.AddDays(5), planAfter.EndDate);
    }
}
