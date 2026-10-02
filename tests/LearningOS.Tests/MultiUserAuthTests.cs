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
}
