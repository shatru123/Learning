using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly ICurriculumProvisioningService _provisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LearningDbContext db,
        IPasswordHasher hasher,
        ITokenService tokenService,
        ICurriculumProvisioningService provisioningService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
        _provisioningService = provisioningService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { message = "Full name, email, and password are required." });

        if (req.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (exists)
            return BadRequest(new { message = "An account with this email already exists." });

        var username = normalizedEmail.Split('@')[0].Replace(".", "").Replace("+", "");
        int suffix = 1;
        var baseUsername = username;
        while (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            username = $"{baseUsername}{suffix++}";
        }

        string status = "PendingApproval";
        string? inviteCodeUsed = null;

        if (!string.IsNullOrWhiteSpace(req.InviteCode))
        {
            var codeUpper = req.InviteCode.Trim().ToUpperInvariant();
            var invite = await _db.InviteCodes.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Code.ToUpper() == codeUpper && i.IsActive);
            if (invite == null || (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow) || (invite.MaxUses > 0 && invite.UsedCount >= invite.MaxUses))
            {
                return BadRequest(new { message = "Invalid or expired invite code. You can register without an invite code for Admin review." });
            }

            invite.UsedCount++;
            inviteCodeUsed = invite.Code;
            status = "Active"; // Instant activation with valid invite!
        }

        var startDate = req.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var user = new AppUser
        {
            FullName = req.FullName.Trim(),
            Email = normalizedEmail,
            Username = username,
            PasswordHash = _hasher.HashPassword(req.Password),
            Role = "Learner",
            Status = status,
            RequestedStartDate = startDate,
            InviteCodeUsed = inviteCodeUsed,
            CreatedAt = DateTime.UtcNow,
            ApprovedAt = status == "Active" ? DateTime.UtcNow : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (status == "Active")
        {
            // Provision their 100 days starting from their chosen date
            await _provisioningService.ProvisionCurriculumForUserAsync(user.Id, startDate);
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = MapUser(user),
                Message = "Registration successful! Your 100-day engineering curriculum is ready."
            });
        }

        return Ok(new AuthResponseDto
        {
            Token = string.Empty,
            User = MapUser(user),
            Message = "Registration received! Your account is currently pending Admin approval (Shatrughna Ambhore). You will gain access once approved."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail || u.Username.ToLower() == normalizedEmail);

        if (user == null || !_hasher.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (user.Status == "PendingApproval")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your registration is currently awaiting Admin approval. You will receive access once approved by Shatrughna Ambhore.",
                status = "PendingApproval"
            });
        }

        if (user.Status == "Rejected")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your registration request was declined.",
                status = "Rejected"
            });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            User = MapUser(user),
            Message = $"Welcome back, {user.FullName}!"
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null) return NotFound();

        return Ok(MapUser(user));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto req)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "Current password and new password are required." });

        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters long." });

        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest(new { message = "New password and confirmation do not match." });

        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null) return NotFound(new { message = "User not found." });

        if (!_hasher.VerifyPassword(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (_hasher.VerifyPassword(req.NewPassword, user.PasswordHash))
            return BadRequest(new { message = "New password cannot be the same as your current password." });

        user.PasswordHash = _hasher.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password successfully updated for user {UserId} ({Email})", user.Id, user.Email);

        return Ok(new { message = "Password updated successfully!" });
    }

    private static UserProfileDto MapUser(AppUser u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        Username = u.Username,
        FullName = u.FullName,
        Role = u.Role,
        Status = u.Status,
        RequestedStartDate = u.RequestedStartDate,
        CreatedAt = u.CreatedAt
    };
}
