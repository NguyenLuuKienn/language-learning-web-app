namespace LanguageLearning.Application.DTOs.Progress;

public sealed record SpecialPracticeCatalogItem(
    Guid CourseId, Guid ContentId, string Kind, string Title, string LanguageCode,
    string Level, string Domain, string Certificate, string Profession, int QuestionCount,
    Guid? ActiveAttemptId, Guid? LastAttemptId, string? LastStatus, bool Archived = false);

public sealed record SpecialPracticeQuestion(
    Guid Id, string Skill, string Prompt, string Passage, string AudioScript,
    string AudioUrl, IReadOnlyList<string> Options, string Rubric, int Points);

public sealed record SpecialPracticeAnswer(int? SelectedIndex, string? Text, string? AudioUrl = null);

public sealed record SpecialPracticeAttemptResponse(
    Guid Id, Guid CourseId, Guid ContentId, string Kind, string Title,
    string Certificate, string Profession, string Status,
    IReadOnlyList<SpecialPracticeQuestion> Questions,
    IReadOnlyDictionary<Guid, SpecialPracticeAnswer> Answers,
    int ObjectiveScore, int ObjectiveMaxScore, int SubjectiveScore, int SubjectiveMaxScore,
    string Feedback, DateTime? SubmittedAt, DateTime? ReviewedAt, DateTime? DeadlineAt);

public sealed record SpecialPracticeReviewItem(Guid AttemptId, Guid UserId, string Kind, string Title, DateTime SubmittedAt);
public sealed record SpecialPracticeReviewDetail(
    Guid AttemptId, string Kind, string Title, string Rubric,
    IReadOnlyList<SpecialPracticeQuestion> Questions,
    IReadOnlyDictionary<Guid, SpecialPracticeAnswer> Answers,
    int ObjectiveScore, int ObjectiveMaxScore);
public sealed record GradeSpecialPracticeRequest(IReadOnlyDictionary<Guid, int> Scores, string Feedback);

public sealed record StartSpecialPracticeRequest(Guid CourseId, Guid ContentId, string Kind);
public sealed record SaveSpecialPracticeAnswerRequest(Guid QuestionId, int? SelectedIndex, string? Text);
