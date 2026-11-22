namespace LanguageLearning.Application.Interfaces;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();
    Guid? GetUserId();
    bool IsAdmin();
}
