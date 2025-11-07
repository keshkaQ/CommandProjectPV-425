using CommandProjectPV_425.Services;

namespace CommandProjectPV_425.Interfaces
{
    public interface IAnalyticService
    {
        Task<List<MethodStatistic>> GetMethodStatisticsAsync();
    }
}
