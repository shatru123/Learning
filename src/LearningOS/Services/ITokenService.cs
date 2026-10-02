using LearningOS.Models;

namespace LearningOS.Services;

public interface ITokenService
{
    string GenerateToken(AppUser user);
}
