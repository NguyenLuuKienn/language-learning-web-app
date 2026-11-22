namespace LanguageLearning.Application.Interfaces;

public interface IPlacementProofService
{
    string Create(Guid userId, Guid courseId);
    bool IsValid(string? proof, Guid userId, Guid courseId);
}
