using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningOS.Tests;

public class DataSurvivalTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public DataSurvivalTests()
    {
        _dbPath = $"survival_test_{Guid.NewGuid():N}.db";
        _dbOptions = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        // Initialize persistent database
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
    public async Task Test1_CreateNote_RestartApplication_VerifyNoteExists()
    {
        // 1. Session 1: Create a note on Day 1
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            var day1 = await db1.DayPlans.FirstAsync(d => d.DayNumber == 1);
            day1.Notes = "Critical learning note: Span<T> requires ref struct and cannot be allocated on the heap.";
            day1.UpdatedAt = DateTime.UtcNow;
            await db1.SaveChangesAsync();
        } // db1 disposed -> simulates application crash / restart / redeploy

        // 2. Session 2: Fresh application instance starting up
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var day1 = await db2.DayPlans.FirstAsync(d => d.DayNumber == 1);
            Assert.NotNull(day1.Notes);
            Assert.Contains("Span<T>", day1.Notes);
        }
    }

    [Fact]
    public async Task Test2_CompleteTask_RedeployApplication_VerifyCompletionRemains()
    {
        int taskId;
        // 1. Session 1: Complete a task and record history
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            var task = await db1.LearningTasks.FirstAsync(t => t.CurrentDayNumber == 1);
            taskId = task.Id;
            task.Status = "Completed";
            task.CompletedAt = DateTime.UtcNow;
            task.History.Add(new TaskHistory
            {
                LearningTaskId = task.Id,
                OldStatus = "Pending",
                NewStatus = "Completed",
                Note = "Successfully finished and benchmarked Span<T>",
                ChangedAt = DateTime.UtcNow
            });
            await db1.SaveChangesAsync();
        } // db1 disposed -> simulates redeploy

        // 2. Session 2: Fresh instance verifying completion and task history persisted
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var task = await db2.LearningTasks
                .Include(t => t.History)
                .FirstAsync(t => t.Id == taskId);

            Assert.Equal("Completed", task.Status);
            Assert.NotNull(task.CompletedAt);
            Assert.Contains(task.History, h => h.NewStatus == "Completed");
        }
    }

    [Fact]
    public async Task Test3_MarkDayMissed_RestartApplication_VerifyMissedDayRecordRemains()
    {
        // 1. Session 1: Mark day 3 as missed with reason & note using RecoveryService
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            var auditService = new AuditService(db1, NullLogger<AuditService>.Instance);
            var recoveryService = new RecoveryService(db1, auditService, NullLogger<RecoveryService>.Instance);

            var day3 = await db1.DayPlans.FirstAsync(d => d.DayNumber == 3);
            await recoveryService.MarkDayMissedAsync(day3.Id, new MissedDayRequestDto
            {
                Reason = "Work",
                Note = "Production issue kept me busy until late night."
            });
        } // db1 disposed

        // 2. Session 2: Verify Day 3 is Missed, record exists, and original tasks remain intact
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var day3 = await db2.DayPlans
                .Include(d => d.MissedRecord)
                .Include(d => d.StatusHistory)
                .Include(d => d.Tasks)
                .FirstAsync(d => d.DayNumber == 3);

            Assert.Equal(DayStatus.Missed, day3.Status);
            Assert.NotNull(day3.MissedRecord);
            Assert.Equal("Work", day3.MissedRecord.Reason);
            Assert.Equal("Production issue kept me busy until late night.", day3.MissedRecord.Note);
            Assert.NotEmpty(day3.Tasks); // Original tasks must remain visible!
            Assert.Contains(day3.StatusHistory, sh => sh.NewStatus == DayStatus.Missed);
        }
    }

    [Fact]
    public async Task Test4_CreateJournalEntry_RestartApplication_VerifyEntryRemains()
    {
        // 1. Session 1: Create Journal Entry
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            db1.JournalEntries.Add(new JournalEntry
            {
                Title = "Async/Await State Machine Discovery",
                Content = "Learned how MoveNext() works and how IAsyncStateMachine controls execution flow without thread blocking.",
                EntryDate = new DateOnly(2026, 9, 23),
                Tags = ".NET,CLR,Async",
                CreatedAt = DateTime.UtcNow
            });
            await db1.SaveChangesAsync();
        } // db1 disposed

        // 2. Session 2: Fresh instance verifies journal entry exists
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var entry = await db2.JournalEntries.FirstOrDefaultAsync(j => j.Title.Contains("Async/Await"));
            Assert.NotNull(entry);
            Assert.Equal(new DateOnly(2026, 9, 23), entry.EntryDate);
            Assert.Contains("MoveNext", entry.Content);
        }
    }

    [Fact]
    public async Task Test5_AddResourceLink_Redeploy_VerifyItRemains()
    {
        // 1. Session 1: Add new learning resource
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            db1.LearningResources.Add(new LearningResource
            {
                Title = "Pro .NET Memory Management by Konrad Kokosa",
                Url = "https://prodotnetmemory.com",
                Category = "Book",
                Description = "Authoritative book on CLR memory internals and GC.",
                IsCompleted = false,
                AddedAt = DateTime.UtcNow
            });
            await db1.SaveChangesAsync();
        } // db1 disposed

        // 2. Session 2: Verify resource remains
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var res = await db2.LearningResources.FirstOrDefaultAsync(r => r.Url == "https://prodotnetmemory.com");
            Assert.NotNull(res);
            Assert.Equal("Book", res.Category);
        }
    }

    [Fact]
    public async Task Test6_CompleteSeveralDays_RestartRedeploy_VerifyHistoricalProgressRemains()
    {
        // 1. Session 1: Complete Days 1 and 2, and add a planned rest day for Day 3
        using (var db1 = new LearningDbContext(_dbOptions))
        {
            var auditService = new AuditService(db1, NullLogger<AuditService>.Instance);
            var recoveryService = new RecoveryService(db1, auditService, NullLogger<RecoveryService>.Instance);

            var day1 = await db1.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 1);
            day1.Status = DayStatus.Completed;
            foreach (var t in day1.Tasks) { t.Status = "Completed"; t.CompletedAt = DateTime.UtcNow; }

            var day2 = await db1.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 2);
            day2.Status = DayStatus.Completed;
            foreach (var t in day2.Tasks) { t.Status = "Completed"; t.CompletedAt = DateTime.UtcNow; }

            // Schedule planned rest day
            await recoveryService.AddRestDayAsync(new AddRestDayRequestDto
            {
                Date = new DateOnly(2026, 9, 23),
                Reason = "Planned Sunday Rest",
                IsPrePlanned = true
            });

            await db1.SaveChangesAsync();
        } // db1 disposed

        // 2. Session 2: Restart / Redeploy verification
        using (var db2 = new LearningDbContext(_dbOptions))
        {
            var day1 = await db2.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 1);
            var day2 = await db2.DayPlans.Include(d => d.Tasks).FirstAsync(d => d.DayNumber == 2);

            Assert.Equal(DayStatus.Completed, day1.Status);
            Assert.All(day1.Tasks, t => Assert.Equal("Completed", t.Status));

            Assert.Equal(DayStatus.Completed, day2.Status);
            Assert.All(day2.Tasks, t => Assert.Equal("Completed", t.Status));

            // Verify Rest Day was recorded and preserved
            var restDay = await db2.DayPlans.FirstOrDefaultAsync(d => d.Status == DayStatus.RestDay);
            Assert.NotNull(restDay);

            // Streak test across the restart
            var streakService = new StreakService();
            var allDays = await db2.DayPlans.ToListAsync();
            var (currentStreak, bestStreak) = streakService.CalculateStreak(allDays);

            // 2 completed days + 1 planned rest day = 2 streak (unbroken by rest day!)
            Assert.Equal(2, currentStreak);
        }
    }
}
