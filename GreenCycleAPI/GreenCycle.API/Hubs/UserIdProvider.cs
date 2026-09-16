using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace GreenCycle.API.Hubs
{
    /// <summary>
    /// Ánh xạ UserId (từ JWT claim NameIdentifier) với ConnectionId của SignalR.
    /// Bắt buộc để dùng Clients.User(userId) hoạt động đúng.
    /// </summary>
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
