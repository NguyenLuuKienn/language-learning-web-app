using System.Text.Json;
using LanguageLearning.Application.Common;
using System.Text.Json.Nodes;

namespace LanguageLearning.Application.Services;

internal static class CourseMaterialValidator
{
    internal static void ValidatePlacementTests(JsonElement tests, string level)
    {
        if (tests.ValueKind != JsonValueKind.Array || tests.GetArrayLength() == 0)
            throw new ArgumentException(Messages.Material.PlacementRequired);
        foreach (var test in tests.EnumerateArray())
        {
            if (!string.Equals(ReadMaterialText(test, "level"), level, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(Messages.Material.PlacementLevelMismatch);
            if (!test.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
                throw new ArgumentException(Messages.Material.PlacementQuestionsRequired);
            var sections = new HashSet<string>();
            foreach (var question in questions.EnumerateArray())
            {
                var section = ReadMaterialText(question, "section");
                if (section is not ("listening" or "speaking" or "reading" or "writing"))
                    throw new ArgumentException(Messages.Material.PlacementSectionInvalid);
                sections.Add(section);
                ReadMaterialText(question, "prompt");
                if (!question.TryGetProperty("points", out var points) || points.ValueKind != JsonValueKind.Number
                    || !points.TryGetInt32(out var pointValue) || pointValue <= 0 || pointValue > 100)
                    throw new ArgumentException(Messages.Material.PlacementPointsInvalid);
                if (section is "speaking" or "writing")
                {
                    ReadMaterialText(question, "expectedResponse");
                    ReadMaterialText(question, "rubric");
                    if (question.TryGetProperty("minWords", out var minWords)
                        && (minWords.ValueKind != JsonValueKind.Number || !minWords.TryGetInt32(out var count) || count < 1 || count > 500))
                        throw new ArgumentException(Messages.Material.PlacementMinWordsInvalid);
                }
                if (section is "listening" or "reading")
                {
                    ReadMaterialText(question, section == "listening" ? "audioScript" : "readingPassage");
                    if (!question.TryGetProperty("options", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() < 2 ||
                        !question.TryGetProperty("correctIndex", out var answer) || answer.ValueKind != JsonValueKind.Number ||
                        !answer.TryGetInt32(out var correctIndex) || correctIndex < 0 || correctIndex >= choices.GetArrayLength())
                        throw new ArgumentException(Messages.Material.PlacementChoicesInvalid);
                    if (choices.EnumerateArray().Any(choice => choice.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(choice.GetString())))
                        throw new ArgumentException(Messages.Material.PlacementOptionsBlank);
                }
            }
            if (!new[] { "listening", "speaking", "reading", "writing" }.All(sections.Contains))
                throw new ArgumentException(Messages.Material.PlacementSkillsRequired);
        }
    }

    internal static Guid ParseMaterialId(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(id.GetString(), out var value) || value == Guid.Empty)
            throw new ArgumentException(Messages.Material.UnitLessonIdInvalid);
        return value;
    }

    internal static void ValidateLessonMaterial(JsonElement units)
    {
        var ids = new HashSet<Guid>();
        foreach (var (unit, unitIndex) in units.EnumerateArray().Select((value, index) => (value, index)))
        {
            var unitPath = $"units[{unitIndex}]";
            if (unit.ValueKind != JsonValueKind.Object) throw new ArgumentException(Messages.Material.ObjectRequiredAt(unitPath));
            if (!ids.Add(ParseMaterialId(unit))) throw new ArgumentException(Messages.Material.DuplicateId(unitPath));
            ReadMaterialText(unit, "title");
            if (!unit.TryGetProperty("lessons", out var lessons) || lessons.ValueKind != JsonValueKind.Array)
                throw new ArgumentException(Messages.Material.ArrayRequired($"{unitPath}.lessons"));
            if (lessons.GetArrayLength() == 0) throw new ArgumentException(Messages.Material.ArrayEmpty($"{unitPath}.lessons"));
            foreach (var (lesson, lessonIndex) in lessons.EnumerateArray().Select((value, index) => (value, index)))
            {
                var path = $"{unitPath}.lessons[{lessonIndex}]";
                if (lesson.ValueKind != JsonValueKind.Object) throw new ArgumentException(Messages.Material.ObjectRequiredAt(path));
                if (!ids.Add(ParseMaterialId(lesson))) throw new ArgumentException(Messages.Material.DuplicateId(path));
                ReadMaterialText(lesson, "title");
                foreach (var section in new[] { "flashcards", "listeningExercises", "speakingExercises", "readingArticles", "writingPrompts", "grammarTopics", "quizQuestions" })
                    if (!lesson.TryGetProperty(section, out var entries) || entries.ValueKind != JsonValueKind.Array)
                        throw new ArgumentException(Messages.Material.ArrayRequired($"{path}.{section}"));
                ValidateCards(lesson.GetProperty("flashcards"));
                ValidateReading(lesson.GetProperty("readingArticles"), $"{path}.readingArticles");
                ValidateWriting(lesson.GetProperty("writingPrompts"), $"{path}.writingPrompts");
                ValidateQuestions(lesson.GetProperty("quizQuestions"), $"{path}.quizQuestions");
                ValidateLessonSteps(lesson, path);
            }
        }
    }

    private static void ValidateLessonSteps(JsonElement lesson, string path)
    {
        if (!lesson.TryGetProperty("lessonSteps", out var steps)) return;
        if (steps.ValueKind != JsonValueKind.Array || steps.GetArrayLength() == 0)
            throw new ArgumentException($"{path}.lessonSteps must be a nonempty array.");
        var sections = new Dictionary<string, (string Section, string Policy)>
        {
            ["introduction"] = ("", "viewed"), ["vocabulary"] = ("flashcards", "session_completed"),
            ["grammar"] = ("grammarTopics", "all_correct"), ["listening"] = ("listeningExercises", "all_correct"),
            ["speaking"] = ("speakingExercises", "practiced"), ["reading"] = ("readingArticles", "all_correct"),
            ["writing"] = ("writingPrompts", "submission_required"), ["quiz"] = ("quizQuestions", "quiz_passed"),
            ["recap"] = ("", "viewed")
        };
        var ids = new HashSet<string>();
        var hasIntroduction = false;
        foreach (var (step, index) in steps.EnumerateArray().Select((value, position) => (value, position)))
        {
            var stepPath = $"{path}.lessonSteps[{index}]";
            if (step.ValueKind != JsonValueKind.Object) throw new ArgumentException(Messages.Material.ObjectRequiredAt(stepPath));
            var id = ReadMaterialText(step, "id");
            var type = ReadMaterialText(step, "type");
            if (!ids.Add(id) || id.Length > 100 || !sections.TryGetValue(type, out var section))
                throw new ArgumentException($"{stepPath} has an invalid or duplicate id/type.");
            if (type == "introduction" && (!step.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.False))
                hasIntroduction = true;
            if (step.TryGetProperty("order", out var order) && (!order.TryGetInt32(out var position) || position < 0))
                throw new ArgumentException($"{stepPath}.order must be a nonnegative integer.");
            if (step.TryGetProperty("completionPolicy", out var policy) &&
                (policy.ValueKind != JsonValueKind.Object || ReadMaterialText(policy, "kind") != section.Policy))
                throw new ArgumentException($"{stepPath}.completionPolicy.kind must be {section.Policy}.");
            var refs = step.TryGetProperty("activityRefs", out var references) ? references : default;
            if (refs.ValueKind != JsonValueKind.Undefined && refs.ValueKind != JsonValueKind.Array)
                throw new ArgumentException($"{stepPath}.activityRefs must be an array.");
            HashSet<string> available = section.Section.Length == 0 ? [] : lesson.GetProperty(section.Section).EnumerateArray()
                .Where(item => item.TryGetProperty("id", out var value) && value.ValueKind == JsonValueKind.String)
                .Select(item => item.GetProperty("id").GetString()!).ToHashSet();
            var listed = refs.ValueKind == JsonValueKind.Array ? refs.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null).ToArray() : [];
            if (listed.Any(item => item == null || !available.Contains(item)) ||
                (section.Section.Length > 0 && type is not ("vocabulary" or "quiz") && listed.Length == 0) ||
                (section.Section.Length > 0 && !lesson.GetProperty(section.Section).EnumerateArray().Any()))
                throw new ArgumentException($"{stepPath}.activityRefs points to missing content.");
        }
        if (!hasIntroduction) throw new ArgumentException($"{path}.lessonSteps needs a required introduction.");
        foreach (var (type, section) in sections)
            if (section.Section.Length > 0 && lesson.GetProperty(section.Section).GetArrayLength() > 0 &&
                !steps.EnumerateArray().Any(step => ReadMaterialText(step, "type") == type))
                throw new ArgumentException($"{path}.lessonSteps is missing the {type} step for existing content.");
    }

    internal static void ValidateCards(JsonElement cards)
    {
        foreach (var (card, index) in cards.EnumerateArray().Select((value, position) => (value, position)))
        {
            ReadMaterialText(card, "front");
            ReadMaterialText(card, "back");
            if (card.TryGetProperty("meanings", out var meanings))
            {
                if (meanings.ValueKind != JsonValueKind.Array || meanings.GetArrayLength() is < 1 or > 20)
                    throw new ArgumentException(Messages.Material.Invalid);
                foreach (var meaning in meanings.EnumerateArray())
                    if (meaning.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(meaning.GetString()) || meaning.GetString()!.Length > 300)
                        throw new ArgumentException(Messages.Material.Invalid);
            }
            if (card.TryGetProperty("partOfSpeech", out var partOfSpeech) &&
                (partOfSpeech.ValueKind != JsonValueKind.String || partOfSpeech.GetString()!.Length > 60))
                throw new ArgumentException(Messages.Material.Invalid);
            if (card.TryGetProperty("phonetic", out var phonetic) &&
                (phonetic.ValueKind != JsonValueKind.String || phonetic.GetString()!.Length > 100))
                throw new ArgumentException(Messages.Material.Invalid);
            if (card.TryGetProperty("example", out var example) &&
                (example.ValueKind != JsonValueKind.String || example.GetString()!.Length > 2000))
                throw new ArgumentException(Messages.Material.Invalid);
            if (card.TryGetProperty("exampleTranslation", out var translation) &&
                (translation.ValueKind != JsonValueKind.String || translation.GetString()!.Length > 2000))
                throw new ArgumentException(Messages.Material.Invalid);
            if (card.TryGetProperty("clozeSentence", out var cloze) || card.TryGetProperty("acceptedAnswers", out _))
            {
                if (cloze.ValueKind != JsonValueKind.String || cloze.GetString() is not { Length: <= 2000 } sentence || !sentence.Contains("___") ||
                    !sentence.Replace("___", "").Any(char.IsLetter) ||
                    !card.TryGetProperty("acceptedAnswers", out var answers) || answers.ValueKind != JsonValueKind.Array ||
                    answers.GetArrayLength() is < 1 or > 10 ||
                    answers.EnumerateArray().Any(answer => answer.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(answer.GetString()) || answer.GetString()!.Length > 100))
                    throw new ArgumentException(Messages.Material.CardPracticeInvalid($"cards[{index}]"));
            }
        }
    }

    internal static void ValidateReading(JsonElement articles, string path)
    {
        foreach (var (article, index) in articles.EnumerateArray().Select((value, i) => (value, i)))
        {
            var articlePath = $"{path}[{index}]";
            ReadMaterialText(article, "title");
            if (!article.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array ||
                content.GetArrayLength() == 0 || content.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString())))
                throw new ArgumentException(Messages.Material.ReadingContentInvalid(articlePath));
            if (!article.TryGetProperty("vocabularies", out var vocabulary) || vocabulary.ValueKind != JsonValueKind.Array)
                throw new ArgumentException(Messages.Material.ArrayRequired($"{articlePath}.vocabularies"));
            foreach (var (entry, vocabularyIndex) in vocabulary.EnumerateArray().Select((value, position) => (value, position)))
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("word", out var word) || word.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(word.GetString()) ||
                    !entry.TryGetProperty("meaning", out var meaning) || meaning.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(meaning.GetString()))
                    throw new ArgumentException(Messages.Material.ReadingVocabularyInvalid($"{articlePath}.vocabularies[{vocabularyIndex}]"));
            }
            if (!article.TryGetProperty("quiz", out var quiz) || quiz.ValueKind != JsonValueKind.Object)
                throw new ArgumentException(Messages.Material.QuizRequired(articlePath));
            ValidateQuestion(quiz, $"{articlePath}.quiz");
        }
    }

    internal static void ValidateWriting(JsonElement prompts, string path)
    {
        foreach (var (prompt, index) in prompts.EnumerateArray().Select((value, position) => (value, position)))
        {
            foreach (var field in new[] { "title", "scenario", "instruction" })
                if (prompt.ValueKind != JsonValueKind.Object || !prompt.TryGetProperty(field, out var value) ||
                    value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                    throw new ArgumentException(Messages.Material.FieldNotBlank($"{path}[{index}].{field}"));
            if (prompt.TryGetProperty("minWords", out var minWords) &&
                (minWords.ValueKind != JsonValueKind.Number || !minWords.TryGetInt32(out var count) || count < 1 || count > 500))
                throw new ArgumentException(Messages.Material.MinWordsInvalid($"{path}[{index}]"));
        }
    }

    internal static void ValidateQuestions(JsonElement questions, string path)
    {
        foreach (var (question, index) in questions.EnumerateArray().Select((value, i) => (value, i)))
            ValidateQuestion(question, $"{path}[{index}]");
    }

    internal static void ValidateQuestion(JsonElement question, string path)
    {
        ReadMaterialText(question, "question");
        if (!question.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array ||
            options.GetArrayLength() < 2 || options.EnumerateArray().Any(option => option.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(option.GetString())) ||
            !question.TryGetProperty("correctIndex", out var answer) || answer.ValueKind != JsonValueKind.Number ||
            !answer.TryGetInt32(out var correctIndex) || correctIndex < 0 || correctIndex >= options.GetArrayLength())
            throw new ArgumentException(Messages.Material.AnswerInvalid(path));
    }

    internal static string ReadMaterialText(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ArgumentException(Messages.Material.FieldRequired(name));
        return value.GetString()!.Trim();
    }

    internal static void ValidateChangedSkillSections(JsonNode? previous, JsonElement lesson)
    {
        foreach (var section in new[] { "listeningExercises", "speakingExercises", "grammarTopics" })
        {
            var current = lesson.GetProperty(section);
            if (previous?[section]?.ToJsonString() == current.GetRawText()) continue;
            foreach (var (item, index) in current.EnumerateArray().Select((value, position) => (value, position)))
            {
                if (item.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException(Messages.Material.ObjectRequiredAt($"{section}[{index}]"));
                ReadMaterialText(item, "title");
                switch (section)
                {
                    case "listeningExercises":
                        ReadMaterialText(item, "audioScript");
                        ValidateQuestion(item, $"{section}[{index}]");
                        break;
                    case "speakingExercises":
                        ReadMaterialText(item, "phrase");
                        ReadMaterialText(item, "translation");
                        break;
                    case "grammarTopics":
                        ReadMaterialText(item, "formula");
                        ReadMaterialText(item, "explanation");
                        ReadMaterialText(item, "example");
                        break;
                }
            }
        }
    }
}
