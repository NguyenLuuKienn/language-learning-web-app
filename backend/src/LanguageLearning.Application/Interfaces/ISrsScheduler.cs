
using LanguageLearning.Application.Srs;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Interfaces;

public interface ISrsScheduler
{
    string AlgorithmVersion { get; }

    (SrsState UpdatedState, DateTime DueAt) Schedule(
        SrsState currentState,
        SrsRating rating,
        DateTime reviewedAtUtc
    );
}
