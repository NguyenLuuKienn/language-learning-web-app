namespace LanguageLearning.Application.Common;

public static class Messages
{
    public static class Notebook
    {
        public const string Empty = "Write a word or a note before saving.";
        public const string TooLong = "Notebook entry exceeds the allowed length.";
        public const string SearchTooLong = "Notebook search may contain at most 200 characters.";
        public const string NotFound = "Notebook entry was not found.";
    }
    public static class VocabularyPractice
    {
        public const string GroupUnavailable = "This lesson does not have three more complete vocabulary items for a learning session.";
        public const string DuplicateMeaning = "Vocabulary meanings in this group must be different for matching practice.";
        public const string SessionNotFound = "Vocabulary learning session was not found.";
        public const string AlreadyComplete = "This vocabulary learning session is already complete.";
        public const string StepMismatch = "The submitted step does not match the current session step.";
    }
    public static class Auth
    {
        public const string EmailInUse = "This email address is already in use.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string AccountDeactivated = "Your account has been deactivated. Please contact support.";
        public const string InvalidRefreshToken = "The refresh token is invalid or has expired.";
        public const string UserNotFound = "User profile was not found.";
        public const string IncorrectPassword = "The current password is incorrect.";
        public const string PasswordChanged = "Password has been successfully changed. All other sessions have been revoked.";
        public const string LogoutSuccess = "Successfully logged out.";
        public const string DailyGoalUpdated = "Daily goal target updated successfully.";
        public const string EmailInvalid = "A valid email address of at most 254 characters is required.";
        public const string DisplayNameInvalid = "Display name must contain 2 to 100 visible characters.";
        public const string PasswordInvalid = "Password must contain 15 to 128 characters and no control characters.";
        public const string TimezoneTooLong = "Timezone is too long.";
        public const string TimezoneUnknown = "Unknown timezone.";
        public const string TimezoneInvalid = "Invalid timezone.";
        public const string InvalidRole = "Invalid user role.";
    }

    public static class Course
    {
        public const string TitleInvalid = "Course title must contain 2 to 200 visible characters.";
        public const string DescriptionInvalid = "Course description must contain at most 2000 characters.";
        public const string LanguageNotFound = "The requested language does not exist.";
        public const string CourseNotFound = "The requested course was not found.";
        public const string UnitNotFound = "The requested unit was not found.";
        public const string LessonNotFound = "The requested lesson was not found.";
        public const string VocabularyNotFound = "The requested vocabulary term was not found.";
        public const string EnrollmentSuccess = "Successfully enrolled in the course.";
        public const string LanguageCodeInUse = "Language code already exists.";
        public const string LanguageInactive = "Language is not active.";
        public const string StudyTargetRequired = "Choose and verify a matching study target first.";
        public const string LevelAndDomainRequired = "Level and at least one domain are required.";
        public const string NoMatchingMaterial = "No published course material matches the selected language, level and domains.";
        public const string PlacementProofRequired = "A passed placement test is required for this language and level.";
        public const string DeckLanguageMismatch = "Deck language must match the verified study target.";
        public const string LessonLanguageMismatch = "Lesson does not belong to the selected language.";
        public const string LanguageAndLessonRequired = "Language and lesson are required.";
        public const string InvalidProgressStatus = "Invalid lesson progress status.";
        public const string CompletionRequiresAssessment = "Lesson completion is recorded after a verified learning activity or passing assessment.";
        public static string RequiredWithMaxLength(string name, int maxLength) => $"{name} is required and must contain at most {maxLength} characters.";
        public static string LengthRange(string name, int maxLength) => $"{name} must contain 1 to {maxLength} characters.";
    }

