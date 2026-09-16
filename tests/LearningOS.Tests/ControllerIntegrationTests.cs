using LearningOS.Controllers;
using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningOS.Tests;

public class ControllerIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public ControllerIntegrationTests()
    {
        _dbPath = $"ctrl_test_{Guid.NewGuid():N}.db";
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
    public async Task HealthEndpoint_ReturnsHealthy_With100Days()
    {
        using var db = new LearningDbContext(_dbOptions);
        var mockEnv = new TestWebHostEnvironment();
        var controller = new HealthController(db, mockEnv);

        var result = await controller.CheckHealth();
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Convert anonymous object properties via reflection
        var val = okResult.Value;
        var statusProp = val.GetType().GetProperty("status")?.GetValue(val)?.ToString();
        var totalDays = (int)(val.GetType().GetProperty("totalLearningDays")?.GetValue(val) ?? 0);

        Assert.Equal("Healthy", statusProp);
        Assert.Equal(100, totalDays);
    }

    [Fact]
    public async Task DashboardController_ReturnsAccurateSummary_AndSeparatedStatusCounts()
    {
        using var db = new LearningDbContext(_dbOptions);
        var streak = new StreakService();
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var recovery = new RecoveryService(db, audit, NullLogger<RecoveryService>.Instance);
        var controller = new DashboardController(db, streak, recovery, audit);

        var actionResult = await controller.GetSummary();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var summary = Assert.IsType<DashboardSummaryDto>(okResult.Value);

        // Requirement 1: 100 Learning Days
        Assert.Equal(100, summary.TotalPlannedDays);
        Assert.Equal(100, summary.PlannedDaysRemainingCount);
        Assert.Equal(0, summary.CompletedDaysCount);
        Assert.Equal(0, summary.MissedDaysCount);
        Assert.Equal(0, summary.RestDaysCount);
        Assert.True(summary.TotalTasksCount >= 200);
        Assert.NotNull(summary.TodayPlan);
    }

    [Fact]
    public async Task DaysController_MarkMissed_AndRecover_WorksFlawlessly()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var recovery = new RecoveryService(db, audit, NullLogger<RecoveryService>.Instance);
        var controller = new DaysController(db, recovery, audit);

        var day1 = await db.DayPlans.FirstAsync(d => d.DayNumber == 1);

        // 1. Mark day missed with reason and note (Requirement 3)
        var missedResult = await controller.MarkDayMissed(day1.Id, new MissedDayRequestDto
        {
            Reason = "Too much workload",
            Note = "Production deployment critical fix"
        });

        var okMissed = Assert.IsType<OkObjectResult>(missedResult.Result);
        var missedDto = Assert.IsType<DayPlanDto>(okMissed.Value);

        Assert.Equal("Missed", missedDto.Status);
        Assert.NotNull(missedDto.MissedRecord);
        Assert.Equal("Too much workload", missedDto.MissedRecord.Reason);
        Assert.Equal("Production deployment critical fix", missedDto.MissedRecord.Note);
        Assert.NotEmpty(missedDto.Tasks); // Original tasks remain visible!

        // 2. Recover day using MoveToTomorrow strategy (Requirement 4 & 5)
        var recoverResult = await controller.RecoverDay(day1.Id, new RecoverDayRequestDto
        {
            Strategy = "MoveToTomorrow",
            MaxExtraMinutesPerDay = 60
        });

        var okRecover = Assert.IsType<OkObjectResult>(recoverResult.Result);
        var recoveredDto = Assert.IsType<DayPlanDto>(okRecover.Value);
        Assert.NotNull(recoveredDto.MissedRecord?.RecoveryStrategy);
        Assert.Equal("MoveToTomorrow", recoveredDto.MissedRecord.RecoveryStrategy);
    }

    [Fact]
    public async Task TasksController_UpdateStatus_RecordsHistoryPermanently()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var controller = new TasksController(db, audit);

        var task = await db.LearningTasks.FirstAsync(t => t.CurrentDayNumber == 1);

        // Update task to InProgress
        var res1 = await controller.UpdateStatus(task.Id, new TaskStatusUpdateDto
        {
            Status = "InProgress",
            Note = "Started studying Span<T> internals"
        });

        // Update task to Completed
        var res2 = await controller.UpdateStatus(task.Id, new TaskStatusUpdateDto
        {
            Status = "Completed",
            Note = "Finished benchmarks with 0 B heap allocation"
        });

        var okRes = Assert.IsType<OkObjectResult>(res2.Result);
        var taskDto = Assert.IsType<LearningTaskDto>(okRes.Value);

        Assert.Equal("Completed", taskDto.Status);
        Assert.NotNull(taskDto.CompletedAt);

        // Requirement 4 & 14: Never lose task history
        Assert.True(taskDto.History.Count >= 2);
        Assert.Contains(taskDto.History, h => h.NewStatus == "InProgress");
        Assert.Contains(taskDto.History, h => h.NewStatus == "Completed");
    }

    [Fact]
    public async Task ReviewsController_SubmitsAndPersistsDailyReview()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var controller = new ReviewsController(db, audit);

        var day1 = await db.DayPlans.FirstAsync(d => d.DayNumber == 1);

        var reviewResult = await controller.SubmitReview(new DailyReviewDto
        {
            DayPlanId = day1.Id,
            ReviewDate = day1.CalendarDate,
            Rating = "Completed everything",
            WhatHappenedNotes = "Great focus session on Span<T> and GC layout",
            CarryForwardNotes = "Keep memory management cheat sheet handy",
            TomorrowPriority = "Ref structs and Memory<T> async constraints"
        });

        var okReview = Assert.IsType<OkObjectResult>(reviewResult.Result);
        var review = Assert.IsType<DailyReview>(okReview.Value);

        Assert.Equal("Completed everything", review.Rating);
        Assert.Equal("Ref structs and Memory<T> async constraints", review.TomorrowPriority);

        // Verify day review summary updated
        var updatedDay = await db.DayPlans.FirstAsync(d => d.Id == day1.Id);
        Assert.Contains("Completed everything", updatedDay.ReviewSummary);
    }
}

// Mock WebHostEnvironment for health check tests
public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "LearningOS";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
