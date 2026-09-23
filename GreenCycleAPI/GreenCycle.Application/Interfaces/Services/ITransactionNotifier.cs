using GreenCycle.Application.DTOs.Transaction;
using System.Threading.Tasks;

namespace GreenCycle.Application.Interfaces.Services
{
    /// <summary>
    /// Abstraction để tách SignalR khỏi Infrastructure layer (tránh circular dependency).
    /// Implementation nằm ở GreenCycle.API.
    /// </summary>
    public interface ITransactionNotifier
    {
        Task SendDoubleConfirmationAsync(string sellerUserId, DoubleConfirmationPayloadDto payload);
        Task SendTransactionResultToYardAsync(string yardUserId, TransactionResultPayloadDto payload);
        Task NotifyCollectorLocationAsync(string sellerUserId, int orderId, double latitude, double longitude);
    }
}
