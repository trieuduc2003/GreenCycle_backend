using GreenCycle.Application.DTOs.Yard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IScrapYardService
    {
        Task<List<ScrapYardDto>> GetNearbyYardsAsync(double latitude, double longitude, double radiusMeters, int limit);
        Task<bool> ToggleYardStatusAsync(int userId, bool isOpening);
        Task<YardStatsDto> GetYardStatsTodayAsync(int userId);
        Task<List<YardChartDataDto>> GetYardChartStatsAsync(int userId, int? month, int? year);
        Task<List<YardActivityDto>> GetRecentActivitiesAsync(int userId);
        Task<YardProfileDto> GetYardProfileAsync(int userId);
        Task<bool> UpdateYardProfileAsync(int userId, string name, string address, string operatingHours, double? latitude = null, double? longitude = null);
    }
}