    public static class Material
    {
        public const string Invalid = "Course material is invalid.";
        public const string ImportRequiresEmptyCourse = "Import is available only before units are added to this course.";
        public const string ObjectRequired = "Course material must be a JSON object.";
        public const string CourseReferenceRequired = "Material must reference the existing course and contain units.";
        public const string UnitsAndPlacementRequired = "Course material must include units and a four-skill placement test.";
        public const string SrsRequired = "Course material must contain an srs configuration.";
        public const string UnsupportedSrs = "Only srs-v1 is supported.";
        public const string DeckRequired = "At least one deck is required.";
        public const string ClassificationMismatch = "Material language, level and domain must match the existing course.";
        public const string UnitLessonsRequired = "Each unit must contain lessons.";
        public const string LessonObjectRequired = "Each lesson must be an object.";
        public const string SkillContentRequired = "Course material must include content for every skill, flashcards and quiz questions.";
        public const string QuizOptionsRequired = "Quiz questions must contain options.";
        public const string QuizCorrectIndexInvalid = "Quiz correctIndex must reference an option.";
        public const string DeckCardsRequired = "Each deck must contain cards.";
        public const string PlacementRequired = "At least one four-skill placement test is required.";
        public const string PlacementLevelMismatch = "Placement test level must match the course.";
        public const string PlacementQuestionsRequired = "Placement tests must contain questions.";
        public const string PlacementSectionInvalid = "Placement question section is invalid.";
        public const string PlacementPointsInvalid = "Placement question points must be between 1 and 100.";
        public const string PlacementMinWordsInvalid = "Placement minWords must be between 1 and 500.";
        public const string PlacementChoicesInvalid = "Listening and reading questions require valid options and correctIndex.";
        public const string PlacementOptionsBlank = "Placement options must not be blank.";
        public const string PlacementSkillsRequired = "Placement tests must cover listening, speaking, reading and writing.";
        public const string UnitLessonIdInvalid = "Unit and lesson IDs must be UUIDs.";
        public static string CardPracticeInvalid(string path) => $"{path} requires a clozeSentence with ___ and 1 to 10 nonblank acceptedAnswers.";
        public static string CourseSectionArray(string section) => $"Course material must contain a {section} array.";
        public static string LessonSectionArray(string section) => $"Each lesson must contain a {section} array.";
        public static string ObjectRequiredAt(string path) => $"{path} must be an object.";
        public static string DuplicateId(string path) => $"{path}.id is duplicated.";
        public static string ArrayRequired(string path) => $"{path} must be an array.";
        public static string ArrayEmpty(string path) => $"{path} must not be empty.";
        public static string ReadingContentInvalid(string path) => $"{path}.content must contain nonblank paragraphs.";
        public static string ReadingVocabularyInvalid(string path) => $"{path} requires word and meaning.";
        public static string QuizRequired(string path) => $"{path}.quiz is required.";
        public static string FieldNotBlank(string path) => $"{path} must not be blank.";
        public static string MinWordsInvalid(string path) => $"{path}.minWords must be between 1 and 500.";
        public static string AnswerInvalid(string path) => $"{path}.options and correctIndex must identify a valid answer.";
        public static string FieldRequired(string name) => $"{name} is required in course material.";
    }

    public static class Deck
    {
        public const string DeckNotFound = "The requested deck was not found.";
        public const string NotDeckOwner = "You do not have permission to modify this deck.";
        public const string InvalidLanguage = "Invalid language ID provided.";
        public const string TitleRequired = "Deck title is required.";
        public const string CannotCloneOwnDeck = "You cannot clone your own deck.";
        public const string NotAccessible = "The requested deck does not exist or is not accessible.";
        public const string TitleEmpty = "Deck title cannot be empty.";
    }

    public static class Card
    {
        public const string CardNotFound = "The requested card was not found.";
        public const string NotCardOwner = "You do not have permission to modify this card.";
        public const string FrontRequired = "Card front content is required.";
        public const string BackRequired = "Card back content is required.";
        public const string BatchSizeInvalid = "Batch must contain 1 to 500 cards.";
        public const string NullBatchItem = "Cards cannot contain null items.";
    }

    public static class Srs
    {
        public const string CardToReviewNotFound = "Card scheduled for review was not found.";
        public const string InvalidRating = "Invalid review rating.";
    }

    public static class Quiz
    {
        public const string QuestionNotFound = "Quiz question was not found.";
        public const string AttemptNotFound = "Quiz attempt was not found.";
    }

    public static class Sync
    {
        public const string OperationAccepted = "Operation accepted.";
        public const string OperationDuplicate = "Duplicate operation ignored.";
        public const string OperationConflict = "Version conflict detected.";
        public const string OperationRejected = "Operation rejected due to invalid payload.";
        public const string InvalidOperation = "Invalid sync operation.";
        public const string UnsupportedOperation = "Unsupported sync operation type.";
        public const string BatchTooLarge = "Sync batch must contain at most 500 operations.";
    }

    public static class Csv
    {
        public const string InvalidFormat = "Invalid CSV file format.";
        public const string EmptyFile = "CSV file is empty.";
        public const string HeaderMissing = "Required CSV headers are missing.";
        public const string FrontEmpty = "Front side cannot be empty.";
        public const string BackEmpty = "Back side cannot be empty.";
    }

    public static class Placement
    {
        public const string SubmissionTooLarge = "Placement submission is too large.";
        public const string SpeakingAudioRequired = "Each speaking answer requires a recorded audio file owned by the learner.";
        public const string WritingAnswerRequired = "Each writing question requires a written answer.";
        public const string AudioQuestionMismatch = "The audio question does not belong to this placement exam.";
        public const string AttemptNotFound = "Placement attempt not found.";
        public const string AlreadyReviewed = "Placement attempt was already reviewed.";
        public const string ScoresInvalid = "Provide a score within the point range for every speaking and writing answer.";
        public const string ExamUnavailable = "Placement exam not available.";
        public const string MaterialInvalid = "Course placement material is invalid.";
        public const string PointsInvalid = "Placement question points must be positive.";
        public const string FourSkillsRequired = "Placement exam must cover four skills.";
    }

    public static class Writing
    {
        public const string PromptIdRequired = "Prompt ID is required.";
        public const string PromptIdInvalid = "A valid prompt ID is required.";
        public const string TextLengthInvalid = "Writing text must contain between 1 and 20000 characters.";
    }

    public static class SkillPractice
    {
        public const string InvalidKind = "Unknown practice skill.";
        public const string InvalidActivity = "Practice activity was not found in this course.";
        public const string InvalidAnswer = "Answer does not match the practice activity.";
    }
}
