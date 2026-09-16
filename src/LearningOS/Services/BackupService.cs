using System.Text;
using System.Text.Json;
using LearningOS.Data;
using LearningOS.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Services;

public class BackupService : IBackupService
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(LearningDbContext db, IAuditService auditService, ILogger<BackupService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<string> ExportEverythingAsJsonAsync()
    {
        var backup = new
        {
            Version = "1.0",
            ExportDate = DateTime.UtcNow,
            Plans = await _db.LearningPlans.Include(p => p.Phases).AsNoTracking().ToListAsync(),
            DayPlans = await _db.DayPlans
                .Include(d => d.Tasks).ThenInclude(t => t.History)
                .Include(d => d.StatusHistory)
                .Include(d => d.MissedRecord)
                .Include(d => d.RestRecord)
                .Include(d => d.LeaveRecord)
                .Include(d => d.DailyReview)
                .AsNoTracking().ToListAsync(),
            RecoveryPlans = await _db.RecoveryPlans.AsNoTracking().ToListAsync(),
            DSAProblems = await _db.DSAProblems.AsNoTracking().ToListAsync(),
            SystemDesignTopics = await _db.SystemDesignTopics.AsNoTracking().ToListAsync(),
            InterviewQuestions = await _db.InterviewQuestions.AsNoTracking().ToListAsync(),
            JobApplications = await _db.JobApplications.AsNoTracking().ToListAsync(),
            JournalEntries = await _db.JournalEntries.AsNoTracking().ToListAsync(),
            LearningResources = await _db.LearningResources.AsNoTracking().ToListAsync(),
            StudySessions = await _db.StudySessions.AsNoTracking().ToListAsync(),
            Goals = await _db.Goals.AsNoTracking().ToListAsync(),
            UserSettings = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(),
            ActivityLogs = await _db.ActivityAuditLogs.OrderByDescending(a => a.Timestamp).Take(200).AsNoTracking().ToListAsync()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(backup, options);
    }

    public async Task<string> ExportTasksAsCsvAsync()
    {
        var tasks = await _db.LearningTasks
            .OrderBy(t => t.CurrentDayNumber)
            .ThenBy(t => t.Id)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TaskId,CurrentDay,OriginalDay,Title,Category,EstimatedMinutes,Priority,Status,CompletedAt");

        foreach (var t in tasks)
        {
            string cleanTitle = t.Title.Replace("\"", "\"\"");
            string completed = t.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            sb.AppendLine($"{t.Id},{t.CurrentDayNumber},{t.OriginalDayNumber},\"{cleanTitle}\",{t.Category},{t.EstimatedMinutes},{t.Priority},{t.Status},{completed}");
        }

        return sb.ToString();
    }

    public Task<RestoreValidationResultDto> ValidateBackupAsync(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("DayPlans", out var dayPlansProp))
            {
                return Task.FromResult(new RestoreValidationResultDto
                {
                    IsValid = false,
                    ErrorMessage = "Missing required 'DayPlans' array in backup payload."
                });
            }

            int dayCount = dayPlansProp.GetArrayLength();
            int taskCount = 0;
            foreach (var d in dayPlansProp.EnumerateArray())
            {
                if (d.TryGetProperty("Tasks", out var tProp))
                {
                    taskCount += tProp.GetArrayLength();
                }
            }

            int dsaCount = root.TryGetProperty("DSAProblems", out var dsaProp) ? dsaProp.GetArrayLength() : 0;
            int sysCount = root.TryGetProperty("SystemDesignTopics", out var sysProp) ? sysProp.GetArrayLength() : 0;
            int journalCount = root.TryGetProperty("JournalEntries", out var jProp) ? jProp.GetArrayLength() : 0;
            int auditCount = root.TryGetProperty("ActivityLogs", out var aProp) ? aProp.GetArrayLength() : 0;

            DateTime exportDate = DateTime.UtcNow;
            if (root.TryGetProperty("ExportDate", out var expProp) && expProp.TryGetDateTime(out var expDt))
            {
                exportDate = expDt;
            }

            return Task.FromResult(new RestoreValidationResultDto
            {
                IsValid = true,
                DayPlansCount = dayCount,
                TasksCount = taskCount,
                DSAProblemsCount = dsaCount,
                SystemDesignCount = sysCount,
                JournalCount = journalCount,
                AuditLogsCount = auditCount,
                ExportedAt = exportDate
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RestoreValidationResultDto
            {
                IsValid = false,
                ErrorMessage = $"Failed to parse JSON backup: {ex.Message}"
            });
        }
    }

    public async Task<RestoreExecutionResultDto> RestoreBackupAsync(string jsonContent, bool confirm)
    {
        if (!confirm)
        {
            return new RestoreExecutionResultDto
            {
                Success = false,
                Message = "User confirmation required to perform database restore."
            };
        }

        var validation = await ValidateBackupAsync(jsonContent);
        if (!validation.IsValid)
        {
            return new RestoreExecutionResultDto
            {
                Success = false,
                Message = $"Validation failed: {validation.ErrorMessage}"
            };
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Clear existing data cleanly within transaction
            _db.TaskHistories.RemoveRange(_db.TaskHistories);
            _db.LearningTasks.RemoveRange(_db.LearningTasks);
            _db.DayStatusHistories.RemoveRange(_db.DayStatusHistories);
            _db.MissedDayRecords.RemoveRange(_db.MissedDayRecords);
            _db.RecoveryPlans.RemoveRange(_db.RecoveryPlans);
            _db.RestDayRecords.RemoveRange(_db.RestDayRecords);
            _db.LeaveDayRecords.RemoveRange(_db.LeaveDayRecords);
            _db.DailyReviews.RemoveRange(_db.DailyReviews);
            _db.DayPlans.RemoveRange(_db.DayPlans);
            _db.Phases.RemoveRange(_db.Phases);
            _db.LearningPlans.RemoveRange(_db.LearningPlans);
            _db.DSAProblems.RemoveRange(_db.DSAProblems);
            _db.SystemDesignTopics.RemoveRange(_db.SystemDesignTopics);
            _db.InterviewQuestions.RemoveRange(_db.InterviewQuestions);
            _db.JobApplications.RemoveRange(_db.JobApplications);
            _db.JournalEntries.RemoveRange(_db.JournalEntries);
            _db.LearningResources.RemoveRange(_db.LearningResources);
            _db.StudySessions.RemoveRange(_db.StudySessions);
            _db.Goals.RemoveRange(_db.Goals);

            await _db.SaveChangesAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Restore Plans & Phases
            if (root.TryGetProperty("Plans", out var plansProp))
            {
                var plans = JsonSerializer.Deserialize<List<LearningPlan>>(plansProp.GetRawText(), options);
                if (plans != null) _db.LearningPlans.AddRange(plans);
            }

            // Restore DayPlans and Tasks
            int restoredDaysCount = 0;
            int restoredTasksCount = 0;
            if (root.TryGetProperty("DayPlans", out var dayPlansProp))
            {
                var days = JsonSerializer.Deserialize<List<DayPlan>>(dayPlansProp.GetRawText(), options);
                if (days != null)
                {
                    _db.DayPlans.AddRange(days);
                    restoredDaysCount = days.Count;
                    restoredTasksCount = days.Sum(d => d.Tasks.Count);
                }
            }

            // Restore DSA
            if (root.TryGetProperty("DSAProblems", out var dsaProp))
            {
                var dsa = JsonSerializer.Deserialize<List<DSAProblem>>(dsaProp.GetRawText(), options);
                if (dsa != null) _db.DSAProblems.AddRange(dsa);
            }

            // Restore System Design
            if (root.TryGetProperty("SystemDesignTopics", out var sysProp))
            {
                var sys = JsonSerializer.Deserialize<List<SystemDesignTopic>>(sysProp.GetRawText(), options);
                if (sys != null) _db.SystemDesignTopics.AddRange(sys);
            }

            // Restore Interview Questions
            if (root.TryGetProperty("InterviewQuestions", out var intProp))
            {
                var questions = JsonSerializer.Deserialize<List<InterviewQuestion>>(intProp.GetRawText(), options);
                if (questions != null) _db.InterviewQuestions.AddRange(questions);
            }

            // Restore Journal
            if (root.TryGetProperty("JournalEntries", out var jProp))
            {
                var journal = JsonSerializer.Deserialize<List<JournalEntry>>(jProp.GetRawText(), options);
                if (journal != null) _db.JournalEntries.AddRange(journal);
            }

            // Restore Resources
            if (root.TryGetProperty("LearningResources", out var resProp))
            {
                var res = JsonSerializer.Deserialize<List<LearningResource>>(resProp.GetRawText(), options);
                if (res != null) _db.LearningResources.AddRange(res);
            }

            // Restore Goals
            if (root.TryGetProperty("Goals", out var gProp))
            {
                var goals = JsonSerializer.Deserialize<List<Goal>>(gProp.GetRawText(), options);
                if (goals != null) _db.Goals.AddRange(goals);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _auditService.LogActivityAsync(
                "DatabaseRestored",
                "Database",
                "PostgreSQL",
                $"Database restored from backup. Restored {restoredDaysCount} days and {restoredTasksCount} tasks.");

            return new RestoreExecutionResultDto
            {
                Success = true,
                Message = $"Successfully restored {restoredDaysCount} days and {restoredTasksCount} tasks.",
                RestoredDays = restoredDaysCount,
                RestoredTasks = restoredTasksCount
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to restore backup.");
            return new RestoreExecutionResultDto
            {
                Success = false,
                Message = $"Database restoration failed: {ex.Message}"
            };
        }
    }
}
