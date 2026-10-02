namespace LearningOS.Services;

public interface ICurriculumProvisioningService
{
    Task ProvisionCurriculumForUserAsync(int userId, DateOnly startDate);
}
