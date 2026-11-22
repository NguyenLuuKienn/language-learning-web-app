using LanguageLearning.Application.DTOs.VocabularyPractice;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vocabulary-sessions")]
public sealed class VocabularySessionsController : ControllerBase
{
    private readonly IVocabularySessionService _service;
    private readonly ICurrentUserService _currentUser;
    public VocabularySessionsController(IVocabularySessionService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("lessons")]
    [ProducesResponseType(typeof(List<VocabularyLessonChoice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(CancellationToken cancellationToken) =>
        Ok(await _service.GetAvailableAsync(_currentUser.GetRequiredUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(VocabularySessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start([FromBody] StartVocabularySessionRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.StartAsync(_currentUser.GetRequiredUserId(), request.LessonId, cancellationToken));

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(VocabularySessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _service.GetAsync(_currentUser.GetRequiredUserId(), sessionId, cancellationToken));

    [HttpPost("{sessionId:guid}/answers")]
    [ProducesResponseType(typeof(VocabularyAnswerResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Answer(Guid sessionId, [FromBody] VocabularyPracticeAnswerRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.AnswerAsync(_currentUser.GetRequiredUserId(), sessionId, request, cancellationToken));

    [HttpPost("{sessionId:guid}/complete-perfect-round")]
    [ProducesResponseType(typeof(VocabularySessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletePerfectRound(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _service.CompleteAfterPerfectRoundAsync(_currentUser.GetRequiredUserId(), sessionId, cancellationToken));
}
