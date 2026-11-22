using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using static LanguageLearning.Application.Services.CourseMaterialValidator;

namespace LanguageLearning.Application.Services;

public sealed class CourseMaterialService : ICourseMaterialService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ISpecialPracticeService _specialPractice;
    private readonly ILessonJourneyRepository _journey;

    public CourseMaterialService(ICourseRepository courseRepository, ISpecialPracticeService specialPractice,
        ILessonJourneyRepository journey)
    {
        _courseRepository = courseRepository;
        _specialPractice = specialPractice;
        _journey = journey;
    }

    public async Task<JsonElement?> GetCourseMaterialAsync(Guid courseId, StudyTargetDto? target, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, includeUnitsAndLessons: isAdmin, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        if (!isAdmin && !CourseAccessPolicy.CanAccess(course, target))
            throw new NotFoundException(Messages.Course.CourseNotFound);
        if (string.IsNullOrWhiteSpace(course.MaterialJson))
        {
            if (!isAdmin) return null;
            using var draft = JsonDocument.Parse(BuildDraftMaterial(course).ToJsonString());
            return draft.RootElement.Clone();
        }
        if (!isAdmin)
        {
            var material = JsonNode.Parse(course.MaterialJson) ?? throw new ArgumentException(Messages.Material.Invalid);
            if (material["placementTests"] is JsonArray tests)
                foreach (var test in tests)
                    if (test?["questions"] is JsonArray questions)
                        foreach (var question in questions)
                            if (question is JsonObject item)
                            {
                                item.Remove("correctIndex");
                                item.Remove("expectedResponse");
                                item.Remove("rubric");
                            }
            if (material["certificateExams"] is JsonArray exams)
                foreach (var exam in exams)
                    if (exam?["sections"] is JsonArray sections)
                        foreach (var section in sections)
                            if (section?["questions"] is JsonArray questions)
                                foreach (var question in questions)
                                    if (question is JsonObject item)
                                    {
                                        item.Remove("correctIndex");
                                        item.Remove("rubric");
                                    }
            if (material["interviewScenarios"] is JsonArray interviews)
                foreach (var interview in interviews)
                    if (interview is JsonObject item) item.Remove("rubric");
            using var sanitized = JsonDocument.Parse(material.ToJsonString());
            return sanitized.RootElement.Clone();
        }
        using var document = JsonDocument.Parse(isAdmin ? GetEditableMaterial(course).ToJsonString() : course.MaterialJson);
        return document.RootElement.Clone();
    }

    public async Task ImportCourseMaterialAsync(Guid courseId, Guid adminId, JsonElement material, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, includeUnitsAndLessons: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        if (course.Units.Count > 0)
            throw new InvalidOperationException(Messages.Material.ImportRequiresEmptyCourse);
        if (!string.IsNullOrWhiteSpace(course.MaterialJson))
        {
            var saved = JsonNode.Parse(course.MaterialJson) as JsonObject;
            if (saved != null)
            {
                var incoming = JsonNode.Parse(material.GetRawText()) as JsonObject
                    ?? throw new ArgumentException(Messages.Material.ObjectRequired);
                if (saved["placementTests"] is JsonArray savedTests && savedTests.Count > 0)
                    incoming["placementTests"] = savedTests.DeepClone();
                foreach (var section in new[] { "certificateExams", "interviewScenarios" })
                    if (saved[section] is JsonArray existing && existing.Count > 0 &&
                        incoming[section] is not JsonArray { Count: > 0 })
                        incoming[section] = existing.DeepClone();
                using var merged = JsonDocument.Parse(incoming.ToJsonString());
                material = merged.RootElement.Clone();
            }
        }
        if (material.ValueKind != JsonValueKind.Object ||
            !material.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || id.GetString() != courseId.ToString() ||
            !material.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array)
            throw new ArgumentException(Messages.Material.CourseReferenceRequired);

        foreach (var section in new[] { "decks", "placementTests" })
            if (!material.TryGetProperty(section, out var items) || items.ValueKind != JsonValueKind.Array)
                throw new ArgumentException(Messages.Material.CourseSectionArray(section));
        if (units.GetArrayLength() == 0 || material.GetProperty("placementTests").GetArrayLength() == 0)
            throw new ArgumentException(Messages.Material.UnitsAndPlacementRequired);
        if (!material.TryGetProperty("srs", out var srs) || srs.ValueKind != JsonValueKind.Object)
            throw new ArgumentException(Messages.Material.SrsRequired);
        if (!srs.TryGetProperty("algorithm", out var algorithm) || algorithm.ValueKind != JsonValueKind.String || algorithm.GetString() != "srs-v1")
            throw new ArgumentException(Messages.Material.UnsupportedSrs);
        ValidatePlacementTests(material.GetProperty("placementTests"), course.Level);
        if (material.TryGetProperty("certificateExams", out _) || material.TryGetProperty("interviewScenarios", out _))
        {
            if (!material.TryGetProperty("certificateExams", out var exams) ||
                !material.TryGetProperty("interviewScenarios", out var interviews))
                throw new ArgumentException("certificateExams and interviewScenarios must both be arrays.");
            using var special = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                certificateExams = exams,
                interviewScenarios = interviews
            }));
            _specialPractice.ValidateContent(special.RootElement, course.Level, course.Language.Code);
        }
        if (!material.TryGetProperty("languageCode", out var languageCode) || languageCode.ValueKind != JsonValueKind.String ||
            !string.Equals(languageCode.GetString(), course.Language.Code, StringComparison.OrdinalIgnoreCase) ||
            !material.TryGetProperty("level", out var level) || level.ValueKind != JsonValueKind.String ||
            !string.Equals(level.GetString(), course.Level, StringComparison.OrdinalIgnoreCase) ||
            !material.TryGetProperty("domain", out var domain) || domain.ValueKind != JsonValueKind.String ||
            !string.Equals(domain.GetString(), course.Domain, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(Messages.Material.ClassificationMismatch);

        var requiredSections = new[] { "flashcards", "listeningExercises", "speakingExercises", "readingArticles", "writingPrompts", "grammarTopics", "quizQuestions" };
        var sectionCounts = requiredSections.ToDictionary(section => section, _ => 0);
        foreach (var unit in units.EnumerateArray())
        {
            if (unit.ValueKind != JsonValueKind.Object || !unit.TryGetProperty("lessons", out var lessons) || lessons.ValueKind != JsonValueKind.Array)
                throw new ArgumentException(Messages.Material.UnitLessonsRequired);
            foreach (var lesson in lessons.EnumerateArray())
            {
                if (lesson.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException(Messages.Material.LessonObjectRequired);
                foreach (var section in requiredSections)
                {
                    if (!lesson.TryGetProperty(section, out var items) || items.ValueKind != JsonValueKind.Array)
                        throw new ArgumentException(Messages.Material.LessonSectionArray(section));
                    sectionCounts[section] += items.GetArrayLength();
                }
            }
        }
        if (sectionCounts.Any(section => section.Value == 0))
            throw new ArgumentException(Messages.Material.SkillContentRequired);

        ValidateLessonMaterial(units);

        var importedUnits = new List<Unit>();
        var unitOrder = 0;
        foreach (var unitJson in units.EnumerateArray())
        {
            var unitId = ParseMaterialId(unitJson);
            var unit = new Unit
            {
                Id = unitId,
                CourseId = courseId,
                Title = ReadMaterialText(unitJson, "title"),
                SortOrder = ++unitOrder
            };
            var lessonOrder = 0;
            foreach (var lessonJson in unitJson.GetProperty("lessons").EnumerateArray())
            {
                var lessonId = ParseMaterialId(lessonJson);
                var lesson = new Lesson
                {
                    Id = lessonId,
                    UnitId = unitId,
                    Title = ReadMaterialText(lessonJson, "title"),
                    Content = lessonJson.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String ? summary.GetString() ?? string.Empty : string.Empty,
                    SortOrder = ++lessonOrder,
                    Status = ContentStatus.Published
                };
                foreach (var cardJson in lessonJson.GetProperty("flashcards").EnumerateArray())
                {
                    lesson.Vocabularies.Add(new Vocabulary
                    {
                        Id = Guid.NewGuid(),
                        LessonId = lessonId,
                        LanguageId = course.LanguageId,
                        Term = ReadMaterialText(cardJson, "front"),
                        Meaning = ReadMaterialText(cardJson, "back"),
                        MeaningsJson = ReadCardMeanings(cardJson),
                        PartOfSpeech = ReadOptionalCardText(cardJson, "partOfSpeech"),
                        Phonetic = cardJson.TryGetProperty("phonetic", out var phonetic) ? phonetic.GetString() ?? string.Empty : string.Empty,
                        Example = cardJson.TryGetProperty("example", out var example) ? example.GetString() ?? string.Empty : string.Empty,
                        AudioUrl = ReadOptionalCardText(cardJson, "audioUrl"),
                        ImageUrl = ReadOptionalCardText(cardJson, "imageUrl")
                    });
                }
                foreach (var questionJson in lessonJson.GetProperty("quizQuestions").EnumerateArray())
                {
                    var question = new Question
                    {
                        Id = Guid.NewGuid(),
                        LessonId = lessonId,
                        Prompt = ReadMaterialText(questionJson, "question"),
                        ConfigJson = questionJson.TryGetProperty("id", out var materialQuestionId) && materialQuestionId.ValueKind == JsonValueKind.String
                            ? JsonSerializer.Serialize(new { materialId = materialQuestionId.GetString() }) : "{}",
                        Explanation = questionJson.TryGetProperty("explanation", out var explanation) ? explanation.GetString() ?? string.Empty : string.Empty,
                        Type = QuestionType.MultipleChoice
                    };
                    if (!questionJson.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
                        throw new ArgumentException(Messages.Material.QuizOptionsRequired);
                    var correctIndex = questionJson.TryGetProperty("correctIndex", out var correct) ? correct.GetInt32() : -1;
                    var optionIndex = 0;
                    foreach (var option in options.EnumerateArray())
                    {
                        question.Options.Add(new QuestionOption
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = question.Id,
                            Content = option.GetString() ?? string.Empty,
                            SortOrder = optionIndex,
                            IsCorrect = optionIndex == correctIndex
                        });
                        optionIndex++;
                    }
                    if (correctIndex < 0 || correctIndex >= optionIndex)
                        throw new ArgumentException(Messages.Material.QuizCorrectIndexInvalid);
                    lesson.Questions.Add(question);
                }
                unit.Lessons.Add(lesson);
            }
            importedUnits.Add(unit);
        }
        course.MaterialJson = material.GetRawText();
        // Course vocabulary belongs to lessons. User decks are created explicitly by learners.
        await _courseRepository.SaveCourseMaterialAsync(course, importedUnits, [], cancellationToken);
    }

    public async Task<JsonElement> GetPlacementTestsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        if (string.IsNullOrWhiteSpace(course.MaterialJson))
        {
            using var empty = JsonDocument.Parse("[]");
            return empty.RootElement.Clone();
        }
        using var document = JsonDocument.Parse(course.MaterialJson);
        return document.RootElement.GetProperty("placementTests").Clone();
    }

    public async Task UpdatePlacementTestsAsync(Guid courseId, JsonElement tests, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        ValidatePlacementTests(tests, course.Level);
        var material = string.IsNullOrWhiteSpace(course.MaterialJson)
            ? new JsonObject
            {
                ["id"] = course.Id.ToString(),
                ["languageCode"] = course.Language.Code,
                ["level"] = course.Level,
                ["domain"] = course.Domain,
                ["title"] = course.Title,
                ["description"] = course.Description,
                ["units"] = new JsonArray(),
                ["decks"] = new JsonArray(),
                ["srs"] = new JsonObject { ["algorithm"] = "srs-v1", ["dailyGoal"] = 10 }
            }
            : JsonNode.Parse(course.MaterialJson) as JsonObject
                ?? throw new ArgumentException(Messages.Material.Invalid);
        material["placementTests"] = JsonNode.Parse(tests.GetRawText());
        course.MaterialJson = material.ToJsonString();
        await _courseRepository.UpdateCourseAsync(course, cancellationToken);
    }

    public async Task RemapLessonStepsAsync(Guid courseId, Guid lessonId, RemapLessonStepsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.StepIds.Count == 0 || request.StepIds.Any(item =>
                string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value) ||
                item.Key == item.Value || item.Value.Length > 100))
            throw new ArgumentException("Provide at least one distinct, valid old and new step ID.");
        var course = await _courseRepository.GetCourseByIdAsync(courseId, cancellationToken: cancellationToken)
            ?? throw new BusinessRuleException("COURSE_NOT_FOUND", "Course not found.");
        if (string.IsNullOrWhiteSpace(course.MaterialJson))
            throw new BusinessRuleException("LESSON_MATERIAL_MISSING", "Lesson material is unavailable.");
        using var original = JsonDocument.Parse(course.MaterialJson);
        var oldLesson = original.RootElement.GetProperty("units").EnumerateArray()
            .SelectMany(unit => unit.GetProperty("lessons").EnumerateArray())
            .FirstOrDefault(lesson => lesson.GetProperty("id").GetString() == lessonId.ToString());
        if (oldLesson.ValueKind == JsonValueKind.Undefined ||
            !oldLesson.TryGetProperty("lessonSteps", out var oldSteps) || oldSteps.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Explicit lessonSteps are required for a step ID migration.");
        var oldVersion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(oldLesson.GetRawText())));
        if (!string.Equals(request.ExpectedContentVersion, oldVersion, StringComparison.Ordinal))
            throw new BusinessRuleException("LESSON_CONTENT_CHANGED", "Reload the lesson before migrating its steps.");
        var existingIds = oldSteps.EnumerateArray().Select(step => step.GetProperty("id").GetString()!).ToHashSet();
        if (request.StepIds.Keys.Any(id => !existingIds.Contains(id)) ||
            request.StepIds.Values.Distinct().Count() != request.StepIds.Count ||
            request.StepIds.Values.Any(id => existingIds.Contains(id)))
            throw new ArgumentException("Step mapping must use existing source IDs and unused, unique target IDs.");
        var material = JsonNode.Parse(course.MaterialJson) ?? throw new ArgumentException("Invalid course material.");
        var lessonNode = material["units"]!.AsArray().SelectMany(unit => unit!["lessons"]!.AsArray())
            .First(node => node!["id"]!.GetValue<string>() == lessonId.ToString())!;
        foreach (var step in lessonNode["lessonSteps"]!.AsArray())
        {
            var id = step!["id"]!.GetValue<string>();
            if (request.StepIds.TryGetValue(id, out var target)) step["id"] = target;
        }
        using var updatedDocument = JsonDocument.Parse(material.ToJsonString());
        ValidateLessonMaterial(updatedDocument.RootElement.GetProperty("units"));
        var newVersion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lessonNode.ToJsonString())));
        await _courseRepository.RemapLessonStepsAsync(courseId, lessonId, course.MaterialJson,
            material.ToJsonString(), request.StepIds, newVersion, cancellationToken);
    }

    public async Task UpdateLessonMaterialAsync(Guid courseId, Guid lessonId, JsonElement updatedLesson, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, includeUnitsAndLessons: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        if (!course.Units.SelectMany(unit => unit.Lessons).Any(item => item.Id == lessonId))
            throw new NotFoundException(Messages.Course.LessonNotFound);
        if (updatedLesson.ValueKind != JsonValueKind.Object || ParseMaterialId(updatedLesson) != lessonId)
            throw new ArgumentException(Messages.Material.UnitLessonIdInvalid);

        var material = GetEditableMaterial(course);
        var units = material["units"] as JsonArray ?? throw new ArgumentException(Messages.Material.UnitLessonsRequired);
        JsonArray? lessons = null;
        JsonNode? selectedUnit = null;
        var position = -1;
        foreach (var unit in units)
        {
            if (unit?["lessons"] is not JsonArray candidates) continue;
            for (var index = 0; index < candidates.Count; index++)
            {
                if (string.Equals(candidates[index]?["id"]?.GetValue<string>(), lessonId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    lessons = candidates;
                    selectedUnit = unit;
                    position = index;
                    break;
                }
            }
            if (position >= 0) break;
        }
        if (lessons == null) throw new NotFoundException(Messages.Course.LessonNotFound);
        var previousLesson = lessons[position]?.DeepClone();
        var oldQuiz = (lessons[position]?["quizQuestions"] as JsonArray)?
            .Where(item => item?["id"] != null)
            .ToDictionary(item => item!["id"]!.GetValue<string>(), item => item?["question"]?.GetValue<string>() ?? string.Empty)
            ?? new Dictionary<string, string>();
        var oldVocabulary = (lessons[position]?["flashcards"] as JsonArray)?
            .Select((item, index) => new { Item = item, Key = item?["id"]?.GetValue<string>() ?? $"#index:{index}" })
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key,
                group => (Term: group.First().Item?["front"]?.GetValue<string>() ?? string.Empty,
                    Meaning: group.First().Item?["back"]?.GetValue<string>() ?? string.Empty))
            ?? new Dictionary<string, (string Term, string Meaning)>();
        lessons[position] = JsonNode.Parse(updatedLesson.GetRawText());
        var validationUnit = new JsonObject
        {
            ["id"] = selectedUnit?["id"]?.GetValue<string>(),
            ["title"] = selectedUnit?["title"]?.GetValue<string>(),
            ["lessons"] = new JsonArray(JsonNode.Parse(updatedLesson.GetRawText()))
        };
        using var validated = JsonDocument.Parse(new JsonArray(validationUnit).ToJsonString());
        ValidateLessonMaterial(validated.RootElement);
        ValidateChangedSkillSections(previousLesson, updatedLesson);
        if (await _journey.HasLearnerStateAsync(lessonId, cancellationToken))
        {
            using var previousDocument = JsonDocument.Parse(previousLesson?.ToJsonString() ?? "{}");
            if (!StepIdentity(previousDocument.RootElement).SetEquals(StepIdentity(updatedLesson)))
                throw new BusinessRuleException("LESSON_STEP_IDS_IN_USE",
                    "Learners have progress in this lesson. Keep existing step IDs and types when editing its content.");
            foreach (var section in new[] { "flashcards", "grammarTopics", "listeningExercises", "speakingExercises",
                         "readingArticles", "writingPrompts", "quizQuestions" })
            {
                var previousSection = previousLesson?[section];
                var updatedSection = JsonNode.Parse(updatedLesson.GetProperty(section).GetRawText());
                if (!JsonNode.DeepEquals(previousSection, updatedSection))
                    throw new BusinessRuleException("LESSON_ACTIVITY_IN_USE",
                        $"Learners have progress in this lesson. {section} cannot change because prior answers would no longer match the lesson. Create a new lesson for changed activities.");
            }
            if (!StepCompletionContract(previousDocument.RootElement).SequenceEqual(StepCompletionContract(updatedLesson)))
                throw new BusinessRuleException("LESSON_STEP_POLICY_IN_USE",
                    "Learners have progress in this lesson. Keep each step's required flag, completion policy and activity references unchanged.");
        }

        var materialCards = updatedLesson.GetProperty("flashcards").EnumerateArray().ToArray();
        var vocabularyMaterialIds = materialCards.Select((card, index) =>
            card.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
                ? id.GetString() ?? $"#index:{index}" : $"#index:{index}").ToArray();
        var vocabularies = materialCards.Select(card => new Vocabulary
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            LanguageId = course.LanguageId,
            Term = ReadMaterialText(card, "front"),
            Meaning = ReadMaterialText(card, "back"),
            MeaningsJson = ReadCardMeanings(card),
            PartOfSpeech = ReadOptionalCardText(card, "partOfSpeech"),
            Phonetic = card.TryGetProperty("phonetic", out var phonetic) && phonetic.ValueKind == JsonValueKind.String ? phonetic.GetString() ?? string.Empty : string.Empty,
            Example = card.TryGetProperty("example", out var example) && example.ValueKind == JsonValueKind.String ? example.GetString() ?? string.Empty : string.Empty,
            AudioUrl = ReadOptionalCardText(card, "audioUrl"),
            ImageUrl = ReadOptionalCardText(card, "imageUrl")
        }).ToList();
        var questions = new List<Question>();
        foreach (var item in updatedLesson.GetProperty("quizQuestions").EnumerateArray())
        {
            var question = new Question
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                Prompt = ReadMaterialText(item, "question"),
                ConfigJson = item.TryGetProperty("id", out var materialQuestionId) && materialQuestionId.ValueKind == JsonValueKind.String
                    ? JsonSerializer.Serialize(new { materialId = materialQuestionId.GetString() }) : "{}",
                Explanation = item.TryGetProperty("explanation", out var explanation) && explanation.ValueKind == JsonValueKind.String ? explanation.GetString() ?? string.Empty : string.Empty,
                Type = QuestionType.MultipleChoice
            };
            var correctIndex = item.GetProperty("correctIndex").GetInt32();
            foreach (var (option, index) in item.GetProperty("options").EnumerateArray().Select((value, index) => (value, index)))
                question.Options.Add(new QuestionOption
                {
                    Id = Guid.NewGuid(), QuestionId = question.Id, Content = option.GetString()!, SortOrder = index, IsCorrect = index == correctIndex
                });
            questions.Add(question);
        }
        course.MaterialJson = material.ToJsonString();
        await _courseRepository.SaveEditedLessonMaterialAsync(course, lessonId, vocabularies, vocabularyMaterialIds, questions, oldVocabulary, oldQuiz, cancellationToken);
    }

    private static HashSet<(string Id, string Type)> StepIdentity(JsonElement lesson)
    {
        if (lesson.TryGetProperty("lessonSteps", out var configured) && configured.ValueKind == JsonValueKind.Array)
            return configured.EnumerateArray().Select(step =>
                (step.GetProperty("id").GetString() ?? string.Empty, step.GetProperty("type").GetString() ?? string.Empty)).ToHashSet();
        var identity = new HashSet<(string Id, string Type)> { ("introduction", "introduction") };
        foreach (var (type, section) in new[]
        {
            ("vocabulary", "flashcards"), ("grammar", "grammarTopics"), ("listening", "listeningExercises"),
            ("speaking", "speakingExercises"), ("reading", "readingArticles"), ("writing", "writingPrompts"),
            ("quiz", "quizQuestions")
        })
            if (lesson.TryGetProperty(section, out var entries) && entries.ValueKind == JsonValueKind.Array &&
                entries.EnumerateArray().Any(item => item.TryGetProperty("id", out var id) &&
                    id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString())))
                identity.Add((type, type));
        return identity;
    }

    private static string[] StepCompletionContract(JsonElement lesson)
    {
        if (!lesson.TryGetProperty("lessonSteps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            return [];
        return steps.EnumerateArray().Select(step =>
        {
            var id = step.GetProperty("id").GetString() ?? string.Empty;
            var required = !step.TryGetProperty("required", out var flag) || flag.ValueKind != JsonValueKind.False;
            var policy = step.TryGetProperty("completionPolicy", out var completion) &&
                completion.ValueKind == JsonValueKind.Object && completion.TryGetProperty("kind", out var kind) &&
                kind.ValueKind == JsonValueKind.String ? kind.GetString() ?? string.Empty : string.Empty;
            var refs = step.TryGetProperty("activityRefs", out var activities) && activities.ValueKind == JsonValueKind.Array
                ? activities.EnumerateArray().Select(item => item.GetString() ?? string.Empty).OrderBy(item => item).ToArray() : [];
            return JsonSerializer.Serialize(new { id, required, policy, refs });
        }).OrderBy(item => item).ToArray();
    }

    private static JsonObject BuildDraftMaterial(Course course)
    {
        return new JsonObject
        {
            ["id"] = course.Id.ToString(),
            ["languageCode"] = course.Language.Code,
            ["level"] = course.Level,
            ["domain"] = course.Domain,
            ["title"] = course.Title,
            ["description"] = course.Description,
            ["units"] = new JsonArray(course.Units.OrderBy(unit => unit.SortOrder).Select(unit => (JsonNode?)new JsonObject
            {
                ["id"] = unit.Id.ToString(),
                ["title"] = unit.Title,
                ["order"] = unit.SortOrder,
                ["lessons"] = new JsonArray(unit.Lessons.OrderBy(lesson => lesson.SortOrder).Select(lesson => (JsonNode?)new JsonObject
                {
                    ["id"] = lesson.Id.ToString(),
                    ["title"] = lesson.Title,
                    ["order"] = lesson.SortOrder,
                    ["summary"] = lesson.Content,
                    ["flashcards"] = new JsonArray(lesson.Vocabularies.Select(word => (JsonNode?)new JsonObject
                    {
                        ["id"] = word.Id.ToString(),
                        ["front"] = word.Term,
                        ["back"] = word.Meaning,
                        ["meanings"] = JsonNode.Parse(word.MeaningsJson),
                        ["partOfSpeech"] = word.PartOfSpeech,
                        ["phonetic"] = word.Phonetic,
                        ["example"] = word.Example,
                        ["audioUrl"] = word.AudioUrl,
                        ["imageUrl"] = word.ImageUrl
                    }).ToArray()),
                    ["listeningExercises"] = new JsonArray(),
                    ["speakingExercises"] = new JsonArray(),
                    ["readingArticles"] = new JsonArray(),
                    ["writingPrompts"] = new JsonArray(),
                    ["grammarTopics"] = new JsonArray(),
                    ["quizQuestions"] = new JsonArray()
                }).ToArray())
            }).ToArray()),
            ["decks"] = new JsonArray(),
            ["srs"] = new JsonObject { ["algorithm"] = "srs-v1", ["dailyGoal"] = 10 },
            ["placementTests"] = new JsonArray()
        };
    }

    private static JsonObject GetEditableMaterial(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.MaterialJson)) return BuildDraftMaterial(course);
        var material = JsonNode.Parse(course.MaterialJson) as JsonObject
            ?? throw new ArgumentException(Messages.Material.Invalid);
        var savedUnits = material["units"] as JsonArray ?? throw new ArgumentException(Messages.Material.UnitLessonsRequired);
        var draftUnits = (JsonArray)BuildDraftMaterial(course)["units"]!;
        foreach (var draftUnit in draftUnits)
        {
            var unitId = draftUnit?["id"]?.GetValue<string>();
            var savedUnit = savedUnits.FirstOrDefault(unit => unit?["id"]?.GetValue<string>() == unitId);
            if (savedUnit == null)
            {
                savedUnits.Add(draftUnit?.DeepClone());
                continue;
            }
            var savedLessons = savedUnit["lessons"] as JsonArray ?? throw new ArgumentException(Messages.Material.UnitLessonsRequired);
            foreach (var draftLesson in (JsonArray)draftUnit!["lessons"]!)
                if (!savedLessons.Any(lesson => lesson?["id"]?.GetValue<string>() == draftLesson?["id"]?.GetValue<string>()))
                    savedLessons.Add(draftLesson?.DeepClone());
        }
        return material;
    }

    private static string ReadOptionalCardText(JsonElement card, string property) =>
        card.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static string ReadCardMeanings(JsonElement card)
    {
        if (!card.TryGetProperty("meanings", out var meanings) || meanings.ValueKind != JsonValueKind.Array)
            return JsonSerializer.Serialize(new[] { ReadMaterialText(card, "back") });
        return JsonSerializer.Serialize(meanings.EnumerateArray().Select(value => value.GetString()!.Trim()));
    }

}
