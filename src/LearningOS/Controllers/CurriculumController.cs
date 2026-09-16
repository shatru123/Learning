using LearningOS.Data;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurriculumController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;

    public CurriculumController(LearningDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    // DSA Problems
    [HttpGet("dsa")]
    public async Task<ActionResult<List<DSAProblem>>> GetDSAProblems([FromQuery] string? status, [FromQuery] string? difficulty)
    {
        var query = _db.DSAProblems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(difficulty)) query = query.Where(d => d.Difficulty == difficulty);
        return Ok(await query.OrderBy(d => d.Id).ToListAsync());
    }

    [HttpPost("dsa")]
    public async Task<ActionResult<DSAProblem>> CreateDSAProblem([FromBody] DSAProblem problem)
    {
        problem.CreatedAt = DateTime.UtcNow;
        _db.DSAProblems.Add(problem);
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("DSAProblemAdded", "DSAProblem", problem.Id.ToString(), $"Added DSA Problem: {problem.Title} ({problem.Difficulty})");
        return Ok(problem);
    }

    [HttpPut("dsa/{id}/status")]
    public async Task<ActionResult<DSAProblem>> UpdateDSAStatus(int id, [FromBody] string status)
    {
        var prob = await _db.DSAProblems.FindAsync(id);
        if (prob == null) return NotFound();
        prob.Status = status;
        if (status == "Solved") prob.SolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("DSAProblemUpdated", "DSAProblem", prob.Id.ToString(), $"DSA Problem '{prob.Title}' updated to {status}");
        return Ok(prob);
    }

    // System Design
    [HttpGet("systemdesign")]
    public async Task<ActionResult<List<SystemDesignTopic>>> GetSystemDesignTopics()
    {
        return Ok(await _db.SystemDesignTopics.OrderBy(s => s.Id).ToListAsync());
    }

    [HttpPost("systemdesign")]
    public async Task<ActionResult<SystemDesignTopic>> CreateSystemDesignTopic([FromBody] SystemDesignTopic topic)
    {
        topic.CreatedAt = DateTime.UtcNow;
        _db.SystemDesignTopics.Add(topic);
        await _db.SaveChangesAsync();
        return Ok(topic);
    }

    [HttpPut("systemdesign/{id}/status")]
    public async Task<ActionResult<SystemDesignTopic>> UpdateSystemDesignStatus(int id, [FromBody] string status)
    {
        var topic = await _db.SystemDesignTopics.FindAsync(id);
        if (topic == null) return NotFound();
        topic.Status = status;
        await _db.SaveChangesAsync();
        return Ok(topic);
    }

    // Interview Questions
    [HttpGet("interview")]
    public async Task<ActionResult<List<InterviewQuestion>>> GetInterviewQuestions([FromQuery] string? category)
    {
        var query = _db.InterviewQuestions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(q => q.Category == category);
        return Ok(await query.OrderBy(q => q.Id).ToListAsync());
    }

    [HttpPost("interview")]
    public async Task<ActionResult<InterviewQuestion>> CreateInterviewQuestion([FromBody] InterviewQuestion q)
    {
        q.CreatedAt = DateTime.UtcNow;
        _db.InterviewQuestions.Add(q);
        await _db.SaveChangesAsync();
        return Ok(q);
    }

    // Job Applications
    [HttpGet("jobs")]
    public async Task<ActionResult<List<JobApplication>>> GetJobApplications()
    {
        return Ok(await _db.JobApplications.OrderByDescending(j => j.AppliedDate).ToListAsync());
    }

    [HttpPost("jobs")]
    public async Task<ActionResult<JobApplication>> CreateJobApplication([FromBody] JobApplication job)
    {
        job.CreatedAt = DateTime.UtcNow;
        _db.JobApplications.Add(job);
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("JobApplicationAdded", "JobApplication", job.Id.ToString(), $"Tracked job application: {job.Company} - {job.Role}");
        return Ok(job);
    }

    [HttpPut("jobs/{id}/status")]
    public async Task<ActionResult<JobApplication>> UpdateJobStatus(int id, [FromBody] string status)
    {
        var job = await _db.JobApplications.FindAsync(id);
        if (job == null) return NotFound();
        job.Status = status;
        await _db.SaveChangesAsync();
        return Ok(job);
    }

    // Journal
    [HttpGet("journal")]
    public async Task<ActionResult<List<JournalEntry>>> GetJournalEntries()
    {
        return Ok(await _db.JournalEntries.OrderByDescending(j => j.EntryDate).ToListAsync());
    }

    [HttpPost("journal")]
    public async Task<ActionResult<JournalEntry>> CreateJournalEntry([FromBody] JournalEntry entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        if (entry.EntryDate == default) entry.EntryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("JournalEntryCreated", "JournalEntry", entry.Id.ToString(), $"Created journal entry: '{entry.Title}'");
        return Ok(entry);
    }

    // Resources
    [HttpGet("resources")]
    public async Task<ActionResult<List<LearningResource>>> GetResources()
    {
        return Ok(await _db.LearningResources.OrderByDescending(r => r.AddedAt).ToListAsync());
    }

    [HttpPost("resources")]
    public async Task<ActionResult<LearningResource>> CreateResource([FromBody] LearningResource res)
    {
        res.AddedAt = DateTime.UtcNow;
        _db.LearningResources.Add(res);
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("ResourceAdded", "LearningResource", res.Id.ToString(), $"Added resource: '{res.Title}'");
        return Ok(res);
    }

    // Goals
    [HttpGet("goals")]
    public async Task<ActionResult<List<Goal>>> GetGoals()
    {
        return Ok(await _db.Goals.OrderBy(g => g.Id).ToListAsync());
    }

    [HttpPost("goals")]
    public async Task<ActionResult<Goal>> CreateGoal([FromBody] Goal goal)
    {
        goal.CreatedAt = DateTime.UtcNow;
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return Ok(goal);
    }

    // Study Sessions
    [HttpGet("sessions")]
    public async Task<ActionResult<List<StudySession>>> GetStudySessions()
    {
        return Ok(await _db.StudySessions.OrderByDescending(s => s.SessionDate).ToListAsync());
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<StudySession>> LogStudySession([FromBody] StudySession session)
    {
        session.CreatedAt = DateTime.UtcNow;
        if (session.SessionDate == default) session.SessionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.StudySessions.Add(session);
        await _db.SaveChangesAsync();
        await _auditService.LogActivityAsync("StudySessionLogged", "StudySession", session.Id.ToString(), $"Logged {session.DurationMinutes}m study session on '{session.Subject}'");
        return Ok(session);
    }
}
