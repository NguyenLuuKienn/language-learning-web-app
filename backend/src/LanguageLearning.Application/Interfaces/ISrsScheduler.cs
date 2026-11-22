using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Interfaces;

public interface ISrsScheduler
{
    string Version { get; }
    SrsState ScheduleAfterLearning(bool correct, DateTime learnedAtUtc);
    SrsState CalculateNext(SrsState currentState, SrsRating rating, DateTime reviewedAtUtc);
}
