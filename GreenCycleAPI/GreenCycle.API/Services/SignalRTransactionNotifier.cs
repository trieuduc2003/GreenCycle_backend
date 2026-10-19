using GreenCycle.API.Hubs;
using GreenCycle.Application.DTOs.Transaction;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GreenCycle.API.Services
{
    /// <summary>
    /// Cài đặt ITransactionNotifier dùng SignalR. Đăng ký ở API layer.
    /// </summary>
    public class SignalRTransactionNotifier : ITransactionNotifier
    {
        private readonly IHubContext<TransactionHub> _hubContext;

        public SignalRTransactionNotifier(IHubContext<TransactionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendDoubleConfirmationAsync(string sellerUserId, DoubleConfirmationPayloadDto payload)
        {
            await _hubContext.Clients.User(sellerUserId).SendAsync("ReceiveDoubleConfirmation", payload);
        }

        public async Task SendTransactionResultToYardAsync(string yardUserId, TransactionResultPayloadDto payload)
        {
            await _hubContext.Clients.User(yardUserId).SendAsync("ReceiveTransactionResult", payload);
        }

        public async Task NotifyCollectorLocationAsync(string sellerUserId, int orderId, double latitude, double longitude)
        {
            var payload = new { OrderId = orderId, Latitude = latitude, Longitude = longitude };
            await _hubContext.Clients.User(sellerUserId).SendAsync("CollectorLocationUpdated", payload);
        }

        public async Task NotifyGeofenceApproachingAsync(string sellerUserId, int orderId, double distanceMeters)
        {
            var payload = new 
            { 
                OrderId = orderId, 
                DistanceMeters = Math.Round(distanceMeters, 0),
                Message = $"Tài xế đang đến rất gần (cách bạn ~{Math.Round(distanceMeters, 0)}m). Vui lòng chuẩn bị sẵn rác nhé!" 
            };
            await _hubContext.Clients.User(sellerUserId).SendAsync("CollectorApproaching", payload);
        }
    }
}
