namespace LanguageLearning.Application.Interfaces;

public interface IAccountStatus
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken ct);
}
