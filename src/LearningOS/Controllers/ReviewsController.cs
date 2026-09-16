using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;

    public ReviewsController(LearningDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet("{dayPlanId}")]
    public async Task<ActionResult<DailyReview>> GetReview(int dayPlanId)
    {
        var review = await _db.DailyReviews.FirstOrDefaultAsync(r => r.DayPlanId == dayPlanId);
        if (review == null)
            return NotFound(new { message = $"No review found for DayPlan {dayPlanId}" });

        return Ok(review);
    }

    [HttpPost]
    public async Task<ActionResult<DailyReview>> SubmitReview([FromBody] DailyReviewDto dto)
    {
        var day = await _db.DayPlans.FirstOrDefaultAsync(d => d.Id == dto.DayPlanId);
        if (day == null)
            return NotFound(new { message = $"DayPlan {dto.DayPlanId} not found." });

        var existing = await _db.DailyReviews.FirstOrDefaultAsync(r => r.DayPlanId == dto.DayPlanId);
        if (existing == null)
        {
            existing = new DailyReview
            {
                DayPlanId = dto.DayPlanId,
                ReviewDate = dto.ReviewDate != default ? dto.ReviewDate : day.CalendarDate,
                Rating = dto.Rating,
                WhatHappenedNotes = dto.WhatHappenedNotes,
                CarryForwardNotes = dto.CarryForwardNotes,
                TomorrowPriority = dto.TomorrowPriority,
                CreatedAt = DateTime.UtcNow
            };
            _db.DailyReviews.Add(existing);
        }
        else
        {
            existing.Rating = dto.Rating;
            existing.WhatHappenedNotes = dto.WhatHappenedNotes;
            existing.CarryForwardNotes = dto.CarryForwardNotes;
            existing.TomorrowPriority = dto.TomorrowPriority;
        }

        day.ReviewSummary = $"Rating: {dto.Rating}. Next Priority: {dto.TomorrowPriority}";
        day.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "ReviewCompleted",
            "DailyReview",
            existing.Id.ToString(),
            $"Completed Daily Review for Day {day.DayNumber ?? 0}. Rating: {dto.Rating}. Priority: {dto.TomorrowPriority}");

        return Ok(existing);
    }
}
