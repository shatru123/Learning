using LearningOS.Data;
using LearningOS.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Services;

public class CurriculumProvisioningService : ICurriculumProvisioningService
{
    private readonly LearningDbContext _db;
    private readonly ILogger<CurriculumProvisioningService> _logger;

    public CurriculumProvisioningService(LearningDbContext db, ILogger<CurriculumProvisioningService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProvisionCurriculumForUserAsync(int userId, DateOnly startDate)
    {
        // Ensure user does not already have a provisioned curriculum
        bool exists = await _db.DayPlans.IgnoreQueryFilters().AnyAsync(d => d.UserId == userId);
        if (exists)
        {
            _logger.LogInformation("Curriculum already exists for UserId: {UserId}", userId);
            return;
        }

        _logger.LogInformation("Provisioning 100-Day Curriculum for UserId: {UserId} starting from {StartDate}", userId, startDate);

        // 1. User Settings
        var settings = new UserSettings
        {
            UserId = userId,
            MaxExtraRecoveryMinutesPerDay = 60,
            DailyTargetStudyMinutes = 120,
            RoadmapStartDate = startDate,
            RoadmapEndDate = startDate.AddDays(101),
            UpdatedAt = DateTime.UtcNow
        };
        _db.UserSettings.Add(settings);

        // 2. Fetch or create Phase references
        var phases = await _db.Phases.OrderBy(p => p.PhaseNumber).ToListAsync();
        if (!phases.Any())
        {
            var plan = await _db.LearningPlans.FirstOrDefaultAsync() ?? new LearningPlan
            {
                Title = "100-Day Full-Stack & AI Systems Mastery",
                TotalLearningDays = 100,
                StartDate = startDate,
                EndDate = startDate.AddDays(101),
                CreatedAt = DateTime.UtcNow
            };
            if (plan.Id == 0) _db.LearningPlans.Add(plan);
            await _db.SaveChangesAsync();

            phases = new List<Phase>
            {
                new() { LearningPlanId = plan.Id, PhaseNumber = 1, Title = "Core .NET/C#, SQL & High-Performance Data Access", Description = "Deep C# internals, memory management, ASP.NET Core internals, SQL execution plans, Dapper & EF Core, and fundamental DSA.", StartDay = 1, EndDay = 25 },
                new() { LearningPlanId = plan.Id, PhaseNumber = 2, Title = "Distributed Systems, Microservices, Cloud & Messaging", Description = "Microservices architecture, RabbitMQ, Refit, YARP reverse proxy, Redis distributed caching, OpenTelemetry & System Design.", StartDay = 26, EndDay = 50 },
                new() { LearningPlanId = plan.Id, PhaseNumber = 3, Title = "AI Engineering, LLMs, Vector DBs & Production Agents", Description = "OpenAI, Claude, Gemini, Ollama, Prompt Engineering, Embeddings, pgvector/Qdrant, RAG architectures, Tool Calling & Multi-Agent systems.", StartDay = 51, EndDay = 75 },
                new() { LearningPlanId = plan.Id, PhaseNumber = 4, Title = "DevOps, Cloud, Observability & Production Architecture", Description = "Docker multi-stage, Kubernetes, Prometheus, Grafana, Jaeger, CI/CD pipelines, Terraform IaC, and end-to-end cloud deployments.", StartDay = 76, EndDay = 90 },
                new() { LearningPlanId = plan.Id, PhaseNumber = 5, Title = "Interview Mastery, Resume/LinkedIn & Job Applications", Description = "System design interview drills, technical deep dives, behavioral STAR stories, resume ATS optimization, and application pipeline.", StartDay = 91, EndDay = 100 }
            };
            _db.Phases.AddRange(phases);
            await _db.SaveChangesAsync();
        }

        // 3. Generate 100 DayPlans & Tasks
        var curDate = startDate;
        var dayPlans = new List<DayPlan>();

        for (int dayNum = 1; dayNum <= 100; dayNum++)
        {
            var phase = phases.First(p => dayNum >= p.StartDay && dayNum <= p.EndDay);
            int weekNum = ((dayNum - 1) / 7) + 1;

            var (title, theme, tasks) = DbInitializer.GetCurriculumForDay(dayNum);

            var dayPlan = new DayPlan
            {
                UserId = userId,
                DayNumber = dayNum,
                IsLearningDay = true,
                CalendarDate = curDate,
                Status = DayStatus.Planned,
                Title = title,
                Theme = theme,
                PhaseId = phase.Id,
                WeekNumber = weekNum,
                CreatedAt = DateTime.UtcNow,
                Tasks = tasks.Select(t => new LearningTask
                {
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    EstimatedMinutes = t.EstimatedMinutes,
                    Priority = t.Priority,
                    Status = "Pending",
                    OriginalDayNumber = dayNum,
                    CurrentDayNumber = dayNum,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            dayPlans.Add(dayPlan);
            curDate = curDate.AddDays(1);
        }

        _db.DayPlans.AddRange(dayPlans);

        // 4. Seed Starter DSA & Trackers for User
        _db.DSAProblems.AddRange(new List<DSAProblem>
        {
            new() { UserId = userId, Title = "Two Sum", Difficulty = "Easy", Platform = "LeetCode", Pattern = "Arrays & Hashing", Status = "Planned" },
            new() { UserId = userId, Title = "Group Anagrams", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Arrays & Hashing", Status = "Planned" },
            new() { UserId = userId, Title = "Top K Frequent Elements", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Heap / Bucket Sort", Status = "Planned" },
            new() { UserId = userId, Title = "Longest Substring Without Repeating Characters", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Sliding Window", Status = "Planned" },
            new() { UserId = userId, Title = "Valid Palindrome", Difficulty = "Easy", Platform = "LeetCode", Pattern = "Two Pointers", Status = "Planned" },
            new() { UserId = userId, Title = "LRU Cache", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Design / Hash + DLL", Status = "Planned" },
            new() { UserId = userId, Title = "Merge Intervals", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Intervals", Status = "Planned" },
            new() { UserId = userId, Title = "Number of Islands", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Graphs / BFS/DFS", Status = "Planned" },
            new() { UserId = userId, Title = "Coin Change", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Dynamic Programming", Status = "Planned" }
        });

        _db.SystemDesignTopics.AddRange(new List<SystemDesignTopic>
        {
            new() { UserId = userId, TopicName = "URL Shortener (TinyURL)", ArchitectureSummary = "Base62 encoding, MD5/SHA256 hashing, distributed ID generator (Snowflake), Redis caching layer, 301 vs 302 redirects." },
            new() { UserId = userId, TopicName = "Rate Limiter", ArchitectureSummary = "Token Bucket vs Leaky Bucket vs Sliding Window Counter. Redis cluster with Lua script for atomic token decrement." },
            new() { UserId = userId, TopicName = "Distributed Job Scheduler", ArchitectureSummary = "Worker nodes, Redis delayed queue, leader election (Raft/Zookeeper), dead-letter queues, idempotent retry semantics." }
        });

        _db.ActivityAuditLogs.Add(new ActivityAuditLog
        {
            UserId = userId,
            ActionType = "CurriculumProvisioned",
            EntityName = "User",
            EntityId = userId.ToString(),
            Description = $"100-Day learning curriculum provisioned successfully starting {startDate:yyyy-MM-dd}.",
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully provisioned 100 days for UserId: {UserId}", userId);
    }
}
