using System.Text.Json.Serialization;

namespace LanguageLearning.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Learner = 1,
    Admin = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SrsRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestionType
{
    MultipleChoice = 1,
    FillInBlank = 2,
    Matching = 3
}
