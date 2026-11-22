using LanguageLearning.Application.DTOs.Statistics;
using LanguageLearning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearning.Api.Controllers;

[ApiController]
[Route("api/v1/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ICurrentUserService _currentUserService;

    public StatisticsController(IStatisticsService statisticsService, ICurrentUserService currentUserService)
    {
        _statisticsService = statisticsService;
        _currentUserService = currentUserService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(StatisticsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _statisticsService.GetOverviewAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("heatmap")]
    [ProducesResponseType(typeof(List<HeatmapDayResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeatmap(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _statisticsService.GetHeatmapAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(List<LeaderboardEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetWeeklyLeaderboardAsync(cancellationToken);
        return Ok(result);
    }
}
