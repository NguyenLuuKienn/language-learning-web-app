using System.Text.Json;
using System.Text.RegularExpressions;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.VocabularyPractice;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public sealed class VocabularySessionService : IVocabularySessionService
{
    private readonly IVocabularySessionRepository _sessions;
    private readonly ICourseRepository _courses;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;
    private readonly ISrsScheduler _scheduler;
    private readonly IQuizRepository _quizzes;
    private readonly ILessonJourneyService _journey;

    public VocabularySessionService(
        IVocabularySessionRepository sessions, ICourseRepository courses,
        IStudyAccessService access, ICurrentUserService currentUser, ISrsScheduler scheduler,
        IQuizRepository quizzes, ILessonJourneyService journey)
    {
        _sessions = sessions;
        _courses = courses;
        _access = access;
        _currentUser = currentUser;
        _scheduler = scheduler;
        _quizzes = quizzes;
        _journey = journey;
    }

    public async Task<List<VocabularyLessonChoice>> GetAvailableAsync(Guid userId, CancellationToken cancellationToken)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return [];
        var domains = target?.Domains is { Count: > 0 } ? target.Domains : target == null ? null : [target.Domain];
        var courses = await _sessions.GetCandidateCoursesAsync(target?.LanguageId, target?.Level, domains, cancellationToken);
        var candidates = courses.SelectMany(course => {
            var materialCards = ReadMaterialCardsByLesson(course.MaterialJson);
            return course.Units.OrderBy(unit => unit.SortOrder)
            .SelectMany(unit => unit.Lessons.Where(lesson => lesson.Status == ContentStatus.Published)
                .OrderBy(lesson => lesson.SortOrder)
                .Select(lesson => new
                {
                    Lesson = lesson, Unit = unit, Course = course,
                    Cards = GetPracticeCards(materialCards.GetValueOrDefault(lesson.Id) ?? [], lesson)
                }));
        })
            .Where(item => item.Cards.Count > 0).ToList();
        var lessonIds = candidates.Select(item => item.Lesson.Id).ToArray();
        var completedSessions = await _sessions.GetCompletedByLessonsAsync(userId, lessonIds, cancellationToken) ?? [];
        var learnedByLesson = completedSessions.GroupBy(session => session.LessonId)
            .ToDictionary(group => group.Key, group => GetIntroducedWords(group));
        var activeSessions = await _sessions.GetActiveByLessonAsync(userId, lessonIds, cancellationToken);
        return candidates.Select(item =>
        {
            var learned = learnedByLesson.GetValueOrDefault(item.Lesson.Id) ?? new HashSet<string>();
            var remaining = item.Cards.Where(card => !learned.Contains(Normalize(card.Word))).ToList();
            return new VocabularyLessonChoice
            {
            CourseId = item.Course.Id, UnitId = item.Unit.Id,
            UnitSortOrder = item.Unit.SortOrder, LessonSortOrder = item.Lesson.SortOrder,
            LessonId = item.Lesson.Id, LessonTitle = item.Lesson.Title,
            UnitTitle = item.Unit.Title, CourseTitle = item.Course.Title,
            AvailableGroups = 1,
            CompletedGroups = remaining.Count == 0 ? 1 : 0,
            NextGroupWords = remaining.Take(3).Select(card => card.Word).ToList(),
            ActiveSessionId = activeSessions.GetValueOrDefault(item.Lesson.Id)?.Id,
            ActivePosition = activeSessions.GetValueOrDefault(item.Lesson.Id)?.Position ?? 0,
            ActiveTotalSteps = activeSessions.GetValueOrDefault(item.Lesson.Id) is { } active
                ? (JsonSerializer.Deserialize<List<PracticeStep>>(active.StepsJson)?.Count ?? 0) : 0
            };
        }).OrderBy(item => item.CourseTitle).ThenBy(item => item.UnitSortOrder)
          .ThenBy(item => item.LessonSortOrder).ToList();
    }

    public async Task<VocabularySessionResponse> StartAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken)
    {
        if (lessonId == Guid.Empty || (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, lessonId, cancellationToken)))
            throw new NotFoundException(Messages.Course.LessonNotFound);
        var active = await _sessions.GetActiveAsync(userId, lessonId, cancellationToken);
        if (active != null) return Map(active);

        var lesson = await _courses.GetLessonByIdAsync(lessonId, includeVocabularies: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.LessonNotFound);
        if (lesson.Status != ContentStatus.Published)
            throw new NotFoundException(Messages.Course.LessonNotFound);
        var course = await _courses.GetCourseByIdAsync(lesson.Unit.CourseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        var words = GetPracticeCards(course.MaterialJson, lesson);
        var completedSessions = await _sessions.GetCompletedByLessonsAsync(userId, [lessonId], cancellationToken) ?? [];
        var learned = GetIntroducedWords(completedSessions);
        var remaining = words.Where(card => !learned.Contains(Normalize(card.Word))).ToList();
        if (remaining.Count == 0)
            throw new BusinessRuleException("VOCABULARY_GROUP_UNAVAILABLE", Messages.VocabularyPractice.GroupUnavailable);

        var session = new VocabularySession
        {
            Id = Guid.NewGuid(), UserId = userId, LessonId = lessonId, GroupIndex = 0,
            StepsJson = JsonSerializer.Serialize(BuildSteps(remaining, words)), ResultsJson = "[]"
        };
        await _sessions.SaveAsync(session, cancellationToken);
        return Map(session);
    }

    public async Task<VocabularySessionResponse> GetAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(userId, sessionId, cancellationToken)
            ?? throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, session.LessonId, cancellationToken))
            throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        return Map(session);
    }

    public async Task<VocabularyAnswerResponse> AnswerAsync(Guid userId, Guid sessionId, VocabularyPracticeAnswerRequest request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(userId, sessionId, cancellationToken)
            ?? throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, session.LessonId, cancellationToken))
            throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        var steps = JsonSerializer.Deserialize<List<PracticeStep>>(session.StepsJson) ?? [];
        var results = JsonSerializer.Deserialize<List<PracticeResult>>(session.ResultsJson) ?? [];
        var previous = results.FirstOrDefault(result => result.StepId == request.StepId);
        if (previous != null)
        {
            var answeredStep = steps.First(item => item.Id == request.StepId);
            return new VocabularyAnswerResponse { Correct = previous.Correct, Expected = Expected(answeredStep), Session = Map(session) };
        }
        if (session.CompletedAt != null || session.Position >= steps.Count)
            throw new BusinessRuleException("VOCABULARY_SESSION_COMPLETE", Messages.VocabularyPractice.AlreadyComplete);
        var step = steps[session.Position];
        if (step.Id != request.StepId)
            throw new BusinessRuleException("VOCABULARY_STEP_MISMATCH", Messages.VocabularyPractice.StepMismatch);

        var correct = Grade(step, request);
        results.Add(new PracticeResult(step.Id, correct));
        session.Position++;
        if (session.Position < steps.Count && steps[session.Position].Round == 2 && step.Round == 1)
        {
            var missed = steps.Where(item => item.Round == 1 &&
                results.Any(result => result.StepId == item.Id && !result.Correct)).ToList();
            if (missed.Count > 0)
            {
                // Round two revisits only the missed exercise types and words.
                steps = steps.Take(session.Position).Concat(steps.Skip(session.Position)
                    .Where(item => missed.Any(failed => failed.Kind == item.Kind &&
                        (failed.Kind == "matching"
                            ? failed.Pairs.Keys.Any(word => item.Pairs.ContainsKey(word))
                            : Normalize(failed.Word) == Normalize(item.Word))))).ToList();
                session.StepsJson = JsonSerializer.Serialize(steps);
            }
        }
        session.ResultsJson = JsonSerializer.Serialize(results);
        if (session.Position == steps.Count)
            await CompleteSessionAsync(session, steps, results, cancellationToken);
        else await _sessions.SaveAsync(session, cancellationToken);
        return new VocabularyAnswerResponse
        {
            Correct = correct,
            Expected = Expected(step),
            Session = Map(session)
        };
    }

    public async Task<VocabularySessionResponse> CompleteAfterPerfectRoundAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(userId, sessionId, cancellationToken)
            ?? throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(userId, session.LessonId, cancellationToken))
            throw new NotFoundException(Messages.VocabularyPractice.SessionNotFound);
        var steps = JsonSerializer.Deserialize<List<PracticeStep>>(session.StepsJson) ?? [];
        var results = JsonSerializer.Deserialize<List<PracticeResult>>(session.ResultsJson) ?? [];
        if (!CanCompleteAfterRoundOne(session, steps, results))
            throw new BusinessRuleException("VOCABULARY_ROUND_NOT_PERFECT", "Complete every exercise in round one correctly before skipping round two.");
        session.Position = steps.Count;
        await CompleteSessionAsync(session, steps, results, cancellationToken);
        return Map(session);
    }

    private async Task CompleteSessionAsync(VocabularySession session, List<PracticeStep> steps,
        List<PracticeResult> results, CancellationToken cancellationToken)
    {
        session.CompletedAt = DateTime.UtcNow;
        var lesson = await _courses.GetLessonByIdAsync(session.LessonId, includeVocabularies: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.LessonNotFound);
        var course = await _courses.GetCourseByIdAsync(lesson.Unit.CourseId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(Messages.Course.CourseNotFound);
        var original = steps.Where(item => item.Kind == "introduction").ToList();
        var seeds = original.Select(item =>
        {
            var related = steps.Where(stepItem => stepItem.Round is 1 or 2 &&
                (Normalize(stepItem.Word) == Normalize(item.Word) ||
                 stepItem.Kind == "matching" && stepItem.Pairs.ContainsKey(item.Word))).ToList();
            var missed = related.Any(relatedStep => results.Any(result => result.StepId == relatedStep.Id && !result.Correct));
            var state = _scheduler.ScheduleAfterLearning(!missed, session.CompletedAt.Value);
            return new VocabularyReviewSeed(item.Word, item.Meaning, JsonSerializer.Serialize(item.Meanings),
                item.Phonetic, item.PartOfSpeech, item.Example, item.AudioUrl,
                state.ToJson(), state.DueAt, _scheduler.Version);
        }).ToList();
        var completedSessions = await _sessions.GetCompletedByLessonsAsync(session.UserId, [session.LessonId], cancellationToken) ?? [];
        var learned = GetIntroducedWords(completedSessions);
        learned.UnionWith(original.Select(item => Normalize(item.Word)));
        await _sessions.CompleteAsync(session, course.LanguageId, course.Level, lesson.Title, seeds, false, cancellationToken);
        // Legacy vocabulary-only courses have no material to map into lesson steps.
        if (!string.IsNullOrWhiteSpace(course.MaterialJson))
            await _journey.RefreshAsync(session.UserId, session.LessonId, cancellationToken);
    }

    private static HashSet<string> GetIntroducedWords(IEnumerable<VocabularySession> sessions)
    {
        var words = new HashSet<string>();
        foreach (var session in sessions)
        {
            List<PracticeStep> steps;
            try { steps = JsonSerializer.Deserialize<List<PracticeStep>>(session.StepsJson) ?? []; }
            catch (JsonException) { continue; }
            foreach (var step in steps.Where(item => item.Kind == "introduction"))
                words.Add(Normalize(step.Word));
        }
        return words;
    }

    private static bool CanCompleteAfterRoundOne(VocabularySession session, List<PracticeStep> steps,
        List<PracticeResult> results)
    {
        var firstRound = steps.Where(item => item.Round == 1).ToList();
        return session.CompletedAt == null && firstRound.Count > 0 &&
            session.Position == steps.FindIndex(item => item.Round == 2) &&
            firstRound.All(step => results.Any(result => result.StepId == step.Id && result.Correct));
    }

    private static string Expected(PracticeStep step) => step.Kind == "introduction" ? string.Empty :
        step.Kind == "matching" ? string.Join("; ", step.Pairs.Select(pair => pair.Key + " → " + pair.Value)) :
        step.AcceptedAnswers.FirstOrDefault() ?? string.Empty;

    private static bool Grade(PracticeStep step, VocabularyPracticeAnswerRequest request)
    {
        if (step.Kind == "introduction") return true;
        if (step.Kind == "matching")
            return request.Matches != null && request.Matches.Count == step.Pairs.Count &&
                step.Pairs.All(pair => request.Matches.TryGetValue(pair.Key, out var meaning) && Normalize(meaning) == Normalize(pair.Value));
        return step.AcceptedAnswers.Any(expected => Normalize(expected) == Normalize(request.Answer));
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().Normalize().ToLowerInvariant();

    private static VocabularySessionResponse Map(VocabularySession session)
    {
        var steps = JsonSerializer.Deserialize<List<PracticeStep>>(session.StepsJson) ?? [];
        var results = JsonSerializer.Deserialize<List<PracticeResult>>(session.ResultsJson) ?? [];
        var step = session.Position < steps.Count ? steps[session.Position] : null;
        var clue = step?.Kind == "cloze"
            ? steps.FirstOrDefault(item => item.Kind == "introduction" && Normalize(item.Word) == Normalize(step.Word))
            : null;
        return new VocabularySessionResponse
        {
            Id = session.Id, LessonId = session.LessonId, GroupIndex = session.GroupIndex,
            Position = session.Position, TotalSteps = steps.Count,
            CorrectCount = results.Count(result => result.Correct && steps.Any(item => item.Id == result.StepId && item.Kind != "introduction")),
            Words = steps.Where(item => item.Kind == "introduction").Select(item => item.Word).ToList(),
            Completed = session.CompletedAt != null,
            CanCompleteAfterRoundOne = CanCompleteAfterRoundOne(session, steps, results),
            Step = step == null ? null : new VocabularyPracticeStepResponse
            {
                Id = step.Id, Kind = step.Kind, Round = step.Round,
                Word = step.Kind == "cloze" ? string.Empty : step.Word,
                Meaning = step.Kind == "cloze" ? clue?.Meaning ?? step.Meaning : step.Meaning,
                Meanings = step.Kind == "introduction" ? step.Meanings : [],
                Phonetic = step.Kind == "introduction" ? step.Phonetic : string.Empty,
                PartOfSpeech = step.Kind is "introduction" or "cloze" ? clue?.PartOfSpeech ?? step.PartOfSpeech : string.Empty,
                Example = step.Kind == "introduction" ? step.Example : string.Empty,
                ExampleTranslation = step.Kind == "cloze" ? clue?.ExampleTranslation ?? step.ExampleTranslation : step.Kind == "introduction" ? step.ExampleTranslation : string.Empty,
                Prompt = step.Prompt, AudioUrl = step.AudioUrl,
                Options = step.Options, MatchWords = step.Pairs.Keys.ToList(),
                MatchMeanings = step.MatchMeanings ?? []
            }
        };
    }

    private static List<PracticeCard> GetPracticeCards(string? materialJson, Lesson lesson)
    {
        return GetPracticeCards(ReadPracticeCards(materialJson, lesson.Id), lesson);
    }

    private static List<PracticeCard> GetPracticeCards(IReadOnlyList<PracticeCard> materialCards, Lesson lesson)
    {
        var fromMaterial = materialCards.ToList();
        var known = fromMaterial.Select(card => Normalize(card.Word)).ToHashSet();
        foreach (var vocabulary in lesson.Vocabularies.OrderBy(item => item.CreatedAt))
        {
            if (known.Contains(Normalize(vocabulary.Term))) continue;
            List<string> meanings;
            try { meanings = JsonSerializer.Deserialize<List<string>>(vocabulary.MeaningsJson) ?? []; }
            catch (JsonException) { meanings = []; }
            var card = CreateCard(vocabulary.Term, vocabulary.Meaning, meanings,
                vocabulary.Phonetic, vocabulary.PartOfSpeech, vocabulary.Example, "", "", [], vocabulary.AudioUrl ?? "");
            if (card == null) continue;
            fromMaterial.Add(card);
            known.Add(Normalize(card.Word));
        }
        return fromMaterial;
    }

    private static List<PracticeCard> ReadPracticeCards(string? materialJson, Guid lessonId)
        => ReadMaterialCardsByLesson(materialJson).GetValueOrDefault(lessonId) ?? [];

    private static Dictionary<Guid, List<PracticeCard>> ReadMaterialCardsByLesson(string? materialJson)
    {
        var result = new Dictionary<Guid, List<PracticeCard>>();
        if (string.IsNullOrWhiteSpace(materialJson)) return result;
        JsonDocument document;
        try { document = JsonDocument.Parse(materialJson); }
        catch (JsonException) { return result; }
        using (document)
        {
        if (!document.RootElement.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array) return result;
        foreach (var unit in units.EnumerateArray())
        {
            if (!unit.TryGetProperty("lessons", out var lessons) || lessons.ValueKind != JsonValueKind.Array) continue;
            foreach (var lesson in lessons.EnumerateArray())
            {
                if (!lesson.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                    !Guid.TryParse(id.GetString(), out var parsed)) continue;
                if (!lesson.TryGetProperty("flashcards", out var cards) || cards.ValueKind != JsonValueKind.Array) continue;
                result[parsed] = cards.EnumerateArray().Select(ReadCard).Where(card => card != null)
                    .Cast<PracticeCard>().GroupBy(card => Normalize(card.Word)).Select(group => group.First())
                    .ToList();
            }
        }
        return result;
        }
    }

    private static PracticeCard? ReadCard(JsonElement card)
    {
        static string Read(JsonElement value, string property) =>
            value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() ?? "" : "";
        var word = Read(card, "front");
        var meaning = Read(card, "back");
        var cloze = Read(card, "clozeSentence");
        var example = Read(card, "example");
        var accepted = card.TryGetProperty("acceptedAnswers", out var answers) && answers.ValueKind == JsonValueKind.Array
            ? answers.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()?.Trim() ?? "").Where(item => item.Length > 0).ToList()
            : new List<string>();
        var meanings = card.TryGetProperty("meanings", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString() ?? "").ToList()
            : new List<string>();
        return CreateCard(word, meaning, meanings, Read(card, "phonetic"), Read(card, "partOfSpeech"),
            example, Read(card, "exampleTranslation"), cloze, accepted, Read(card, "audioUrl"));
    }

    private static PracticeCard? CreateCard(string word, string meaning, List<string> meanings, string phonetic,
        string partOfSpeech, string example, string exampleTranslation, string cloze, List<string> accepted, string audioUrl)
    {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(meaning)) return null;
        if (!cloze.Contains("___") && example.Length > 0)
            cloze = Regex.Replace(example, @"(?<!\p{L})" + Regex.Escape(word) + @"(?!\p{L})", "___",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        // A bare blank cannot be graded as a sentence-completion exercise.
        if (!cloze.Contains("___") || !Regex.IsMatch(cloze.Replace("___", ""), @"\p{L}")) return null;
        if (accepted.Count == 0) accepted = [word];
        if (accepted.Count == 0) return null;
        if (meanings.Count == 0) meanings = [meaning];
        return new PracticeCard(word, meaning, meanings, phonetic, partOfSpeech,
            example, exampleTranslation, cloze, accepted, audioUrl);
    }

    private static List<PracticeStep> BuildSteps(List<PracticeCard> cards, List<PracticeCard> lessonCards)
    {
        var steps = new List<PracticeStep>();
        var batches = cards.Chunk(3).Select(batch => batch.ToList()).ToList();
        foreach (var batch in batches)
        {
            foreach (var card in batch)
                steps.Add(new PracticeStep(Guid.NewGuid(), "introduction", 0, card.Word, card.Meaning, card.Meanings,
                    card.Phonetic, card.PartOfSpeech, card.Example, card.ExampleTranslation, "", card.AudioUrl, [], [], new()));
            AddPracticeBatch(steps, batch, lessonCards, 1);
        }
        foreach (var batch in batches)
            AddPracticeBatch(steps, batch, lessonCards, 2);
        return steps;
    }

    private static void AddPracticeBatch(List<PracticeStep> steps, List<PracticeCard> batch,
        List<PracticeCard> lessonCards, int round)
    {
        if (batch.Select(card => Normalize(card.Meaning)).Distinct().Count() != batch.Count)
            throw new BusinessRuleException("VOCABULARY_MEANING_DUPLICATE", Messages.VocabularyPractice.DuplicateMeaning);
        var listening = batch[0];
        var cloze = batch[Math.Min(1, batch.Count - 1)];
        var dictation = batch[Math.Min(2, batch.Count - 1)];
        steps.Add(new PracticeStep(Guid.NewGuid(), "listenMeaning", round, listening.Word, "", [], "", "", "", "",
            "", listening.AudioUrl, new[] { listening.Meaning }.Concat(lessonCards.Select(item => item.Meaning)
                .Where(meaning => Normalize(meaning) != Normalize(listening.Meaning)).Distinct()
                .OrderBy(_ => Random.Shared.Next()).Take(3)).OrderBy(_ => Random.Shared.Next()).ToList(),
            [listening.Meaning], new()));
        steps.Add(new PracticeStep(Guid.NewGuid(), "cloze", round, cloze.Word, "", [], "", "", "", "",
            cloze.ClozeSentence, "", [], cloze.AcceptedAnswers, new()));
        var pairs = batch.ToDictionary(card => card.Word, card => card.Meaning);
        steps.Add(new PracticeStep(Guid.NewGuid(), "matching", round, "", "", [], "", "", "", "", "",
            "", [], [], pairs, ShuffleMeanings(batch.Select(card => card.Meaning).ToList())));
        steps.Add(new PracticeStep(Guid.NewGuid(), "dictation", round, dictation.Word, "", [], "", "", "", "",
            "", dictation.AudioUrl, [], [dictation.Word], new()));
    }

    private static List<string> ShuffleMeanings(List<string> meanings)
    {
        if (meanings.Count < 2) return meanings;
        // Rotate by a non-zero offset: no meaning stays opposite its own word.
        var offset = Random.Shared.Next(1, meanings.Count);
        return meanings.Skip(offset).Concat(meanings.Take(offset)).ToList();
    }

    private sealed record PracticeCard(string Word, string Meaning, List<string> Meanings, string Phonetic,
        string PartOfSpeech, string Example, string ExampleTranslation, string ClozeSentence,
        List<string> AcceptedAnswers, string AudioUrl);
    private sealed record PracticeResult(Guid StepId, bool Correct);
    private sealed record PracticeStep(Guid Id, string Kind, int Round, string Word, string Meaning,
        List<string> Meanings, string Phonetic, string PartOfSpeech, string Example, string ExampleTranslation,
        string Prompt, string AudioUrl, List<string> Options, List<string> AcceptedAnswers,
        Dictionary<string, string> Pairs, List<string>? MatchMeanings = null);
}
