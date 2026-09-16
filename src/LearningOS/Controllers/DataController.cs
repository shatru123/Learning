using System.Text;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IBackupService _backupService;

    public DataController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson()
    {
        var json = await _backupService.ExportEverythingAsJsonAsync();
        string fileName = $"LearningOS_Backup_{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var csv = await _backupService.ExportTasksAsCsvAsync();
        string fileName = $"LearningOS_Tasks_{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpPost("restore/validate")]
    public async Task<ActionResult<RestoreValidationResultDto>> ValidateBackup([FromBody] RestorePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.JsonContent))
            return BadRequest(new { message = "JSON payload cannot be empty." });

        var result = await _backupService.ValidateBackupAsync(payload.JsonContent);
        return Ok(result);
    }

    [HttpPost("restore/confirm")]
    public async Task<ActionResult<RestoreExecutionResultDto>> ConfirmRestore([FromBody] RestoreConfirmPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.JsonContent))
            return BadRequest(new { message = "JSON payload cannot be empty." });

        var result = await _backupService.RestoreBackupAsync(payload.JsonContent, payload.Confirm);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    public class RestorePayload
    {
        public string JsonContent { get; set; } = string.Empty;
    }

    public class RestoreConfirmPayload
    {
        public string JsonContent { get; set; } = string.Empty;
        public bool Confirm { get; set; }
    }
}
