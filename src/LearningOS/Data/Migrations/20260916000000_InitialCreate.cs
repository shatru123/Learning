using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LearningOS.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DayPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayNumber = table.Column<int>(type: "integer", nullable: true),
                    IsLearningDay = table.Column<bool>(type: "boolean", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReviewSummary = table.Column<string>(type: "text", nullable: true),
                    PhaseId = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DSAProblems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: true),
                    ProblemUrl = table.Column<string>(type: "text", nullable: true),
                    SolutionNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DSAProblems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    AnswerNotes = table.Column<string>(type: "text", nullable: true),
                    ConfidenceLevel = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AppliedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    JobUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TotalLearningDays = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    ExtraMinutesPerDay = table.Column<int>(type: "integer", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemDesignTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TopicName = table.Column<string>(type: "text", nullable: false),
                    ArchitectureSummary = table.Column<string>(type: "text", nullable: true),
                    KeyComponents = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DiagramUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemDesignTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxExtraRecoveryMinutesPerDay = table.Column<int>(type: "integer", nullable: false),
                    DailyTargetStudyMinutes = table.Column<int>(type: "integer", nullable: false),
                    WorkdayStartHour = table.Column<int>(type: "integer", nullable: false),
                    WorkdayEndHour = table.Column<int>(type: "integer", nullable: false),
                    PreferredStudyStartTime = table.Column<string>(type: "text", nullable: false),
                    PreferredStudyEndTime = table.Column<string>(type: "text", nullable: false),
                    WeekdayDailyAvailableMinutes = table.Column<int>(type: "integer", nullable: false),
                    WeekendDailyAvailableMinutes = table.Column<int>(type: "integer", nullable: false),
                    ProtectWorkingHours = table.Column<bool>(type: "boolean", nullable: false),
                    RoadmapStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RoadmapEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rating = table.Column<string>(type: "text", nullable: false),
                    WhatHappenedNotes = table.Column<string>(type: "text", nullable: true),
                    CarryForwardNotes = table.Column<string>(type: "text", nullable: true),
                    TomorrowPriority = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReviews_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DayStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayStatusHistories_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OriginalDayNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentDayNumber = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningTasks_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveDayRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LeaveType = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    PreservesStreak = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveDayRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveDayRecords_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MissedDayRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: false),
                    MissedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RecoveryStrategy = table.Column<string>(type: "text", nullable: true),
                    RecoveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissedDayRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissedDayRecords_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LearningPlanId = table.Column<int>(type: "integer", nullable: false),
                    PhaseNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDay = table.Column<int>(type: "integer", nullable: false),
                    EndDay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Phases_LearningPlans_LearningPlanId",
                        column: x => x.LearningPlanId,
                        principalTable: "LearningPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestDayRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayPlanId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    IsPrePlanned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestDayRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestDayRecords_DayPlans_DayPlanId",
                        column: x => x.DayPlanId,
                        principalTable: "DayPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaskHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LearningTaskId = table.Column<int>(type: "integer", nullable: false),
                    OldStatus = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskHistories_LearningTasks_LearningTaskId",
                        column: x => x.LearningTaskId,
                        principalTable: "LearningTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAuditLogs_ActionType",
                table: "ActivityAuditLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAuditLogs_Timestamp",
                table: "ActivityAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReviews_DayPlanId",
                table: "DailyReviews",
                column: "DayPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayPlans_CalendarDate",
                table: "DayPlans",
                column: "CalendarDate");

            migrationBuilder.CreateIndex(
                name: "IX_DayPlans_DayNumber",
                table: "DayPlans",
                column: "DayNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DayPlans_Status",
                table: "DayPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DayStatusHistories_DayPlanId",
                table: "DayStatusHistories",
                column: "DayPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTasks_Category",
                table: "LearningTasks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTasks_DayPlanId",
                table: "LearningTasks",
                column: "DayPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTasks_Status",
                table: "LearningTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDayRecords_DayPlanId",
                table: "LeaveDayRecords",
                column: "DayPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissedDayRecords_DayPlanId",
                table: "MissedDayRecords",
                column: "DayPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Phases_LearningPlanId",
                table: "Phases",
                column: "LearningPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RestDayRecords_DayPlanId",
                table: "RestDayRecords",
                column: "DayPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistories_LearningTaskId",
                table: "TaskHistories",
                column: "LearningTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ActivityAuditLogs");
            migrationBuilder.DropTable(name: "DailyReviews");
            migrationBuilder.DropTable(name: "DayStatusHistories");
            migrationBuilder.DropTable(name: "DSAProblems");
            migrationBuilder.DropTable(name: "Goals");
            migrationBuilder.DropTable(name: "InterviewQuestions");
            migrationBuilder.DropTable(name: "JobApplications");
            migrationBuilder.DropTable(name: "JournalEntries");
            migrationBuilder.DropTable(name: "LearningResources");
            migrationBuilder.DropTable(name: "LeaveDayRecords");
            migrationBuilder.DropTable(name: "MissedDayRecords");
            migrationBuilder.DropTable(name: "Phases");
            migrationBuilder.DropTable(name: "RecoveryPlans");
            migrationBuilder.DropTable(name: "RestDayRecords");
            migrationBuilder.DropTable(name: "StudySessions");
            migrationBuilder.DropTable(name: "SystemDesignTopics");
            migrationBuilder.DropTable(name: "TaskHistories");
            migrationBuilder.DropTable(name: "UserSettings");
            migrationBuilder.DropTable(name: "LearningTasks");
            migrationBuilder.DropTable(name: "DayPlans");
            migrationBuilder.DropTable(name: "LearningPlans");
        }
    }
}
