namespace LanguageLearning.Application.DTOs.VocabularyPractice;

public sealed class StartVocabularySessionRequest
{
    public Guid LessonId { get; set; }
}

public sealed class VocabularyLessonChoice
{
    public Guid CourseId { get; init; }
    public Guid UnitId { get; init; }
    public Guid LessonId { get; init; }
    public int UnitSortOrder { get; init; }
    public int LessonSortOrder { get; init; }
    public string LessonTitle { get; init; } = string.Empty;
    public string UnitTitle { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public int AvailableGroups { get; init; }
    public int CompletedGroups { get; init; }
    public List<string> NextGroupWords { get; init; } = [];
    public Guid? ActiveSessionId { get; init; }
    public int ActivePosition { get; init; }
    public int ActiveTotalSteps { get; init; }
}

public sealed class VocabularyPracticeAnswerRequest
{
    public Guid StepId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public Dictionary<string, string>? Matches { get; set; }
}

public sealed class VocabularyPracticeStepResponse
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = string.Empty;
    public int Round { get; init; }
    public string Word { get; init; } = string.Empty;
    public string Meaning { get; init; } = string.Empty;
    public List<string> Meanings { get; init; } = [];
    public string Phonetic { get; init; } = string.Empty;
    public string PartOfSpeech { get; init; } = string.Empty;
    public string Example { get; init; } = string.Empty;
    public string ExampleTranslation { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string AudioUrl { get; init; } = string.Empty;
    public List<string> Options { get; init; } = [];
    public List<string> MatchWords { get; init; } = [];
    public List<string> MatchMeanings { get; init; } = [];
}

public sealed class VocabularySessionResponse
{
    public Guid Id { get; init; }
    public Guid LessonId { get; init; }
    public int GroupIndex { get; init; }
    public int Position { get; init; }
    public int TotalSteps { get; init; }
    public int CorrectCount { get; init; }
    public List<string> Words { get; init; } = [];
    public bool Completed { get; init; }
    public bool CanCompleteAfterRoundOne { get; init; }
    public VocabularyPracticeStepResponse? Step { get; init; }
}

public sealed class VocabularyAnswerResponse
{
    public bool Correct { get; init; }
    public string Expected { get; init; } = string.Empty;
    public VocabularySessionResponse Session { get; init; } = new();
}
