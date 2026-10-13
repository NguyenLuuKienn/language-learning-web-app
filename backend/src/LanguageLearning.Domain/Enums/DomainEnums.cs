namespace LanguageLearning.Domain.Enums;

public enum UserRole
{
    Learner = 1,
    Admin = 2
}

public enum ContentStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum SrsRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4
}

public enum QuestionType
{
    MultipleChoice = 1,
    FillInBlank = 2,
    Matching = 3
}
