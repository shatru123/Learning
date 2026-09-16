using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningOS.Tests;

public class BackupRestoreTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public BackupRestoreTests()
    {
        _dbPath = $"backup_test_{Guid.NewGuid():N}.db";
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
    public async Task ExportJson_ProducesValidCompleteBackup()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var backup = new BackupService(db, audit, NullLogger<BackupService>.Instance);

        string json = await backup.ExportEverythingAsJsonAsync();
        Assert.NotEmpty(json);

        var validation = await backup.ValidateBackupAsync(json);
        Assert.True(validation.IsValid);
        Assert.Equal(100, validation.DayPlansCount);
        Assert.True(validation.TasksCount > 200);
        Assert.True(validation.DSAProblemsCount >= 10);
    }

    [Fact]
    public async Task RestoreBackup_WithConfirmation_RestoresAccurately()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var backup = new BackupService(db, audit, NullLogger<BackupService>.Instance);

        // Customize something
        var day1 = await db.DayPlans.FirstAsync(d => d.DayNumber == 1);
        day1.Notes = "Custom backup test note";
        await db.SaveChangesAsync();

        string exportedJson = await backup.ExportEverythingAsJsonAsync();

        // Mutate day 1
        day1.Notes = "Changed after export";
        await db.SaveChangesAsync();

        // Restore from exported JSON
        var result = await backup.RestoreBackupAsync(exportedJson, confirm: true);
        Assert.True(result.Success);
        Assert.Equal(100, result.RestoredDays);

        // Verify restoration of original exported note
        var reloadedDay1 = await db.DayPlans.FirstAsync(d => d.DayNumber == 1);
        Assert.Equal("Custom backup test note", reloadedDay1.Notes);
    }

    [Fact]
    public async Task RestoreBackup_WithoutConfirmation_FailsSafely()
    {
        using var db = new LearningDbContext(_dbOptions);
        var audit = new AuditService(db, NullLogger<AuditService>.Instance);
        var backup = new BackupService(db, audit, NullLogger<BackupService>.Instance);

        string json = await backup.ExportEverythingAsJsonAsync();
        var result = await backup.RestoreBackupAsync(json, confirm: false);

        Assert.False(result.Success);
        Assert.Contains("confirmation required", result.Message);
    }
}
