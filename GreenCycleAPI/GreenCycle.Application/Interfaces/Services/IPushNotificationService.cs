using System.Threading.Tasks;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IPushNotificationService
    {
        void RegisterDeviceToken(int userId, string fcmToken);
        Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, object? data = null);
        Task<bool> SendPushToUserAsync(int userId, string title, string body, object? data = null);
    }
}
