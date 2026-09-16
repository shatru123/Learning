using LearningOS.Dtos;

namespace LearningOS.Services;

public interface IBackupService
{
    Task<string> ExportEverythingAsJsonAsync();
    Task<string> ExportTasksAsCsvAsync();
    Task<RestoreValidationResultDto> ValidateBackupAsync(string jsonContent);
    Task<RestoreExecutionResultDto> RestoreBackupAsync(string jsonContent, bool confirm);
}

public class RestoreValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int DayPlansCount { get; set; }
    public int TasksCount { get; set; }
    public int DSAProblemsCount { get; set; }
    public int SystemDesignCount { get; set; }
    public int JournalCount { get; set; }
    public int AuditLogsCount { get; set; }
    public DateTime ExportedAt { get; set; }
}

public class RestoreExecutionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RestoredDays { get; set; }
    public int RestoredTasks { get; set; }
}
