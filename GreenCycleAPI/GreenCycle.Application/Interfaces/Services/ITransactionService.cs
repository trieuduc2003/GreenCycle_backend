using System.Threading.Tasks;
using GreenCycle.Application.DTOs.Common;
using GreenCycle.Application.DTOs.Transaction;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        /// <summary>Chủ Vựa nhập cân thực tế → gửi Double Confirmation tới Seller (Drop-off).</summary>
        Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiateTransactionAsync(int yardUserId, InitiateTransactionRequestDto request);

        /// <summary>Tài xế nhập cân thực tế → gửi Double Confirmation tới Seller (Pick-up).</summary>
        Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiatePickupTransactionAsync(int collectorUserId, InitiateTransactionRequestDto request);

        /// <summary>Seller bấm Đồng ý → cộng GP vào ví, hoàn tất giao dịch.</summary>
        Task<ApiResponse<object>> ConfirmTransactionAsync(int sellerUserId, ConfirmTransactionRequestDto request);
    }
}
