using Microsoft.EntityFrameworkCore;
using LearningOS.Models;

namespace LearningOS.Data;

public class LearningDbContext : DbContext
{
    public LearningDbContext(DbContextOptions<LearningDbContext> options) : base(options)
    {
    }

    public DbSet<LearningPlan> LearningPlans => Set<LearningPlan>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<LearningTask> LearningTasks => Set<LearningTask>();
    public DbSet<TaskHistory> TaskHistories => Set<TaskHistory>();
    public DbSet<DayStatusHistory> DayStatusHistories => Set<DayStatusHistory>();
    public DbSet<MissedDayRecord> MissedDayRecords => Set<MissedDayRecord>();
    public DbSet<RecoveryPlan> RecoveryPlans => Set<RecoveryPlan>();
    public DbSet<RestDayRecord> RestDayRecords => Set<RestDayRecord>();
    public DbSet<LeaveDayRecord> LeaveDayRecords => Set<LeaveDayRecord>();
    public DbSet<DailyReview> DailyReviews => Set<DailyReview>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<DSAProblem> DSAProblems => Set<DSAProblem>();
    public DbSet<SystemDesignTopic> SystemDesignTopics => Set<SystemDesignTopic>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<ActivityAuditLog> ActivityAuditLogs => Set<ActivityAuditLog>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DayPlan
        modelBuilder.Entity<DayPlan>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.DayNumber);
            entity.HasIndex(d => d.CalendarDate);
            entity.HasIndex(d => d.Status);

            entity.HasMany(d => d.Tasks)
                  .WithOne()
                  .HasForeignKey(t => t.DayPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.StatusHistory)
                  .WithOne()
                  .HasForeignKey(sh => sh.DayPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.MissedRecord)
                  .WithOne()
                  .HasForeignKey<MissedDayRecord>(m => m.DayPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.RestRecord)
                  .WithOne()
                  .HasForeignKey<RestDayRecord>(r => r.DayPlanId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.LeaveRecord)
                  .WithOne()
                  .HasForeignKey<LeaveDayRecord>(l => l.DayPlanId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.DailyReview)
                  .WithOne()
                  .HasForeignKey<DailyReview>(r => r.DayPlanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LearningTask
        modelBuilder.Entity<LearningTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.DayPlanId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Category);

            entity.HasMany(t => t.History)
                  .WithOne()
                  .HasForeignKey(h => h.LearningTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ActivityAuditLog
        modelBuilder.Entity<ActivityAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.ActionType);
        });

        // Configure JournalEntry
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.HasIndex(j => j.EntryDate);
        });
    }
}
