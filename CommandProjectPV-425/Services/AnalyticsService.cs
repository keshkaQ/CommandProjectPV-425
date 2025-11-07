using CommandProjectPV_425.Interfaces;

namespace CommandProjectPV_425.Services
{
    public class AnalyticsService : IAnalyticService
    {
        private readonly IDataService _dataService;
        public AnalyticsService(IDataService dataService)
        {
            _dataService = dataService;
        }
        public async Task<List<MethodStatistic>> GetMethodStatisticsAsync()
        {
            try
            {
                return await _dataService.GetAverageTimePerMethodAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
