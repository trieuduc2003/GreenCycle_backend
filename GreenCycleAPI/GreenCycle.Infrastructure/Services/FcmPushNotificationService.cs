using GreenCycle.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GreenCycle.Infrastructure.Services
{
    public class FcmPushNotificationService : IPushNotificationService
    {
        private static readonly ConcurrentDictionary<int, string> UserFcmTokens = new();
        private readonly ILogger<FcmPushNotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _fcmServerKey;

        public FcmPushNotificationService(ILogger<FcmPushNotificationService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _fcmServerKey = configuration["Firebase:ServerKey"];
        }

        public void RegisterDeviceToken(int userId, string fcmToken)
        {
            if (string.IsNullOrWhiteSpace(fcmToken)) return;
            UserFcmTokens[userId] = fcmToken;
            _logger.LogInformation("FCM Device Token registered for User {UserId}: {TokenPrefix}...", userId, fcmToken.Substring(0, Math.Min(10, fcmToken.Length)));
        }

        public async Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(fcmToken)) return false;

            _logger.LogInformation("Sending FCM Push Notification to token {Token}: [{Title}] {Body}", 
                fcmToken.Substring(0, Math.Min(10, fcmToken.Length)), title, body);

            // Nếu chưa cấu hình Server Key thật, log để test môi trường dev
            if (string.IsNullOrEmpty(_fcmServerKey))
            {
                _logger.LogWarning("Firebase ServerKey not configured in appsettings.json. FCM notification logged in Dev Mode.");
                return true;
            }

            try
            {
                var payload = new
                {
                    to = fcmToken,
                    notification = new
                    {
                        title = title,
                        body = body,
                        sound = "default",
                        vibrate = 1
                    },
                    data = data
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={_fcmServerKey}");

                var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM push notification.");
                return false;
            }
        }

        public async Task<bool> SendPushToUserAsync(int userId, string title, string body, object? data = null)
        {
            if (UserFcmTokens.TryGetValue(userId, out var token))
            {
                return await SendPushNotificationAsync(token, title, body, data);
            }

            _logger.LogInformation("No active FCM Device Token found for User {UserId}. Notification [{Title}] will rely on SignalR.", userId, title);
            return false;
        }
    }
}
