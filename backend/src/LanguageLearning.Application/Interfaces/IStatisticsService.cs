using LanguageLearning.Application.DTOs.Statistics;

namespace LanguageLearning.Application.Interfaces;

public interface IStatisticsService
{
    Task<StatisticsOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<HeatmapDayResponse>> GetHeatmapAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<LeaderboardEntryResponse>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default);
}
