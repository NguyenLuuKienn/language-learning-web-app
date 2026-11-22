using LanguageLearning.Application.DTOs.Quiz;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyTargetService _studyTargets;
    private readonly ICourseService _courses;

    public QuizController(IQuizService quizService, ICurrentUserService currentUserService, IStudyTargetService studyTargets, ICourseService courses)
    {
        _quizService = quizService;
        _currentUserService = currentUserService;
        _studyTargets = studyTargets;
        _courses = courses;
    }

    [HttpGet("lessons/{lessonId:guid}/quiz")]
    [ProducesResponseType(typeof(List<QuizQuestionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuiz(Guid lessonId, CancellationToken cancellationToken)
    {
        await EnsureLessonAccessAsync(lessonId, cancellationToken);
        var result = await _quizService.GetLessonQuizAsync(lessonId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("quiz-attempts")]
    [ProducesResponseType(typeof(QuizResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await EnsureLessonAccessAsync(request.LessonId, cancellationToken);
        var result = await _quizService.SubmitQuizAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("quiz-attempts")]
    [ProducesResponseType(typeof(List<QuizAttemptHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] Guid? lessonId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _quizService.GetAttemptHistoryAsync(userId, lessonId, cancellationToken);
        return Ok(result);
    }

    private async Task EnsureLessonAccessAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var userId = _currentUserService.GetRequiredUserId();
        var target = isAdmin ? null : await _studyTargets.GetAsync(userId, cancellationToken);
        await _courses.GetLessonDetailAsync(lessonId, userId, cancellationToken, target, isAdmin);
    }
}
