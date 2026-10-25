using LearningOS.Controllers;
using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace LearningOS.Tests;

public class MultiUserAuthTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<LearningDbContext> _dbOptions;

    public MultiUserAuthTests()
    {
        _dbPath = $"auth_test_{Guid.NewGuid():N}.db";
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

    private static IConfiguration CreateTestConfig()
    {
        var myConfig = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "TestSecretKey_For_UnitTesting_MultiUser_Authentication_2026!"}
        };
        return new ConfigurationBuilder().AddInMemoryCollection(myConfig).Build();
    }

    private static ICurrentUserService MockUserContext(int? userId, string role = "Learner", string email = "user@test.com")
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.Email, email)
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }
        var accessor = new HttpContextAccessor { HttpContext = context };
        return new CurrentUserService(accessor);
    }

    [Fact]
    public async Task DbInitializer_SeedsAdminUser_AndBackfills100Days()
    {
        using var db = new LearningDbContext(_dbOptions);
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

        Assert.NotNull(admin);
        Assert.Equal("Shatrughna Ambhore", admin.FullName);
        Assert.Equal("ambhoreshatrughna@gmail.com", admin.Email);
        Assert.Equal("Active", admin.Status);

        // Verify the 100 days belong to admin (UserId == admin.Id)
        var adminDays = await db.DayPlans.Where(d => d.UserId == admin.Id).CountAsync();
        Assert.Equal(100, adminDays);

        // Verify default invite code exists
        var invite = await db.InviteCodes.FirstOrDefaultAsync(i => i.Code == "MASTERY-2026");
        Assert.NotNull(invite);
        Assert.True(invite.IsActive);
    }

    [Fact]
    public async Task DbInitializer_WithAdminProgress_Seeds23CompletedDaysAndStreak()
    {
        var dbPath = $"admin_progress_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        try
        {
            using (var db = new LearningDbContext(options))
            {
                await DbInitializer.InitializeAsync(db, NullLogger.Instance, seedAdminProgress: true);

                var admin = await db.Users.FirstAsync(u => u.Role == "Admin");
                var completedDays = await db.DayPlans
                    .Include(d => d.Tasks)
                    .Include(d => d.DailyReview)
                    .Where(d => d.UserId == admin.Id && d.Status == DayStatus.Completed)
                    .OrderBy(d => d.DayNumber)
                    .ToListAsync();

                Assert.Equal(23, completedDays.Count);
                Assert.All(completedDays, d =>
                {
                    Assert.NotNull(d.DailyReview);
                    Assert.Equal("Completed everything", d.DailyReview.Rating);
                    Assert.All(d.Tasks, t => Assert.Equal("Completed", t.Status));
                });

                var completedTasksCount = completedDays.SelectMany(d => d.Tasks).Count();
                Assert.Equal(47, completedTasksCount);

                var allDays = await db.DayPlans.Where(d => d.UserId == admin.Id).ToListAsync();
                var streakService = new StreakService();
                var (currentStreak, bestStreak) = streakService.CalculateStreak(allDays);
                Assert.Equal(23, currentStreak);
                Assert.Equal(23, bestStreak);
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task DbInitializer_SynchronizesAdminProgress_OnExistingDatabase()
    {
        var dbPath = $"admin_sync_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        try
        {
            // Initial run with seedAdminProgress = false
            using (var db = new LearningDbContext(options))
            {
                await DbInitializer.InitializeAsync(db, NullLogger.Instance, seedAdminProgress: false);
            }

            // Second run with seedAdminProgress = true
            using (var db = new LearningDbContext(options))
            {
                await DbInitializer.InitializeAsync(db, NullLogger.Instance, seedAdminProgress: true);

                var admin = await db.Users.FirstAsync(u => u.Role == "Admin");
                var completedCount = await db.DayPlans.CountAsync(d => d.UserId == admin.Id && d.Status == DayStatus.Completed);
                Assert.Equal(23, completedCount);
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task Register_WithInviteCode_ActivatesInstantly_AndProvisions100DaysFromCustomDate()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);
        var currentUserService = MockUserContext(null);

        var controller = new AuthController(db, hasher, tokenService, prov, currentUserService, NullLogger<AuthController>.Instance);

        var customStart = new DateOnly(2026, 11, 1);
        var regReq = new RegisterRequestDto
        {
            FullName = "Alice Engineer",
            Email = "alice@example.com",
            Password = "Password123!",
            StartDate = customStart,
            InviteCode = "MASTERY-2026"
        };

        var result = await controller.Register(regReq);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resp = Assert.IsType<AuthResponseDto>(ok.Value);

        Assert.NotEmpty(resp.Token);
        Assert.Equal("Active", resp.User.Status);
        Assert.Equal(customStart, resp.User.RequestedStartDate);

        // Verify 100 days were created specifically for Alice starting on 2026-11-01
        var aliceContext = MockUserContext(resp.User.Id);
        using var aliceDb = new LearningDbContext(_dbOptions, aliceContext);
        var aliceDays = await aliceDb.DayPlans.OrderBy(d => d.DayNumber).ToListAsync();

        Assert.Equal(100, aliceDays.Count);
        Assert.Equal(customStart, aliceDays.First().CalendarDate);
    }

    [Fact]
    public async Task Register_WithoutStartDate_DefaultsToCurrentDate()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);
        var currentUserService = MockUserContext(null);

        var controller = new AuthController(db, hasher, tokenService, prov, currentUserService, NullLogger<AuthController>.Instance);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var regReq = new RegisterRequestDto
        {
            FullName = "New Learner Today",
            Email = "today_learner@example.com",
            Password = "Password123!",
            StartDate = null, // No start date provided!
            InviteCode = "MASTERY-2026"
        };

        var result = await controller.Register(regReq);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resp = Assert.IsType<AuthResponseDto>(ok.Value);

        Assert.Equal(today, resp.User.RequestedStartDate);

        var userContext = MockUserContext(resp.User.Id);
        using var userDb = new LearningDbContext(_dbOptions, userContext);
        var userDays = await userDb.DayPlans.OrderBy(d => d.DayNumber).ToListAsync();

        Assert.Equal(100, userDays.Count);
        Assert.Equal(today, userDays.First().CalendarDate);
    }

    [Fact]
    public async Task Register_WithoutInviteCode_SetsPendingApproval_AndBlocksLoginUntilApproved()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);
        var currentUserService = MockUserContext(null);

        var controller = new AuthController(db, hasher, tokenService, prov, currentUserService, NullLogger<AuthController>.Instance);

        var regReq = new RegisterRequestDto
        {
            FullName = "Bob Pending",
            Email = "bob@example.com",
            Password = "Password123!",
            StartDate = new DateOnly(2026, 10, 15)
        };

        var regResult = await controller.Register(regReq);
        var regOk = Assert.IsType<OkObjectResult>(regResult.Result);
        var regResp = Assert.IsType<AuthResponseDto>(regOk.Value);

        Assert.Equal("PendingApproval", regResp.User.Status);
        Assert.Empty(regResp.Token); // No token until approved!

        // Attempt login while pending approval
        var loginResult = await controller.Login(new LoginRequestDto
        {
            Email = "bob@example.com",
            Password = "Password123!"
        });

        var objResult = Assert.IsType<ObjectResult>(loginResult.Result);
        Assert.Equal(403, objResult.StatusCode);

        // Now Admin approves Bob
        var adminContext = MockUserContext(1, "Admin");
        var adminController = new AdminController(db, prov, adminContext, NullLogger<AdminController>.Instance);
        var approveResult = await adminController.ApproveUser(regResp.User.Id);
        Assert.IsType<OkObjectResult>(approveResult);

        // Attempt login again after approval -> Should succeed!
        var secondLogin = await controller.Login(new LoginRequestDto
        {
            Email = "bob@example.com",
            Password = "Password123!"
        });

        var loginOk = Assert.IsType<OkObjectResult>(secondLogin.Result);
        var loginResp = Assert.IsType<AuthResponseDto>(loginOk.Value);
        Assert.NotEmpty(loginResp.Token);
        Assert.Equal("Active", loginResp.User.Status);
    }

    [Fact]
    public async Task MultiTenantIsolation_UserACannotMutateOrSeeUserBData()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);

        // Create User A
        var userA = new AppUser { FullName = "User A", Email = "a@test.com", Username = "usera", Role = "Learner", Status = "Active", PasswordHash = "hash" };
        var userB = new AppUser { FullName = "User B", Email = "b@test.com", Username = "userb", Role = "Learner", Status = "Active", PasswordHash = "hash" };
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync();

        await prov.ProvisionCurriculumForUserAsync(userA.Id, new DateOnly(2026, 10, 1));
        await prov.ProvisionCurriculumForUserAsync(userB.Id, new DateOnly(2026, 10, 1));

        // In User A's context: Mark User A's Day 1 as completed
        var userAContext = MockUserContext(userA.Id);
        using (var dbA = new LearningDbContext(_dbOptions, userAContext))
        {
            var dayA = await dbA.DayPlans.FirstAsync(d => d.DayNumber == 1);
            dayA.Status = DayStatus.Completed;
            await dbA.SaveChangesAsync();
        }

        // In User B's context: Verify User B's Day 1 is still Planned (not Completed)
        var userBContext = MockUserContext(userB.Id);
        using (var dbB = new LearningDbContext(_dbOptions, userBContext))
        {
            var dayB = await dbB.DayPlans.FirstAsync(d => d.DayNumber == 1);
            Assert.Equal(DayStatus.Planned, dayB.Status);
        }
    }

    [Fact]
    public async Task AdminController_CanViewAllLearnersProgress()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);

        var learner = new AppUser { FullName = "Learner Dave", Email = "dave@test.com", Username = "dave", Role = "Learner", Status = "Active", PasswordHash = "hash" };
        db.Users.Add(learner);
        await db.SaveChangesAsync();
        await prov.ProvisionCurriculumForUserAsync(learner.Id, new DateOnly(2026, 10, 1));

        var adminContext = MockUserContext(1, "Admin");
        var adminController = new AdminController(db, prov, adminContext, NullLogger<AdminController>.Instance);

        var result = await adminController.GetLearners();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var learners = Assert.IsType<List<LearnerSummaryDto>>(ok.Value);

        Assert.Contains(learners, l => l.Email == "ambhoreshatrughna@gmail.com" && l.Role == "Admin");
        Assert.Contains(learners, l => l.Email == "dave@test.com" && l.Role == "Learner");
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_UpdatesPasswordAndAllowsSubsequentLogin()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);

        var adminUser = await db.Users.FirstAsync(u => u.Role == "Admin");
        var adminContext = MockUserContext(adminUser.Id, "Admin", adminUser.Email);
        var authController = new AuthController(db, hasher, tokenService, prov, adminContext, NullLogger<AuthController>.Instance);

        // 1. Change password from Admin@2026 to NewSecurePass@2026
        var changeResult = await authController.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Admin@2026",
            NewPassword = "NewSecurePass@2026",
            ConfirmNewPassword = "NewSecurePass@2026"
        });

        Assert.IsType<OkObjectResult>(changeResult);

        // 2. Old password should fail login
        var oldLoginResult = await authController.Login(new LoginRequestDto
        {
            Email = adminUser.Email,
            Password = "Admin@2026"
        });
        Assert.IsType<UnauthorizedObjectResult>(oldLoginResult.Result);

        // 3. New password should succeed login
        var newLoginResult = await authController.Login(new LoginRequestDto
        {
            Email = adminUser.Email,
            Password = "NewSecurePass@2026"
        });
        var okLogin = Assert.IsType<OkObjectResult>(newLoginResult.Result);
        var authResp = Assert.IsType<AuthResponseDto>(okLogin.Value);
        Assert.False(string.IsNullOrWhiteSpace(authResp.Token));
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);

        var adminUser = await db.Users.FirstAsync(u => u.Role == "Admin");
        var adminContext = MockUserContext(adminUser.Id, "Admin", adminUser.Email);
        var authController = new AuthController(db, hasher, tokenService, prov, adminContext, NullLogger<AuthController>.Instance);

        var result = await authController.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewSecurePass@2026",
            ConfirmNewPassword = "NewSecurePass@2026"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithMismatchedConfirmation_ReturnsBadRequest()
    {
        var config = CreateTestConfig();
        var hasher = new PasswordHasher();
        var tokenService = new TokenService(config);

        using var db = new LearningDbContext(_dbOptions);
        var prov = new CurriculumProvisioningService(db, NullLogger<CurriculumProvisioningService>.Instance);

        var adminUser = await db.Users.FirstAsync(u => u.Role == "Admin");
        var adminContext = MockUserContext(adminUser.Id, "Admin", adminUser.Email);
        var authController = new AuthController(db, hasher, tokenService, prov, adminContext, NullLogger<AuthController>.Instance);

        var result = await authController.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Admin@2026",
            NewPassword = "NewSecurePass@2026",
            ConfirmNewPassword = "DifferentPass@2026"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void ConvertPostgresUrlToNpgsql_HandlesPasswordsWithSpecialCharacters()
    {
        string raw = "postgresql://postgres:Shatru102016@#@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres";
        string converted = LearningOS.Common.ConnectionStringHelper.ConvertPostgresUrlToNpgsql(raw);

        Assert.Contains("Host=aws-0-ap-southeast-1.pooler.supabase.com;", converted);
        Assert.Contains("Port=5432;", converted);
        Assert.Contains("Database=postgres;", converted);
        Assert.Contains("Username=postgres;", converted);
        Assert.Contains("Password=Shatru102016@#;", converted);
    }

    [Fact]
    public void ConvertPostgresUrlToNpgsql_AutoTranslatesDirectSupabaseHostToPooler()
    {
        string raw = "postgresql://postgres:Shatru102026%40%23@db.epyxzcrroeanpgrhecqj.supabase.co:5432/postgres";
        string converted = LearningOS.Common.ConnectionStringHelper.ConvertPostgresUrlToNpgsql(raw);

        Assert.Contains("Host=aws-0-ap-southeast-1.pooler.supabase.com;", converted);
        Assert.Contains("Port=5432;", converted);
        Assert.Contains("Database=postgres;", converted);
        Assert.Contains("Username=postgres.epyxzcrroeanpgrhecqj;", converted);
        Assert.Contains("Password=Shatru102026@#;", converted);
    }

    [Fact]
    public async Task UnauthenticatedUser_GetsCanonicalPlannedRoadmap_WithoutAdminCompletedData()
    {
        string testDbPath = $"unauth_test_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={testDbPath}")
            .Options;

        try
        {
            // Seed DB with Admin progress (Days 1-23 completed)
            using (var initDb = new LearningDbContext(options))
            {
                await DbInitializer.InitializeAsync(initDb, NullLogger.Instance, seedAdminProgress: true);
            }

            // 1. Unauthenticated context (No JWT token / null user)
            var unauthContext = MockUserContext(null);
            using var unauthDb = new LearningDbContext(options, unauthContext);

            var audit = new AuditService(unauthDb, NullLogger<AuditService>.Instance);
            var recovery = new RecoveryService(unauthDb, audit, NullLogger<RecoveryService>.Instance);
            var daysController = new DaysController(unauthDb, recovery, audit, unauthContext);
            var dashController = new DashboardController(unauthDb, new StreakService(), recovery, audit, unauthContext);
            var currController = new CurriculumController(unauthDb, audit, unauthContext);

            // Verify GetDays() returns 100 planned days with 0 completed tasks and no reviews/notes
            var daysResult = await daysController.GetDays(null, null, null);
            var okDays = Assert.IsType<OkObjectResult>(daysResult.Result);
            var daysList = Assert.IsType<List<DayPlanDto>>(okDays.Value);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            Assert.Equal(100, daysList.Count);
            Assert.Equal(today, daysList.First().CalendarDate);
            Assert.All(daysList, d =>
            {
                Assert.Equal("Planned", d.Status);
                Assert.Null(d.Notes);
                Assert.Null(d.ReviewSummary);
                Assert.Null(d.MissedRecord);
                Assert.Null(d.RestRecord);
                Assert.Null(d.LeaveRecord);
                Assert.All(d.Tasks, t => Assert.Equal("Pending", t.Status));
            });

            // Verify single day details for Day 1 is planned and clean, starting today
            var day1Result = await daysController.GetDay(1);
            var okDay1 = Assert.IsType<OkObjectResult>(day1Result.Result);
            var day1Dto = Assert.IsType<DayPlanDto>(okDay1.Value);
            Assert.Equal("Planned", day1Dto.Status);
            Assert.Equal(today, day1Dto.CalendarDate);
            Assert.Null(day1Dto.Notes);
            Assert.All(day1Dto.Tasks, t => Assert.Equal("Pending", t.Status));

            // Verify Dashboard summary has 0 streak, 0 completed tasks, 0 completed days
            var dashResult = await dashController.GetSummary();
            var okDash = Assert.IsType<OkObjectResult>(dashResult.Result);
            var dashDto = Assert.IsType<DashboardSummaryDto>(okDash.Value);

            Assert.Equal(0, dashDto.CurrentStreak);
            Assert.Equal(0, dashDto.BestStreak);
            Assert.Equal(0, dashDto.CompletedDaysCount);
            Assert.Equal(0, dashDto.CompletedTasksCount);
            Assert.Equal(0, dashDto.TaskCompletionPercent);
            Assert.Equal(100, dashDto.PlannedDaysRemainingCount);
            Assert.Empty(dashDto.RecoveryQueue);
            Assert.Empty(dashDto.RecentActivity);

            // Verify Curriculum Trackers for unauthenticated visitor
            var dsaResult = await currController.GetDSAProblems(null, null);
            var okDsa = Assert.IsType<OkObjectResult>(dsaResult.Result);
            var dsaList = Assert.IsType<List<DSAProblem>>(okDsa.Value);
            Assert.NotEmpty(dsaList);
            Assert.All(dsaList, p => Assert.Equal("Planned", p.Status));

            var jobsResult = await currController.GetJobApplications();
            var okJobs = Assert.IsType<OkObjectResult>(jobsResult.Result);
            var jobsList = Assert.IsType<List<JobApplication>>(okJobs.Value);
            Assert.Empty(jobsList);

            var journalResult = await currController.GetJournalEntries();
            var okJournal = Assert.IsType<OkObjectResult>(journalResult.Result);
            var journalList = Assert.IsType<List<JournalEntry>>(okJournal.Value);
            Assert.Empty(journalList);

            // 2. Authenticated Admin context (Shatrughna, UserId = 1)
            var adminContext = MockUserContext(1, "Admin", "ambhoreshatrughna@gmail.com");
            using var adminDb = new LearningDbContext(options, adminContext);

            var adminDaysController = new DaysController(adminDb, recovery, audit, adminContext);
            var adminDashController = new DashboardController(adminDb, new StreakService(), recovery, audit, adminContext);

            var adminDaysResult = await adminDaysController.GetDays(null, null, null);
            var adminOkDays = Assert.IsType<OkObjectResult>(adminDaysResult.Result);
            var adminDaysList = Assert.IsType<List<DayPlanDto>>(adminOkDays.Value);

            int adminCompletedDays = adminDaysList.Count(d => d.Status == "Completed");
            Assert.Equal(23, adminCompletedDays);

            var adminDashResult = await adminDashController.GetSummary();
            var adminOkDash = Assert.IsType<OkObjectResult>(adminDashResult.Result);
            var adminDashDto = Assert.IsType<DashboardSummaryDto>(adminOkDash.Value);

            Assert.Equal(23, adminDashDto.CompletedDaysCount);
            Assert.True(adminDashDto.CompletedTasksCount >= 46);
            Assert.True(adminDashDto.CurrentStreak >= 23);
        }
        finally
        {
            if (File.Exists(testDbPath))
            {
                try { File.Delete(testDbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task LearnerAlice_CannotSeeAdminProgress_AndUnauthenticatedCannotSeeAliceProgress()
    {
        string testDbPath = $"alice_iso_test_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite($"Data Source={testDbPath}")
            .Options;

        try
        {
            // Seed DB with Admin progress (Days 1-23 completed)
            using (var initDb = new LearningDbContext(options))
            {
                await DbInitializer.InitializeAsync(initDb, NullLogger.Instance, seedAdminProgress: true);

                // Register and provision Alice (Learner, UserId = 2)
                var prov = new CurriculumProvisioningService(initDb, NullLogger<CurriculumProvisioningService>.Instance);
                var alice = new AppUser
                {
                    FullName = "Alice Learner",
                    Email = "alice@example.com",
                    Username = "alice",
                    PasswordHash = "hash",
                    Role = "Learner",
                    Status = "Active",
                    RequestedStartDate = new DateOnly(2026, 10, 1)
                };
                initDb.Users.Add(alice);
                await initDb.SaveChangesAsync();
                await prov.ProvisionCurriculumForUserAsync(alice.Id, new DateOnly(2026, 10, 1));
            }

            // Alice context (UserId = 2)
            var aliceContext = MockUserContext(2, "Learner", "alice@example.com");
            using (var aliceDb = new LearningDbContext(options, aliceContext))
            {
                var audit = new AuditService(aliceDb, NullLogger<AuditService>.Instance);
                var recovery = new RecoveryService(aliceDb, audit, NullLogger<RecoveryService>.Instance);
                var aliceDaysController = new DaysController(aliceDb, recovery, audit, aliceContext);
                var aliceDashController = new DashboardController(aliceDb, new StreakService(), recovery, audit, aliceContext);

                // Alice initially has 0 completed days
                var initialResult = await aliceDaysController.GetDays(null, null, null);
                var okInit = Assert.IsType<OkObjectResult>(initialResult.Result);
                var aliceInitialDays = Assert.IsType<List<DayPlanDto>>(okInit.Value);
                Assert.Equal(100, aliceInitialDays.Count);
                Assert.Equal(0, aliceInitialDays.Count(d => d.Status == "Completed")); // Alice does NOT see Admin's 23 completed days!

                // Alice completes Day 1
                var aliceDay1 = await aliceDb.DayPlans.FirstAsync(d => d.DayNumber == 1);
                aliceDay1.Status = DayStatus.Completed;
                await aliceDb.SaveChangesAsync();

                // Alice dashboard shows 1 completed day, NOT Admin's 23
                var aliceDashResult = await aliceDashController.GetSummary();
                var okDash = Assert.IsType<OkObjectResult>(aliceDashResult.Result);
                var aliceDashDto = Assert.IsType<DashboardSummaryDto>(okDash.Value);
                Assert.Equal(1, aliceDashDto.CompletedDaysCount);
            }

            // Unauthenticated context: does NOT see Alice's Day 1 completion or Admin's Day 1-23 completion
            var unauthContext = MockUserContext(null);
            using (var unauthDb = new LearningDbContext(options, unauthContext))
            {
                var audit = new AuditService(unauthDb, NullLogger<AuditService>.Instance);
                var recovery = new RecoveryService(unauthDb, audit, NullLogger<RecoveryService>.Instance);
                var unauthDaysController = new DaysController(unauthDb, recovery, audit, unauthContext);
                var unauthDashController = new DashboardController(unauthDb, new StreakService(), recovery, audit, unauthContext);

                var unauthDaysResult = await unauthDaysController.GetDays(null, null, null);
                var okUnauth = Assert.IsType<OkObjectResult>(unauthDaysResult.Result);
                var unauthDays = Assert.IsType<List<DayPlanDto>>(okUnauth.Value);
                Assert.Equal(0, unauthDays.Count(d => d.Status == "Completed"));

                var unauthDashResult = await unauthDashController.GetSummary();
                var okUnauthDash = Assert.IsType<OkObjectResult>(unauthDashResult.Result);
                var unauthDash = Assert.IsType<DashboardSummaryDto>(okUnauthDash.Value);
                Assert.Equal(0, unauthDash.CompletedDaysCount);
            }

            // Admin context: Shatrughna sees his 23 completed days, AND admin monitoring sees both Shatrughna and Alice
            var adminContext = MockUserContext(1, "Admin", "ambhoreshatrughna@gmail.com");
            using (var adminDb = new LearningDbContext(options, adminContext))
            {
                var audit = new AuditService(adminDb, NullLogger<AuditService>.Instance);
                var recovery = new RecoveryService(adminDb, audit, NullLogger<RecoveryService>.Instance);
                var prov = new CurriculumProvisioningService(adminDb, NullLogger<CurriculumProvisioningService>.Instance);
                var adminController = new AdminController(adminDb, prov, adminContext, NullLogger<AdminController>.Instance);

                var learnersResult = await adminController.GetLearners();
                var okLearners = Assert.IsType<OkObjectResult>(learnersResult.Result);
                var learners = Assert.IsType<List<LearnerSummaryDto>>(okLearners.Value);

                var aliceSummary = learners.FirstOrDefault(l => l.Email == "alice@example.com");
                Assert.NotNull(aliceSummary);
                Assert.Equal(1, aliceSummary.CompletedDaysCount);

                var adminSummary = learners.FirstOrDefault(l => l.Email == "ambhoreshatrughna@gmail.com");
                Assert.NotNull(adminSummary);
                Assert.Equal(23, adminSummary.CompletedDaysCount);
            }
        }
        finally
        {
            if (File.Exists(testDbPath))
            {
                try { File.Delete(testDbPath); } catch { }
            }
        }
    }
}
